namespace ROOT.Shared.Utils.OS
{
    /// <summary>
    /// enumeration with detected linux types - undetected is <see cref="LinuxType.Other"/>
    /// </summary>
    public enum LinuxType
    {
        Other,
        CentOS,
        Debian,
        Fedora,
        Redhat,
        OpenSuse,
        Oracle,
        SLES,
        Ubuntu,
    }
}