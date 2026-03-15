using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OfficeOpenXml;

namespace EduSyncAI
{
    public class StudentImportService
    {
        private readonly DatabaseService _dbService;

        public StudentImportService()
        {
            _dbService = new DatabaseService();
            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public class ImportResult
        {
            public int SuccessCount { get; set; }
            public int ErrorCount { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
            public List<Student> ImportedStudents { get; set; } = new List<Student>();
        }

        /// <summary>
        /// Import students from Excel file
        /// Expected columns: MatricNumber, FullName, Email, WindowsUsername (optional)
        /// </summary>
        public ImportResult ImportFromExcel(string filePath, int courseId)
        {
            var result = new ImportResult();

            try
            {
                using var package = new ExcelPackage(new FileInfo(filePath));
                var worksheet = package.Workbook.Worksheets[0]; // First sheet
                int rowCount = worksheet.Dimension?.Rows ?? 0;

                if (rowCount < 2) // Need at least header + 1 data row
                {
                    result.Errors.Add("Excel file is empty or has no data rows");
                    return result;
                }

                // Read header row to find column indices
                int matricCol = -1, nameCol = -1, emailCol = -1, windowsUserCol = -1;
                
                for (int col = 1; col <= worksheet.Dimension.Columns; col++)
                {
                    var header = worksheet.Cells[1, col].Text?.Trim().ToLower();
                    if (header == "matricnumber" || header == "matric number" || header == "matric")
                        matricCol = col;
                    else if (header == "fullname" || header == "full name" || header == "name")
                        nameCol = col;
                    else if (header == "email")
                        emailCol = col;
                    else if (header == "windowsusername" || header == "windows username" || header == "username")
                        windowsUserCol = col;
                }

                if (matricCol == -1 || nameCol == -1 || emailCol == -1)
                {
                    result.Errors.Add("Required columns missing. Need: MatricNumber, FullName, Email");
                    return result;
                }

                // Process each row
                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var matric = worksheet.Cells[row, matricCol].Text?.Trim();
                        var name = worksheet.Cells[row, nameCol].Text?.Trim();
                        var email = worksheet.Cells[row, emailCol].Text?.Trim();
                        var windowsUser = windowsUserCol > 0 ? worksheet.Cells[row, windowsUserCol].Text?.Trim() : null;

                        // Validate
                        if (string.IsNullOrWhiteSpace(matric) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
                        {
                            result.Errors.Add($"Row {row}: Missing required fields");
                            result.ErrorCount++;
                            continue;
                        }

                        // Check if student already exists
                        var existingStudents = _dbService.GetAllStudents();
                        if (existingStudents.Any(s => s.MatricNumber == matric))
                        {
                            result.Errors.Add($"Row {row}: Student {matric} already exists");
                            result.ErrorCount++;
                            continue;
                        }

                        // Create student
                        var student = new Student
                        {
                            MatricNumber = matric,
                            FullName = name,
                            Email = email,
                            WindowsUsername = windowsUser
                        };

                        int studentId = _dbService.CreateStudent(student);
                        student.Id = studentId;

                        // Enroll in course
                        _dbService.EnrollStudent(studentId, courseId);

                        result.ImportedStudents.Add(student);
                        result.SuccessCount++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Row {row}: {ex.Message}");
                        result.ErrorCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Error reading Excel file: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Import students from CSV file
        /// </summary>
        public ImportResult ImportFromCSV(string filePath, int courseId)
        {
            var result = new ImportResult();

            try
            {
                var lines = File.ReadAllLines(filePath);
                if (lines.Length < 2)
                {
                    result.Errors.Add("CSV file is empty or has no data rows");
                    return result;
                }

                // Parse header
                var headers = lines[0].Split(',').Select(h => h.Trim().ToLower()).ToArray();
                int matricCol = Array.IndexOf(headers, "matricnumber");
                if (matricCol == -1) matricCol = Array.IndexOf(headers, "matric");
                int nameCol = Array.IndexOf(headers, "fullname");
                if (nameCol == -1) nameCol = Array.IndexOf(headers, "name");
                int emailCol = Array.IndexOf(headers, "email");
                int windowsUserCol = Array.IndexOf(headers, "windowsusername");
                if (windowsUserCol == -1) windowsUserCol = Array.IndexOf(headers, "username");

                if (matricCol == -1 || nameCol == -1 || emailCol == -1)
                {
                    result.Errors.Add("Required columns missing. Need: MatricNumber, FullName, Email");
                    return result;
                }

                // Process data rows
                for (int i = 1; i < lines.Length; i++)
                {
                    try
                    {
                        var values = lines[i].Split(',').Select(v => v.Trim()).ToArray();
                        
                        var matric = values[matricCol];
                        var name = values[nameCol];
                        var email = values[emailCol];
                        var windowsUser = windowsUserCol >= 0 && windowsUserCol < values.Length ? values[windowsUserCol] : null;

                        if (string.IsNullOrWhiteSpace(matric) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
                        {
                            result.Errors.Add($"Row {i + 1}: Missing required fields");
                            result.ErrorCount++;
                            continue;
                        }

                        var existingStudents = _dbService.GetAllStudents();
                        if (existingStudents.Any(s => s.MatricNumber == matric))
                        {
                            result.Errors.Add($"Row {i + 1}: Student {matric} already exists");
                            result.ErrorCount++;
                            continue;
                        }

                        var student = new Student
                        {
                            MatricNumber = matric,
                            FullName = name,
                            Email = email,
                            WindowsUsername = windowsUser
                        };

                        int studentId = _dbService.CreateStudent(student);
                        student.Id = studentId;
                        _dbService.EnrollStudent(studentId, courseId);

                        result.ImportedStudents.Add(student);
                        result.SuccessCount++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Row {i + 1}: {ex.Message}");
                        result.ErrorCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Error reading CSV file: {ex.Message}");
            }

            return result;
        }
    }
}
