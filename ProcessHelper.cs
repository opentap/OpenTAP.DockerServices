using System;
using System.Diagnostics;
using System.Threading;
using OpenTap;

namespace OpenTAP.Docker;

public class ProcessHelper
{
    public static void StartNew(string file, string args, Action<string> outputHandler, Action<string> errorHandler, TimeSpan timeout = default)
    {
        if (timeout == default)
            timeout = TimeSpan.FromSeconds(30);
        
        var startInfo = new ProcessStartInfo(file, args)
        {
            // WorkingDirectory = cwd,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true
        };
        var process = new Process();
        process.StartInfo = startInfo;
        
        // foreach (var environmentVariable in env)
        //     process.StartInfo.Environment.Add(environmentVariable.Name, environmentVariable.Value);
        
        var abortRegistration = TapThread.Current.AbortToken.Register(() =>
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
            catch(Exception ex)
            {
                // Log.Warning("Caught exception when killing process. {0}", ex.Message);
            }
        });

        using var OutputWaitHandle = new ManualResetEvent(false);
        using var ErrorWaitHandle = new ManualResetEvent(false);
        using (process)
        using (abortRegistration)
        {
            process.OutputDataReceived += OutputDataRecv;
            process.ErrorDataReceived += ErrorDataRecv;

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (process.WaitForExit((int)timeout.TotalMilliseconds) &&
                OutputWaitHandle.WaitOne((int)timeout.TotalMilliseconds) &&
                ErrorWaitHandle.WaitOne((int)timeout.TotalMilliseconds))
            {
                // var resultData = output.ToString();
                // ProcessOutput(resultData);
            }
            else
            {
                process.OutputDataReceived -= OutputDataRecv;
                process.ErrorDataReceived -= ErrorDataRecv;
                process.Kill();
            }
        
        
            void OutputDataRecv(object sender, DataReceivedEventArgs e)
            {
                try
                {
                    if (e.Data == null)
                    {
                        OutputWaitHandle.Set();
                    }
                    else
                    {
                        outputHandler(e.Data);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Suppress - Test plan has been aborted and process is disconnected
                }
            }
            void ErrorDataRecv(object sender, DataReceivedEventArgs e)
            {
                try
                {
                    if (e.Data == null)
                    {
                        ErrorWaitHandle.Set();
                    }
                    else
                    {
                        errorHandler(e.Data);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Suppress - Test plan has been aborted and process is disconnected
                }
            }
        }
    }
}