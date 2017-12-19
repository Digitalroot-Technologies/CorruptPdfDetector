using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace CorruptPdfDetector
{
  /// <summary>
  /// http://stackoverflow.com/questions/7377396/how-to-make-a-c-sharp-wrapper-around-a-console-application-any-language
  /// http://stackoverflow.com/questions/139593/processstartinfo-hanging-on-waitforexit-why?lq=1
  /// </summary>
  public class Program
  {
    [MTAThread]
    public static int Main(string[] args)
    {
      DirectoryInfo di = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, "docs"));
      var files = new Dictionary<string, string>();
      foreach (FileInfo file in di.EnumerateFiles())
      {
        Console.WriteLine(file);
        var result = CheckPdf(file);
        files.Add(file.ToString(), result == 0 ? "Good" : "Bad");
      }

      foreach (var file in files)
      {
        Console.WriteLine($"{file.Key} - {file.Value}");
      }

#if DEBUG
      Console.ReadKey();
#endif
      return 0;
    }

    /// <summary>
    /// https://stackoverflow.com/questions/3108201/detect-if-pdf-file-is-correct-header-pdf
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    private static int CheckPdf(FileInfo file)
    {
      int returnCode = 0;
      using (Process process = new Process())
      {
        process.StartInfo.FileName = "C:\\Program Files\\gs\\gs9.22\\bin\\gswin64c.exe"; // Default install location of Ghostscript.
        process.StartInfo.Arguments = $"-o nul -sDEVICE=nullpage -r36x36 {file.FullName}"; // This tells Ghostscript to not render the file. 
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        Console.WriteLine(process.StartInfo.Arguments);

        StringBuilder output = new StringBuilder();
        StringBuilder error = new StringBuilder();

        using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
        using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
        {
          process.OutputDataReceived += (sender, e) =>
          {
            if (e.Data == null)
            {
              outputWaitHandle.Set();
            }
            else
            {
              Console.WriteLine(e.Data);
              output.AppendLine(e.Data);
            }
          };
          process.ErrorDataReceived += (sender, e) =>
          {
            if (e.Data == null)
            {
              errorWaitHandle.Set();
            }
            else
            {
              Console.WriteLine(e.Data);
              error.AppendLine(e.Data);
            }
          };

          process.Start();

          process.BeginOutputReadLine();
          process.BeginErrorReadLine();

          if (process.WaitForExit(5000) &&
              outputWaitHandle.WaitOne(5000) &&
              errorWaitHandle.WaitOne(5000))
          {
            // Process completed. Check process.ExitCode here.
#if DEBUG
            Console.WriteLine("Ghostscript Exit Code: " + process.ExitCode);
#endif


            if (process.ExitCode == 1)
            {
              // Check error message.
#if DEBUG
              Console.WriteLine("Error Message: '" + error + "'");
#endif
              returnCode = 1;
            }
            else
            {
              // Ghostscript has Errors that it tries to recover from. We want to capture these errors too.
              if (output.ToString().Contains("Error"))
              {
                returnCode = 1;
              }
              else
              {
                returnCode = process.ExitCode;
              }
            }
          }
        }
      }
#if DEBUG
      Console.WriteLine("Exit Code: " + returnCode);
#endif
      return returnCode;
    }
  }
}