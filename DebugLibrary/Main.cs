using System;
using System.IO;
using System.IO.Pipes;

namespace Debugger
{
    public class DebuggerConsole : IDebuggerConsole
    {
        private static DebuggerConsole instance;
        private static readonly object padlock = new object();

        private DebuggerConsole()
        {
            
        }
        public static void Instaciate() {
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

        public void Instaciate();
        private DebuggerConsole();
        public void Dispose();
    }


    internal interface ICommand
    {
        public void Execute();
    }
    internal static class CreateCommand
    {
        private static PipeManager pm;
        public static void Instaciate(PipeManager pm)
        {
            this.pm = pm;
        }

        public static CloseCommand() => new CloseCommand();
        public static DeleteLineCommand() => new DeleteLineCommand();
        public static CloseCommand() => new CloseCommand();
        public static LogCommand(string message) => new LogCommand(message);


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



    internal class PipeManager : IDisposable
    {
        private const string pipeName = "ContentPipe";
        private NamedPipeServerStream pipeServer;
        private readonly string executionString;
        private StreamWriter writer;


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
            if(!this.disposed)
            {
                if(disposing)
                {
                    pipeServer.Dispose();
                    writer.Dispose();
                    component.Dispose();
                }
                disposed = true;
            }
        }

    }
}