using System;
using System.Diagnostics;

namespace IncrementBuildNumber
{
    internal class Git
    {
        public static Status GetStatus(string workingDirectory)
        {
            try
            {
                Process process = Process.Start(new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "status --untracked-files=no --porcelain",
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                if (process == null)
                {
                    return Status.Unknown;
                }

                process.WaitForExit();
                return string.IsNullOrWhiteSpace(process.StandardOutput.ReadToEnd()) ? Status.Clean : Status.Dirty;
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Exception: {exception.Message}");
                return Status.Unknown;
            }
        }

        public enum Status
        {
            Unknown,
            Clean,
            Dirty
        }
    }
}
