//
// IObjectValueTreeView.cs
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

namespace MonoDevelop.Debugger
{
	/// <summary>
	/// Defines the interface to the view that ObjectValueTreeViewController can interact with
	/// </summary>
	public interface IObjectValueTreeView
	{
		/// <summary>
		/// Gets a value indicating whether the user should be able to edit values in the tree
		/// </summary>
		bool AllowEditing { get; set; }

		/// <summary>
		/// Gets a value indicating whether or not the user should be able to expand nodes in the tree
		/// </summary>
		bool AllowExpanding { get; set; }

		/// <summary>
		/// Gets a value indicating whether the user should be able to add watch expressions to the tree
		/// </summary>
		bool AllowWatchExpressions { get; set; }

		/// <summary>
		/// Reloads the tree from the root node
		/// </summary>
		void Reload (ObjectValueNode root);

		/// <summary>
		/// Informs the view to load the children of the given node. startIndex and count may specify a range of
		/// the children of the node to load (for when children are being paged in from an enumerable for example).
		/// </summary>
		void LoadNodeChildren (ObjectValueNode node, int startIndex, int count);

		/// <summary>
		/// Informs the view to load the new values into the given node, optionally replacing that node with
		/// the set of replacement nodes. Handles the case where, for example, the "locals" is replaced
		/// with the set of local values
		/// </summary>
		void LoadEvaluatedNode (ObjectValueNode node, ObjectValueNode [] replacementNodes);



		event EventHandler<ObjectValueNodeEventArgs> NodeExpanded;
		event EventHandler<ObjectValueNodeEventArgs> NodeCollapsed;

		void OnNodeExpanded (ObjectValueNode node);
	}
}
