using System.Collections.Generic;

namespace Backpack.DependencyMap.Renderers
{
    public class TreeNode<T> where T : class
    {
        public TreeNode() => Children = new List<TreeNode<T>>();

        public T Value { get; set; }

        public List<TreeNode<T>> Children { get; set; }
    }
}