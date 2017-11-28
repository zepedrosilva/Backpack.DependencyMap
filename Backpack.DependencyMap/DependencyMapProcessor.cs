using System;
using System.Collections.Generic;
using Backpack.DependencyMap.PostProcessors;
using Backpack.DependencyMap.PreProcessors;
using Backpack.DependencyMap.Processors;
using log4net;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Diagnostics;
using Backpack.DependencyMap.Finders;
using Backpack.DependencyMap.Brokers;

namespace Backpack.DependencyMap
{
    public class DependencyMapProcessor
    {
        private readonly ILog _logger;
        private readonly ApplicationArguments _arguments;
        private readonly IFileFinder _finder;
        private readonly IFileBroker _broker;
        private readonly IPreProcessor[] _preProcessors;
        private readonly IProcessor[] _processors;
        private readonly IPostProcessor[] _postProcessors;

        public DependencyMapProcessor(ApplicationArguments arguments, IFileFinder finder, IFileBroker broker, IPreProcessor[] preProcessors, IProcessor[] processors, IPostProcessor[] postProcessors)
        {
            _logger = LogManager.GetLogger(typeof(DependencyMapProcessor));
            _arguments = arguments;
            _finder = finder;
            _broker = broker;
            _preProcessors = preProcessors;
            _processors = processors;
            _postProcessors = postProcessors;
        }

        public void Start()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // Execute all pre processors synchronously

            foreach (var preProcessor in _preProcessors)
            {
                preProcessor.Run();
            }

            // Find the appropriate files based on the file patterns handled in the file processors and process them in paralell

            var queue = new BlockingCollection<string>();

            var tasks = new List<Task> {
                Task.Factory.StartNew(() => _finder.FindFiles(_processors, queue))
            };

            for (var cores = 0; cores < Environment.ProcessorCount; cores++)
            {
                tasks.Add(Task.Factory.StartNew(() => _broker.Process(_processors, queue)));
            }

            Task.WaitAll(tasks.ToArray());

            // When all files are processed, execute the post processors synchronously

            foreach (var postProcessor in _postProcessors)
            {
                postProcessor.Run();
            }

            // Done

            stopwatch.Stop();

            _logger.InfoFormat("\nDone! Elapsed time: {0} second(s)", TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds).Seconds);
        }
    }
}