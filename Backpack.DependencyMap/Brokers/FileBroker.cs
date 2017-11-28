using System;
using log4net;
using System.Collections.Concurrent;
using Backpack.DependencyMap.Processors;
using System.Linq;

namespace Backpack.DependencyMap.Brokers
{
    public class FileBroker : IFileBroker
    {
        private readonly ILog _logger;

        public FileBroker()
        {
            _logger = LogManager.GetLogger(typeof(FileBroker));
        }

        public void Process(IProcessor[] processors, BlockingCollection<string> queue)
        {
            while (!queue.IsCompleted)
            {
                if (queue.TryTake(out string fileToProcess))
                {
                    try
                    {
                        _logger.InfoFormat("Processing file {0}", fileToProcess);
                        processors.First(p => p.IsProcessorFor(fileToProcess)).Process(fileToProcess);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex);
                    }
                }
            }
        }
    }
}