using System;
using System.IO;
using System.Linq;

namespace IncrementBuildNumber
{
    internal class Program
    {
        internal static int Main(string[] args)
        {
            Option option = Option.ProcessCommandLineParameters(args);

            if (option.ShowHelp)
            {
                ShowHelp();

                ConsoleHelper.PauseIfRequired();
                return 0;
            }

            if (!Directory.EnumerateFiles(option.WorkingDirectory, "*.sln", SearchOption.TopDirectoryOnly).Any())
            {
                Console.WriteLine($"Error: directory \"{option.WorkingDirectory}\" does not contain a solution file.");

                ConsoleHelper.PauseIfRequired();
                return 99;
            }

            if (Git.GetStatus(option.WorkingDirectory) == Git.Status.Dirty)
            {
                Console.WriteLine("Git status: Commit your changes before incrementing the build number.");

                ConsoleHelper.PauseIfRequired();
                return 99;
            }

            var processor = new FileProcessor(option.WorkingDirectory, option.ForceIncrement);
            var projectVersions = processor.ProcessProjectFiles();
            var assemblyVersions = processor.ProcessAssemblyInfo();
            var packageJsonVersions = processor.ProcessPackageJsonInfo();

            var newVersions = projectVersions.Concat(assemblyVersions).Concat(packageJsonVersions).Distinct().ToArray();
            var returnValue = ReportVersionInfoAndDetermineReturnValue(newVersions);

            ConsoleHelper.PauseIfRequired();
            return returnValue;
        }

        private static int ReportVersionInfoAndDetermineReturnValue(string[] newVersions)
        {
            switch (newVersions.Length)
            {
                case 0:
                    Console.WriteLine("Warning: no new versions found.");
                    return 1;
                case 1:
                    Console.WriteLine($"New version: {newVersions[0]}");
                    return 0;
                default:
                    Console.WriteLine("Note: your application uses multiple versions.");
                    Console.WriteLine($"New versions: {string.Join(", ", newVersions)}");
                    return 0;
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine(@"
Usage: increment-build-number [PATH] [--major | --minor] [--build]

  PATH must point to a git repository containing a Visual Studio solution.

  Options:
    --major    Increment the major version number, set minor version number to
               zero.
    --minor    Increment the minor version number.
    --build    Increment the build number (default).

  If you specify --major or --minor and omit --build, the build number will be
  reset to zero.
  If you use --major or --minor in combination with --build, the build number
  will be incremented as well.

  The revision number is not used and will always be zero.");
        }
    }
}