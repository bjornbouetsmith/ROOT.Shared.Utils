using Microsoft.Extensions.DependencyInjection;

namespace ROOT.Shared.Utils.OS;

public static class SharedUtilsOsServiceCollectionExtentions
{
    /// <summary>
    /// Adds the given remote connection to the service collection.
    /// If null is passed, and local machine process call is injected that will make all process calls be run towards the local machine.
    /// </summary>
    public static IServiceCollection AddProcessCall(this IServiceCollection services, IProcessCall remoteConnection = null)
    {
        if (remoteConnection != null)
        {
            services.AddSingleton(remoteConnection);
        }
        else
        {
            services.AddSingleton<IProcessCall, LocalMachineProcessCall>();
        }

        return services;
    }

    /// <summary>
    /// Adds a SSH Process call to the service collection.
    /// This ensures that when you resolve an IProcessCall from the container that you can run commands towards the host
    /// </summary>
    public static IServiceCollection AddSSHConnection(this IServiceCollection services, string username, string hostName, bool requiresSudo = false)
    {
        var sshConnection = new SSHProcessCall(username, hostName, requiresSudo);
        return services.AddProcessCall(sshConnection);
    }
}