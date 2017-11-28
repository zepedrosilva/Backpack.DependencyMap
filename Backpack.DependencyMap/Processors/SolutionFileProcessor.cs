using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Onion.SolutionParser.Parser;
using Neo4jClient;

namespace Backpack.DependencyMap.Processors
{
    [DebuggerDisplay("Pattern: {" + nameof(FilePattern) + "}")]
    public class SolutionFileProcessor : IProcessor
    {
        private readonly Regex _regex;
        private readonly IGraphClient _client;

        public SolutionFileProcessor(IGraphClient client)
        {
            _regex = new Regex(FilePattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            _client = client;
        }

        public string FilePattern => @"\.sln";

        public bool IsProcessorFor(string file)
        {
            return _regex.IsMatch(file);
        }

        public void Process(string filePath)
        {
            // Get the solution name
            var solution = new FileInfo(filePath).Name.Replace(".sln", "");

            // Get the list of projects in the solution
            var solutionFile = SolutionParser.Parse(filePath);
            if (solutionFile.Projects.Any())
            {
                foreach (var project in solutionFile.Projects)
                {
                    _client.Cypher
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