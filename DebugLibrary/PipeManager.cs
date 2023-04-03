using System.IO.Pipes;

namespace PipeManaging {
    internal class PipeManager
    {
        private StreamWriter writer;

        public PipeManager(string pExecutionString) {
            PipeObject.setExecutionString(pExecutionString);
            PipeObject.setPipeServer();

            writer = new StreamWriter(PipeObject.getPipeServer());
            writer.WriteLineAsync(PipeObject.getExecutionString());
            writer.FlushAsync();
        }

        public void SendMessage(string message) {
            writer.WriteLine(message);
            writer.Flush();
        }
    }

    
    internal static class PipeObject {

        public const string pipeName = "ContentPipe";

        private static StreamWriter writer;
        private static NamedPipeServerStream pipeServer;
        public static NamedPipeServerStream getPipeServer() {
            if (pipeServer == null) throw new ArgumentException("PipeServer has not been initiated yet.");
            return pipeServer;
        }
        public static void setPipeServer() {
            if (pipeServer != null) throw new ArgumentException("PipeServer can only be initiated once.");
            pipeServer = new NamedPipeServerStream(PipeObject.pipeName);
            pipeServer.WaitForConnection();
            writer = new StreamWriter(pipeServer);
        }

        private static string executionString;
        public static string getExecutionString() {
            if (executionString == null) throw new ArgumentException("ExecutionString has not been initiated yet.");
            return executionString;
        }
        public static void setExecutionString(string value) {
            if (executionString != null) throw new ArgumentException("ExecutionString can only be initiated once.");
            executionString = value;
        }

        public static void Dispose() {
            pipeServer.Disconnect();
            pipeServer.Close();
            pipeServer.Dispose();
            writer.Dispose();
        }

        public static void SendMessage(string message) {
            if (pipeServer == null) throw new ArgumentException("PipeServer has not been initiated yet.");
            writer.WriteLine(message);
            writer.Flush();
        }
    }
}