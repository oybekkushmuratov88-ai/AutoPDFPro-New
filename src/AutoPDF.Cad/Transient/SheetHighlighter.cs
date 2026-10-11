using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AutoPDF.Core.Geometry;
using AutoPDF.Core.Models;
#if ZWCAD
using ZwSoft.ZwCAD.GraphicsInterface;
#else
using Autodesk.AutoCAD.GraphicsInterface;
#endif

namespace AutoPDF.Cad.Transient
{
    /// <summary>
    /// Draws temporary (non-database) outlines and big numbers over the detected sheets so the user can
    /// check detection and numbering on the drawing itself. Nothing is added to the DWG.
    /// </summary>
    public sealed class SheetHighlighter : IDisposable
    {
        private readonly List<Drawable> _drawables = new List<Drawable>();

        public void Show(IEnumerable<SheetModel> sheets)
        {
            TransientManager tm = TransientManager.CurrentTransientManager;
            foreach (SheetModel s in sheets)
            {
                Bounds2d b = s.Bounds;
                double size = Math.Min(b.Width, b.Height);
                short color = ColorFor(s.Status);

                // Non-database entities: take layer/text style defaults from the drawing first, then colour them.
                var outline = new CadPolyline(4);
                outline.SetDatabaseDefaults();
                outline.AddVertexAt(0, new Point2d(b.MinX, b.MinY), 0, 0, 0);
                outline.AddVertexAt(1, new Point2d(b.MaxX, b.MinY), 0, 0, 0);
                outline.AddVertexAt(2, new Point2d(b.MaxX, b.MaxY), 0, 0, 0);
                outline.AddVertexAt(3, new Point2d(b.MinX, b.MaxY), 0, 0, 0);
                outline.Closed = true;
                outline.ConstantWidth = size * 0.006;
                outline.ColorIndex = color;
                Add(tm, outline);

                Add(tm, Text(new Point3d(b.CenterX, b.CenterY, 0), AttachmentPoint.MiddleCenter, size * 0.18,
                    s.Number.ToString("00", CultureInfo.InvariantCulture), color));

                if (s.Paper != null)
                {
                    Add(tm, Text(new Point3d(b.CenterX, b.CenterY - size * 0.16, 0), AttachmentPoint.TopCenter, size * 0.045,
                        s.Paper.FormatName ?? s.Paper.Paper.ToString(), color));
                }
            }
        }

        /// <summary>Zooms the current view to all sheets (+5 % margin).</summary>
        public static void ZoomTo(Editor ed, IEnumerable<SheetModel> sheets, Matrix3d wcsToDcs)
        {
            List<SheetModel> list = sheets.ToList();
            if (list.Count == 0) return;

            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            foreach (SheetModel s in list)
            {
                foreach (Point3d p in new[]
                {
                    new Point3d(s.Bounds.MinX, s.Bounds.MinY, 0), new Point3d(s.Bounds.MaxX, s.Bounds.MinY, 0),
                    new Point3d(s.Bounds.MaxX, s.Bounds.MaxY, 0), new Point3d(s.Bounds.MinX, s.Bounds.MaxY, 0)
                })
                {
                    Point3d d = p.TransformBy(wcsToDcs);
                    minX = Math.Min(minX, d.X);
                    minY = Math.Min(minY, d.Y);
                    maxX = Math.Max(maxX, d.X);
                    maxY = Math.Max(maxY, d.Y);
                }
            }

            using (ViewTableRecord view = ed.GetCurrentView())
            {
                double w = (maxX - minX) * 1.05;
                double h = (maxY - minY) * 1.05;
                double aspect = view.Width / view.Height;
                if (w / h > aspect) h = w / aspect;
                else w = h * aspect;
                view.CenterPoint = new Point2d((minX + maxX) / 2.0, (minY + maxY) / 2.0);
                view.Width = w;
                view.Height = h;
                ed.SetCurrentView(view);
            }
        }

        private static MText Text(Point3d location, AttachmentPoint attachment, double height, string contents, short color)
        {
            var text = new MText();
            text.SetDatabaseDefaults();
            text.Location = location;
            text.Attachment = attachment;
            text.TextHeight = height;
            text.Contents = contents;
            text.ColorIndex = color;
            return text;
        }

        private void Add(TransientManager tm, Drawable d)
        {
            tm.AddTransient(d, TransientDrawingMode.Main, 128, new IntegerCollection());
            _drawables.Add(d);
        }

        private static short ColorFor(SheetStatus status)
        {
            switch (status)
            {
                case SheetStatus.Error:
                case SheetStatus.Failed: return 1;      // red
                case SheetStatus.Warning:
                case SheetStatus.DoneWithWarning: return 2; // yellow
                case SheetStatus.ReadyWithCrop: return 4;   // cyan
                default: return 3;                          // green
            }
        }

        public void Dispose()
        {
            TransientManager tm = TransientManager.CurrentTransientManager;
            foreach (Drawable d in _drawables)
            {
                try
                {
                    tm.EraseTransient(d, new IntegerCollection());
                }
                catch
                {
                    // ignore
                }
                d.Dispose();
            }
            _drawables.Clear();
        }
    }
}
