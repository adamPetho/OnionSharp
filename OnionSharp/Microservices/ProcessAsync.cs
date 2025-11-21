using System.Diagnostics;

namespace OnionSharp.Microservices;

/// <summary>
/// Async wrapper class for <see cref="System.Diagnostics._process"/> class that implements <see cref="WaitForExitAsync(CancellationToken)"/>
/// to asynchronously wait for a process to exit.
/// </summary>
public class ProcessAsync : IDisposable
{
    /// <summary>
    /// To detect redundant calls.
    /// </summary>
    private bool _disposed = false;

    public ProcessAsync(ProcessStartInfo startInfo) : this(new Process() { StartInfo = startInfo })
    {
    }

    internal ProcessAsync(Process process)
    {
        _process = process;
    }

    private readonly Process _process;

    /// <inheritdoc cref="_process.StartInfo"/>
    public ProcessStartInfo StartInfo => _process.StartInfo;

    /// <inheritdoc cref="_process.ExitCode"/>
    public int ExitCode => _process.ExitCode;

    /// <inheritdoc cref="_process.HasExited"/>
    public virtual bool HasExited => _process.HasExited;

    /// <inheritdoc cref="_process.Id"/>
    public int Id => _process.Id;

    /// <inheritdoc cref="_process.Handle"/>
    public virtual nint Handle => _process.Handle;

    /// <inheritdoc cref="_process.StandardInput"/>
    public StreamWriter StandardInput => _process.StandardInput;

    /// <inheritdoc cref="_process.StandardOutput"/>
    public StreamReader StandardOutput => _process.StandardOutput;

    /// <inheritdoc cref="_process.Start()"/>
    public void Start()
    {
        try
        {
            _process.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);

            throw;
        }
    }

    /// <inheritdoc cref="_process.Kill(bool)"/>
    public virtual void Kill(bool entireProcessTree = false)
    {
        _process.Kill(entireProcessTree);
    }

    /// <summary>
    /// Waits until the process either finishes on its own or when user cancels the action.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Task"/>.</returns>
    public virtual async Task WaitForExitAsync(CancellationToken cancellationToken)
    {
        if (_process.HasExited)
        {
            return;
        }

        try
        {
            await _process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            throw new TaskCanceledException("Waiting for process exiting was canceled.", ex, cancellationToken);
        }
    }

    // Protected implementation of Dispose pattern.
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // Dispose managed state (managed objects).
            _process.Dispose();
        }

        _disposed = true;
    }

    public virtual void Dispose()
    {
        // Dispose of unmanaged resources.
        Dispose(true);

        // Suppress finalization.
        GC.SuppressFinalize(this);
    }
}
