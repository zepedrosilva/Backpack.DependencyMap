using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Neo4jClient;

namespace Backpack.DependencyMap.Processors
{
    [DebuggerDisplay("Pattern: {" + nameof(FilePattern) + "}")]
    public class ProjectFileProcessor : IProcessor
    {
        private readonly Regex _regex;
        private readonly IGraphClient _client;

        public ProjectFileProcessor(IGraphClient client)
        {
            _regex = new Regex(FilePattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            _client = client;
        }

        public string FilePattern => @"\.csproj$";

        public bool IsProcessorFor(string file)
        {
            return _regex.IsMatch(file);
        }

        private static readonly Regex HintPathRegex = new Regex(@"^(?>\.\.\\)+(?>packages)\\(([a-zA-Z][a-zA-Z0-9_-]+\.?)+)\.((\d+\.?){1,})(-\\[a-zA-Z0-9_-]+)?", RegexOptions.Compiled);

        public void Process(string filePath)
        {
            var xml = XDocument.Load(filePath);

            // Get the project name
            var project = new FileInfo(filePath).Name.Replace(".csproj", "").Replace(".vbproj", "");

            // Get the list of project references that the project dependens upon
            var projects = xml.Descendants("{http://schemas.microsoft.com/developer/msbuild/2003}ProjectReference")
                .Where(p => p.Attribute("Include").Value.EndsWith(".csproj", StringComparison.InvariantCultureIgnoreCase))
                .SelectMany(p => p.Descendants("{http://schemas.microsoft.com/developer/msbuild/2003}Name"))
                .ToArray();

            if (projects.Length > 0)
            {
                foreach (var proj in projects)
                {
                    var dependencyName = proj.Value;

                    _client.Cypher
                        .Merge("(project:Project {name:{projectFile}})")
                        .Merge("(dependency:Project {name:{dependencyName}})")
                        .Merge("(project)-[:DEPENDS_ON]->(dependency)")
                        .WithParams(new Dictionary<string, object> {
                            { "projectFile", project },
                            { "dependencyName", dependencyName }
                        })
                        .ExecuteWithoutResults();
                }
            }

            // Get the list of packages that the project depends upon
            var packages = xml.Descendants("{http://schemas.microsoft.com/developer/msbuild/2003}HintPath")
                .Where(p => p.Value.IndexOf(@"\packages\", StringComparison.InvariantCultureIgnoreCase) > 0)
                .ToArray();

            if (packages.Length > 0)
            {
                foreach (var package in packages)
                {
                    var matches = HintPathRegex.Matches(package.Value);
                    var match = matches[0];
                    var packageId = match.Groups[1].Value;
                    var packageVersion = match.Groups[3].Value;

                    _client.Cypher
                        .Merge("(project:Project {name:{projectFile}})")
                        .Merge("(package:Package {name:{name}})")
                        .Merge("(version:PackageVersion {version:{version}, name:{name}})")
                        .Merge("(package)-[:HAS_VERSION]->(version)")
                        .Merge("(project)-[:DEPENDS_ON]->(version)")
                        .WithParams(new Dictionary<string, object> {
                            { "name", packageId },
                            { "version", packageVersion },
                            { "projectFile", project }
                        })
                        .ExecuteWithoutResults();
                }
            }
        }
    }
}