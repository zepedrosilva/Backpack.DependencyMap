using log4net;
using Neo4jClient;

namespace Backpack.DependencyMap.PostProcessors
{
    public class SolutionDependencyPostProcessor : IPostProcessor
    {
        private readonly ILog _logger;
        private readonly IGraphClient _client;

        public SolutionDependencyPostProcessor(IGraphClient client)
        {
            _logger = LogManager.GetLogger(typeof(SolutionDependencyPostProcessor));
            _client = client;
        }

        public void Run()
        {
            _logger.Info("Calculating dependencies between solutions");

            _client.Cypher
                .Match("(sln:Solution)-[ref1:REFERENCES]->(prj1:Project)-[:DEPENDS_ON]->(PackageVersion)<-[:HAS_VERSION]-(pkg:Package)<-[:PUBLISHES]-(prj2:Project)<-[ref2:REFERENCES]-(dep:Solution)")
                .Merge("(sln)-[:DEPENDS_ON]->(dep)")
                .ExecuteWithoutResults();
        }
    }
}