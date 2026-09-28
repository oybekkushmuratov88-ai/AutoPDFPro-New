using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AutoPDF.Core.Plotting
{
    /// <summary>A point argument of the -PLOT command, in the current UCS.</summary>
    public readonly struct PlotPoint
    {
        public PlotPoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; }
        public double Y { get; }

        public override string ToString() =>
            X.ToString("0.######", CultureInfo.InvariantCulture) + "," + Y.ToString("0.######", CultureInfo.InvariantCulture);
    }

    public sealed class PlotCommandInput
    {
        public string DeviceName { get; set; }

        /// <summary>
        /// The paper name as the command line expects it: the *locale* name returned by
        /// PlotSettingsValidator.GetLocaleMediaName — never a canonical name with '_' replaced by spaces
        /// (that was one reason the old -PLOT fallback failed in the Russian AutoCAD).
        /// </summary>
        public string LocaleMediaName { get; set; }

        /// <summary>PlotRotation 90° = "Landscape", 0° = "Portrait".</summary>
        public bool Landscape { get; set; }

        public PlotPoint LowerLeft { get; set; }
        public PlotPoint UpperRight { get; set; }

        /// <summary>Drawing units per plotted millimetre ("1=N").</summary>
        public double DrawingUnitsPerMm { get; set; } = 1;

        public bool Center { get; set; } = true;

        /// <summary>Plot style table (.ctb/.stb) or null/empty for none.</summary>
        public string StyleTable { get; set; }

        public bool Lineweights { get; set; } = true;

        /// <summary>When the device may plot to paper, -PLOT first asks "Write the plot to a file?".</summary>
        public bool DeviceAsksWriteToFile { get; set; }

        public string OutputPath { get; set; }
    }

    /// <summary>
    /// Builds the answers for AutoCAD's detailed "-PLOT" command in model space. Every keyword uses the
    /// global (underscore) form so the sequence works in a localized (e.g. Russian) AutoCAD; the layout
    /// name is answered with Enter (current layout = Model) because its display name is localized.
    /// </summary>
    public static class PlotCommandBuilder
    {
        public static IReadOnlyList<object> Build(PlotCommandInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (string.IsNullOrWhiteSpace(input.DeviceName)) throw new ArgumentException("Device name is required.", nameof(input));
            if (string.IsNullOrWhiteSpace(input.LocaleMediaName)) throw new ArgumentException("Media name is required.", nameof(input));
            if (string.IsNullOrWhiteSpace(input.OutputPath)) throw new ArgumentException("Output path is required.", nameof(input));

            var tokens = new List<object>
            {
                "_.-PLOT",
                "_Yes",                                   // Detailed plot configuration?
                "",                                       // Layout name <Model> → current
                input.DeviceName,                         // Output device
                input.LocaleMediaName,                    // Paper size
                "_Millimeters",                           // Paper units
                input.Landscape ? "_Landscape" : "_Portrait",
                "_No",                                    // Plot upside down?
                "_Window",                                // Plot area
                input.LowerLeft,
                input.UpperRight,
                "1=" + input.DrawingUnitsPerMm.ToString("0.########", CultureInfo.InvariantCulture),
                input.Center ? "_Center" : "0,0",         // Plot offset
                "_Yes",                                   // Plot with plot styles? (always Yes: the table prompt follows either way)
                string.IsNullOrWhiteSpace(input.StyleTable) ? "." : input.StyleTable,
                input.Lineweights ? "_Yes" : "_No",       // Plot with lineweights?
                "_As",                                    // Shade plot: As displayed (model space only)
            };

            if (input.DeviceAsksWriteToFile)
            {
                tokens.Add("_Yes");                       // Write the plot to a file?
            }

            tokens.Add(input.OutputPath);                 // File name
            tokens.Add("_No");                            // Save changes to page setup?
            tokens.Add("_Yes");                           // Proceed with plot?
            return tokens;
        }

        /// <summary>Readable one-line form for the log.</summary>
        public static string Describe(IEnumerable<object> tokens) =>
            string.Join(" | ", tokens.Select(t => t is string s ? (s.Length == 0 ? "<Enter>" : s) : t?.ToString()));
    }
}
