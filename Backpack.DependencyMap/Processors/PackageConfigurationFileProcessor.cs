using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using log4net;
using Neo4jClient.Transactions;

namespace Backpack.DependencyMap.Processors
{
    [DebuggerDisplay("Pattern: {" + nameof(FilePattern) + "}")]
    public class PackageConfigurationFileProcessor : IProcessor
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PackageConfigurationFileProcessor));
        private readonly Regex _regex;

        public PackageConfigurationFileProcessor()
        {
            _regex = new Regex(FilePattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public string FilePattern => @"\\packages\.config$";

        public bool IsProcessorFor(string file)
        {
            return _regex.IsMatch(file);
        }

        public void Process(string filePath, ITransactionalGraphClient client)
        {
            Log.InfoFormat("[P]rocessing file: {0}", filePath);

            var xml = XDocument.Load(filePath);

            // Get the project name
            var projectFilePath = Directory.EnumerateFiles(Path.GetDirectoryName(filePath), "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (projectFilePath == null) return;

            var project = new FileInfo(projectFilePath).Name.Replace(".csproj", "").Replace(".vbproj", "");

            // Get the list of packages that the project depends upon
            var packages = xml.Descendants("package").ToArray();
            foreach (var package in packages)
            {
                var id = package.Attribute("id").Value;
                var version = package.Attribute("version").Value;

                client.Cypher
                    .Merge("(project:Project {name:{projectFile}})")
                    .Merge("(package:Package {name:{name}})")
                    .Merge("(version:PackageVersion {version:{version}, name:{name}})")
                    .Merge("(package)-[:HAS_VERSION]->(version)")
                    .Merge("(project)-[:DEPENDS_ON]->(version)")
                    //.Merge("(version)-[:IS_REFERENCED_BY]->(project)")
                    .WithParams(new Dictionary<string, object> {
                        {"name", id},
                        {"version", version},
                        {"projectFile", project},
                    })
                    .ExecuteWithoutResults();
            }
        }
    }
}