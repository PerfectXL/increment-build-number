using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using Formatting = Newtonsoft.Json.Formatting;

namespace IncrementBuildNumber
{
    internal class FileProcessor
    {
        private readonly ForceIncrement _forceIncrement;
        private readonly string _workingDirectory;

        public FileProcessor(string workingDirectory, ForceIncrement forceIncrement)
        {
            _workingDirectory = workingDirectory;
            _forceIncrement = forceIncrement;
        }

        public IEnumerable<string> ProcessAssemblyInfo()
        {
            foreach (var file in Directory.EnumerateFiles(_workingDirectory, "AssemblyInfo.cs", SearchOption.AllDirectories))
            {
                string[] lines;
                try
                {
                    lines = File.ReadAllLines(file);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error reading {file}: {e.Message}");
                    continue;
                }

                var collectedVersions = new List<string>();
                for (var i = 0; i < lines.Length; i++)
                {
                    lines[i] = ReplaceVersionInLine(lines[i], collectedVersions, _forceIncrement);
                }

                var newVersions = collectedVersions.Distinct().ToArray();
                if (!newVersions.Any())
                {
                    continue;
                }

                foreach (var version in newVersions)
                {
                    yield return version;
                }

                Console.WriteLine(file);
                try
                {
                    File.WriteAllLines(file, lines, Encoding.UTF8);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error writing {file}: {e.Message}");
                }
            }
        }

        public IEnumerable<string> ProcessProjectFiles()
        {
            foreach (var file in Directory.EnumerateFiles(_workingDirectory, "*.csproj", SearchOption.AllDirectories))
            {
                XDocument xDoc;
                try
                {
                    xDoc = XDocument.Load(file, LoadOptions.PreserveWhitespace);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error loading {file}: {e.Message}");
                    continue;
                }

                XElement element = xDoc.Descendants("Version").FirstOrDefault();
                if (element == null)
                {
                    continue;
                }

                yield return element.Value = GetNewVersion(element.Value, _forceIncrement);

                var hasDeclaration = string.IsNullOrWhiteSpace(xDoc.Declaration?.ToString());
                using (XmlWriter writer = XmlWriter.Create(file, GetXmlWriterSettings(hasDeclaration)))
                {
                    Console.WriteLine(file);
                    try
                    {
                        xDoc.WriteTo(writer);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error writing {file}: {e.Message}");
                    }
                }
            }
        }

        private static string GetNewVersion(string currentVersion, ForceIncrement forceIncrement = ForceIncrement.None)
        {
            Version version;
            try
            {
                version = new Version(currentVersion);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error. Could not parse version string {currentVersion}: {e.Message}");
                Console.WriteLine("Not changing current version.");
                return currentVersion;
            }

            int major;
            int minor;
            int build;
            switch (forceIncrement)
            {
                case ForceIncrement.None:
                    major = version.Major;
                    minor = version.Minor;
                    build = version.Build;
                    break;
                case ForceIncrement.MinorAndReset:
                    major = version.Major;
                    minor = version.Minor + 1;
                    build = 0;
                    break;
                case ForceIncrement.MajorAndReset:
                    major = version.Major + 1;
                    minor = 0;
                    build = 0;
                    break;
                case ForceIncrement.MinorAndBuild:
                    major = version.Major;
                    minor = version.Minor + 1;
                    build = version.Build + 1;
                    break;
                case ForceIncrement.MajorAndBuild:
                    major = version.Major + 1;
                    minor = 0;
                    build = version.Build + 1;
                    break;
                case ForceIncrement.Build:
                    major = version.Major;
                    minor = version.Minor;
                    build = version.Build + 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(forceIncrement), forceIncrement, null);
            }

            return new Version(major, minor, build, 0).ToString(3);
        }

        private static XmlWriterSettings GetXmlWriterSettings(bool omitXmlDeclaration)
        {
            return new XmlWriterSettings {Encoding = Encoding.UTF8, Indent = true, NewLineChars = "\r\n", OmitXmlDeclaration = omitXmlDeclaration};
        }

        private static string ReplaceVersionInLine(string line, ICollection<string> collectedVersions, ForceIncrement forceIncrement)
        {
            // Pattern contains search for version number surrounded by look-behind and look-ahead assertions.
            const string lookBehind = @" (?<= \[ \s* assembly \s* : \s* AssemblyVersion \s* \( \s* "" ) ";
            const string lookAhead = @" (?= "" \s* \) \s* \] ) ";
            const string pattern = lookBehind + @" \d+\.\d+\.\d+(\.\d+)? " + lookAhead;

            return Regex.Replace(line, pattern, s =>
            {
                var newVersion = GetNewVersion(s.Value, forceIncrement);
                collectedVersions.Add(newVersion);
                return newVersion;
            }, RegexOptions.IgnorePatternWhitespace);
        }

        public IEnumerable<string> ProcessPackageJsonInfo()
        {
            foreach (var directory in Directory.EnumerateDirectories(_workingDirectory, "*", SearchOption.TopDirectoryOnly))
            foreach (var searchPattern in new[] {"package.json", "package-lock.json"})
            foreach (var file in Directory.EnumerateFiles(directory, searchPattern, SearchOption.TopDirectoryOnly))
            {
                string text;
                try
                {
                    text = File.ReadAllText(file);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error reading {file}: {e.Message}");
                    continue;
                }

                JObject obj = JObject.Parse(text);

                if (obj["version"] == null)
                {
                    continue;
                }

                var currentVersion = (string) obj["version"];
                if (string.IsNullOrEmpty(currentVersion))
                {
                    continue;
                }

                var newVersion = GetNewVersion(currentVersion, _forceIncrement);

                if (currentVersion == newVersion)
                {
                    continue;
                }

                yield return newVersion;
                obj["version"] = newVersion;

                var contents = obj.ToString(Formatting.Indented).TrimEnd() + "\r\n";
                if (!Regex.IsMatch(text, @"\r\n") /* Unix line-endings only */)
                {
                    contents = Regex.Replace(contents, @"\r\n", "\n");
                }

                Console.WriteLine(file);
                try
                {
                    File.WriteAllText(file, contents, new UTF8Encoding(false));
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error writing {file}: {e.Message}");
                }
            }
        }
    }
}