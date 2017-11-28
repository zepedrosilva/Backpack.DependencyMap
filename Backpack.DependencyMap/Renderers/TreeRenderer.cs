using log4net;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Backpack.DependencyMap.Renderers
{

    public class TreeRenderer<T> where T : class
    {
        private readonly ILog _logger;
        private readonly Stack<string> _prefix = new Stack<string>();

        private const string VerticalAndRight = "├─";
        private const string Vertical = "│ ";
        private const string LowerLeftCorner = "└─";
        private const string Space = "  ";

        public TreeRenderer()
        {
            _logger = LogManager.GetLogger(typeof(TreeRenderer<T>));
        }

        public void Visit(T root, Func<T, string> getName, Func<T, IEnumerable<T>> getChildren)
        {

            var prefix = string.Join(string.Empty, _prefix.Reverse().ToArray());
            _logger.Info(prefix + getName(root));

            if (_prefix.Any())
                _prefix.Push(_prefix.Pop() == VerticalAndRight ? Vertical : Space);

            Visit(getChildren(root), getName, getChildren);
        }

        public void Visit(IEnumerable<T> nodes, Func<T, string> getName, Func<T, IEnumerable<T>> getChildren)
        {
            if (nodes.Any())
            {
                var lastNode = nodes.Last();

                foreach (var node in nodes)
                {
                    if (node != lastNode)
                    {
                        _prefix.Push(VerticalAndRight);
                        Visit(node, getName, getChildren);
                        _prefix.Pop();
                    }
                    else
                    {
                        _prefix.Push(LowerLeftCorner);
                        Visit(node, getName, getChildren);
                        _prefix.Pop();
                    }
                }
            }
        }
    }
}