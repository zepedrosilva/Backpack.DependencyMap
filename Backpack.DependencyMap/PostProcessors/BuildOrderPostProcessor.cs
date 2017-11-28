using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Fclp.Internals.Extensions;
using Neo4jClient.Cypher;
using Neo4jClient;
using log4net;
using Backpack.DependencyMap.Renderers;

namespace Backpack.DependencyMap.PostProcessors
{
    public class BuildOrderPostProcessor : IPostProcessor
    {
        [DebuggerDisplay("Solution: {Solution}, DependsOn: {Dependency}")]
        private class SolutionDependency
        {
            public string Solution { get; set; }
            public string Dependency { get; set; }
        }

        [DebuggerDisplay("Solution: {Name}, DependsOn: {DependsOn.Count}, Dependants: {Dependants.Count}")]
        private class Solution
        {
            public Solution()
            {
                DependsOn = new List<Solution>();
                Dependants = new List<Solution>();
            }

            public string Name { get; set; }
            public ICollection<Solution> DependsOn { get; private set; }
            public ICollection<Solution> Dependants { get; private set; }
        }

        private readonly ILog _logger;
        private readonly IGraphClient _client;

        public BuildOrderPostProcessor(IGraphClient client)
        {
            _logger = LogManager.GetLogger(typeof(BuildOrderPostProcessor));
            _client = client;
        }

        public void Run()
        {
            _logger.Info("Calculating build order");

            var results = _client.Cypher
                .Match("(sln:Solution)-[:DEPENDS_ON*0..1]->(dep:Solution)")
                .Where("sln <> dep")
                .ReturnDistinct((sln, dep) => new SolutionDependency {
                    Solution = Return.As<string>("sln.name"),
                    Dependency = Return.As<string>("dep.name")
                })
                .Results.ToList();

            var output = new List<Solution>();

            foreach (var relationship in results)
            {
                if (relationship.Solution == relationship.Dependency) continue;

                var sln = output.FirstOrDefault(s => s.Name == relationship.Solution);
                if (sln == null)
                {
                    sln = new Solution { Name = relationship.Solution };
                    output.Add(sln);
                }

                var dep = output.FirstOrDefault(d => d.Name == relationship.Dependency);
                if (dep == null)
                {
                    dep = new Solution { Name = relationship.Dependency };
                    output.Add(dep);
                }

                sln.DependsOn.Add(dep);
                dep.Dependants.Add(sln);
            }

            // Calculate the build order (topological sort of the previous graph)

            _logger.InfoFormat("\nBuild order");

            var cnt = 0;
            var cyclicGraphFound = false;
            var tree = new List<TreeNode<string>>();

            while (output.Count > 0)
            {
                var sln = new TreeNode<string> { Value = "Step " + ++cnt };
                tree.Add(sln);

                var count = output.Count;

                var nodesToDelete = new List<Solution>();
                foreach (var node in output)
                {
                    if (node.DependsOn.Count == 0)
                    {
                        sln.Children.Add(new TreeNode<string> { Value = "Build " + node.Name });

                        _client.Cypher
                            .Match("(sln:Solution {name:{solutionName}})")
                            .Set("sln.buildOrder = {buildOrder}")
                            .WithParams(new Dictionary<string, object> {
                                {"solutionName", node.Name},
                                {"buildOrder", cnt},
                            })
                            .ExecuteWithoutResults();

                        nodesToDelete.Add(node);
                    }
                }

                foreach (var tmp in nodesToDelete)
                {
                    tmp.Dependants.ForEach(d => d.DependsOn.Remove(tmp));
                    output.Remove(tmp);
                }

                if (count == output.Count)
                {
                    sln.Children.Add(new TreeNode<string> { Value = "Error! This is a cyclic graph. Aborting." });
                    cyclicGraphFound = true;
                    break;
                }
            }

            new TreeRenderer<TreeNode<string>>().Visit(tree, s => s.Value, d => d.Children);

            // 

            if (cyclicGraphFound)
            {
                _logger.Info("\nRemaining solutions");

                tree.Clear();
                foreach (var remaining in output.OrderBy(o => o.DependsOn.Count))
                {
                    var tmp = new TreeNode<string> { Value = remaining.Name };
                    var depsOn = new TreeNode<string> { Value = "Depends on" };
                    tmp.Children.Add(depsOn);
                    tree.Add(tmp);

                    foreach (var dep in remaining.DependsOn)
                        depsOn.Children.Add(new TreeNode<string> { Value = dep.Name });

                    if (remaining.Dependants.Any())
                    {
                        var depsOf = new TreeNode<string> { Value = "Dependency of" };
                        tmp.Children.Add(depsOf);

                        foreach (var dep in remaining.Dependants)
                            depsOf.Children.Add(new TreeNode<string> { Value = dep.Name });
                    }
                }

                new TreeRenderer<TreeNode<string>>().Visit(tree, s => s.Value, d => d.Children);
            }
        }
    }
}