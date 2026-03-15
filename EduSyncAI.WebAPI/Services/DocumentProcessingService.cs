using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace EduSyncAI.WebAPI.Services
{
    public class DocumentProcessingService
    {
        public async Task<string> ExtractTextFromFileAsync(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            return extension switch
            {
                ".txt" => await ExtractTextFromTxtAsync(filePath),
                ".pdf" => await ExtractTextFromPdfAsync(filePath),
                ".docx" => await ExtractTextFromDocxAsync(filePath),
                _ => throw new NotSupportedException($"File type {extension} is not supported")
            };
        }

        private async Task<string> ExtractTextFromTxtAsync(string filePath)
        {
            return await File.ReadAllTextAsync(filePath);
        }

        private async Task<string> ExtractTextFromPdfAsync(string filePath)
        {
            try
            {
                // Using iText7 for PDF extraction
                var text = new StringBuilder();
                
                using (var pdfReader = new iText.Kernel.Pdf.PdfReader(filePath))
                using (var pdfDocument = new iText.Kernel.Pdf.PdfDocument(pdfReader))
                {
                    for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
                    {
                        var page = pdfDocument.GetPage(i);
                        var strategy = new iText.Kernel.Pdf.Canvas.Parser.Listener.LocationTextExtractionStrategy();
                        var pageText = iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(page, strategy);
                        text.AppendLine(pageText);
                    }
                }

                return await Task.FromResult(text.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception($"Error extracting text from PDF: {ex.Message}", ex);
            }
        }

        private async Task<string> ExtractTextFromDocxAsync(string filePath)
        {
            try
            {
                // Using DocumentFormat.OpenXml for DOCX extraction
                var text = new StringBuilder();

                using (var document = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(filePath, false))
                {
                    var body = document.MainDocumentPart?.Document?.Body;
                    if (body != null)
                    {
                        text.Append(body.InnerText);
                    }
                }

                return await Task.FromResult(text.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception($"Error extracting text from DOCX: {ex.Message}", ex);
            }
        }
    }
}
