using log4net;
using Neo4jClient.Transactions;

namespace Backpack.DependencyMap.PreProcessors
{
    public class CleanUpPreProcessor : IPreProcessor
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CleanUpPreProcessor));

        private readonly ApplicationArguments _arguments;

        public CleanUpPreProcessor(ApplicationArguments arguments)
        {
            _arguments = arguments;
        }

        public void Process(ITransactionalGraphClient client)
        {
            if (_arguments.CleanDatabase)
            {
                if (Log.IsInfoEnabled)
                    Log.Info("Cleaning up the database");

                client.Cypher.Match("(n)")
                    .DetachDelete("n")
                    .ExecuteWithoutResults();
            }
        }
    }
}