namespace ROOT.Shared.Utils.OS
{
    /// <summary>
    /// SSH process call.
    /// Requires that the calling user's public key has been installed on the remote host in the authorized_keys file so passwordless ssh is possible
    /// The class is not capable or responding to a password request
    /// Using sudo requires no password required for sudo command on the remote server
    /// i.e. [username]  ALL=(ALL)       NOPASSWD: ALL
    /// in /etc/sudoers or similar
    /// </summary>
    public class SSHProcessCall : ProcessCall
    {
        public bool RequiresSudo { get; }

        public SSHProcessCall(string username, string hostName, bool requiresSudo = false)
            : base(SSH.BinPath, $"{username}@{hostName}")
        {
            RequiresSudo = requiresSudo;
        }

        public static ProcessCall operator |(SSHProcessCall first, ProcessCall second)
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
    }
}