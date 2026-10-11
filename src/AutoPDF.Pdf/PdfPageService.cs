using System;
using System.IO;
using AutoPDF.Core.Plotting;
using AutoPDF.Core.Localization;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace AutoPDF.Pdf
{
    /// <summary>Reads the page box of a produced PDF and crops it (PdfSharp).</summary>
    public static class PdfPageService
    {
        static PdfPageService()
        {
#if NETCOREAPP
            // PdfSharp 1.50 writes WinAnsi (code page 1252) strings; .NET 8 (AutoCAD 2025+) ships that code page
            // only through this provider.
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
#endif
        }

        public static PdfPageBox ReadFirstPage(string path, out int pageCount)
        {
            using (PdfDocument doc = PdfReader.Open(path, PdfDocumentOpenMode.Import))
            {
                pageCount = doc.PageCount;
                if (pageCount == 0) throw new InvalidDataException(L.PdfNoPages);
                PdfPage page = doc.Pages[0];
                PdfRectangle box = page.MediaBox;
                return new PdfPageBox(box.X1, box.Y1, box.X2, box.Y2, page.Rotate);
            }
        }

        /// <summary>
        /// Sets MediaBox/CropBox (and Trim/Bleed/Art boxes) of the first page to <paramref name="crop"/>.
        /// Written to a temporary file first and swapped in, so a failure never destroys the original.
        /// </summary>
        public static void Crop(string path, PdfPageBox crop)
        {
            string temp = path + ".autopdf-crop.tmp";
            try
            {
                using (PdfDocument doc = PdfReader.Open(path, PdfDocumentOpenMode.Modify))
                {
                    var rect = new PdfRectangle(new XPoint(crop.X1, crop.Y1), new XPoint(crop.X2, crop.Y2));
                    PdfPage page = doc.Pages[0];
                    page.MediaBox = rect;
                    page.CropBox = rect;
                    page.TrimBox = rect;
                    page.BleedBox = rect;
                    page.ArtBox = rect;
                    doc.Save(temp);
                }

                File.Copy(temp, path, true);
            }
            finally
            {
                try
                {
                    if (File.Exists(temp)) File.Delete(temp);
                }
                catch
                {
                    // leftover temp file is harmless
                }
            }
        }
    }
}
