using System;
using System.IO;

namespace EduSyncAI
{
    public class FileService
    {
    private const string SyllabiDirectory = "Data/Syllabi";

    public FileService()
    {
        // Ensure syllabi directory exists
        if (!Directory.Exists(SyllabiDirectory))
        {
            Directory.CreateDirectory(SyllabiDirectory);
        }
    }

    public string SaveSyllabus(string sourcePath, string courseCode)
    {
        if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
        {
            throw new FileNotFoundException("Source file not found", sourcePath);
        }

        // Validate file extension
        var extension = Path.GetExtension(sourcePath).ToLower();
        if (extension != ".pdf" && extension != ".doc" && extension != ".docx")
        {
            throw new InvalidOperationException("Only PDF and DOC/DOCX files are supported");
        }

        // Create destination path
        var fileName = $"{courseCode}_Syllabus{extension}";
        var destinationPath = Path.Combine(SyllabiDirectory, fileName);

        // Copy file
        File.Copy(sourcePath, destinationPath, overwrite: true);

        return destinationPath;
    }

    public string GetSyllabusPath(string courseCode)
    {
        // Check for PDF first, then DOC, then DOCX
        var extensions = new[] { ".pdf", ".doc", ".docx" };
        
        foreach (var ext in extensions)
        {
            var path = Path.Combine(SyllabiDirectory, $"{courseCode}_Syllabus{ext}");
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    public bool SyllabusExists(string courseCode)
    {
        return GetSyllabusPath(courseCode) != null;
    }
    }
}
