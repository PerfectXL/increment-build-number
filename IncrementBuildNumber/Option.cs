using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace IncrementBuildNumber
{
    internal class Option
    {
        private Option(bool showHelp, string workingDirectory, ForceIncrement forceIncrement)
        {
            ShowHelp = showHelp;
            WorkingDirectory = workingDirectory;
            ForceIncrement = forceIncrement;
        }

        public ForceIncrement ForceIncrement { get; }
        public bool ShowHelp { get; }
        public string WorkingDirectory { get; }

        public static Option ProcessCommandLineParameters(string[] args)
        {
            var showHelp = args.Any(s => Regex.IsMatch(s, @"^--?h(?:elp)?$", RegexOptions.IgnoreCase));

            var path = args.FirstOrDefault(s => !s.StartsWith("-"));
            var workingDirectory = !string.IsNullOrEmpty(path) && Directory.Exists(path) ? path : Directory.GetCurrentDirectory();

            ForceIncrement forceIncrement = HasOption(args, "major") && HasOption(args, "build") ? ForceIncrement.MajorAndBuild
                : HasOption(args, "minor") && HasOption(args, "build") ? ForceIncrement.MinorAndBuild
                : HasOption(args, "major") ? ForceIncrement.MajorAndReset
                : HasOption(args, "minor") ? ForceIncrement.MinorAndReset
                : ForceIncrement.Build;

            return new Option(showHelp, workingDirectory, forceIncrement);
        }

        private static bool HasOption(IEnumerable<string> args, string option)
        {
            return args.Any(s => Regex.IsMatch(s, $@"^--?{option}$", RegexOptions.IgnoreCase));
        }
    }
}