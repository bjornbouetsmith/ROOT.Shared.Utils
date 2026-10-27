using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ROOT.Shared.Utils.OS;

namespace ROOT.Shared.Utils.Tests;

[TestClass]
public class ServiceCollectionExtensionTest
{
    [TestMethod]
    public void ShouldBeAbleToResolveIProcessCall()
    {
        var collection = new ServiceCollection();

        collection.AddProcessCall();

        var provider = collection.BuildServiceProvider();

        var processCall = provider.GetService<IProcessCall>();
        Assert.IsNotNull(processCall);
        Assert.IsInstanceOfType<LocalMachineProcessCall>(processCall);
    }

    [TestMethod]
    public void InjectedProcessCallTest()
    {
        var collection = new ServiceCollection();

        collection.AddProcessCall(new LocalMachineProcessCall());

        var provider = collection.BuildServiceProvider();

        var processCall = provider.GetService<IProcessCall>();
        Assert.IsNotNull(processCall);
        Assert.IsInstanceOfType<LocalMachineProcessCall>(processCall);
    }

    [TestMethod]
    public void InjectedSSHProcessCall()
    {
        var collection = new ServiceCollection();

        collection.AddProcessCall(new SSHProcessCall("bbs", "localhost", true));

        var provider = collection.BuildServiceProvider();

        var processCall = provider.GetService<IProcessCall>();
        Assert.IsNotNull(processCall);
        Assert.IsInstanceOfType<SSHProcessCall>(processCall);
    }
}