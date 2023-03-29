using System;
using System.IO;
using System.IO.Pipes;

namespace Debugger
{
    public class DebuggerConsole
    {
        //Singleton behaviour n shit
        

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
        public void Close();
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

    internal class PipeManager
    {
        private readonly string executionString;

        public PipeManager(string executionString) {
            this.executionString = executionString;
        }

        public void SendMessage(string message)
        {

        }

        public string getExecutionString() {
            return executionString;
        }
    }
}