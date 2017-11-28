using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Backpack.DependencyMap.Processors;
using System.Collections.Concurrent;

namespace Backpack.DependencyMap.Finders
{
    public class FileSystemFileFinder : IFileFinder
    {
        private readonly ApplicationArguments _arguments;

        public FileSystemFileFinder(ApplicationArguments arguments)
        {
            _arguments = arguments;
        }

        public void FindFiles(IProcessor[] processors, BlockingCollection<string> queue)
        {
            Regex excludePattern = null;
            if (!string.IsNullOrWhiteSpace(_arguments.Exclude))
                excludePattern = new Regex(_arguments.Exclude, RegexOptions.IgnoreCase | RegexOptions.Compiled);

            var filePattern = "(" + string.Join("|", processors.Select(p => p.FilePattern)) + ")";
            if (!string.IsNullOrWhiteSpace(_arguments.Filter))
                filePattern = _arguments.Filter + ".*" + filePattern;

            var searchPattern = new Regex(filePattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var searchOption = _arguments.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            foreach (var file in Directory.EnumerateFiles(_arguments.Path, "*", searchOption).Where(file => searchPattern.IsMatch(file)))
            {
                if (excludePattern != null && excludePattern.IsMatch(file))
                    continue;

                queue.Add(file);
            }

            queue.CompleteAdding();
        }
    }
}