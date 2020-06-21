using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace PlayWithJSON
{
    class Program
    {
        private const string output = "c:\\FIFA\\junk\\reportUsageStatistics.csv";
        private const string folderPath = @"C:\Users\DavidFedor\Downloads\FIFA\UserStories\Create_CSV_About_Report_Usage";
        private const string reportAPI = "http://localhost:2222/PlatformApi/Reports/gridData";

        public static bool ExistHeader { get; set; }
        public static StreamWriter textWriter { get; set; }

        static void Main(string[] args)
        {
            textWriter = new StreamWriter(output);
            Console.WriteLine("Export started. Please wait...");

            foreach (string file in Directory.EnumerateFiles(folderPath, "*.json"))
            {
                var json = File.ReadAllText(file);
                var data = GetReportObjectList(json);

                if (!ExistHeader)
                {
                    AddHeaderToCSV();
                }

                ExportDataToFile(data).Wait();
                Console.WriteLine("Processing file - " + file);
            }
            Console.WriteLine("Export finished...");
        }

        public static void AddHeaderToCSV()
        {
            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(typeof(ReportObject));
            foreach (PropertyDescriptor prop in props)
            {
                textWriter.Write(prop.DisplayName);
                textWriter.Write(";");
            }
            textWriter.WriteLine();
            ExistHeader = true;
        }

        public static string GetReportName(List<IfesReport> reportSettings, string report)
        {
            var reportNumber = new String(report.Where(Char.IsDigit).ToArray());
            var reportName = reportSettings.Where(r => r.Id == reportNumber).Single();

            return reportName.ReportName;
        }

        public static List<ReportObject> GetReportObjectList(string json)
        {
            List<ReportObject> reportObjecList = new List<ReportObject>();
            List<IfesReport> ifesReportList = GetIfesReportList();

            JArray jsonResultArray = JArray.Parse(json) as JArray;

            dynamic reports = jsonResultArray;

            foreach (dynamic report in reports)
            {
                ReportObject reportObject = new ReportObject();

                reportObject.User = report._source.user;
                reportObject.Environment = report._source.Environment;
                reportObject.Report = GetReportName(ifesReportList, (String)report._source.message);
                reportObject.RequestStartTime = report._source["Request.StartTime"];
                reportObject.IndividualId = report._source["User.IndividualId"];
                reportObject.Tags = GetReportTags(ifesReportList, (String)report._source.message);

                reportObjecList.Add(reportObject);
            }

            return reportObjecList;
        }

        public static string GetReportTags(List<IfesReport> reportSettings, string report)
        {
            var reportNumber = new String(report.Where(Char.IsDigit).ToArray());
            var reportTags = reportSettings.Where(r => r.Id == reportNumber).Single();

            return reportTags.ReportTags;
        }

        public static List<IfesReport> GetIfesReportList()
        {
            var result = GetIfesReportsAsync().Result;
            dynamic dynamicResult = JArray.Parse(result);

            List<IfesReport> reportsSettingsObjectList = new List<IfesReport>();

            foreach (var reportSetting in dynamicResult)
            {
                string tag = reportSetting["Tags"];
                string newTag = tag.Replace(",","#");

                reportsSettingsObjectList.Add(new IfesReport
                {
                    Id = reportSetting["Id"],
                    ReportName = reportSetting["Name"],
                    ReportTags = newTag
                }) ;
            }

            return reportsSettingsObjectList;
        }

        public static async Task<string> GetIfesReportsAsync()
        {
            using (var client = new HttpClient())
            {
                return await client.GetStringAsync(reportAPI);
            }
        }

        public static async Task ExportDataToFile<T>(IEnumerable<T> data)
        {
            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(typeof(T));

            foreach (T item in data)
            {
                foreach (PropertyDescriptor prop in props)
                {
                    textWriter.Write(prop.Converter.ConvertToString(prop.GetValue(item)));
                    textWriter.Write(";");
                }
                textWriter.WriteLine();
            }
        }
    }
}
