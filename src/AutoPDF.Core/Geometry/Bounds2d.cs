using System;
using System.Collections.Generic;
using System.Globalization;

namespace AutoPDF.Core.Geometry
{
    /// <summary>A 2D point in world (WCS) coordinates.</summary>
    public readonly struct Point2
    {
        public Point2(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; }
        public double Y { get; }

        public double DistanceTo(Point2 other)
        {
            double dx = other.X - X;
            double dy = other.Y - Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public override string ToString() =>
            string.Format(CultureInfo.InvariantCulture, "({0:0.###},{1:0.###})", X, Y);
    }

    /// <summary>Axis-aligned bounding box in drawing units (WCS).</summary>
    public readonly struct Bounds2d : IEquatable<Bounds2d>
    {
        public Bounds2d(double x1, double y1, double x2, double y2)
        {
            MinX = Math.Min(x1, x2);
            MinY = Math.Min(y1, y2);
            MaxX = Math.Max(x1, x2);
            MaxY = Math.Max(y1, y2);
        }

        public double MinX { get; }
        public double MinY { get; }
        public double MaxX { get; }
        public double MaxY { get; }

        public double Width => MaxX - MinX;
        public double Height => MaxY - MinY;
        public double CenterX => (MinX + MaxX) / 2.0;
        public double CenterY => (MinY + MaxY) / 2.0;
        public double Area => Width * Height;
        public bool IsEmpty => !(Width > 0) || !(Height > 0);

        public static Bounds2d FromPoints(IEnumerable<Point2> points)
        {
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;
            bool any = false;
            foreach (Point2 p in points)
            {
                any = true;
                minX = Math.Min(minX, p.X);
                minY = Math.Min(minY, p.Y);
                maxX = Math.Max(maxX, p.X);
                maxY = Math.Max(maxY, p.Y);
            }
            return any ? new Bounds2d(minX, minY, maxX, maxY) : default;
        }

        /// <summary>True when <paramref name="other"/> lies completely inside this box (with tolerance).</summary>
        public bool Contains(Bounds2d other, double tolerance) =>
            other.MinX >= MinX - tolerance &&
            other.MinY >= MinY - tolerance &&
            other.MaxX <= MaxX + tolerance &&
            other.MaxY <= MaxY + tolerance;

        public bool NearlyEquals(Bounds2d other, double tolerance) =>
            Math.Abs(MinX - other.MinX) <= tolerance &&
            Math.Abs(MinY - other.MinY) <= tolerance &&
            Math.Abs(MaxX - other.MaxX) <= tolerance &&
            Math.Abs(MaxY - other.MaxY) <= tolerance;

        public Bounds2d Union(Bounds2d other) =>
            new Bounds2d(
                Math.Min(MinX, other.MinX), Math.Min(MinY, other.MinY),
                Math.Max(MaxX, other.MaxX), Math.Max(MaxY, other.MaxY));

        public bool Equals(Bounds2d other) =>
            MinX.Equals(other.MinX) && MinY.Equals(other.MinY) && MaxX.Equals(other.MaxX) && MaxY.Equals(other.MaxY);

        public override bool Equals(object obj) => obj is Bounds2d b && Equals(b);

        public override int GetHashCode()
        {
            unchecked
            {
                int h = MinX.GetHashCode();
                h = (h * 397) ^ MinY.GetHashCode();
                h = (h * 397) ^ MaxX.GetHashCode();
                return (h * 397) ^ MaxY.GetHashCode();
            }
        }

        public override string ToString() =>
            string.Format(CultureInfo.InvariantCulture, "({0:0.###},{1:0.###})-({2:0.###},{3:0.###})", MinX, MinY, MaxX, MaxY);
    }
}
