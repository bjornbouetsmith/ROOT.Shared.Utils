namespace ROOT.Shared.Utils.OS
{
    /// <summary>
    /// This is just a dummy process call class meant to be used for dependency injection
    /// where processes should be run on local machine but your classes depends on IProcessCall
    /// So some instances must be injected
    /// </summary>
    public class LocalMachineProcessCall : ProcessCall
    {
        public LocalMachineProcessCall() : base(string.Empty, string.Empty)
        {
        }

        public static IProcessCall operator |(LocalMachineProcessCall first, IProcessCall second) 
        {
            return second;
        }

        public static IProcessCall operator |(IProcessCall first, LocalMachineProcessCall second)
        {
            return first;
        }
    }
}