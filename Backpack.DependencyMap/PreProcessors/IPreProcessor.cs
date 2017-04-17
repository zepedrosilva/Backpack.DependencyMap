using Neo4jClient.Transactions;

namespace Backpack.DependencyMap.PreProcessors
{
    public interface IPreProcessor
    {
        void Process(ITransactionalGraphClient client);
    }
}