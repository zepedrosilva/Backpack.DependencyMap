using System;
using Fclp;
using log4net;
using log4net.Config;
using StructureMap;
using System.Reflection;

namespace Backpack.DependencyMap
{
    class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
            XmlConfigurator.Configure();

            // Process arguments
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Log.InfoFormat("Dependency map calculator, version {0}\n", string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Revision));

            var parser = new FluentCommandLineParser<ApplicationArguments>();

            parser.SetupHelp("h", "help", "?")
                .Callback(text => {
                    Log.Info("Available options:\n");
                });

            parser.Setup(arg => arg.Path)
                .As('p', "path")
                .Required();

            parser.Setup(arg => arg.Filter)
                .As('f', "filter");

            parser.Setup(arg => arg.Exclude)
                .As('e', "exclude");

            parser.Setup(arg => arg.CleanDatabase)
                .As('c', "clean")
                .SetDefault(false);

            parser.Setup(arg => arg.Recursive)
                .As('r', "recursive")
                .SetDefault(false);

            var result = parser.Parse(args);

            if (result.HasErrors)
            {
                Log.InfoFormat("ERROR: {0}\n       (--help for more information)", result.ErrorText);
                Environment.Exit(-1);
            }

            if (result.HelpCalled)
            {
                Environment.Exit(0);
            }

            var arguments = parser.Object;

            Log.InfoFormat("Path: {0}", arguments.Path);

            if (!string.IsNullOrEmpty(arguments.Filter))
                Log.InfoFormat("Filter: {0}", arguments.Filter);

            if (!string.IsNullOrEmpty(arguments.Exclude))
                Log.InfoFormat("Exclude: {0}", arguments.Exclude);

            Log.InfoFormat("Recursive? {0}", arguments.Recursive);

            Log.InfoFormat("Clean up database? {0}\n", arguments.CleanDatabase);

            // Process the files and calculate the dependency map

            var container = new Container(c => {
                c.For<ApplicationArguments>().Singleton().Use(arguments);
                c.AddRegistry<Bootstrapper>();
            });
            container.GetInstance<DependencyMapProcessor>().Start();
        }
    }
}