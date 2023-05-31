using System;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Linq;
using CommandLibrary;
using DebugLibrary.PipeManaging;
using System.Diagnostics;


namespace DebugLibrary
{
    public static class Console {
        private static ConsoleManager _manager = ConsoleManager.Instaciate;


        public static void Log(string message) => _manager.Log(message);
        public static void Log(object message) => Log(message.ToString());
        public static void ClearLine() => _manager.ClearLine();
        public static void ClearLine(int index) => _manager.ClearLine(index);
        public static void ClearAll() => _manager.ClearAll();
        public static void ClearRange(int bottom, int top) => _manager.ClearRange(bottom, top);

        public static void Save() => _manager.Save();
        public static void SaveNew() => _manager.SaveNew(); 
        public static void Save(string filename) => _manager.Save(filename);
        public static void SaveNew(string filename) => _manager.Save(filename); 
        public static void Load() => _manager.Load();
        public static void Load(string  filename) => _manager.Load(filename);

        public static string ApplicationPath => ApplicationFolderPath + "\\WpfApp.exe";
        public static string ApplicationFolderPath => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\ConsoleWindow";
        public static string? ProjectDirectory => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string ProcessStartargumentPath => ApplicationFolderPath + "\\startArguments.txt";
    }

    internal class ConsoleManager : IConsole
    {
        internal static List<string> content = new List<string>();

        private static Process process = new Process();

        private static ConsoleManager ?instance;
        private static readonly object padlock = new object();

        private const string executionString = "```";
        internal static CommandManager cm = new CommandManager();

        private PipeManager pipeManager;


        private ConsoleManager()
        {
            WriteProcessStartArguments("DebugLibrary");
            process.StartInfo.FileName = ApplicationPath;
            process.Start();

            pipeManager = new PipeManager(executionString);
        }
        public static ConsoleManager Instaciate {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new ConsoleManager();
                    }
                    return instance;
                }
            }
        }
        public void Dispose() {
            process.Kill();
            instance = null;
            PipeObject.Dispose();
        }

        public string ApplicationPath => ApplicationFolderPath + "\\WpfApp.exe";
        public string ApplicationFolderPath => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\ConsoleWindow";
        internal string? ProjectDirectory => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        internal string ProcessStartargumentPath => ApplicationFolderPath + "\\startArguments.txt";

        public void Log(string message) {
            if (String.IsNullOrWhiteSpace(message)) {
                return;
                throw new ArgumentException("Message was white space.");
            }
            if (message.Contains(executionString)) {
                throw new ArgumentException($"Message cannot contain {executionString}");
            }

            new LogCommand(message).Execute();
            content.Add(message);
        }
        public void ClearLine()  {
            new DeleteLineCommand().Execute();
            content.RemoveAt(content.Count-1);
        }
        public void ClearLine(int index) {
            if (index < 0 || index > content.Count-1) {
                throw new ArgumentException("Index was outside the bounds of the content.");
            }
            new DeleteLineCommand(index).Execute();
            content.RemoveAt(index);
        }
        public void ClearAll() { 
            new ClearCommand().Execute();
            content.Clear();
        }
        public void ClearRange(int bottom, int top) {
            if (bottom < 0 || top < 0) {
                throw new ArgumentException("Index was outside the bounds of the content.");
            }
            if (bottom > content.Count-1 || top > content.Count-1) {
                throw new ArgumentException("Index was outside the bounds of the content.");
            }
            if (bottom > top) {
                throw new ArgumentException("Bottom Index cannot be greater than Top Index.");
            }

            for (int i = bottom; i < top; i++) {
                ClearLine(i);
            }
        }

        public void Save() {
            if (ProjectDirectory != null)
            Save(ProjectDirectory);
        }
        public void SaveNew() {
            if (ProjectDirectory != null)
            SaveNew(ProjectDirectory);
        }
        public void Save(string filename) {
            if (filename == null) throw new ArgumentException("Filename was null.");

            if (!Directory.Exists(filename)) Directory.CreateDirectory(Path.GetDirectoryName(filename)!);
            if (!File.Exists(filename)) File.Create(filename);

            File.WriteAllLines(filename, content);
        }
        public void SaveNew(string filename) {
            if (filename == null) throw new ArgumentException("Filename was null.");

            if (!Directory.Exists(filename)) Directory.CreateDirectory(Path.GetDirectoryName(filename)!);
            if (!File.Exists(filename)) File.Create(filename);

            while (File.Exists(filename)) {
                filename = GetUniqueFilename(filename);
            }

            File.WriteAllLines(filename, content);
        }

        public void Load() {
            if (ProjectDirectory != null)
            Load(ProjectDirectory);
        }
        public void Load(string filename) {
            if (filename == null) throw new ArgumentException("Filename was null.");
            if (!File.Exists(filename)) throw new ArgumentException("File does not exist.");

            content = File.ReadAllLines(filename).ToList();
        }

        internal string GetUniqueFilename(string filename)
        {
            string directory = Path.GetDirectoryName(filename)!;
            string extension = Path.GetExtension(filename);
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
            string newFilename = filename;

            int i = 0;
            while (File.Exists(newFilename))
            {
                i++;
                newFilename = Path.Combine(directory, $"{nameWithoutExtension} ({i}){extension}");
            }

            return newFilename;
        }

        private void WriteProcessStartArguments(string argument) {
            if (!File.Exists(ProcessStartargumentPath)) {
                using (FileStream stream = File.Create(ProcessStartargumentPath)) {
                    stream.Close();
                }
            }
            File.WriteAllText(ProcessStartargumentPath, argument);
        }
    }

    internal interface IConsole
    {

        public void Log(string message);
        public void ClearLine();
        public void ClearAll();

        public void Save();
        public void SaveNew();
        public void Save(string filename);
        public void SaveNew(string filename);
        public void Load();
        public void Load(string filename);
    }


    internal class KillCommand : ICommand {
        public void Execute() => PipeObject.SendMessage($"{PipeObject.GetExecutionString()}Kill{PipeObject.GetExecutionString()}");
    }
    internal class LogCommand : ICommand {
        private string message;
        public LogCommand(string message) => this.message = message;
        public void Execute() => PipeObject.SendMessage(message);
    }
    internal class DeleteLineCommand : ICommand {
        private int index;
        public DeleteLineCommand() => index = -1;
        public DeleteLineCommand(int index) => this.index = index;

        public void Execute() {
            if (index == -1) {
                PipeObject.SendMessage($"{PipeObject.GetExecutionString()}DeleteLine{PipeObject.GetExecutionString()}");
            }
            else {
                PipeObject.SendMessage($"{PipeObject.GetExecutionString()}DeleteLineWithIndexOf{PipeObject.GetExecutionString()}{index}");
            }
        }    
    }
    internal class ClearCommand : ICommand {
        public void Execute() => PipeObject.SendMessage($"{PipeObject.GetExecutionString()}Clear{PipeObject.GetExecutionString()}");
    }
}
    
