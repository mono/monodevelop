// 
// DockItemToolbar.cs
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

namespace MonoDevelop.Components.Docking
{
	public class DockItemToolbar
	{
		DockItem parentItem;
		DockPositionType position;
		bool empty = true;

		internal DockItemToolbar (DockItem parentItem, DockPositionType position)
		{
			this.parentItem = parentItem;
			this.position = position;
		}

		internal void SetStyle (DockVisualStyle style)
		{
		}

		public DockItem DockItem {
			get { return parentItem; }
		}
		
		public DockPositionType Position {
			get { return this.position; }
		}
		
		public void Add (Control widget)
		{
			Add (widget, false);
		}
		
		public void Add (Control widget, bool fill)
		{
			Add (widget, fill, -1);
		}
		
		public void Add (Control widget, bool fill, int padding)
		{
			Add (widget, fill, padding, -1);
		}
		
		void Add (Control widget, bool fill, int padding, int index)
		{
		}
		
		public void Insert (Control w, int index)
		{
			Add (w, false, 0, index);
		}
		
		public void Remove (Control widget)
		{
		}
		
		public bool Visible {
			get {
				return empty;
			}
			set {
			}
		}
		
		public bool Sensitive {
			get { return true; }
			set { ; }
		}
		
		public void ShowAll ()
		{
		}
		
		public Control[] Children {
			get { return new Control[0]; }
		}
	}
}

