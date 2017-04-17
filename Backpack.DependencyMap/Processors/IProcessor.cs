using Neo4jClient.Transactions;

namespace Backpack.DependencyMap.Processors
{
    public interface IProcessor
    {
        string FilePattern { get; }

        bool IsProcessorFor(string file);

        void Process(string filePath, ITransactionalGraphClient client);
    }
}