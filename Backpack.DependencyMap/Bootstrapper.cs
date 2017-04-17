using System;
using Backpack.DependencyMap.PostProcessors;
using Backpack.DependencyMap.PreProcessors;
using Backpack.DependencyMap.Processors;
using Neo4jClient;
using StructureMap;

namespace Backpack.DependencyMap
{
    public class Bootstrapper : Registry
    {
        public Bootstrapper()
        {
            For<NeoServerConfiguration>()
                .Singleton()
                .Use(ctx => NeoServerConfiguration.GetConfiguration(new Uri("http://localhost:7474/db/data"), "test", "test"));

            For<IGraphClientFactory>()
                .Singleton()
                .Use<GraphClientFactory>();

            Scan(s => {
                s.TheCallingAssembly();
                s.AddAllTypesOf<IPreProcessor>();
                s.AddAllTypesOf<IProcessor>();
                s.AddAllTypesOf<IPostProcessor>();
            });
        }
    }
}