using System;
using System.Collections.Generic;

namespace AutoPDF.Core.Geometry
{
    public enum RectangleRejectReason
    {
        None,
        NotClosed,
        WrongVertexCount,
        HasArcs,
        NotAxisAligned,
        NotRightAngles,
        Degenerate
    }

    /// <summary>
    /// Decides whether a polyline is a sheet frame: closed, exactly 4 vertices, no arc segments,
    /// edges parallel to the X/Y axes. A 5-vertex polyline whose last vertex repeats the first
    /// (a "manually closed" rectangle) is accepted as well.
    /// </summary>
    public static class RectangleRecognizer
    {
        /// <summary>Allowed deviation of an edge from the X/Y axis, as |minor| / |major| (≈0.006°).</summary>
        public const double DefaultAxisTolerance = 1e-4;

        private const double BulgeTolerance = 1e-9;

        public static bool TryRecognize(
            IReadOnlyList<Point2> vertices,
            bool closed,
            IReadOnlyList<double> bulges,
            out Bounds2d bounds,
            out RectangleRejectReason reason,
            double axisTolerance = DefaultAxisTolerance)
        {
            bounds = default;
            reason = RectangleRejectReason.None;

            if (vertices == null || vertices.Count < 4)
            {
                reason = RectangleRejectReason.WrongVertexCount;
                return false;
            }

            var pts = new List<Point2>(vertices);
            Bounds2d all = Bounds2d.FromPoints(pts);
            double size = Math.Max(all.Width, all.Height);
            double pointTol = Math.Max(size * 1e-7, 1e-9);

            if (pts.Count == 5 && pts[0].DistanceTo(pts[4]) <= pointTol)
            {
                // Explicit duplicate closing vertex – treat as closed rectangle.
                pts.RemoveAt(4);
                closed = true;
            }

            if (!closed)
            {
                reason = RectangleRejectReason.NotClosed;
                return false;
            }

            if (pts.Count != 4)
            {
                reason = RectangleRejectReason.WrongVertexCount;
                return false;
            }

            if (bulges != null)
            {
                for (int i = 0; i < Math.Min(4, bulges.Count); i++)
                {
                    if (Math.Abs(bulges[i]) > BulgeTolerance)
                    {
                        reason = RectangleRejectReason.HasArcs;
                        return false;
                    }
                }
            }

            // Classify each edge as horizontal (H) or vertical (V).
            var kinds = new char[4];
            for (int i = 0; i < 4; i++)
            {
                Point2 a = pts[i];
                Point2 b = pts[(i + 1) % 4];
                double dx = Math.Abs(b.X - a.X);
                double dy = Math.Abs(b.Y - a.Y);
                if (dx <= pointTol && dy <= pointTol)
                {
                    reason = RectangleRejectReason.Degenerate;
                    return false;
                }

                if (dy <= dx * axisTolerance) kinds[i] = 'H';
                else if (dx <= dy * axisTolerance) kinds[i] = 'V';
                else kinds[i] = '?';
            }

            bool alternating = kinds[0] != '?' && kinds[1] != '?' && kinds[2] != '?' && kinds[3] != '?' &&
                               kinds[0] != kinds[1] && kinds[1] != kinds[2] && kinds[2] != kinds[3] && kinds[3] != kinds[0];

            if (!alternating)
            {
                reason = IsRightAngled(pts) ? RectangleRejectReason.NotAxisAligned : RectangleRejectReason.NotRightAngles;
                return false;
            }

            bounds = Bounds2d.FromPoints(pts);
            if (bounds.IsEmpty)
            {
                reason = RectangleRejectReason.Degenerate;
                return false;
            }

            return true;
        }

        private static bool IsRightAngled(IReadOnlyList<Point2> pts)
        {
            for (int i = 0; i < 4; i++)
            {
                Point2 a = pts[i];
                Point2 b = pts[(i + 1) % 4];
                Point2 c = pts[(i + 2) % 4];
                double ux = b.X - a.X, uy = b.Y - a.Y;
                double vx = c.X - b.X, vy = c.Y - b.Y;
                double lu = Math.Sqrt(ux * ux + uy * uy);
                double lv = Math.Sqrt(vx * vx + vy * vy);
                if (lu <= 0 || lv <= 0) return false;
                double cos = Math.Abs((ux * vx + uy * vy) / (lu * lv));
                if (cos > 1e-4) return false;
            }
            return true;
        }
    }
}
