using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ClassLibrary1
{
    public class Trie
    {
        // Root node for Trie
        private TrieNode rootNode;

        //Constructor
        public Trie()
        {
            //Set root to ^
            rootNode = new TrieNode(null, '^', 0);
        }

        //Method that takes in a string and returns the node in the Trie that represents the Prefix
        public TrieNode PrefixNode(string input)
        {

            try
            {
                var currentNode = rootNode;
                var returnNode = currentNode;

                foreach (var currentChar in input)
                {
                    currentNode = currentNode.SearchChildren(currentChar);
                    if (currentNode == null)
                    {
                        break;
                    }

                    returnNode = currentNode;
                }
                return returnNode;
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("ERROR -> " + input);
                return null;
            }

            
        }

        //Insertion method for Trie. Takes a string and inserts each character into the Trie forming a chain that represents a word
        public void InsertString(string input)
        {
            try
            {
                //prefix node
                var preNode = PrefixNode(input);
                var currentNode = preNode;

                //for each character in the input
                for (var i = currentNode.NodeDepth; i < input.Length; i++)
                {
                    var newNode = new TrieNode(currentNode, input[i], currentNode.NodeDepth + 1);
                    currentNode.Children.Add(newNode);
                    currentNode = newNode;

                }

                //last node is $ to represent the end of a word
                var endingNode = new TrieNode(currentNode, '$', currentNode.NodeDepth + 1);
                //add the last node
                currentNode.Children.Add(endingNode);
            }
            catch(Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Error Message:" + e.Message);
                System.Diagnostics.Debug.WriteLine("Input Error: " + input);
            }

        }

        //Search method to search through the Trie tree and return the list of matching words with the prefix
        public List<string> Search(string input)
        {


            //setup the prefix lookup 
            var currentNode = rootNode;

            //loop through input
            foreach (var c in input)
            {

                var newNode = currentNode.SearchChildren(c);

                if (newNode == null)
                {
                    return null;
                }
                currentNode = newNode;
            }

            //set up the list that will be returned 
            List<string> returnList = new List<string>();


            //recursively depth first search using the prefix node we found 
            dfs(returnList, currentNode, input.Remove(input.Length - 1));

            //return what we get back
            return returnList;
        }

        //DFS method to perform the search while manipulating the list by adding found words 
        public void dfs(List<string> returnList, TrieNode node, string input)
        {

            //while we haven't found 10 results
            if (returnList.Count() <= 10)
            {
                //if we're at the end of a word
                if (node.NodeValue == '$')
                {
                    if (!returnList.Contains(input))
                    {
                        returnList.Add(input);
                    }
                }
                //otherwise keep searching
                else
                {
                    input = input + node.NodeValue;
                }

                foreach (var child in node.Children)
                {
                    if (child != null)
                    {
                        dfs(returnList, child, input);
                    }

                }
            }

        }

    }
}