using System;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Linq;
using CommandLibrary;
using System.Diagnostics;

namespace Debugger
{
    public class DebuggerConsole : IDebuggerConsole
    {
        internal static List<string> content = new List<string>();

        internal const string processPath = @"D:\Programmmieren\__DebugLibrary\Application\New Folder #2\ConsoleWindow.exe";
        private static Process process;

        private static DebuggerConsole ?instance;
        private static readonly object padlock = new object();

        private const string executionString = "```";
        internal static CommandManager cm = new CommandManager();


        private DebuggerConsole()
        {
            process = new Process();
            process.StartInfo.FileName = processPath;
            process.Start();
            
            PipeManager.Instaciate(executionString);
        }
        public static DebuggerConsole Instaciate {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new DebuggerConsole();
                    }
                    return instance;
                }
            }
        }
        public void Dispose() {
            process.Kill();
            instance = null;
            PipeManager.Dispose();
        }

        public void Log(string message) {
            if (String.IsNullOrWhiteSpace(message)) {
                throw new ArgumentException("Message was white space.");
            }

            new LogCommand(message).Execute();
            content.Add(message);
        }
        public void DeleteLine()  {
            new DeleteLineCommand().Execute();
            content.RemoveAt(content.Count-1);
        }
        public void DeleteLine(int index) {
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
                DeleteLine(i);
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

            if (!Directory.Exists(filename)) Directory.CreateDirectory(Path.GetDirectoryName(filename));
            if (!File.Exists(filename)) File.Create(filename);

            File.WriteAllLines(filename, content);
        }
        public void SaveNew(string filename) {
            if (filename == null) throw new ArgumentException("Filename was null.");

            if (!Directory.Exists(filename)) Directory.CreateDirectory(Path.GetDirectoryName(filename));
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

        internal string ?ProjectDirectory => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        internal string GetUniqueFilename(string filename)
        {
            string directory = Path.GetDirectoryName(filename);
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
    }

    internal interface IDebuggerConsole
    {

        public void Log(string message);
        public void DeleteLine();
        public void ClearAll();

        public void Save();
        public void SaveNew();
        public void Save(string filename);
        public void SaveNew(string filename);
        public void Load();
        public void Load(string filename);
    }


    internal class KillCommand : ICommand {
        public void Execute() => PipeManager.SendMessage($"{PipeManager.getExecutionString()}Kill{PipeManager.getExecutionString()}");
    }
    internal class LogCommand : ICommand {
        private string message;
        public LogCommand(string message) => this.message = message;
        public void Execute() => PipeManager.SendMessage(message);
    }
    internal class DeleteLineCommand : ICommand {
        private int index;
        public DeleteLineCommand() => index = -1;
        public DeleteLineCommand(int index) => this.index = index;

        public void Execute() {
            if (index == -1) {
                PipeManager.SendMessage($"{PipeManager.getExecutionString()}DeleteLine{PipeManager.getExecutionString()}");
            }
            else {
                PipeManager.SendMessage($"{PipeManager.getExecutionString()}DeleteLineWithIndexOf{index}{PipeManager.getExecutionString()}");
            }
        }    
    }
    internal class ClearCommand : ICommand {
        public void Execute() => PipeManager.SendMessage($"{PipeManager.getExecutionString()}Clear{PipeManager.getExecutionString()}");
    }

    internal static class PipeManager
    {
        private const string pipeName = "ContentPipe";
        private static NamedPipeServerStream pipeServer = new NamedPipeServerStream(pipeName);
        private static StreamWriter writer = new StreamWriter(pipeServer);
        private static string executionString = "";
        private static bool instanciated = false;


        public static void Instaciate(string pExecutionString) {
            if (instanciated) throw new ArgumentException("Pipemanager has already been instanciated.");

            executionString = pExecutionString;
            pipeServer.WaitForConnection();
            SendMessage(executionString);
            instanciated = true;
        }

        public static void SendMessage(string message) {
            if (!instanciated) throw new ArgumentException("Pipemanager has not been instanciated yet.");

            writer.WriteLine(message);
            writer.Flush();
        }
        public static string getExecutionString() {
            if (!instanciated) throw new ArgumentException("Pipemanager has not been instanciated yet.");

            return executionString;
        }

        public static void Dispose() {
            if (!instanciated) throw new ArgumentException("Pipemanager has not been instanciated yet.");

            pipeServer.Dispose();
            writer.Dispose();
            instanciated = false;
        }
            
    }
} 

    



/* Old Code

    internal interface ICommand
    {
        public void Execute();
    }
    internal class CreateCommand
    {
        private static PipeManager pm;
        public static void Instaciate(PipeManager pPm)
        {
            pm = pPm;
        }

        public static ICommand Close() => new CloseCommand();
        public static ICommand DeleteLine() => new DeleteLineCommand();
        public static ICommand Clear() => new ClearCommand();
        public static ICommand Log(string message) => new LogCommand(message);


        internal class ClearCommand : ICommand {
            public void Execute() => pm.SendMessage($"{pm.getExecutionString()}Clear{pm.getExecutionString()}");
        }
        internal class DeleteLineCommand : ICommand {
            public void Execute() => pm.SendMessage($"{pm.getExecutionString()}DeleteLine{pm.getExecutionString()}");
        }
        internal class CloseCommand : ICommand
        {
            public void Execute() => pm.SendMessage($"{pm.getExecutionString()}Kill{pm.getExecutionString()}");
        }
        internal class LogCommand : ICommand
        {
            private string message;
            public LogCommand(string message) => this.message = message;
            public void Execute() => pm.SendMessage(message);
        }
    }


    internal class CommandManager {
        private PipeManager pm;
        private Queue<ICommand> commands;

        public CommandManager(string executionString) {
            pm = new PipeManager(executionString);
            commands = new Queue<ICommand>();
        }

        public void AddCommand(ICommand command) {
            commands.Append(command);
        }

        public void Execute(ICommand command) {
            command.Execute();
        }

        public void ExecuteAll() {
            foreach (var command in commands) {
                commands.Dequeue().Execute();
            }
        }
    }
*/


    
