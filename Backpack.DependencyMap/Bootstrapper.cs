using System;
using Backpack.DependencyMap.PostProcessors;
using Backpack.DependencyMap.PreProcessors;
using Backpack.DependencyMap.Processors;
using Neo4jClient;
using StructureMap;
using Backpack.DependencyMap.Finders;
using Backpack.DependencyMap.Brokers;

namespace Backpack.DependencyMap
{
    public class Bootstrapper : Registry
    {
        public Bootstrapper()
        {
            For<NeoServerConfiguration>()
                .Singleton()
                .Use(ctx => NeoServerConfiguration.GetConfiguration(new Uri("http://localhost:7474/db/data"), "neo4j", "neo4j"));

            For<IGraphClient>()
                .Singleton()
                .Use(ctx => ctx.GetInstance<GraphClientFactory>().Create());

            For<IFileFinder>()
                .Singleton()
                .Use<FileSystemFileFinder>();

            For<IFileBroker>()
                .Singleton()
                .Use<FileBroker>();

            // Add one or more pre processors in the correct execution order

            For<IPreProcessor[]>()
                .Use(ctx => new IPreProcessor[] {
                    ctx.GetInstance<CleanUpPreProcessor>()
                    // add more pre processors, if needed
                });

            // Add all the available processors

            Scan(s => {
                s.TheCallingAssembly();
                s.AddAllTypesOf<IProcessor>();
            });

            // Add one or more post processors in the correct execution order

            For<IPostProcessor[]>()
                .Use(ctx => new IPostProcessor[] {
                    ctx.GetInstance<SolutionDependencyPostProcessor>(),
                    ctx.GetInstance<BuildOrderPostProcessor>()
                    // add more post processors, if needed
                });
        }
    }
}