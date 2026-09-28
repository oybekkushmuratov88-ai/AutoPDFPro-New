using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AutoPDF.AutoCAD.Infrastructure;
using AutoPDF.Core.Localization;
using AutoPDF.Core.Logging;
using AutoPDF.Core.Models;
using AutoPDF.Core.Paper;
using AutoPDF.Core.Settings;
using AutoPDF.Core.Text;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace AutoPDF.AutoCAD.Detection
{
    public sealed class DetectionOutcome
    {
        public List<SheetModel> Sheets { get; set; } = new List<SheetModel>();
        public bool Cancelled { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// Finds sheet frames in model space. Supported modes: manual selection, block name, sample frame
    /// (whole drawing) and sample frame + selected range. A frame is a closed axis-aligned 4-vertex
    /// polyline without arcs, or a block reference (see <see cref="FrameReader"/>).
    /// </summary>
    public sealed class SheetDetector
    {
        private static readonly SelectionFilter FrameFilter =
            new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "INSERT,LWPOLYLINE,POLYLINE") });

        private readonly Document _doc;
        private readonly PluginSettings _settings;
        private readonly LoggingService _log;
        private readonly BlockFrameCache _blockCache = new BlockFrameCache();

        public SheetDetector(Document doc, PluginSettings settings, LoggingService log)
        {
            _doc = doc;
            _settings = settings;
            _log = log;
        }

        /// <summary>Paper-size resolver configured from the settings and the drawing's INSUNITS.</summary>
        public static PaperSizeResolver CreateResolver(Database db, PluginSettings settings)
        {
            return new PaperSizeResolver(new PaperResolverOptions
            {
                MillimetersPerUnit = DrawingUnits.MillimetersPerUnit((int)db.Insunits),
                FixedScale = settings.FixedScale,
                SnapToleranceMm = settings.SnapToleranceMm,
                MinShortSideMm = settings.MinSheetSizeMm,
                CustomFormats = PaperFormatCatalog.ParseCustom(settings.CustomFormats)
            });
        }

        public DetectionOutcome Detect(DetectionMode mode)
        {
            _log.Info("Aniqlash boshlandi: usul=" + mode + ", INSUNITS=" + DrawingUnits.Describe((int)_doc.Database.Insunits) +
                      ", masshtab=" + (_settings.FixedScale > 0 ? "1:" + _settings.FixedScale.ToString(CultureInfo.InvariantCulture) : "avto"));
            switch (mode)
            {
                case DetectionMode.Manual: return DetectManual();
                case DetectionMode.BlockName: return DetectByBlockName();
                case DetectionMode.Sample: return DetectBySample(withRange: false);
                case DetectionMode.SampleRange: return DetectBySample(withRange: true);
                default: throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }

        // ---- Manual ------------------------------------------------------------------------

        private DetectionOutcome DetectManual()
        {
            var opts = new PromptSelectionOptions { MessageForAdding = Uz.PromptSelectFrames, AllowDuplicates = false };
            PromptSelectionResult sel = _doc.Editor.GetSelection(opts, FrameFilter);
            if (sel.Status != PromptStatus.OK || sel.Value == null)
            {
                return new DetectionOutcome { Cancelled = true, Message = "Tanlash bekor qilindi." };
            }

            List<FrameCandidate> frames = ReadFrames(sel.Value.GetObjectIds(), out int rejected);
            _log.Info(string.Format(CultureInfo.InvariantCulture, "Qo‘lda tanlash: {0} obyekt, {1} ramka, {2} ta ramka emas.",
                sel.Value.Count, frames.Count, rejected));
            return Finish(frames, keepInvalid: true, rejected);
        }

        // ---- Block name --------------------------------------------------------------------

        private DetectionOutcome DetectByBlockName()
        {
            var matcher = new WildcardMatcher(_settings.BlockNames);
            if (matcher.IsEmpty)
            {
                return new DetectionOutcome { Message = "Blok nomlarini kiriting (masalan: Ramka_A3, Format*)." };
            }

            var frames = new List<FrameCandidate>();
            var namesSeen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int rejected = 0;

            using (Transaction tr = _doc.Database.TransactionManager.StartTransaction())
            {
                var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(_doc.Database), OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    if (id.ObjectClass.DxfName != "INSERT") continue;
                    var br = (BlockReference)tr.GetObject(id, OpenMode.ForRead);
                    string name = FrameReader.EffectiveName(br, tr);
                    namesSeen[name] = namesSeen.TryGetValue(name, out int c) ? c + 1 : 1;
                    if (!matcher.IsMatch(name)) continue;

                    if (FrameReader.TryRead(br, tr, out FrameCandidate frame, out string why, _blockCache)) frames.Add(frame);
                    else
                    {
                        rejected++;
                        _log.Debug("Blok " + br.Handle + " '" + name + "' o‘tkazildi: " + why);
                    }
                }
                tr.Commit();
            }

            _log.Info("Model fazosidagi bloklar: " + string.Join(", ", namesSeen.OrderByDescending(kv => kv.Value).Take(25)
                .Select(kv => kv.Key + "×" + kv.Value)));
            _log.Info("Blok nomi bo‘yicha (" + _settings.BlockNames + "): " + frames.Count + " ta blok mos keldi.");

            if (frames.Count == 0)
            {
                return new DetectionOutcome
                {
                    Message = "“" + _settings.BlockNames + "” nomli blok topilmadi. Chizmadagi bloklar: " +
                              string.Join(", ", namesSeen.Keys.Take(10))
                };
            }
            return Finish(frames, keepInvalid: true, rejected);
        }

        // ---- Sample (+ range) ----------------------------------------------------------------

        private DetectionOutcome DetectBySample(bool withRange)
        {
            var peo = new PromptEntityOptions(Uz.PromptSelectSample);
            peo.SetRejectMessage(Uz.PromptSampleReject);
            peo.AddAllowedClass(typeof(Polyline), false);
            peo.AddAllowedClass(typeof(Polyline2d), false);
            peo.AddAllowedClass(typeof(BlockReference), false);

            PromptEntityResult per = _doc.Editor.GetEntity(peo);
            if (per.Status != PromptStatus.OK)
            {
                return new DetectionOutcome { Cancelled = true, Message = "Namuna tanlanmadi." };
            }

            FrameCandidate sample;
            using (Transaction tr = _doc.Database.TransactionManager.StartTransaction())
            {
                var ent = (Entity)tr.GetObject(per.ObjectId, OpenMode.ForRead);
                if (!FrameReader.TryRead(ent, tr, out sample, out string why))
                {
                    tr.Commit();
                    return new DetectionOutcome { Message = "Namuna ramka emas: " + why + "." };
                }
                tr.Commit();
            }

            string sampleText = sample.Kind == SheetSourceKind.Block
                ? "blok “" + sample.Name + "”"
                : "to‘rtburchak, qatlam “" + sample.Layer + "”";
            _log.Info("Namuna: " + sampleText + ", " + Format(sample.Bounds.Width) + " x " + Format(sample.Bounds.Height) +
                      (_settings.SampleMatchSize ? " (faqat shu o‘lcham)" : string.Empty));

            IEnumerable<ObjectId> pool;
            if (withRange)
            {
                var opts = new PromptSelectionOptions { MessageForAdding = Uz.PromptSelectRange, AllowDuplicates = false };
                PromptSelectionResult sel = _doc.Editor.GetSelection(opts, FrameFilter);
                if (sel.Status != PromptStatus.OK || sel.Value == null)
                {
                    return new DetectionOutcome { Cancelled = true, Message = "Hudud tanlanmadi." };
                }
                var ids = new List<ObjectId>(sel.Value.GetObjectIds());
                if (!ids.Contains(sample.Id)) ids.Add(sample.Id);
                pool = ids;
            }
            else
            {
                pool = ModelSpaceFrameIds();
            }

            // Cheap pre-filter (type, layer, block name) before any geometry is read.
            Func<Entity, Transaction, bool> prefilter;
            if (sample.Kind == SheetSourceKind.Block)
            {
                prefilter = (e, t) => e is BlockReference br &&
                                      string.Equals(FrameReader.EffectiveName(br, t), sample.Name, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                prefilter = (e, t) => (e is Polyline || e is Polyline2d) &&
                                      (!_settings.SampleMatchLayer || string.Equals(e.Layer, sample.Layer, StringComparison.OrdinalIgnoreCase));
            }

            List<FrameCandidate> frames = ReadFrames(pool, out int rejected, prefilter)
                .Where(f => IsSimilar(sample, f))
                .ToList();
            _log.Info("Namunaga o‘xshash ramkalar: " + frames.Count + ".");
            return Finish(frames, keepInvalid: false, rejected);
        }

        private bool IsSimilar(FrameCandidate sample, FrameCandidate f)
        {
            if (f.Kind != sample.Kind) return false;

            if (sample.Kind == SheetSourceKind.Block)
            {
                if (!string.Equals(f.Name, sample.Name, StringComparison.OrdinalIgnoreCase)) return false;
            }
            else if (_settings.SampleMatchLayer && !string.Equals(f.Layer, sample.Layer, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (_settings.SampleMatchSize)
            {
                double tolW = Math.Max(sample.Bounds.Width, sample.Bounds.Height) * 0.005;
                bool same = Math.Abs(f.Bounds.Width - sample.Bounds.Width) <= tolW && Math.Abs(f.Bounds.Height - sample.Bounds.Height) <= tolW;
                bool turned = Math.Abs(f.Bounds.Width - sample.Bounds.Height) <= tolW && Math.Abs(f.Bounds.Height - sample.Bounds.Width) <= tolW;
                if (!same && !turned) return false;
            }
            return true;
        }

        // ---- Shared ------------------------------------------------------------------------

        private IEnumerable<ObjectId> ModelSpaceFrameIds()
        {
            var ids = new List<ObjectId>();
            using (Transaction tr = _doc.Database.TransactionManager.StartTransaction())
            {
                var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(_doc.Database), OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    string dxf = id.ObjectClass.DxfName;
                    if (dxf == "LWPOLYLINE" || dxf == "POLYLINE" || dxf == "INSERT") ids.Add(id);
                }
                tr.Commit();
            }
            return ids;
        }

        private List<FrameCandidate> ReadFrames(IEnumerable<ObjectId> ids, out int rejected, Func<Entity, Transaction, bool> prefilter = null)
        {
            var frames = new List<FrameCandidate>();
            rejected = 0;
            using (Transaction tr = _doc.Database.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in ids)
                {
                    if (id.IsNull || id.IsErased) continue;
                    try
                    {
                        if (!(tr.GetObject(id, OpenMode.ForRead) is Entity ent)) continue;
                        if (prefilter != null && !prefilter(ent, tr)) continue;
                        if (FrameReader.TryRead(ent, tr, out FrameCandidate frame, out string why, _blockCache)) frames.Add(frame);
                        else
                        {
                            rejected++;
                            _log.Debug("Obyekt " + ent.Handle + " (" + ent.GetType().Name + ") o‘tkazildi: " + why);
                        }
                    }
                    catch (Exception ex)
                    {
                        rejected++;
                        _log.Debug("Obyekt " + id + " o‘qilmadi: " + AcadErrors.Describe(ex));
                    }
                }
                tr.Commit();
            }
            return frames;
        }

        /// <summary>Resolve paper sizes, remove inner/duplicate/container frames, build the result message.</summary>
        private DetectionOutcome Finish(List<FrameCandidate> frames, bool keepInvalid, int rejected)
        {
            PaperSizeResolver resolver = CreateResolver(_doc.Database, _settings);

            var candidates = new List<SheetModel>(frames.Count);
            int invalid = 0;
            foreach (FrameCandidate f in frames)
            {
                var sheet = new SheetModel
                {
                    Handle = f.Handle,
                    SourceKind = f.Kind,
                    BlockName = f.Name,
                    Layer = f.Layer,
                    Bounds = f.Bounds,
                    Paper = resolver.Resolve(f.Bounds.Width, f.Bounds.Height)
                };
                if (!string.IsNullOrEmpty(f.Note)) sheet.Warnings.Add(f.Note);

                if (!sheet.Paper.IsValid)
                {
                    invalid++;
                    _log.Debug("Ramka " + f.Handle + " " + Format(f.Bounds.Width) + " x " + Format(f.Bounds.Height) + ": " + sheet.Paper.Problem);
                    if (!keepInvalid) continue;
                    sheet.Status = SheetStatus.Error;
                    sheet.StatusDetail = sheet.Paper.Problem;
                }
                candidates.Add(sheet);
            }

            List<SheetModel> sheets = SheetCandidateFilter.SelectSheets(candidates, out CandidateFilterStats stats);

            var parts = new List<string>
            {
                sheets.Count.ToString(CultureInfo.InvariantCulture) + " ta list topildi"
            };
            var formats = sheets.Where(s => s.Paper != null && s.Paper.IsValid)
                .GroupBy(s => s.Paper.FormatName ?? s.Paper.Paper.Key)
                .Select(g => g.Key + "×" + g.Count())
                .ToList();
            if (formats.Count > 0) parts[0] += " (" + string.Join(", ", formats) + ")";
            if (stats.Nested > 0) parts.Add(stats.Nested + " ta ichki ramka chiqarildi");
            if (stats.Duplicates > 0) parts.Add(stats.Duplicates + " ta takror ramka chiqarildi");
            if (stats.Containers > 0) parts.Add(stats.Containers + " ta umumiy chegara chiqarildi");
            if (invalid > 0) parts.Add(invalid + " ta juda kichik/katta to‘rtburchak " + (keepInvalid ? "xato deb belgilandi" : "e’tiborsiz qoldirildi"));

            string message = string.Join("; ", parts) + ".";
            _log.Info(message + (rejected > 0 ? " (" + rejected + " ta obyekt ramka emas)" : string.Empty));

            foreach (SheetModel s in sheets)
            {
                _log.Debug("Ramka " + s.Handle + " " + s.BlockName + " " + s.Bounds + " → " + s.Paper.DisplayText +
                           (s.Paper.FormatName != null ? " [" + s.Paper.FormatName + "]" : " [nostandart]"));
            }

            return new DetectionOutcome { Sheets = sheets, Message = message };
        }

        private static string Format(double v) => PaperSize.FormatMm(v);
    }
}
