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

namespace MonoDevelop.AnalysisCore
{
	internal static class AnalysisExtensions
	{
		//TODO: this will have to be built and updated from the addin extension point events
		static Dictionary<string,List<AnalysisRuleAddinNode>> rulesByInput
			= new Dictionary<string, List<AnalysisRuleAddinNode>> ();
		
		// This is a re-usable cache of computed trees. it will need to be flushed nodesByInput is cached.
		// We should probably clean it via a LRU too.
		static Dictionary<NodeTreeType,RuleTreeRoot> analysisTreeCache
			= new Dictionary<NodeTreeType, RuleTreeRoot> ();
		
		static Dictionary<string,AnalysisTypeExtensionNode> ruleInputTypes
			= new Dictionary<string, AnalysisTypeExtensionNode> ();
		
		static AnalysisExtensions ()
		{
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/AnalysisCore/Rules", OnRuleNodeChanged);
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/AnalysisCore/Types", OnTypeNodeChanged);
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
			switch (args.Change) {
			case ExtensionChange.Add:
				AddType ((AnalysisTypeExtensionNode) args.ExtensionNode);
				break;
			case ExtensionChange.Remove:
				AddType ((AnalysisTypeExtensionNode) args.ExtensionNode);
				break;
			}
			analysisTreeCache.Clear ();
		}
		
		static void AddRule (ExtensionNode extNode)
		{
			if (extNode is CategoryNode) {
				foreach (ExtensionNode child in extNode.ChildNodes)
					AddRule (child);
				return;
			}
			
			var node = (AnalysisRuleAddinNode)extNode;
			
			List<AnalysisRuleAddinNode> list;
			if (!rulesByInput.TryGetValue (node.Input, out list))
				list = rulesByInput[node.Input] = new List<AnalysisRuleAddinNode> ();
			
			list.Add (node);
		}
		
		static void RemoveRule (ExtensionNode extNode)
		{
			if (extNode is CategoryNode) {
				foreach (ExtensionNode child in extNode.ChildNodes)
					AddRule (child);
				return;
			}
			
			var node = (AnalysisRuleAddinNode)extNode;
			List<AnalysisRuleAddinNode> list;
			if (!rulesByInput.TryGetValue (node.Input, out list))
				return;
			list.Remove (node);
			if (list.Count == 0)
				rulesByInput.Remove (node.Input);
		}
		
		static void AddType (AnalysisTypeExtensionNode node)
		{
			if (ruleInputTypes.ContainsKey (node.Name))
				throw new InvalidOperationException ("Duplicate analysis node type '" + node.Name + "' registered");
			ruleInputTypes[node.Name] = node;
		}
		
		static void RemoveType (AnalysisTypeExtensionNode node)
		{
			ruleInputTypes.Remove (node.Name);
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
		public static RuleTreeRoot GetAnalysisTree (NodeTreeType treeType)
		{
			RuleTreeRoot tree;
			if (analysisTreeCache.TryGetValue (treeType, out tree))
				return tree;
			
			analysisTreeCache [treeType] = tree = BuildTree (treeType);

#if DEBUG
			if (tree != null)
				Console.WriteLine (tree.GetTreeStructure ());
#endif
			return tree;
		}
		
		static RuleTreeRoot BuildTree (NodeTreeType treeType)
		{
			var nodes = GetTreeNodes (treeType, treeType.Input, 0);
			if (nodes == null || nodes.Length == 0)
				return null;

			return new RuleTreeRoot (nodes, treeType);
		}

		//recursively builds the rule tree for branches that terminate in leaves (rules with result outputs)
		static IRuleTreeNode[] GetTreeNodes (NodeTreeType treeType, string input, int depth)
		{
			List<AnalysisRuleAddinNode> addinNodes;
			if (!rulesByInput.TryGetValue (input, out addinNodes))
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
	}
}

