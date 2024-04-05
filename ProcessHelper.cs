using System;
using System.Diagnostics;
using System.Threading;

namespace OpenTAP.Docker;

public class ProcessHelper
{
    public static int StartNew(string file, string args, CancellationToken cancellationToken, Action<string> outputHandler, Action<string> errorHandler, int? timeout = default)
    {
        if (timeout == default)
            timeout = 30 * 1000;

        var startInfo = new ProcessStartInfo(file, args)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true
        };
        var process = new Process();
        process.StartInfo = startInfo;

        var cancellationRegistration = cancellationToken.Register(() =>
        {
            try
            {
                try
                {
                    process.StandardInput.Close();
                }
                catch
                {
                    // ignored
                }

                if (!process.WaitForExit(500)) // give some time for the process to close by itself.
                    process.Kill();
            }
            catch
            {
                // ignored
            }
        });

        using var OutputWaitHandle = new ManualResetEvent(false);
        using var ErrorWaitHandle = new ManualResetEvent(false);
        using (process)
        using (cancellationRegistration)
        {
            process.OutputDataReceived += OutputDataRecv;
            process.ErrorDataReceived += ErrorDataRecv;

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (process.WaitForExit((int)timeout) &&
                OutputWaitHandle.WaitOne((int)timeout) &&
                ErrorWaitHandle.WaitOne((int)timeout))
            {
                return process.ExitCode;
            }
            else
            {
                process.OutputDataReceived -= OutputDataRecv;
                process.ErrorDataReceived -= ErrorDataRecv;
                process.Kill();
                return process.ExitCode == 0 ? -1 : process.ExitCode;
            }
        
            void OutputDataRecv(object sender, DataReceivedEventArgs e)
            {
                if (e.Data == null)
                    OutputWaitHandle.Set();
                else
                    outputHandler(e.Data);
            }
            void ErrorDataRecv(object sender, DataReceivedEventArgs e)
            {
                if (e.Data == null)
                    ErrorWaitHandle.Set();
                else
                    errorHandler(e.Data);
            }
        }
    }
}