using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Backpack.DependencyMap.PostProcessors;
using Backpack.DependencyMap.PreProcessors;
using Backpack.DependencyMap.Processors;
using log4net;
using Neo4jClient;
using Neo4jClient.Transactions;

namespace Backpack.DependencyMap
{
    public class DependencyMapProcessor
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DependencyMapProcessor));

        private readonly ApplicationArguments _arguments;
        private readonly IGraphClientFactory _graphClientFactory;
        private readonly IEnumerable<IPreProcessor> _preProcessors;
        private readonly IEnumerable<IProcessor> _processors;
        private readonly IEnumerable<IPostProcessor> _postProcessors;

        public DependencyMapProcessor(ApplicationArguments arguments, IGraphClientFactory graphClientFactory, IEnumerable<IPreProcessor> preProcessors, IEnumerable<IProcessor> processors, IEnumerable<IPostProcessor> postProcessors)
        {
            _arguments = arguments;
            _graphClientFactory = graphClientFactory;
            _preProcessors = preProcessors;
            _processors = processors;
            _postProcessors = postProcessors;
        }

        public void Start()
        {
            using (var client = (ITransactionalGraphClient) _graphClientFactory.Create())
            {
                //using (var tx = client.BeginTransaction())
                //{
                    RunPreProcessingTasks(client);
                    ProcessFiles(client);
                    RunPostProcessingTasks(client);

                    //tx.Commit();
                //}
            }
        }

        private void RunPreProcessingTasks(ITransactionalGraphClient client)
        {
            Log.Info("Running pre processing tasks");

            foreach (var preProcessor in _preProcessors)
            {
                preProcessor.Process(client);
            }
        }

        private void ProcessFiles(ITransactionalGraphClient client)
        {
            var filePattern = "(" + string.Join("|", _processors.Select(p => p.FilePattern)) + ")";
            if (!string.IsNullOrWhiteSpace(_arguments.Filter))
                filePattern = _arguments.Filter + ".*" + filePattern;

            Log.InfoFormat("Finding files with the pattern: {0}", filePattern);

            var searchPattern = new Regex(filePattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var searchOption = _arguments.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            Regex excludePattern = null;
            if (!string.IsNullOrWhiteSpace(_arguments.Exclude))
                excludePattern = new Regex(_arguments.Exclude, RegexOptions.IgnoreCase | RegexOptions.Compiled);

            foreach (var file in Directory.EnumerateFiles(_arguments.Path, "*", searchOption).Where(file => searchPattern.IsMatch(file)))
            {
                if (excludePattern != null && excludePattern.IsMatch(file))
                {
                    Log.InfoFormat("Skipping {0}", file);
                    continue;
                }

                var processor = _processors.FirstOrDefault(p => p.IsProcessorFor(file));
                if (processor != null)
                {
                    Log.InfoFormat("Processing {0}", file);
                    try
                    {
                        processor.Process(file, client);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                    }
                }
                else
                {
                    Log.WarnFormat("No processor found for file: {0}", file);
                }
            }
        }

        private void RunPostProcessingTasks(ITransactionalGraphClient client)
        {
            Log.Info("Running post processing tasks");

            foreach (var postProcessor in _postProcessors)
            {
                postProcessor.Process(client);
            }
        }
    }
}