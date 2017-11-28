using Backpack.DependencyMap.Processors;
using System.Collections.Concurrent;

namespace Backpack.DependencyMap.Finders
{
    public interface IFileFinder
    {
        void FindFiles(IProcessor[] processors, BlockingCollection<string> queue);
    }
}