using System.Diagnostics;
using System.IO;

namespace ROOT.Shared.Utils.OS
{
    public class ProcessCall
    {
        private readonly string _binPath;

        public ProcessCall(string binPath)
        {
            _binPath = binPath;
        }

        public ProcessCallResult LoadResponse(params string[] arguments)
        {
            return LoadResponse(null, arguments);
        }

        public ProcessCallResult LoadResponse(Stream inputStream, params string[] arguments)
        {
            string args = string.Empty;
            if (arguments != null && arguments.Length > 0)
            {
                args = string.Join(" ", arguments);
            }
            var startInfo = new ProcessStartInfo(_binPath, args)
            {
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                
            };

            
            using (var process = Process.Start(startInfo))
            {
                CopyStreamToProcessInput(inputStream, process);

                process.WaitForExit();
                var exitCode = process.ExitCode;
                return new ProcessCallResult { Success = exitCode == 0, StdOut = process.StandardOutput.ReadToEnd(), StdError = process.StandardError.ReadToEnd() };
            }
        }

        private static void CopyStreamToProcessInput(Stream inputStream, Process process)
        {
            if (inputStream != null)
            {
                byte[] buffer = new byte[1024];
                int bytesRead = inputStream.Read(buffer, 0, 1024);
                while (bytesRead > 0)
                {
                    process.StandardInput.BaseStream.Write(buffer, 0, bytesRead);
                    bytesRead = inputStream.Read(buffer, 0, 1024);
                }
            }
        }
    }

    public class ProcessCallResult
    {
        public bool Success { get; set; }
        public string StdOut { get; set; }
        public string StdError { get; set; }
    }
}