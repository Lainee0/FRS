using FamilyRegistrySystem.Models;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamilyRegistrySystem
{
    internal class ExcelImportHelper
    {
        public List<FamilyMember> ImportFromExcel(string filePath)
        {
            var members = new List<FamilyMember>();

            FileInfo fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
                throw new FileNotFoundException("Excel file not found");

            if (fileInfo.Length == 0)
                throw new Exception("Excel file is empty");

            using (ExcelPackage package = new ExcelPackage(fileInfo))
            {
                // Check if any worksheets exist
                if (package.Workbook.Worksheets.Count == 0)
                    throw new Exception("No worksheets found in Excel file");

                // Get first worksheet - safer than index [0]
                ExcelWorksheet worksheet = package.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                    throw new Exception("Worksheet is null");

                // Verify data exists
                if (worksheet.Dimension == null)
                    throw new Exception("Worksheet has no data");

                int rowCount = worksheet.Dimension.Rows;
                int colCount = worksheet.Dimension.Columns;

                // Validate column count matches expectation
                if (colCount < 11) // Your data shows 11 columns (A-K)
                    throw new Exception($"Expected 11 columns, found {colCount}");

                for (int row = 2; row <= rowCount; row++) // Skip header row
                {
                    try
                    {
                        var member = new FamilyMember
                        {
                            // Map columns based on your screenshot (A-K)
                            RowIndicator = GetCellValue(worksheet, row, 2), // B
                            LastName = GetCellValue(worksheet, row, 3),     // C
                            FirstName = GetCellValue(worksheet, row, 4),    // D
                            MiddleName = GetCellValue(worksheet, row, 5),   // E
                            Relationship = GetCellValue(worksheet, row, 6), // F
                            Birthday = SafeParseDate(worksheet.Cells[row, 8].Text), // H
                            Age = ParseInt(GetCellValue(worksheet, row, 9)), // I
                            Sex = GetCellValue(worksheet, row, 10),         // J
                            CivilStatus = GetCellValue(worksheet, row, 11),  // K (previously L)
                            HouseholdNumber = GetCellValue(worksheet, row, 12) // K is HouseholdNumber
                        };

                        members.Add(member);
                    }
                    catch (Exception ex)
                    {
                        // Log and continue
                        Debug.WriteLine($"Error row {row}: {ex.Message}");
                    }
                }
            }

            return members;
        }

        // Helper methods
        private string GetCellValue(ExcelWorksheet ws, int row, int col)
        {
            return ws.Cells[row, col]?.Text?.Trim() ?? string.Empty;
        }

        private DateTime SafeParseDate(string dateString)
        {
            if (DateTime.TryParse(dateString, out DateTime result))
            {
                // Ensure date is within SQL Server's valid range
                if (result < new DateTime(1753, 1, 1))
                    return new DateTime(1753, 1, 1);
                if (result > new DateTime(9999, 12, 31))
                    return new DateTime(9999, 12, 31);
                return result;
            }
            return new DateTime(1753, 1, 1); // Default minimum date
        }

        private int ParseInt(string value)
        {
            return int.TryParse(value, out int result) ? result : 0;
        }

        public DateTime ParseExcelDate(object excelDate)
        {
            if (excelDate is DateTime)
                return (DateTime)excelDate;

            if (DateTime.TryParse(excelDate.ToString(), out DateTime result))
                return result;

            return DateTime.MinValue;
        }
    }
}
