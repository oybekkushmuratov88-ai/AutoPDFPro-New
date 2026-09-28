using System.Collections.Generic;
using System.Linq;
using AutoPDF.Core.Geometry;
using AutoPDF.Core.Models;
using AutoPDF.Core.Sorting;
using Xunit;

namespace AutoPDF.Core.Tests
{
    public class RectangleRecognizerTests
    {
        private static List<Point2> Rect(double x, double y, double w, double h) => new List<Point2>
        {
            new Point2(x, y), new Point2(x + w, y), new Point2(x + w, y + h), new Point2(x, y + h)
        };

        [Fact]
        public void ClosedFourVertexRectangle_IsAccepted()
        {
            bool ok = RectangleRecognizer.TryRecognize(Rect(10, 20, 210, 297), true, new double[4], out Bounds2d b, out _);
            Assert.True(ok);
            Assert.Equal(210, b.Width, 9);
            Assert.Equal(297, b.Height, 9);
        }

        [Fact]
        public void FiveVerticesWithRepeatedFirst_IsAccepted()
        {
            var pts = Rect(0, 0, 420, 297);
            pts.Add(new Point2(0, 0));
            Assert.True(RectangleRecognizer.TryRecognize(pts, false, new double[5], out _, out _));
        }

        [Fact]
        public void OpenPolyline_IsRejected()
        {
            Assert.False(RectangleRecognizer.TryRecognize(Rect(0, 0, 10, 10), false, null, out _, out var reason));
            Assert.Equal(RectangleRejectReason.NotClosed, reason);
        }

        [Fact]
        public void Bulge_IsRejected()
        {
            Assert.False(RectangleRecognizer.TryRecognize(Rect(0, 0, 10, 10), true, new[] { 0, 0.5, 0, 0 }, out _, out var reason));
            Assert.Equal(RectangleRejectReason.HasArcs, reason);
        }

        [Fact]
        public void Parallelogram_IsRejected()
        {
            var pts = new List<Point2> { new Point2(0, 0), new Point2(100, 0), new Point2(120, 50), new Point2(20, 50) };
            Assert.False(RectangleRecognizer.TryRecognize(pts, true, null, out _, out var reason));
            Assert.Equal(RectangleRejectReason.NotRightAngles, reason);
        }

        [Fact]
        public void RotatedRectangle_IsRejectedAsNotAxisAligned()
        {
            var pts = new List<Point2> { new Point2(0, 0), new Point2(100, 100), new Point2(50, 150), new Point2(-50, 50) };
            Assert.False(RectangleRecognizer.TryRecognize(pts, true, null, out _, out var reason));
            Assert.Equal(RectangleRejectReason.NotAxisAligned, reason);
        }

        [Fact]
        public void ThreeVertices_IsRejected()
        {
            var pts = new List<Point2> { new Point2(0, 0), new Point2(1, 0), new Point2(1, 1) };
            Assert.False(RectangleRecognizer.TryRecognize(pts, true, null, out _, out var reason));
            Assert.Equal(RectangleRejectReason.WrongVertexCount, reason);
        }
    }

    public class SheetSorterTests
    {
        private static SheetModel S(string id, double x, double y, double w, double h) =>
            new SheetModel { Handle = id, Bounds = new Bounds2d(x, y, x + w, y + h) };

        [Fact]
        public void LeftToRight_SingleRowWithDifferentHeights()
        {
            var sheets = new[]
            {
                S("c", 900, 0, 420, 594),
                S("a", 0, 0, 210, 297),
                S("b", 300, 0, 594, 420),
            };

            var sorted = SheetSorter.Sort(sheets, SortDirection.LeftToRight, 1);
            Assert.Equal(new[] { "a", "b", "c" }, sorted.Select(s => s.Handle));
            Assert.Equal(new[] { 1, 2, 3 }, sorted.Select(s => s.Number));
        }

        [Fact]
        public void LeftToRight_TwoRows_ReadsTopRowFirst()
        {
            var sheets = new[]
            {
                S("d", 300, 0, 210, 297), S("c", 0, 0, 210, 297),
                S("b", 300, 500, 210, 297), S("a", 0, 500, 210, 297),
            };
            var sorted = SheetSorter.Sort(sheets, SortDirection.LeftToRight, 1);
            Assert.Equal(new[] { "a", "b", "c", "d" }, sorted.Select(s => s.Handle));
        }

        [Fact]
        public void TopToBottom_TwoColumns_ReadsLeftColumnFirst()
        {
            var sheets = new[]
            {
                S("d", 300, 0, 210, 297), S("b", 0, 0, 210, 297),
                S("c", 300, 500, 210, 297), S("a", 0, 500, 210, 297),
            };
            var sorted = SheetSorter.Sort(sheets, SortDirection.TopToBottom, 1);
            Assert.Equal(new[] { "a", "b", "c", "d" }, sorted.Select(s => s.Handle));
        }

        [Fact]
        public void StartNumberIsApplied()
        {
            var sorted = SheetSorter.Sort(new[] { S("a", 0, 0, 1, 1), S("b", 5, 0, 1, 1) }, SortDirection.LeftToRight, 7);
            Assert.Equal(new[] { 7, 8 }, sorted.Select(s => s.Number));
        }

        private static SheetModel P(string id, double x, double y, double w, double h, bool known)
        {
            var s = S(id, x, y, w, h);
            s.Paper = new AutoPDF.Core.Paper.PaperResolution { IsKnownFormat = known, IsValid = true };
            return s;
        }

        [Fact]
        public void NestedAndDuplicateFramesAreRemoved()
        {
            var outer = P("outer", 0, 0, 420, 297, true);
            var inner = P("inner", 20, 5, 395, 287, false);
            var dup = P("dup", 0, 0, 420, 297, true);
            var other = P("other", 500, 0, 210, 297, true);

            var kept = SheetCandidateFilter.SelectSheets(new[] { inner, outer, dup, other }, out var stats);

            Assert.Equal(2, kept.Count);
            Assert.Contains(kept, s => s.Handle == "other");
            Assert.Equal(1, stats.Duplicates);
            Assert.Equal(1, stats.Nested);
        }

        [Fact]
        public void BoundaryRectangleAroundSheets_IsDroppedInsteadOfTheSheets()
        {
            var boundary = P("boundary", -50, -50, 1500, 500, false);
            var a = P("a", 0, 0, 420, 297, true);
            var aInner = P("aInner", 20, 5, 395, 287, false);
            var b = P("b", 500, 0, 210, 297, true);
            var c = P("c", 800, 0, 594, 420, true);

            var kept = SheetCandidateFilter.SelectSheets(new[] { boundary, a, aInner, b, c }, out var stats);

            Assert.Equal(new[] { "a", "b", "c" }, kept.Select(s => s.Handle).OrderBy(h => h));
            Assert.Equal(1, stats.Containers);
            Assert.Equal(1, stats.Nested);
        }

        [Fact]
        public void KnownFormatSheetWithTwoDetailBoxes_StaysOneSheet()
        {
            var sheet = P("A1", 0, 0, 841, 594, true);
            var d1 = P("d1", 50, 50, 210, 297, true);
            var d2 = P("d2", 300, 50, 210, 297, true);

            var kept = SheetCandidateFilter.SelectSheets(new[] { d1, sheet, d2 }, out _);

            Assert.Single(kept);
            Assert.Equal("A1", kept[0].Handle);
        }
    }
}
