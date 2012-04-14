// 
// RuleTreeNode.cs
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
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MonoDevelop.AnalysisCore.Extensions;
using System.Threading;

namespace MonoDevelop.AnalysisCore
{
	// This is a tree for holding the structure of rules, connected with matched inputs and outputs.
	// It yields Results from the tree's leaves via the intermediate nodes.
	interface IRuleTreeNode
	{
		IEnumerable<Result> Analyze (object input, CancellationToken cancellationToken);
	}

	sealed class RuleTreeLeaf : IRuleTreeNode
	{
		AnalysisRuleAddinNode rule;
		
		internal const string TYPE = "Results";
	
		public RuleTreeLeaf (AnalysisRuleAddinNode rule)
		{
			this.rule = rule;
			Debug.Assert (rule != null);
			Debug.Assert (rule.Output == TYPE);
		}
		
		public AnalysisRuleAddinNode Rule { get { return rule; } }
		
		//re-use to reduce pointless allocations
		public static Result[] Empty = new Result[0];
		
		public IEnumerable<Result> Analyze (object input, CancellationToken cancellationToken)
		{
			//we construct the tree such that only a rule with an output of Result can be a leaf
			//therefore its result must be castable to IEnumerable<Result>.
			var results = (IEnumerable<Result>)rule.Analyze (input, cancellationToken);
			if (results == null)
				return Empty;
			
			//tag all the results with the source rule
			foreach (var result in results)
				result.Source = rule;
			
			return results;
		}
	}
	
	sealed class RuleTreeBranch : IRuleTreeNode
	{
		AnalysisRuleAddinNode rule;
		IRuleTreeNode[] children;
		
		public RuleTreeBranch (IRuleTreeNode[] children, AnalysisRuleAddinNode rule)
		{
			this.rule = rule;
			this.children = children;
			Debug.Assert (rule != null);
			Debug.Assert (children != null && children.Length > 0);
			Debug.Assert (children.All (c =>
				   (c is RuleTreeLeaf && ((RuleTreeLeaf)c).Rule.Input == rule.Output)
				|| (c is RuleTreeBranch && ((RuleTreeBranch)c).Rule.Input == rule.Output)
			));
		}
		
		public AnalysisRuleAddinNode Rule { get { return rule; } }
		public IRuleTreeNode[] Children { get { return children; } }
	
		// This handles walking the tree - running this node's rule, and collecting results from child nodes
		public IEnumerable<Result> Analyze (object input, CancellationToken cancellationToken = default (CancellationToken))
		{
			// It's not a leaf node, so it has children which all expect the object output by this rule
			var intermediate = rule.Analyze (input, cancellationToken);
	
			// Rules may return null, in which case there is no point running their children
			// This allows "adaptor" rules, which can check an input's type or state to choose whether to return an output 
			if (intermediate == null)
				return RuleTreeLeaf.Empty;
	
			//collect the results from all the children
			//TODO: this could be parallelized trivially, since the tree nodes and the rules do not have any state
			return children.SelectMany (child => child.Analyze (intermediate, cancellationToken));
		}
	}
	
	sealed class RuleTreeRoot
	{
		IRuleTreeNode[] children;
		RuleTreeType treeType;
		
		public RuleTreeRoot (IRuleTreeNode[] children, RuleTreeType treeType)
		{
			this.children = children;
			this.treeType = treeType;
			Debug.Assert (treeType != null);
			Debug.Assert (children != null && children.Length > 0);
			Debug.Assert (children.All (c =>
				   (c is RuleTreeLeaf && ((RuleTreeLeaf)c).Rule.Input == treeType.Input)
				|| (c is RuleTreeBranch && ((RuleTreeBranch)c).Rule.Input == treeType.Input)
			));
		}
		
		public RuleTreeType TreeType { get { return treeType; } }
		
		public IEnumerable<Result> Analyze (object input, CancellationToken cancellationToken)
		{
			return children.SelectMany (child => child.Analyze (input, cancellationToken));
		}
		
		public string GetTreeStructure ()
		{
			var builder = new StringBuilder ();
			builder.AppendFormat ("[AnalysisTree (Input='{0}', Extension='{1}')\n", treeType.Input, treeType.FileExtension);
			PrintTreeStructure (builder, children, "  ");
			builder.Append ("]\n");
			return builder.ToString ();
		}
		
		void PrintTreeStructure (StringBuilder builder, IRuleTreeNode[] children, string indent)
		{
			foreach (var c in children) {
				builder.Append (indent);
				
				var leaf = c as RuleTreeLeaf;
				if (leaf != null) {
					builder.AppendFormat ("[Leaf (Rule='{0}')]\n", leaf.Rule.FuncName);
					continue;
				}
				
				var branch = (RuleTreeBranch) c;
				builder.AppendFormat ("[Branch (Output='{0}',Rule='{1}')\n", branch.Rule.Output, branch.Rule.FuncName);
				
				PrintTreeStructure (builder, branch.Children, indent + "  ");
				
				builder.Append (indent);
				builder.Append ("]\n");
			}
		}
	}
	
	// Type of the analysis tree. Basically a key for the analysis tree cache.
	public class RuleTreeType
	{
		string input, fileExtension;
		
		public string Input { get { return input; } }
		public string FileExtension { get { return fileExtension; } }
		
		public RuleTreeType (string input, string fileExtension)
		{
			Debug.Assert (!string.IsNullOrEmpty (input));
			Debug.Assert (!string.IsNullOrEmpty (fileExtension));
			
			this.input = input;
			this.fileExtension = fileExtension;
		}
		
		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;
			if (ReferenceEquals (this, obj))
				return true;
			var other = obj as RuleTreeType;
			return other != null && input == other.input && fileExtension == other.fileExtension;
		}

		public override int GetHashCode ()
		{
			unchecked {
				return (input != null ? input.GetHashCode () : 0)
					^ (fileExtension != null ? fileExtension.GetHashCode () : 0);
			}
		}
	}
}