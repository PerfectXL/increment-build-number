using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

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
            foreach (string file in Directory.EnumerateFiles(_workingDirectory, "AssemblyInfo.cs", SearchOption.AllDirectories))
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

                string[] newVersions = collectedVersions.Distinct().ToArray();
                if (!newVersions.Any())
                {
                    continue;
                }

                foreach (string version in newVersions)
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
            foreach (string file in Directory.EnumerateFiles(_workingDirectory, "*.csproj", SearchOption.AllDirectories))
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

                bool hasDeclaration = string.IsNullOrWhiteSpace(xDoc.Declaration?.ToString());
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

            int major = version.Major + (forceIncrement == ForceIncrement.Major ? 1 : 0);
            int minor = forceIncrement == ForceIncrement.Major ? 0 : version.Minor + (forceIncrement == ForceIncrement.Minor ? 1 : 0);
            int build = version.Build + 1;

            return new Version(major, minor, build, 0).ToString();
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

            return Regex.Replace(line,
                pattern,
                s =>
                {
                    string newVersion = GetNewVersion(s.Value, forceIncrement);
                    collectedVersions.Add(newVersion);
                    return newVersion;
                },
                RegexOptions.IgnorePatternWhitespace);
        }
    }
}
