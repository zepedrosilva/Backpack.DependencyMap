using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Fclp.Internals.Extensions;
using Neo4jClient.Cypher;
using Neo4jClient.Transactions;

namespace Backpack.DependencyMap.PostProcessors
{
    public class SolutionDependencyPostProcessor : IPostProcessor
    {
        public class SolutionDependency
        {
            public string Solution { get; set; }
            public string Dependency { get; set; }
        }

        [DebuggerDisplay("Solution: {Name}, DependsOn: {DependsOn.Count}, Dependants: {Dependants.Count}")]
        public class Solution
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

        public void Process(ITransactionalGraphClient client)
        {
            // Add the solution dependency dependencies
            client.Cypher
                .Match("(sln:Solution)-[ref1:REFERENCES]->(prj1:Project)-[:DEPENDS_ON]->(PackageVersion)<-[:HAS_VERSION]-(pkg:Package)<-[:PUBLISHES]-(prj2:Project)<-[ref2:REFERENCES]-(dep:Solution)")
                //.Where("NOT (prj1)-[:DEPENDS_ON]-(prj2)")
                .Merge("(sln)-[:DEPENDS_ON]->(dep)")
                .ExecuteWithoutResults();

            // Get the list of solutions and calculate the build order
            var results = client.Cypher
                .Match("(sln:Solution)-[:DEPENDS_ON*0..]->(dep:Solution)")
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
                    sln = new Solution {Name = relationship.Solution};
                    output.Add(sln);
                }

                var dep = output.FirstOrDefault(d => d.Name == relationship.Dependency);
                if (dep == null)
                {
                    dep = new Solution {Name = relationship.Dependency};
                    output.Add(dep);
                }

                sln.DependsOn.Add(dep);
                dep.Dependants.Add(sln);
            }

            // Calculate the build order (topological sort of the previous graph)

            //client.Cypher
            //    .Match("(sln:Solution {name:{})")
            //    .Merge("SET sln.dependencies = {dependencies}")
            //    .ExecuteWithoutResults();

            var cnt = 0;
            while (output.Count > 0)
            {
                Console.WriteLine("    Step {0}", ++cnt);
                var count = output.Count;

                var nodesToDelete = new List<Solution>();
                foreach (var node in output)
                {
                    if (node.DependsOn.Count == 0)
                    {
                        Console.WriteLine("      Build {0}", node.Name);

                        client.Cypher
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
                    Console.WriteLine("\n    Error! This is a cyclic graph. Aborting.");
                    Console.WriteLine("\n    Remaining solutions:");

                    foreach (var remaining in output)
                        Console.WriteLine("      {0} (depends on: {1})", remaining.Name, remaining.DependsOn.Count);

                    break;
                }
            }
        }
    }
}