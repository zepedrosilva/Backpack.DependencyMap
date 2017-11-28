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
    public class PackageSpecificationFileProcessor : IProcessor
    {
        private readonly Regex _regex;
        private readonly IGraphClient _client;

        public PackageSpecificationFileProcessor(IGraphClient client)
        {
            _regex = new Regex(FilePattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            _client = client;
        }

        public string FilePattern => @"\.nuspec$";

        public bool IsProcessorFor(string file)
        {
            return _regex.IsMatch(file);
        }

        public void Process(string filePath)
        {
            var xml = XDocument.Load(filePath);

            // Get the project name
            var projectFilePath = Directory.EnumerateFiles(Path.GetDirectoryName(filePath), "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (projectFilePath == null) return;

            var project = new FileInfo(projectFilePath).Name.Replace(".csproj", "").Replace(".vbproj", "");

            // Get the list of packages that the package depends upon
            var package = xml.Descendants("{http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd}id").FirstOrDefault() ??
                          xml.Descendants("id").FirstOrDefault();
            var packageId = package.Value;

            _client.Cypher
                .Merge("(project:Project {name:{projectFile}})")
                .Merge("(package:Package {name:{name}})")
                .Merge("(project)-[:PUBLISHES]->(package)")
                .WithParams(new Dictionary<string, object> {
                    { "name", packageId },
                    { "projectFile", project },
                })
                .ExecuteWithoutResults();

            var dependencies = xml.Root.Descendants("dependency").ToArray();
            foreach (var dependency in dependencies)
            {
                var id = dependency.Attribute("id").Value;
                var version = dependency.Attribute("version").Value;

                _client.Cypher
                    .Merge("(nuspec:Package {name:{packageId}})")
                    .Merge("(package:Package {name:{packageName}})")
                    .Merge("(nuspec)-[:DEPENDS_ON]->(package)")
                    .WithParams(new Dictionary<string, object> {
                        { "packageId", packageId },
                        { "packageName", id },
                    })
                    .ExecuteWithoutResults();

                if (version != "$version$")
                {
                    _client.Cypher
                        .Merge("(nuspec:Package {name:{packageId}})")
                        .Merge("(packageVersion:PackageVersion {version:{packageVersion}, name:{packageName}})")
                        .Merge("(nuspec)-[:DEPENDS_ON]->(packageVersion)")
                        .WithParams(new Dictionary<string, object> {
                            { "packageId", packageId },
                            { "packageVersion", packageId },
                            { "packageName", id },
                        })
                        .ExecuteWithoutResults();
                }
            }
        }
    }
}