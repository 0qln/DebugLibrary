using System;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Linq;

namespace Debugger
{
    public class DebuggerConsole : IDebuggerConsole
    {
        private static DebuggerConsole ?instance;
        private static readonly object padlock = new object();

        private const string executionString = "```"; 
        private CommandManager cm;

        private DebuggerConsole()
        {
            cm = new CommandManager(executionString);
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

        }

        public void Log(string message) {
            if (String.IsNullOrWhiteSpace(message)) {
                return;
            }
            new CreateCommand.LogCommand(message).Execute();
        }
        public void DeleteLine() => new CreateCommand.DeleteLineCommand().Execute();
        public void Clear() => new CreateCommand.ClearCommand().Execute();

        public void Save() {

        }
        public void SaveNew() {
            
        }
        public void Save(string filename) {
            
        }
        public void SaveNew(string filename) {
            
        }

        public void Load() {

        }
        public void Load(string filename) {

        }

        internal string ?ProjectDirectory => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    }

    internal interface IDebuggerConsole
    {

        public void Log(string message);
        public void DeleteLine();
        public void Clear();

        public void Save();
        public void SaveNew();
        public void Save(string filename);
        public void SaveNew(string filename);
        public void Load();
        public void Load(string filename);
    }


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


    internal class PipeManager : IDisposable
    {
        private const string pipeName = "ContentPipe";
        private NamedPipeServerStream ?pipeServer;
        private readonly string executionString;
        private StreamWriter writer;
        private bool disposed;


        public PipeManager(string executionString) {
            NamedPipeServerStream pipeServer = new NamedPipeServerStream(pipeName);
            this.executionString = executionString;
            pipeServer.WaitForConnection();
            writer = new StreamWriter(pipeServer);
            SendMessage(executionString);
        }

        public void SendMessage(string message)
        {
            writer.WriteLine(message);
            writer.Flush();
        }
        public string getExecutionString() {
            return executionString;
        }

        public void Dispose() {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if(!disposed)
            {
                if(disposing)
                {
                    pipeServer.Dispose();
                    writer.Dispose();
                }
                disposed = true;
            }
        }

    }
}