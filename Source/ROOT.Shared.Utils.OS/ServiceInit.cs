namespace ROOT.Shared.Utils.OS
{
    /// <summary>
    /// Supported service initialization in the current linux installation, i.e.
    /// SysV vs SystemD - <see cref="ServiceInit.Unknown"/> when it cannot be detected
    /// </summary>
    public enum ServiceInit
    {
        Unknown,
        SysVInit,
        SystemD
    }
}