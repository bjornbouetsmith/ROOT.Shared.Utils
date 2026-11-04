using System;

namespace ROOT.Shared.Utils.OS;

/// <summary>
/// Arguments passed to a process call.
/// </summary>
public readonly record struct ProcessCallArguments
{
    /// <summary>
    /// Creates a new set of process call arguments.
    /// </summary>
    /// <param name="arguments">The arguments to pass to the process.</param>
    public ProcessCallArguments(params string[] arguments)
    {
        Arguments = arguments ?? Array.Empty<string>();
    }

    /// <summary>
    /// The arguments to pass to the process. Never null.
    /// </summary>
    public string[] Arguments => field ?? Array.Empty<string>();
}
