using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Windows.Data.Pdf;
using Windows.Storage.Streams;

namespace AutoPDF.UI.Services
{
    /// <summary>
    /// Renders the first page of a PDF with the PDF engine built into Windows 10/11 (Windows.Data.Pdf).
    /// The file is read into memory first, so the PDF is never kept locked by the preview.
    /// </summary>
    public static class PdfPreviewRenderer
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

        public static BitmapSource TryRender(string path, int maxPixels, out string error)
        {
            error = null;
            try
            {
                byte[] pdf = File.ReadAllBytes(path);

                // WinRT async work runs on the thread pool: blocking the WPF/AutoCAD UI thread on an
                // await that needs the UI thread would dead-lock.
                Task<byte[]> task = Task.Run(() => RenderPngAsync(pdf, maxPixels));
                if (!task.Wait(Timeout))
                {
                    error = "PDF ko‘rinishini tayyorlash juda uzoq davom etdi.";
                    return null;
                }

                var image = new BitmapImage();
                using (var ms = new MemoryStream(task.Result))
                {
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = ms;
                    image.EndInit();
                }
                image.Freeze();
                return image;
            }
            catch (AggregateException ex)
            {
                error = ex.GetBaseException().Message;
                return null;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return null;
            }
        }

        private static async Task<byte[]> RenderPngAsync(byte[] pdf, int maxPixels)
        {
            using (var input = new InMemoryRandomAccessStream())
            {
                using (var writer = new DataWriter(input.GetOutputStreamAt(0)))
                {
                    writer.WriteBytes(pdf);
                    await writer.StoreAsync();
                    await writer.FlushAsync();
                    writer.DetachStream();
                }

                input.Seek(0);
                PdfDocument document = await PdfDocument.LoadFromStreamAsync(input);
                if (document.PageCount == 0) throw new InvalidDataException("PDF faylda sahifa yo‘q.");

                using (PdfPage page = document.GetPage(0))
                {
                    double longest = Math.Max(page.Size.Width, page.Size.Height);
                    double scale = longest > 0 ? maxPixels / longest : 1.0;
                    var options = new PdfPageRenderOptions
                    {
                        DestinationWidth = (uint)Math.Max(1, Math.Round(page.Size.Width * scale)),
                        DestinationHeight = (uint)Math.Max(1, Math.Round(page.Size.Height * scale)),
                        BackgroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255)
                    };

                    using (var output = new InMemoryRandomAccessStream())
                    {
                        await page.RenderToStreamAsync(output, options);
                        output.Seek(0);
                        using (Stream s = output.AsStream())
                        using (var ms = new MemoryStream())
                        {
                            await s.CopyToAsync(ms);
                            return ms.ToArray();
                        }
                    }
                }
            }
        }
    }
}
