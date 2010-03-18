// 
// LayoutExtensionNode.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using Mono.Addins;
using MonoDevelop.Components.Docking;

namespace MonoDevelop.Ide.Extensions
{
	[ExtensionNodeChild (typeof(LayoutPadExtensionNode), "LayoutPad")]
	class LayoutExtensionNode: ExtensionNode
	{
		[NodeAttribute (Required=true)]
		protected string _name;
		
		public string Name {
			get { return _name; }
		}
	}
	
	class LayoutPadExtensionNode: ExtensionNode
	{
		[NodeAttribute]
		protected string placement;
		
		[NodeAttribute]
		protected DockItemStatus status = (DockItemStatus) (-1);
		
		public string Placement {
			get { return this.placement; }
		}

		public DockItemStatus Status {
			get { return this.status; }
		}
		
		public bool StatusSet {
			get { return status != (DockItemStatus) (-1); }
		}
	}
}

