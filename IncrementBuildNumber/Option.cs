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
            bool showHelp = args.Any(s => Regex.IsMatch(s, @"^--?h(?:elp)?$", RegexOptions.IgnoreCase));

            string path = args.FirstOrDefault(s => !s.StartsWith("-"));
            string workingDirectory = !string.IsNullOrEmpty(path) && Directory.Exists(path) ? path : Directory.GetCurrentDirectory();

            ForceIncrement forceIncrement = args.Any(s => Regex.IsMatch(s, @"^--?major$", RegexOptions.IgnoreCase))
                ? ForceIncrement.Major
                : (args.Any(s => Regex.IsMatch(s, @"^--?minor$", RegexOptions.IgnoreCase)) ? ForceIncrement.Minor : ForceIncrement.Build);

            return new Option(showHelp, workingDirectory, forceIncrement);
        }
    }
}
