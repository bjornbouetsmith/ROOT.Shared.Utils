using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ROOT.Shared.Utils.OS;

/// <summary>
/// Interface for process call, so other implementations are possible, and testing with mocks possible
/// </summary>
public interface IProcessCall
{
    /// <summary>
    /// Loads the response from the process call, using the passed arguments if any
    /// </summary>
    /// <param name="throwOnFailure">Whether or not throw an exception if statuscode indicates a failure or not</param>
    /// <param name="arguments">Any arguments to pass to the command</param>
    ProcessCallResult LoadResponse(bool throwOnFailure, params string[] arguments);
    /// <summary>
    /// Loads the response from the process call, using the passed input stream and arguments if any
    /// </summary>
    ProcessCallResult LoadResponse(bool throwOnFailure, Stream inputStream, params string[] arguments);

    /// <summary>
    /// Asynchronously loads the response from the process call, using the passed arguments if any
    /// </summary>
    /// <param name="throwOnFailure">Whether or not throw an exception if statuscode indicates a failure or not</param>
    /// <param name="arguments">Any arguments to pass to the command</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    Task<ProcessCallResult> LoadResponseAsync(bool throwOnFailure, ProcessCallArguments arguments = default, CancellationToken cancellationToken = default);
    /// <summary>
    /// Asynchronously loads the response from the process call, using the passed input stream and arguments if any
    /// </summary>
    /// <param name="throwOnFailure">Whether or not throw an exception if statuscode indicates a failure or not</param>
    /// <param name="inputStream">Optional input stream to pass to the process</param>
    /// <param name="arguments">Any arguments to pass to the command</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    Task<ProcessCallResult> LoadResponseAsync(bool throwOnFailure, Stream inputStream, ProcessCallArguments arguments = default, CancellationToken cancellationToken = default);

    string BinPath { get; }
    string Arguments { get; }
    string FullCommandLine { get; }
    string Shell { get; set; }
    bool UseShell { get; set; }
    TimeSpan Timeout { get; set; }
    bool Started { get; }
    bool RequiresSudo { get; set; }

    public IProcessCall Pipe(IProcessCall other);
}
