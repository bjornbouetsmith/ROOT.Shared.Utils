namespace ROOT.Shared.Utils.OS
{
    public class ProcessCallResult
    {
        public bool Success => ExitCode == 0;
        public int ExitCode { get; set; }
        public string StdOut { get; set; }
        public string StdError { get; set; }
        public string CommandLine { get; set; }
    }
}