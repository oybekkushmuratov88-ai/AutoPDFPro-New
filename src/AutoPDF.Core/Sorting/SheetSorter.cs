using System;
using System.Collections.Generic;
using System.Linq;
using AutoPDF.Core.Models;

namespace AutoPDF.Core.Sorting
{
    /// <summary>
    /// Orders sheets and assigns numbers.
    /// Left → right: sheets are grouped into rows (by vertical overlap); rows go top → bottom and each
    /// row is read left → right. Top → bottom: grouped into columns; columns go left → right and each
    /// column is read top → bottom. Grouping by overlap (not by exact coordinates) keeps a row together
    /// even when the sheets have different heights.
    /// </summary>
    public static class SheetSorter
    {
        /// <summary>Two sheets belong to the same row/column when they overlap by at least this share of the smaller one.</summary>
        public const double OverlapShare = 0.5;

        public static IReadOnlyList<SheetModel> Sort(IEnumerable<SheetModel> sheets, SortDirection direction, int startNumber)
        {
            if (sheets == null) throw new ArgumentNullException(nameof(sheets));
            List<SheetModel> list = sheets.ToList();
            if (list.Count == 0) return list;

            List<SheetModel> ordered;
            if (direction == SortDirection.TopToBottom)
            {
                ordered = Cluster(list, s => s.Bounds.MinX, s => s.Bounds.MaxX)
                    .OrderBy(g => g.Center)
                    .SelectMany(g => g.Items.OrderByDescending(s => s.Bounds.CenterY).ThenBy(s => s.Bounds.CenterX))
                    .ToList();
            }
            else
            {
                ordered = Cluster(list, s => s.Bounds.MinY, s => s.Bounds.MaxY)
                    .OrderByDescending(g => g.Center)
                    .SelectMany(g => g.Items.OrderBy(s => s.Bounds.CenterX).ThenByDescending(s => s.Bounds.CenterY))
                    .ToList();
            }

            for (int i = 0; i < ordered.Count; i++)
            {
                ordered[i].Number = startNumber + i;
            }
            return ordered;
        }

        private static List<Group> Cluster(List<SheetModel> sheets, Func<SheetModel, double> lo, Func<SheetModel, double> hi)
        {
            var groups = new List<Group>();
            // Largest sheets first so small sheets attach to an established band.
            foreach (SheetModel s in sheets.OrderByDescending(x => hi(x) - lo(x)).ThenBy(x => lo(x)))
            {
                double sLo = lo(s), sHi = hi(s), extent = sHi - sLo;
                Group best = null;
                double bestOverlap = 0;
                foreach (Group g in groups)
                {
                    double overlap = Math.Min(sHi, g.Hi) - Math.Max(sLo, g.Lo);
                    double needed = OverlapShare * Math.Min(extent, g.MinExtent);
                    if (overlap > 0 && overlap >= needed && overlap > bestOverlap)
                    {
                        best = g;
                        bestOverlap = overlap;
                    }
                }

                if (best == null)
                {
                    best = new Group { Lo = sLo, Hi = sHi, MinExtent = extent };
                    groups.Add(best);
                }
                else
                {
                    best.MinExtent = Math.Min(best.MinExtent, extent);
                }
                best.Items.Add(s);
            }
            return groups;
        }

        private sealed class Group
        {
            public double Lo;
            public double Hi;
            public double MinExtent;
            public readonly List<SheetModel> Items = new List<SheetModel>();
            public double Center => (Lo + Hi) / 2.0;
        }
    }
}
