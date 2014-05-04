using System;
using System.Collections.Generic;
using System.Linq;

namespace Habitat.Core
{
    /// <summary>
    /// Represents a group of name-value pairs and/or other objects used in a block of application config
    /// </summary>
    public class ConfigNode
    {
        /// <summary>
        /// The name of the node
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The string value, if any, assigned to the node
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Child nodes, if any, for this ConfigNode
        /// </summary>
        public List<ConfigNode> Children { get; set; }

        /// <summary>
        /// Ensures that all paths in the tree (i.e. namespaces) yield unique name/value pairs and returns the results as a dictionary.
        /// </summary>
        /// <remarks>
        /// A given path through the tree from the root to a Value Node should yield a unique name based on all the Nodes along the path.
        /// For example, consider the following tree:
        /// 
        /// -Application1
        /// --Logging
        /// ---Log1Location : somewhere1
        /// ---Log2Location : somewhere2
        /// --Services
        /// ---Service1Location : somewhere3
        /// ---Service2Location : somewhere4
        /// 
        /// Application1 is the root, and under it are 2 Object Nodes named Logging and Services.  Each of those Object Nodes
        /// contains 2 uniquely named Value Nodes.  The path to the unique value "somewhere3" is therefore "Application1.Services.Service1Location".
        /// </remarks>
        public Dictionary<string, string> ToDictionary()
        {
            var nameValuePairs = GetNameValuePairs();
            var allNames = nameValuePairs.Select(x => x.Item1).ToArray();
            if (nameValuePairs.Count() != allNames.Distinct().Count())
            {
                throw new InvalidOperationException(string.Format("Invalid configuration - the following list contains duplicate names: {0}{1}{0}", Environment.NewLine, string.Join(Environment.NewLine, allNames)));
            }

            return nameValuePairs.ToDictionary(x => x.Item1, x => x.Item2);
        }

        /// <summary>
        /// Recursive method to get all of the names for all child values underneath the current node
        /// </summary>
        /// <returns></returns>
        private Tuple<string, string>[] GetNameValuePairs()
        {
            var names = new List<Tuple<string, string>>();

            if (Children != null && Children.Count > 0)
            {
                foreach (var child in Children)
                {
                    var childNames = child.GetNameValuePairs().Select(x => new Tuple<string, string>(string.Format("{0}.{1}", Name, x.Item1), x.Item2)).ToArray();
                    names.AddRange(childNames);
                }
            }
            else
            {
                return new[] { new Tuple<string, string>((Name ?? string.Empty).Trim(), (Value ?? string.Empty).Trim()) };
            }

            return names.ToArray();
        }
    }
}
