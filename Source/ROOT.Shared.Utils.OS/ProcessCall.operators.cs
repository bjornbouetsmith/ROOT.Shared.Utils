namespace ROOT.Shared.Utils.OS
{
    public partial class ProcessCall
    {
        public static ProcessCall operator |(ProcessCall first, ProcessCall second)
        {
            if (first == null)
            {
                return second;
            }

            if (second == null)
            {
                return first;
            }

            return ProcessCallExtensions.Pipe((ProcessCall)first, (ProcessCall)second);
        }

        public static IProcessCall operator |(IProcessCall first, ProcessCall second)
        {
            if (first == null)
            {
                return second;
            }

            if (second == null)
            {
                return first;
            }

            return first.Pipe(second);
        }

        public static IProcessCall operator |(ProcessCall first, IProcessCall second)
        {
            if (first == null)
            {
                return second;
            }

            if (second == null)
            {
                return first;
            }

            return ProcessCallExtensions.Pipe(first, second);
        }
    }
}