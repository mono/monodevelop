 /* 
 * CategoryToolboxNode.cs - A ToolboxNode that represents a group of items
 * 
 * Authors: 
 *  Michael Hutchinson <m.j.hutchinson@gmail.com>
 *  
 * Copyright (C) 2006 Michael Hutchinson
 *
 * This sourcecode is licenced under The MIT License:
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit
 * persons to whom the Software is furnished to do so, subject to the
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
 * NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;
using Gtk;

namespace AspNetEdit.Gui.Toolbox
{
	public class CategoryToolboxNode: BaseToolboxNode
	{
		private ArrayList children = new ArrayList ();
		private string name;
		
		public CategoryToolboxNode (string name)
		{
			this.name = name;
		}
		
		public void Add (ItemToolboxNode child)
		{
			child.SetParent (this);
			children.Add (child);
			OnChildAdded (child);
		}
		
		public void Remove (ItemToolboxNode child)
		{
			int pos = children.IndexOf (child);
			
			if (pos < 0)
				throw new InvalidOperationException ("A node cannot be removed if it is not in the store.");
			
			children.Remove (child);
			child.SetParent (null);
			OnChildRemoved (child, pos);
		}
		
		public void Clear ()
		{
			for (int i = 0; i < children.Count; i++)
			{
				ItemToolboxNode child = (ItemToolboxNode) children[i];
				child.SetParent (null);
				OnChildRemoved (child, i);
			}
				
			children.Clear ();
		}
		
		//return true if the search is successful
		//should remove children if they fail test
		public override bool Filter (string keyword)
		{
			int target = children.Count;
			int pos = 0;
			
			while (pos < target)
			{
				if (!((ItemToolboxNode) children[pos]).Filter (keyword)) {
					Remove ((ItemToolboxNode) children[pos]);
					target --;
				}
				else {
					pos++;
				}
			}
			
			return (children.Count > 0);
		}
		
		#region Tree columns
		
		public override string Label {
			get { return name; }
		}
		
		public override int FontWeight {
			get { return 600; }
		}
		
	//	public override Gdk.Color BackgroundColour {
	//		get { return Gdk.Color.White; }
	//	}
		
		public override bool IconVisible {
			get { return false; }
		}
		
		public override bool ExpanderVisible {
			get { return true; }
		}
		
		public override bool CanDrag {
			get { return false; }
		}
		
		#endregion Tree columns
		
		#region ITreeNode Members
		
		public override int ChildCount {
			get { return children.Count ; }
		}
		
		public override ITreeNode this [int i] {
    		get { return (ItemToolboxNode) children[i]; }
    	}
    	
    	public override int IndexOf (object o)
    	{
    		return children.IndexOf (o);
    	}
		
		#endregion ITreeNode Members
	}
}
