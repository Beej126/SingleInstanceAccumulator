////////////////////////////
// original code copied from:
// https://www.codeproject.com/tips/1017834/how-to-send-data-from-one-process-to-another-in-cs
////////////////////////////

using System;
using System.Threading;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace SingleInstanceArgAggregator
{
  static class Program
  {
    const string WindowTitle = "SingleInstanceThingy";
    const string DefaultCmdLine = "cmd /c echo $files && pause";
    const string DefaultDelimiter = ",";
    const string DefaultQuote = "\"";
    const int DefaultTimeout = 200;

    static private List<string> result = new List<string>();

    static private string GetArg(string[] args, char arg) {
      return args.Where(s => s.StartsWith("-" + arg)).FirstOrDefault();
    }
    static private bool GetBoolArg(string[] args, char arg) {
      return GetArg(args, arg) != null;
    }
    static private string GetStringArg(string[] args, char arg, string def) {
      return GetArg(args, arg)?.Replace("-" + arg + ":", "") ?? def;
    }
    static private int GetIntArg(string[] args, char arg, int def) {
      return int.TryParse(GetStringArg(args, arg, null), out int timeout) ? timeout : def;
    }

    [STAThread]
    static void Main(/*doesn't happen in Windows App: string[] args*/)
    {
      var inArgs = Environment.GetCommandLineArgs();
      //[0] is exe, true args start at [1]
      
      // Get the filename if it exists
      string newArg = null;
      if (inArgs.Length > 1) newArg = string.Join("", inArgs.Skip(1).Where(s => !s.StartsWith("-")));

      var cmdLine = GetStringArg(inArgs, 'c', DefaultCmdLine);
      bool hidden = GetBoolArg(inArgs, 'w');
      bool usage = GetBoolArg(inArgs, 'h') || string.IsNullOrWhiteSpace(newArg);
      bool hiddenPause = (hidden && cmdLine.ToLower().Contains("pause")); //warn on usage if we're hidding a dead cmd.exe

      // If a mutex with the name below already exists, 
      // one instance of the application is already running 
      Mutex singleMutex = new Mutex(true, WindowTitle, out bool isNewInstance);
      if (isNewInstance)
      {
        var Debug = GetBoolArg(inArgs, 'v');

        var inConsole = Win32.AttachConsole(-1 /*ATTACH_PARENT_PROCESS*/);
        if (!inConsole && (Debug || usage)) Win32.AllocConsole();

        if (usage || hiddenPause) {

          if (hiddenPause) {
            var currentTextColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("Aborting");
            Console.ForegroundColor = currentTextColor;
            Console.WriteLine(": hidden (-w) with PAUSE in command line");
          }

          Console.WriteLine($@"
Usage:
  ""-c:command line"" (default: {DefaultCmdLine})
    $files will be replace with aggregated list

  -f = output each item on separate line to new tempfile
    $files will be replaced by the tempfile path
    quote will default to nothing

  -d:delimiter (default: {DefaultDelimiter})
  -q:quote (default: {DefaultQuote})
  -t:timeout millisecs (default: {DefaultTimeout})
  -w = hidden launch
  -v = debug");
          if (!inConsole) { Console.WriteLine("press Enter to exit..."); Console.ReadLine(); }
          return;
        };

        var TempFile = GetBoolArg(inArgs, 'f');
        var Delimiter = GetStringArg(inArgs, 'd', DefaultDelimiter);
        var Quote = GetStringArg(inArgs, 'q', TempFile ? "" : DefaultQuote);
        Action<string> onNewValue = (string value)=>result.Add($"{Quote}{value}{Quote}");
        onNewValue(newArg);

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var timeout = GetIntArg(inArgs, 't', DefaultTimeout);

        Application.Run(new SingleInstanceMessageForm(WindowTitle, onNewValue, timeout));

        //we drop out here when the form closes upon timer expiration

        var cmd = cmdLine.Split(' ')[0];
        var args = cmdLine.Substring(cmd.Length + 1);

        string output = null;
        if (TempFile) {
          output = Path.GetTempFileName();
          File.AppendAllLines(output, result);
        }
        else output = string.Join(Delimiter, result);

        args = args.Replace("$files", output);

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
          FileName = cmd,
          Arguments = args,
          WindowStyle = hidden ? System.Diagnostics.ProcessWindowStyle.Hidden : System.Diagnostics.ProcessWindowStyle.Normal,
          CreateNoWindow = hidden
        });
        if (Debug) Console.WriteLine($"debug - timeout: {timeout}, cmd: {cmd}, args: {args}");
        if (Debug && !inConsole) { Console.WriteLine("press Enter to exit..."); Console.ReadLine(); }
      }
      else
      {
        SingleInstanceMessageForm.SendStringToWindow(WindowTitle, newArg);
      }
    }
  }
}
