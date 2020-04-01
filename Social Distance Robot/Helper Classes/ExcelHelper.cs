using Syncfusion.XlsIO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;

namespace robot_head
{
    class ExcelHelper
    {
        public static List<string> _questionBank = new List<string>();

        public static void LoadQuestionBank()
        {
            string path = @"C:\Users\LattePanda\Desktop\ROSY_DATABASE\rosy.xlsx";

            Excel.Application xlApp = new Excel.Application();
            Excel.Workbook xlWorkbook = xlApp.Workbooks.Open(path);
            Excel._Worksheet xlWorksheet = xlWorkbook.Sheets[1];
            Excel.Range xlRange = xlWorksheet.UsedRange;

            int rowCount = xlRange.Rows.Count;
            int colCount = xlRange.Columns.Count;


            _questionBank.Clear();
            for (int i = 1; i <= rowCount; i++)
            {
                _questionBank.Add(xlRange.Cells[i, 1].Value2);
            }


            GC.Collect();
            GC.WaitForPendingFinalizers();

            //rule of thumb for releasing com objects:
            //  never use two dots, all COM objects must be referenced and released individually
            //  ex: [somthing].[something].[something] is bad

            //release com objects to fully kill excel process from running in the background
            Marshal.ReleaseComObject(xlRange);
            Marshal.ReleaseComObject(xlWorksheet);

            //close and release
            xlWorkbook.Close();
            Marshal.ReleaseComObject(xlWorkbook);

            //quit and release
            xlApp.Quit();
            Marshal.ReleaseComObject(xlApp);
        }

        public static int GetNumberOfSimilarWords(string s1, string s2)
        {
            var list1 = s1.ToLower().Split(' ');
            var list2 = s2.ToLower().Split(' ');

            Dictionary<string, bool> wordsCounter = new Dictionary<string, bool>();
            foreach (var word in list1)
            {
                if (wordsCounter.ContainsKey(word) == false)
                {
                    wordsCounter[word] = true;
                }

            }

            int res = 0;
            foreach (var word in list2)
            {
                if (wordsCounter.ContainsKey(word))
                {
                    res++;
                }
            }

            return res;

        }

        public static List<string> FindSimilarQuestions(string question)
        {

            List<string> list = new List<string>();

            foreach (var q in _questionBank)
            {
                if (GetNumberOfSimilarWords(q, question) >= 1)
                {
                    list.Add(q);
                }
            }

            list = list.OrderByDescending(x => GetNumberOfSimilarWords(question, x)).ToList();

            if (list.Count == 0)
            {
                int minI = 47;

                for (int i = 1; i <= 5; i++)
                {
                    var x = new Random().Next(10) + minI;

                    minI = x + 1;

                    list.Add(_questionBank[x]);
                }
            }


            return list.GetRange(0, Math.Min(list.Count, 5));

        }
        public static DataTable table = new DataTable();

        public static void CreateTable()
        {
            table.Columns.Add("Time", typeof(string));
            table.Columns.Add("Student Question", typeof(string));
            table.Columns.Add("Robot Answer", typeof(string));
        }

        public static void AddData(string studentQue, string robotAns)
        {
            Debug.WriteLine("Saving record " + studentQue + ": " + robotAns);
            table.Rows.Add(DateTime.Now.ToShortTimeString(), studentQue, robotAns);
        }

        public static void ExportToFile()
        {
            using (ExcelEngine excelEngine = new ExcelEngine())
            {
                //Initialize Application
                IApplication application = excelEngine.Excel;

                //Set the default application version as Excel 2016
                application.DefaultVersion = ExcelVersion.Excel2016;

                //Create a new workbook
                IWorkbook workbook = application.Workbooks.Create(1);

                //Access first worksheet from the workbook instance
                IWorksheet worksheet = workbook.Worksheets[0];

                //Exporting DataTable to worksheet                
                worksheet.ImportDataTable(table, true, 1, 1);
                worksheet.UsedRange.AutofitColumns();

                //Save the workbook to disk in xlsx format
                string path = @"C:\Users\LattePanda\Desktop\Alpha Records\";
                                
                string savedDate = DateTime.Now.Year + "_" + DateTime.Now.Month
                     + "_" + DateTime.Now.Day + "_" +
                     DateTime.Now.Hour + "_" + DateTime.Now.Minute
                     + "_" + DateTime.Now.Second;

                path += "Alpha_" + savedDate + ".xlsx";
                workbook.SaveAs(path);

                Excel.Application xlApp = new Microsoft.Office.Interop.Excel.Application();

                xlApp.DisplayAlerts = false;

                Excel.Workbook xlWorkBook = xlApp.Workbooks.Open(path, 0, false, 5, "", "", false, Microsoft.Office.Interop.Excel.XlPlatform.xlWindows, "", true, false, 0, true, false, false);
                Excel.Sheets worksheets = xlWorkBook.Worksheets;
                worksheets[2].Delete();
                xlWorkBook.Save();
                xlWorkBook.Close();

                releaseObject(worksheets);
                releaseObject(xlWorkBook);
                releaseObject(xlApp);


                table.Clear();
            }


        }
        private static void releaseObject(object obj)
        {
            try
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(obj);
                obj = null;
            }
            catch (Exception ex)
            {
                obj = null;
            }
            finally
            {
                GC.Collect();
            }
        }
    }
}
