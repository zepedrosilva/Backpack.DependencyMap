using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using log4net;
using Neo4jClient.Transactions;
using Onion.SolutionParser.Parser;

namespace Backpack.DependencyMap.Processors
{
    [DebuggerDisplay("Pattern: {" + nameof(FilePattern) + "}")]
    public class SolutionFileProcessor : IProcessor
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SolutionFileProcessor));
        private readonly Regex _regex;

        public SolutionFileProcessor()
        {
            _regex = new Regex(FilePattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public string FilePattern => @"\.sln";

        public bool IsProcessorFor(string file)
        {
            return _regex.IsMatch(file);
        }

        public void Process(string filePath, ITransactionalGraphClient client)
        {
            Log.InfoFormat("Proce[s]sing file: {0}", filePath);

            // Get the solution name
            var solution = new FileInfo(filePath).Name.Replace(".sln", "");

            // Get the list of projects in the solution
            var solutionFile = SolutionParser.Parse(filePath);
            if (solutionFile.Projects.Any())
            {
                foreach (var project in solutionFile.Projects)
                {
                    client.Cypher
                        .Merge("(solution:Solution {name:{solutionName}})")
                        .Merge("(project:Project {name:{projectName}})")
                        .Merge("(solution)-[:REFERENCES]->(project)")
                        .WithParams(new Dictionary<string, object> {
                            {"solutionName", solution.Clone()},
                            {"projectName", project.Name.Clone()}
                        })
                        .ExecuteWithoutResults();
                }
            }
        }
    }
}