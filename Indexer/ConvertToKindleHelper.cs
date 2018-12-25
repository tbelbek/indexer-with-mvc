using System.Diagnostics;
using System.IO;
using System.Web;

namespace Indexer.Helper
{
    public static class ConvertToKindleHelper
    {
        public static string ConvertToMobi(string filePath)
        {
            string fileNameOnly = Path.GetFileNameWithoutExtension(filePath);
            string directory = Path.GetDirectoryName(filePath);
            string outPath = $"{fileNameOnly}.mobi";
            if (File.Exists($"{directory}\\{outPath}"))
            {
                return $"{directory}\\{outPath}";
            }
            string fileName = Path.GetFileName(filePath);

            ProcessStartInfo procStartInfo = new ProcessStartInfo("cmd", $"/c {HttpContext.Current.Server.MapPath(@"~/App_Data/kindlegen.exe")} {filePath} -o {outPath}");

            procStartInfo.RedirectStandardOutput = true;
            procStartInfo.UseShellExecute = false;
            procStartInfo.CreateNoWindow = true;

            // wrap IDisposable into using (in order to release hProcess) 
            using (Process process = new Process())
            {
                process.StartInfo = procStartInfo;
                process.Start();

                // Add this: wait until process does its work
                process.WaitForExit();

                // and only then read the result
                string result = process.StandardOutput.ReadToEnd();
            }

            return $"{directory}\\{outPath}";
        }

    }
}