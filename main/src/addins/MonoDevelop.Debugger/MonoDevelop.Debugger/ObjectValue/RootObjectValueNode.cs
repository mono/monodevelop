//
// RootObjectValueNode.cs
//
// Author:
//       Jeffrey Stedfast <jestedfa@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corp.
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
using System.Diagnostics;
using System.Collections.Generic;

namespace MonoDevelop.Debugger
{
	/// <summary>
	/// Special node used as the root of the treeview. 
	/// </summary>
	sealed class RootObjectValueNode : ObjectValueNode, ISupportChildObjectValueNodeReplacement
	{
		public RootObjectValueNode () : base (string.Empty)
		{
			IsExpanded = true;
		}

		public override bool HasChildren => true;

		public void AddValue (ObjectValueNode value)
		{
			AddChild (value);
		}

		public void AddValues (IEnumerable<ObjectValueNode> values)
		{
			AddChildren (values);
		}

		public void RemoveValueAt (int index)
		{
			RemoveChildAt (index);
		}

		public void ReplaceValueAt (int index, ObjectValueNode value)
		{
			ReplaceChildAt (index, value);
		}

		void ISupportChildObjectValueNodeReplacement.ReplaceChildNode (ObjectValueNode node, ObjectValueNode[] newNodes)
		{
			var index = Children.IndexOf (node);

			Debug.Assert (index >= 0, "The node being replaced should be a child of this node");

			if (newNodes.Length == 0) {
				RemoveChildAt (index);
				return;
			}

			ReplaceChildAt (index, newNodes [0]);

			for (int i = 1; i < newNodes.Length; i++)
				InsertChildAt (++index, newNodes[i]);
		}
	}
}
