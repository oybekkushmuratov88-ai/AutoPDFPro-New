using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoPDF.Core.Models
{
    public sealed class CandidateFilterStats
    {
        public int Duplicates { get; set; }
        public int Nested { get; set; }
        public int Containers { get; set; }
    }

    /// <summary>
    /// Cleans up raw frame candidates:
    /// • exact duplicates (the same frame drawn twice) are dropped;
    /// • a frame inside another frame (the inner border of a ГОСТ frame, title-block cells, detail boxes)
    ///   is dropped — the outer rectangle is the trimmed sheet edge and therefore the paper size;
    /// • but a rectangle that merely surrounds several sheets (a "drawing boundary" on the frame layer)
    ///   is treated as a container and dropped instead of the sheets it contains. A rectangle counts as a
    ///   container when it directly contains two or more candidates and is not itself a known paper format.
    /// Paper sizes must be resolved before calling this (IsKnownFormat is used).
    /// </summary>
    public static class SheetCandidateFilter
    {
        public static List<SheetModel> SelectSheets(IEnumerable<SheetModel> candidates, out CandidateFilterStats stats)
        {
            stats = new CandidateFilterStats();

            // Biggest first so every candidate's parent is already known when it is visited.
            List<SheetModel> ordered = candidates.OrderByDescending(s => s.Bounds.Area).ToList();
            var nodes = new List<Node>(ordered.Count);

            foreach (SheetModel s in ordered)
            {
                double tol = Tolerance(s);
                if (nodes.Any(n => n.Sheet.Bounds.NearlyEquals(s.Bounds, tol)))
                {
                    stats.Duplicates++;
                    continue;
                }

                // Parent = the smallest already-visited node that contains this one.
                Node parent = null;
                foreach (Node n in nodes)
                {
                    if (!n.Sheet.Bounds.Contains(s.Bounds, tol)) continue;
                    if (parent == null || n.Sheet.Bounds.Area < parent.Sheet.Bounds.Area) parent = n;
                }

                var node = new Node(s, parent);
                parent?.Children.Add(node);
                nodes.Add(node);
            }

            var kept = new List<SheetModel>();
            var queue = new Queue<Node>(nodes.Where(n => n.Parent == null));
            while (queue.Count > 0)
            {
                Node n = queue.Dequeue();
                bool knownFormat = n.Sheet.Paper != null && n.Sheet.Paper.IsKnownFormat;
                if (n.Children.Count >= 2 && !knownFormat)
                {
                    stats.Containers++;
                    foreach (Node child in n.Children) queue.Enqueue(child);
                    continue;
                }

                kept.Add(n.Sheet);
                stats.Nested += CountDescendants(n);
            }

            return kept;
        }

        private static int CountDescendants(Node n) => n.Children.Sum(c => 1 + CountDescendants(c));

        private static double Tolerance(SheetModel s) =>
            Math.Max(Math.Max(s.Bounds.Width, s.Bounds.Height) * 1e-6, 1e-6);

        private sealed class Node
        {
            public Node(SheetModel sheet, Node parent)
            {
                Sheet = sheet;
                Parent = parent;
            }

            public SheetModel Sheet { get; }
            public Node Parent { get; }
            public List<Node> Children { get; } = new List<Node>();
        }
    }
}
