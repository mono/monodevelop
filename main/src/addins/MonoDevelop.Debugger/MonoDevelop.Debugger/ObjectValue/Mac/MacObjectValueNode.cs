//
// MacObjectValueNode.cs
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
using System.Collections.Generic;

using AppKit;
using Foundation;

namespace MonoDevelop.Debugger
{
	/// <summary>
	/// NSObject wrapper for data items in the Cocoa implementation of the ObjectValueTreeView.
	/// </summary>
	class MacObjectValueNode : NSObject
	{
		public readonly List<MacObjectValueNode> Children = new List<MacObjectValueNode> ();
		public readonly MacObjectValueNode Parent;
		public readonly ObjectValueNode Target;
		public nfloat OptimalValueWidth;
		public NSFont OptimalValueFont;
		public nfloat OptimalNameWidth;
		public NSFont OptimalNameFont;
		public nfloat OptimalXOffset;
		public bool HideValueButton;

		public MacObjectValueNode (MacObjectValueNode parent, ObjectValueNode target)
		{
			OptimalValueWidth = -1.0f;
			OptimalNameWidth = -1.0f;
			OptimalValueFont = null;
			OptimalNameFont = null;
			OptimalXOffset = -1.0f;
			Parent = parent;
			Target = target;
		}

		public void Measure (MacObjectValueTreeView treeView)
		{
			if (OptimalXOffset < 0) {
				nfloat offset = 17.0f;
				var node = Target;

				while (!(node.Parent is RootObjectValueNode)) {
					node = node.Parent;
					offset += 16.0f;
				}

				OptimalXOffset = offset;
			}

			if (OptimalNameFont != treeView.CustomFont || OptimalNameWidth < 0) {
				OptimalNameWidth = MacDebuggerObjectNameView.GetOptimalWidth (treeView, Target);
				OptimalNameFont = treeView.CustomFont;
			}

			if (OptimalValueFont != treeView.CustomFont || OptimalValueWidth < 0) {
				OptimalValueWidth = MacDebuggerObjectValueView.GetOptimalWidth (treeView, Target, HideValueButton);
				OptimalValueFont = treeView.CustomFont;
			}
		}
	}
}
