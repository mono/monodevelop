 /* 
 * BaseToolboxNode.cs - Base class for holding tree in a NodeStore, with columns for TreeView.
 *						Shared functionality for ItemToolboxNodes and CategoryToolboxNodes.
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
using System.Drawing.Design;
using Gtk;

namespace AspNetEdit.Gui.Toolbox
{
	public abstract class BaseToolboxNode : Gtk.ITreeNode
	{
		private static Gdk.Pixbuf _defaultIcon;
		private static Gdk.Color _defaultColor;
		private static int nextId = int.MinValue;
		private int id;
		private ITreeNode parent = null;
		
		public BaseToolboxNode ()
		{
			id = nextId;
			nextId++;
			if (nextId == int.MaxValue)
				throw new InvalidOperationException ("Have run out of integer indices for ToolboxNodes");
		}
		
		
		private Gdk.Pixbuf defaultIcon {
			get {
				if (_defaultIcon == null) {
					Gtk.Label lab = new Gtk.Label ();
					lab.EnsureStyle ();
					_defaultIcon = lab.RenderIcon (Stock.MissingImage, IconSize.SmallToolbar, string.Empty);
					lab.Destroy ();
				}
				return _defaultIcon;
			}
		}
		
		private Gdk.Color defaultColor {
			get {
				if (_defaultIcon == null) {
					Gtk.Label lab = new Gtk.Label ();
					lab.EnsureStyle ();
					_defaultColor = lab.Style.Base (Gtk.StateType.Normal);
					lab.Destroy ();
				}
				return _defaultColor;
			}
		}

		//return true if the search is successful
		//should remove children if they fail test
		public abstract bool Filter (string keyword);
		
		#region Tree columns

		[TreeNodeValue (Column=0)]
		public virtual Gdk.Pixbuf ViewIcon {
			get { return defaultIcon; }
		}
		
		[TreeNodeValue (Column=1)]
		public abstract string Label {
			get;
		}
		
		[TreeNodeValue (Column=2)]
		public virtual int FontWeight {
			get { return 400; }
		}
		
		[TreeNodeValue (Column=3)]
		public virtual Gdk.Color BackgroundColour {
			get { return defaultColor; }
		}
		
		[TreeNodeValue (Column=4)]
		public virtual bool IconVisible {
			get { return true; }
		}
		
		[TreeNodeValue (Column=5)]
		public virtual bool ExpanderVisible {
			get { return false; }
		}
		
		[TreeNodeValue (Column=6)]
		public virtual bool CanDrag {
			get { return true; }
		}
		
		#endregion Tree columns
		
		#region ITreeNode Members
		
		public virtual int ChildCount {
			get { return 0 ; }
		}
		
		public virtual int ID {
			get { return id; }
		}
		
    	public virtual ITreeNode this [int i] {
    		get { throw new System.IndexOutOfRangeException (); }
    	}
    	
    	public ITreeNode Parent {
   	 		get { return parent; }
    	}
    	
    	internal void SetParent (ITreeNode parent)
    	{
    		this.parent = parent;
    	}
    	
    	public virtual int IndexOf (object o)
    	{
    		return -1;
    	}
    	
    	public event System.EventHandler Changed;
    	public event TreeNodeAddedHandler ChildAdded;
		public event TreeNodeRemovedHandler ChildRemoved;
		
		protected void OnChanged ()
		{
			if (Changed != null)
				Changed (this, new EventArgs ());
		}
		
		protected void OnChildAdded (ITreeNode node)
		{
			if (ChildAdded != null)
				ChildAdded (this, node);
		}
		
		protected void OnChildRemoved (ITreeNode node, int oldPosition)
		{
			if (ChildRemoved != null)
				ChildRemoved (this, node, oldPosition);
		}
		
		#endregion ITreeNode Members
	}
}
