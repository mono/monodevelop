//
// IIssueTreeNode.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2013 Simon Lindgren
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
using System.Collections;
using System.Collections.Generic;

namespace MonoDevelop.CodeIssues
{
	public interface IIssueTreeNode
	{
		/// <summary>
		/// The text that should be displayed in the ui.
		/// </summary>
		/// <value>The text.</value>
		string Text { get; }
		
		/// <summary>
		/// The children of this instance.
		/// </summary>
		/// <value>The children.</value>
		ICollection<IIssueTreeNode> Children { get; }

		/// <summary>
		/// Indicates whether this instance has any children.
		/// </summary>
		/// <value><c>true</c> if this instance has children; otherwise, <c>false</c>.</value>
		bool HasVisibleChildren {
			get;
		}

		/// <summary>
		/// Indicates whether this node should be shown in the ui.
		/// </summary>
		/// <value><c>true</c> if the current instance should be shown; otherwise, <c>false</c>.</value>
		bool Visible { get; set; }

		/// <summary>
		/// Gets all children including nested children of this instance.
		/// </summary>
		/// <value>All children.</value>
		ICollection<IIssueTreeNode> AllChildren { get; }
		
		/// <summary>
		/// Occurs when children of this node are invalidated.
		/// </summary>
		event EventHandler<IssueGroupEventArgs> ChildrenInvalidated;
		
		/// <summary>
		/// Occurs when children of this node are invalidated.
		/// </summary>
		event EventHandler<IssueTreeNodeEventArgs> ChildAdded;
		
		/// <summary>
		/// Occurs when <see cref="Text"/> is updated.
		/// </summary>
		event EventHandler<IssueGroupEventArgs> TextChanged;
		
		/// <summary>
		/// Occurs when <see cref="Visible"/> is updated.
		/// </summary>
		event EventHandler<IssueGroupEventArgs> VisibleChanged;
	}
}

