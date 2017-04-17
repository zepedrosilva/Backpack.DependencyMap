using Neo4jClient.Transactions;

namespace Backpack.DependencyMap.PostProcessors
{
    public interface IPostProcessor
    {
        void Process(ITransactionalGraphClient client);
    }
}