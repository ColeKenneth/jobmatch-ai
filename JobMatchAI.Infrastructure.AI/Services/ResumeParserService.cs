using System.Text;
using DocumentFormat.OpenXml.Packaging;
using JobMatchAI.Application.Services.Interfaces;
using UglyToad.PdfPig;
using Paragraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;


namespace JobMatchAI.Infrastructure.AI.Services;

public class ResumeParserService : IResumeParserService
{
    public async Task<string> ExtractTextAsync(Stream fileStream, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".pdf" => await ExtractFromPdfAsync(fileStream),
            ".docx" => await ExtractFromDocxAsync(fileStream),
            _ => throw new NotSupportedException(
                $"File format {extension} is not supported. Please use PDF or DOCX when uploading.")
        };

    }

    private static Task<string> ExtractFromPdfAsync(Stream fileStream)
    {
        var text = new StringBuilder();

        using var memoryStream = new MemoryStream();
        fileStream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        using var pdf = PdfDocument.Open(memoryStream);
        foreach (var page in pdf.GetPages())
        {
            text.AppendLine(page.Text);
        }
        
        return Task.FromResult(text.ToString());
    }

    private static Task<string> ExtractFromDocxAsync(Stream fileStream)
    {
        var text = new StringBuilder();

        using var wordDocument = WordprocessingDocument.Open(fileStream, false);
        var body = wordDocument.MainDocumentPart?.Document?.Body;

        if (body != null)
        {
            foreach (var paragraph in body.Elements<Paragraph>())
            {
                text.AppendLine(paragraph.InnerText);
            }
        }
        
        return Task.FromResult(text.ToString());
    }
}