using log4net;
using Neo4jClient;

namespace Backpack.DependencyMap.PreProcessors
{
    public class CleanUpPreProcessor : IPreProcessor
    {
        private readonly ILog _logger;
        private readonly ApplicationArguments _arguments;
        private readonly IGraphClient _client;

        public CleanUpPreProcessor(ApplicationArguments arguments, IGraphClient client)
        {
            _logger = LogManager.GetLogger(typeof(CleanUpPreProcessor));
            _arguments = arguments;
            _client = client;
        }

        public void Run()
        {
            if (_arguments.CleanDatabase)
            {
                _logger.Info("Cleaning the database");

                _client.Cypher.Match("(n)")
                    .DetachDelete("n")
                    .ExecuteWithoutResults();
            }
        }
    }
}