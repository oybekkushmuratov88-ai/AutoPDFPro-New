using System;
using System.Collections.Generic;
using AutoPDF.Core.Geometry;
using AutoPDF.Core.Models;
using AutoPDF.Core.Localization;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace AutoPDF.AutoCAD.Detection
{
    /// <summary>A frame read from one entity.</summary>
    public sealed class FrameCandidate
    {
        public ObjectId Id { get; set; }
        public string Handle { get; set; }
        public SheetSourceKind Kind { get; set; }
        public string Name { get; set; }
        public string Layer { get; set; }
        public int ColorIndex { get; set; }
        public Bounds2d Bounds { get; set; }
        public string Note { get; set; }
    }

    /// <summary>
    /// Per-scan cache: the largest rectangle of each block definition (in block coordinates), so a drawing
    /// with thousands of references to the same block reads its definition only once.
    /// </summary>
    public sealed class BlockFrameCache
    {
        internal readonly Dictionary<ObjectId, Bounds2d?> Rectangles = new Dictionary<ObjectId, Bounds2d?>();
    }

    /// <summary>Reads sheet frames from rectangles (LWPOLYLINE / 2D POLYLINE) and block references.</summary>
    public static class FrameReader
    {
        public static bool TryRead(Entity entity, Transaction tr, out FrameCandidate frame, out string rejectReason, BlockFrameCache cache = null)
        {
            frame = null;
            rejectReason = null;

            switch (entity)
            {
                case Polyline pl:
                    return TryReadPolyline(pl, out frame, out rejectReason);
                case Polyline2d p2d:
                    return TryReadPolyline2d(p2d, tr, out frame, out rejectReason);
                case BlockReference br:
                    return TryReadBlock(br, tr, out frame, out rejectReason, cache ?? new BlockFrameCache());
                default:
                    rejectReason = L.NotAFrame(entity.GetType().Name);
                    return false;
            }
        }

        public static string EffectiveName(BlockReference br, Transaction tr)
        {
            try
            {
                ObjectId id = br.IsDynamicBlock ? br.DynamicBlockTableRecord : br.BlockTableRecord;
                var btr = (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead);
                return btr.Name;
            }
            catch
            {
                return br.Name;
            }
        }

        private static bool TryReadPolyline(Polyline pl, out FrameCandidate frame, out string reject)
        {
            frame = null;
            if (!pl.Normal.IsParallelTo(Vector3d.ZAxis))
            {
                reject = L.NotInXyPlane;
                return false;
            }

            int n = pl.NumberOfVertices;
            var points = new List<Point2>(n);
            var bulges = new List<double>(n);
            for (int i = 0; i < n; i++)
            {
                Point3d p = pl.GetPoint3dAt(i);
                points.Add(new Point2(p.X, p.Y));
                bulges.Add(pl.GetBulgeAt(i));
            }

            if (!RectangleRecognizer.TryRecognize(points, pl.Closed, bulges, out Bounds2d bounds, out RectangleRejectReason why))
            {
                reject = L.NotRectangle(why.ToString());
                return false;
            }

            reject = null;
            frame = new FrameCandidate
            {
                Id = pl.ObjectId,
                Handle = pl.Handle.ToString(),
                Kind = SheetSourceKind.Rectangle,
                Name = SheetModel.RectangleName,
                Layer = pl.Layer,
                ColorIndex = pl.ColorIndex,
                Bounds = bounds
            };
            return true;
        }

        private static bool TryReadPolyline2d(Polyline2d p2d, Transaction tr, out FrameCandidate frame, out string reject)
        {
            frame = null;
            if (p2d.PolyType != Poly2dType.SimplePoly)
            {
                reject = L.CurvedPolyline;
                return false;
            }
            if (!p2d.Normal.IsParallelTo(Vector3d.ZAxis))
            {
                reject = L.NotInXyPlane;
                return false;
            }

            var points = new List<Point2>();
            var bulges = new List<double>();
            foreach (ObjectId vid in p2d)
            {
                var v = (Vertex2d)tr.GetObject(vid, OpenMode.ForRead);
                Point3d p = p2d.VertexPosition(v);
                points.Add(new Point2(p.X, p.Y));
                bulges.Add(v.Bulge);
            }

            if (!RectangleRecognizer.TryRecognize(points, p2d.Closed, bulges, out Bounds2d bounds, out RectangleRejectReason why))
            {
                reject = L.NotRectangle(why.ToString());
                return false;
            }

            reject = null;
            frame = new FrameCandidate
            {
                Id = p2d.ObjectId,
                Handle = p2d.Handle.ToString(),
                Kind = SheetSourceKind.Rectangle,
                Name = SheetModel.RectangleName,
                Layer = p2d.Layer,
                ColorIndex = p2d.ColorIndex,
                Bounds = bounds
            };
            return true;
        }

        /// <summary>
        /// A title-block frame is usually a block containing a rectangle. The largest rectangle inside the
        /// block definition, transformed by the insertion, is the sheet edge; attributes or text sticking out
        /// of the frame are thereby ignored. Without such a rectangle the block's extents are used.
        /// </summary>
        private static bool TryReadBlock(BlockReference br, Transaction tr, out FrameCandidate frame, out string reject, BlockFrameCache cache)
        {
            frame = null;
            reject = null;
            string note = null;
            Bounds2d? bounds = null;

            try
            {
                Bounds2d? local = LargestRectangle(br.BlockTableRecord, tr, cache);
                if (local.HasValue)
                {
                    Bounds2d b = local.Value;
                    Matrix3d xf = br.BlockTransform;
                    var pts = new List<Point2>();
                    foreach (Point3d c in new[]
                    {
                        new Point3d(b.MinX, b.MinY, 0).TransformBy(xf),
                        new Point3d(b.MaxX, b.MinY, 0).TransformBy(xf),
                        new Point3d(b.MaxX, b.MaxY, 0).TransformBy(xf),
                        new Point3d(b.MinX, b.MaxY, 0).TransformBy(xf),
                    })
                    {
                        pts.Add(new Point2(c.X, c.Y));
                    }

                    bounds = Bounds2d.FromPoints(pts);
                    note = RectangleRecognizer.TryRecognize(pts, true, null, out _, out _)
                        ? null
                        : L.BlockRotated;
                }
            }
            catch
            {
                bounds = null;
            }

            if (bounds == null)
            {
                try
                {
                    Extents3d e;
                    try { e = br.GeometryExtentsBestFit(); }
                    catch { e = br.GeometricExtents; }
                    bounds = new Bounds2d(e.MinPoint.X, e.MinPoint.Y, e.MaxPoint.X, e.MaxPoint.Y);
                    note = L.BlockNoRectangle;
                }
                catch (Exception ex)
                {
                    reject = L.BlockExtentsFailed(ex.Message);
                    return false;
                }
            }

            if (bounds.Value.IsEmpty)
            {
                reject = L.BlockEmpty;
                return false;
            }

            frame = new FrameCandidate
            {
                Id = br.ObjectId,
                Handle = br.Handle.ToString(),
                Kind = SheetSourceKind.Block,
                Name = EffectiveName(br, tr),
                Layer = br.Layer,
                ColorIndex = br.ColorIndex,
                Bounds = bounds.Value,
                Note = note
            };
            return true;
        }

        /// <summary>Largest frame rectangle directly inside a block definition (block coordinates), or null.</summary>
        private static Bounds2d? LargestRectangle(ObjectId blockDefinitionId, Transaction tr, BlockFrameCache cache)
        {
            if (cache.Rectangles.TryGetValue(blockDefinitionId, out Bounds2d? cached)) return cached;

            Bounds2d? best = null;
            var btr = (BlockTableRecord)tr.GetObject(blockDefinitionId, OpenMode.ForRead);
            foreach (ObjectId id in btr)
            {
                if (id.ObjectClass.DxfName != "LWPOLYLINE") continue;
                if (!(tr.GetObject(id, OpenMode.ForRead) is Polyline pl)) continue;
                if (!TryReadPolyline(pl, out FrameCandidate inner, out _)) continue;
                if (best == null || inner.Bounds.Area > best.Value.Area) best = inner.Bounds;
            }

            cache.Rectangles[blockDefinitionId] = best;
            return best;
        }
    }
}
