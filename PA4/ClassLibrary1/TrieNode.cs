using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ClassLibrary1
{
    public class TrieNode
    {
        //List of children for node
        public List<TrieNode> Children { get; set; }

        //Value of node
        public char NodeValue { get; set; }

        //Parent for this node
        public TrieNode Parent { get; set; }

        //Depth for this node
        public int NodeDepth { get; set; }

        //constructor for the node
        public TrieNode(TrieNode parent, char value, int depth)
        {
            Children = new List<TrieNode>();
            NodeValue = value;
            Parent = parent;
            NodeDepth = depth;

        }

        //Method to return the children of the node
        public TrieNode SearchChildren(char searchChar)
        {
            foreach (var child in Children)
            {
                if (child.NodeValue == searchChar)
                {
                    return child;
                }

            }

            return null;
        }
    }
}