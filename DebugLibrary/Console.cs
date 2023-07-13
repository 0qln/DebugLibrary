using System.Reflection;
using CommandLibrary;
using DebugLibrary.PipeManaging;
using System.Diagnostics;


namespace DebugLibrary
{
    public class ConsoleFeatureNotSupportedException : Exception {
        public ConsoleFeatureNotSupportedException() 
            : base("System.Console does not support such a Feature. \n" + 
                    "Try using the ConsoleWindow application: https://github.com/0qln/ConsoleApplication.") { }
        public ConsoleFeatureNotSupportedException(string message) : base(message) { }
        public ConsoleFeatureNotSupportedException(string message, Exception inner) : base(message, inner) { }
    }

    public static class Console {
        public static bool UseSystemConsole {get; set;} = false;

        private static ConsoleManager _manager = ConsoleManager.Instaciate;

        internal static void TryUnless(Action tryAction, Action fallbackAction, bool condition) {
            if (condition) {
                try {
                    tryAction.Invoke();
                }
                catch (Exception e) {
                    fallbackAction.Invoke();
                    System.Console.WriteLine(e.ToString());
                }
            }
            else {
                fallbackAction.Invoke();
            }
        }
        public static void Log(string message) {
            TryUnless(() => _manager.Log(message), 
                    () => System.Console.WriteLine(message), 
                    !UseSystemConsole);
        }
        public static void Log(object? message) {
            string messageStr = String.Empty;
            if (message is not null) {
                messageStr = message.ToString()!;
            }
            TryUnless(() => _manager.Log(messageStr), 
                    () => System.Console.WriteLine(messageStr), 
                    !UseSystemConsole); 
        }
        public static void ClearLine() { 
            TryUnless(() => _manager.ClearLine(),
                    () => throw new NotSupportedException(),
                    !UseSystemConsole); 
        }
        public static void ClearLine(int index) {
            TryUnless(()=>_manager.ClearLine(index), () => {
                    System.Console.SetCursorPosition(0, index);
                    System.Console.Write(new string(' ', System.Console.WindowWidth));},
                    !UseSystemConsole); 
        }
        public static void ClearAll() {
            TryUnless(() => _manager.ClearAll(),
                    () => System.Console.Clear(),
                    !UseSystemConsole);
        }
        public static void ClearRange(int bottom, int top) {
            TryUnless(() => _manager.ClearRange(bottom, top),
                    () => {
                        for (int i = 0; i < Math.Abs(bottom-top); i++) {
                            System.Console.SetCursorPosition(0, bottom + 1);
                            System.Console.Write(new string(' ', System.Console.WindowWidth));
                        }
                    },
                    !UseSystemConsole);
        }

        public static void Save() {
            TryUnless(_manager.Save,
                    () => throw new ConsoleFeatureNotSupportedException(),
                    !UseSystemConsole);
        }
        public static void SaveNew() {
            TryUnless(_manager.SaveNew,
                    () => throw new ConsoleFeatureNotSupportedException(),
                    !UseSystemConsole);
        }
        public static void Save(string filename) {
            TryUnless(() => _manager.Save(filename),
                    () => throw new ConsoleFeatureNotSupportedException(),
                    !UseSystemConsole);
        }
        public static void SaveNew(string filename) {
            TryUnless(() => _manager.SaveNew(filename),
                    () => throw new ConsoleFeatureNotSupportedException(),
                    !UseSystemConsole);
        }
        public static void Load() {
            TryUnless(_manager.Load,
                    () => throw new ConsoleFeatureNotSupportedException(),
                    !UseSystemConsole);
        }
        public static void Load(string filename) {
            TryUnless(() => _manager.Load(filename),
                    () => throw new ConsoleFeatureNotSupportedException(),
                    !UseSystemConsole);
        }

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
    
