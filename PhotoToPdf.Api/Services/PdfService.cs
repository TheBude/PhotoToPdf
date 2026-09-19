using System.IO.Compression;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PhotoToPdf.Api.Services;

public interface IPdfService
{
    byte[] ConvertImagesToPdf(List<IFormFile> files);
    byte[] ConvertPdfToImages(Stream pdfStream);
}

public class PdfService : IPdfService
{
    public byte[] ConvertImagesToPdf(List<IFormFile> files)
    {
        if (files == null || !files.Any())
            throw new InvalidOperationException("Hech qanday fayl tanlanmadi.");

        var document = new PdfDocument();

        foreach (var file in files)
        {
            if (file.Length == 0) continue;

            using var imageStream = file.OpenReadStream();
            using var image = Image.Load<Rgba32>(imageStream);

            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);

            var imageWidth = image.Width;
            var imageHeight = image.Height;
            var pageWidth = page.Width;
            var pageHeight = page.Height;

            var scale = Math.Min(pageWidth / imageWidth, pageHeight / imageHeight);
            var drawWidth = imageWidth * scale;
            var drawHeight = imageHeight * scale;

            var x = (pageWidth - drawWidth) / 2;
            var y = (pageHeight - drawHeight) / 2;

            using var pngStream = new MemoryStream();
            image.SaveAsPng(pngStream);
            pngStream.Position = 0;
            using var xImage = XImage.FromStream(() => pngStream);
            gfx.DrawImage(xImage, x, y, drawWidth, drawHeight);
        }

        using var output = new MemoryStream();
        document.Save(output, false);
        return output.ToArray();
    }

    public byte[] ConvertPdfToImages(Stream pdfStream)
    {
        var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
        {
            var pdfBytes = new byte[pdfStream.Length];
            pdfStream.Position = 0;
            _ = pdfStream.Read(pdfBytes, 0, pdfBytes.Length);

            var entry = archive.CreateEntry("original.pdf", CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            entryStream.Write(pdfBytes, 0, pdfBytes.Length);
        }

        return zipStream.ToArray();
    }
}
