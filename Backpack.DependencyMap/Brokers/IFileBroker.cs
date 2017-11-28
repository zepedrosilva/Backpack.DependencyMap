using Backpack.DependencyMap.Processors;
using System.Collections.Concurrent;

namespace Backpack.DependencyMap.Brokers
{
    public interface IFileBroker
    {
        void Process(IProcessor[] processors, BlockingCollection<string> queue);
    }
}