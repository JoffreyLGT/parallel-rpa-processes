﻿using Microsoft.Office.Interop.Excel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ParallelBotsExecution.FormFilling
{
    class ExcelManager
    {
        public int FirstLineToProcess { get; }
        public int LastLineToProcess { get; }
        
        private readonly Application app;
        private readonly Workbook wb;
        private readonly Worksheet ws;

        private static readonly object managerLock = new object();
        private int lastReturnedLine;
        private bool emptyLineReached = false;

        /// <summary>
        /// Default constructor.
        /// If Excel is already opened, connects to the opened worksheet.
        /// Otherwise, open the file at the path in parameter.
        /// </summary>
        /// <param name="file"></param>
        internal ExcelManager(string file)
        {
            // Use the existing instance if there is one
            try
            {
                app = (Application)Marshal.GetActiveObject("Excel.Application");
                wb = app.ActiveWorkbook;
            }
            catch (COMException)
            {
                app = new Application
                {
                    Visible = true
                };
                wb = app.Workbooks.Open(file);
            }

            ws = wb.ActiveSheet;

            FirstLineToProcess = app.Selection.Row;
            LastLineToProcess = GetLastLine();
            lastReturnedLine = 1; // We have a header in the file so we start at 1, not 0.

        }

        /// <summary>
        /// Get the row number of the next empty cell in column A.
        /// </summary>
        /// <returns></returns>
        internal int GetLastLine()
        {
            return ws.Range[ws.Cells[FirstLineToProcess, 1], ws.Cells[ws.Rows.Count, 1]].End[XlDirection.xlDown].Row;
        }

        /// <summary>
        /// Read the next line in the Excel file.
        /// </summary>
        /// <returns></returns>
        internal ExcelLine ReadNextLine()
        {
            if (emptyLineReached)
            {
                return null;
            }
            ExcelLine line = null;
            lock (managerLock)
            {
                lastReturnedLine++;
                line = new ExcelLine
                {
                    LineNumber = lastReturnedLine,
                    Content = ReadLine(lastReturnedLine)
                };
                if (line.Content == null)
                {
                    emptyLineReached = true;
                }
            }

            return emptyLineReached
                    ? null : line.Content != null
                        ? line : null;
        }

        /// <summary>
        /// Read the content of the Excel line.
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        private ExcelLineContent ReadLine(int number)
        {
            ExcelLineContent line = new ExcelLineContent
            {
                Number = GetCellValue(number, (int)ExcelLineContent.Columns.number),
                FirstName = GetCellValue(number, (int)ExcelLineContent.Columns.firstName),
                LastName = GetCellValue(number, (int)ExcelLineContent.Columns.lastName),
                UserName = GetCellValue(number, (int)ExcelLineContent.Columns.userName),
                Address = GetCellValue(number, (int)ExcelLineContent.Columns.address),
                Country = GetCellValue(number, (int)ExcelLineContent.Columns.country),
                State = GetCellValue(number, (int)ExcelLineContent.Columns.state),
                Zip = GetCellValue(number, (int)ExcelLineContent.Columns.zip),
                NameOnCard = GetCellValue(number, (int)ExcelLineContent.Columns.nameOnCard),
                CreditCardNumber = GetCellValue(number, (int)ExcelLineContent.Columns.creditCardNumber),
                Expirationdate = GetCellValue(number, (int)ExcelLineContent.Columns.expirationdate),
                Cvv = GetCellValue(number, (int)ExcelLineContent.Columns.cvv),
                BotStatus = GetCellValue(number, (int)ExcelLineContent.Columns.botStatus)
            };
            if (string.IsNullOrEmpty(line.Number)) return null;

            return line;
        }

        /// <summary>
        /// Write the bot status in its column in Excel.
        /// </summary>
        /// <param name="line"></param>
        internal void WriteBotStatus(ExcelLine line)
        {
            lock (managerLock)
            {
                ws.Cells[line.LineNumber, (int)ExcelLineContent.Columns.botStatus] = line.Content.BotStatus;
            }
        }

        private string GetCellValue(int row, int col)
        {
            string content = string.Empty;
            try
            {
                content = ((Range)ws.Cells[row, col]).Value.ToString();
            }
            catch (Exception)
            {
                // Do nothing. For example, an exception is thrown when the cell is empty.
            }
            return content;
        }
    }
}
