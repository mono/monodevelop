// 
// AnalysisServiceExtensions.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using MonoDevelop.Core;
using Mono.Addins;
using System.Linq;
using MonoDevelop.Ide.Codons;
using MonoDevelop.AnalysisCore.Extensions;

namespace MonoDevelop.AnalysisCore
{
	internal static class AnalysisExtensions
	{
		const string EXT_RULES = "/MonoDevelop/AnalysisCore/Rules";
		const string EXT_TYPES = "/MonoDevelop/AnalysisCore/Types";
		const string EXT_FIX_HANDLERS = "/MonoDevelop/AnalysisCore/FixHandlers";
		
		static KeyedNodeList<string,AnalysisRuleAddinNode> rulesByInput
			= new KeyedNodeList<string,AnalysisRuleAddinNode> ();
		
		// This is a re-usable cache of computed trees. it will need to be flushed nodesByInput is cached.
		// We should probably clean it via a LRU too.
		static Dictionary<RuleTreeType,RuleTreeRoot> analysisTreeCache
			= new Dictionary<RuleTreeType, RuleTreeRoot> ();
		
		static Dictionary<string,AnalysisTypeExtensionNode> ruleInputTypes
			= new Dictionary<string, AnalysisTypeExtensionNode> ();
		
		static KeyedNodeList<string,FixHandlerExtensionNode> fixHandlers
			= new KeyedNodeList<string,FixHandlerExtensionNode> ();
		
		static AnalysisExtensions ()
		{
			AddinManager.AddExtensionNodeHandler (EXT_RULES, OnRuleNodeChanged);
			AddinManager.AddExtensionNodeHandler (EXT_TYPES, OnTypeNodeChanged);
			AddinManager.AddExtensionNodeHandler (EXT_FIX_HANDLERS, OnFixHandlerNodeChanged);
		}
		
		static void OnRuleNodeChanged (object sender, ExtensionNodeEventArgs args)
		{
			switch (args.Change) {
			case ExtensionChange.Add:
				AddRule (args.ExtensionNode);
				break;
			case ExtensionChange.Remove:
				RemoveRule (args.ExtensionNode);
				break;
			}
			analysisTreeCache.Clear ();
		}
		
		static void OnTypeNodeChanged (object sender, ExtensionNodeEventArgs args)
		{
			var node = (AnalysisTypeExtensionNode) args.ExtensionNode;
			switch (args.Change) {
			case ExtensionChange.Add:
				if (ruleInputTypes.ContainsKey (node.Name))
					throw new InvalidOperationException ("Duplicate analysis node type '" + node.Name + "' registered");
				ruleInputTypes[node.Name] = node;
				break;
			case ExtensionChange.Remove:
				ruleInputTypes.Remove (node.Name);
				break;
			}
			analysisTreeCache.Clear ();
		}
		
		static void OnFixHandlerNodeChanged (object sender, ExtensionNodeEventArgs args)
		{
			var node = (FixHandlerExtensionNode) args.ExtensionNode;
			switch (args.Change) {
			case ExtensionChange.Add:
				fixHandlers.Add (node.FixName, node);
				break;
			case ExtensionChange.Remove:
				fixHandlers.Remove (node.FixName, node);
				break;
			}
		}
		
		static void AddRule (ExtensionNode extNode)
		{
			if (extNode is CategoryNode) {
				foreach (ExtensionNode child in extNode.ChildNodes)
					AddRule (child);
				return;
			}
			
			var node = (AnalysisRuleAddinNode)extNode;
			rulesByInput.Add (node.Input, node);
		}
		
		static void RemoveRule (ExtensionNode extNode)
		{
			if (extNode is CategoryNode) {
				foreach (ExtensionNode child in extNode.ChildNodes)
					RemoveRule (child);
				return;
			}
			var node = (AnalysisRuleAddinNode)extNode;
			rulesByInput.Remove (node.Input, node);
		}
		
		internal static Type GetType (string name)
		{
			//this is hardcoded as the tree leaf node
			if (name == RuleTreeLeaf.TYPE)
				return typeof (IEnumerable<Result>);
			
			//throws if not present
			return ruleInputTypes[name].Type;
		}
		
		// Gets an analysis tree from the cache, or creates one.
		// Cache may have null value if there were no nodes for the type.
		public static RuleTreeRoot GetAnalysisTree (RuleTreeType treeType)
		{
			RuleTreeRoot tree;
			if (analysisTreeCache .TryGetValue (treeType, out tree))
				return tree;
			
			analysisTreeCache [treeType] = tree = BuildTree (treeType);

#if DEBUG_ANALYSIS_TREE
			if (tree != null)
				Console.WriteLine (tree.GetTreeStructure ());
#endif
			return tree;
		}
		
		static RuleTreeRoot BuildTree (RuleTreeType treeType)
		{
			var nodes = GetTreeNodes (treeType, treeType.Input, 0);
			if (nodes == null || nodes.Length == 0)
				return null;

			return new RuleTreeRoot (nodes, treeType);
		}

		//recursively builds the rule tree for branches that terminate in leaves (rules with result outputs)
		static IRuleTreeNode[] GetTreeNodes (RuleTreeType treeType, string input, int depth)
		{
			var addinNodes = rulesByInput.Get (input);
			if (addinNodes == null)
				return null;
	
			var validNodes = addinNodes.Where (n => n.Supports (treeType.FileExtension)).ToList ();
			if (validNodes.Count == 0)
				return null;
			
			var list = new List<IRuleTreeNode> ();
			
			foreach (var n in validNodes) {
				//leaf node, return directly
				if (n.Output == RuleTreeLeaf.TYPE) {
					list.Add (new RuleTreeLeaf (n));
					continue;
				}
	
				if (depth > 50)
					throw new InvalidOperationException ("Analysis tree too deep. Check for circular dependencies " +
						n.GetErrSource ());
	
				//get the nodes that will handle the output - by recursively calling this
				var childNodes = GetTreeNodes (treeType, n.Output, depth + 1);
	
				//if no child nodes are returned, don't even return this, because its output won't be used
				if (childNodes == null || childNodes.Length == 0)
					continue;
				
				list.Add (new RuleTreeBranch (childNodes, n));
			}
			
			return list.ToArray ();
		}
		
		public static IEnumerable<IFixHandler> GetFixHandlers (string fixType)
		{
			var nodes = fixHandlers.Get (fixType);
			if (nodes != null)
				return nodes.Select (node => node.FixHandler);
			return new IFixHandler[0];
		}
	}
	
	class KeyedNodeList<K,V>
	{
		Dictionary<K,List<V>> dict = new Dictionary<K,List<V>> ();
		
		public List<V> Get (K key)
		{
			List<V> list;
			dict.TryGetValue (key, out list);
			return list;
		}
		
		public void Remove (K key, V value)
		{
			List<V> list;
			if (!dict.TryGetValue (key, out list) || !list.Remove (value))
				throw new Exception ("Item missing");
			if (list.Count == 0)
				dict.Remove (key);
		}
		
		public void Add (K key, V value)
		{
			List<V> list;
			if (!dict.TryGetValue (key, out list))
				dict[key] = list = new List<V> ();
			list.Add (value);
		}
	}
}

