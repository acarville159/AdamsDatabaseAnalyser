using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AdamsDatabaseAnalyser
{
    class ExportBuilder
    {
        public static void CreateIssueListDoc(string fileName, List<Issue> issues)
        {
            using (SpreadsheetDocument document = SpreadsheetDocument.Create(fileName, SpreadsheetDocumentType.Workbook))
            {
                WorkbookPart workbookPart = document.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                worksheetPart.Worksheet = new Worksheet();

                Sheets sheets = workbookPart.Workbook.AppendChild(new Sheets());

                Sheet sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Issues" };

                sheets.Append(sheet);

                workbookPart.Workbook.Save();

                SheetData sheetData = worksheetPart.Worksheet.AppendChild(new SheetData());


                // Constructing header
                Row row = new Row();

                row.Append(
                    ConstructCell("Issue Id", CellValues.String),
                    ConstructCell("Location", CellValues.String),
                    ConstructCell("Cause", CellValues.String),
                    ConstructCell("Type", CellValues.String));

                // Insert the header row to the Sheet Data
                sheetData.AppendChild(row);

                // Inserting each issue
                int issueNumber = 1;
                foreach (Issue issue in issues)
                {
                    row = new Row();

                    row.Append(
                        ConstructCell(issueNumber.ToString(), CellValues.Number),
                        ConstructCell(issue.FileName.ToString(), CellValues.String),
                        ConstructCell(issue.Cause.ToString(), CellValues.String),
                        ConstructCell(Utils.GetIssueString(issue.Type), CellValues.String));

                    sheetData.AppendChild(row);
                    issueNumber++;
                }

                worksheetPart.Worksheet.Save();
            }
        }


        private static Cell ConstructCell(string value, CellValues dataType)
        {
            return new Cell()
            {
                CellValue = new CellValue(value),
                DataType = new EnumValue<CellValues>(dataType)
            };
        }
    }
}
