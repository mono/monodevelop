//
// TreeNode.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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
using System.Collections;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Gui.Components.Internal
{
/*
  TransactedTreeBuilder is a ITreeBuilder which does not directly modify the tree, but instead
  it stores all changes in a special node store and. Those changes can be later applied all
  together to the tree.
 */

	internal class TreeNode
	{
		public NodeInfo NodeInfo;

		public bool Selected;
		public bool Filled;
		public bool Expanded;
		public TypeNodeBuilder TypeNodeBuilder;
		public TreeNode Parent;
		public object DataItem;
		public string Name;
		public List<TreeNode> Children;
		public NodePosition NodePosition;
		public NodeBuilder[] BuilderChain;
		public bool Deleted;
		public bool Modified;
		public bool Reset;
		public bool DeleteDone;
		public bool ChildrenDeleted;

		public bool HasPosition {
			get { return NodePosition != null && NodePosition.IsValid; }
		}
	}
	
}
