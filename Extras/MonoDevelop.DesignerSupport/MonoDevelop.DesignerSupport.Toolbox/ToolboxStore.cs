 /* 
 * ToolboxItemStore.cs - A Gtk.NodeStore for ToolboxItems
 * 
 * Authors: 
 *  Michael Hutchinson <m.j.hutchinson@gmail.com>
 *  
 * Copyright (C) 2005 Michael Hutchinson
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

using Gtk;
using System.Collections;
using System.Drawing.Design;

namespace AspNetEdit.Gui.Toolbox
{
	internal class ToolboxStore : Gtk.NodeStore
	{
		private ArrayList innerStore = new ArrayList ();
		
		public ToolboxStore () : base (typeof (BaseToolboxNode))
		{
		}
		
		public void SetNodes (ICollection nodes)
		{
			innerStore.Clear ();
			innerStore.AddRange (nodes);
			Build ();
		}
		
		#region Categorisation, filtering, sorting
		
		private bool categorised = false;
		private string filter = "";
		
		public bool Categorised
		{
			get { return categorised; }
		}
		
		public void SetCategorised (bool categorised)
		{
			if (this.categorised == categorised) return;
			
			this.categorised = categorised;
			Build ();
		}
		
		public string Filter
		{
			get { return filter; }
		}
		
		//removes child nodes if they do not match filter string
		public void SetFilter (string keyword)
		{
			string oldfilter = filter;
			filter = (keyword == null)? "" : keyword.ToLower ();
			
			//run filtering on existing nodes if it's a more vicious filter
			if (filter.IndexOf (oldfilter) >= 0) {
				ArrayList toRemove = new ArrayList ();
				foreach (BaseToolboxNode node in this)
					if (!node.Filter (filter))
						toRemove.Add (node);
				//have to remove nodes outside of foreach, or we break things
				foreach (BaseToolboxNode node in toRemove)
					base.RemoveNode (node);
			}
			//different or shorter filter, so may have to add nodes - rebuild
			else
				Build ();
		}
			
		private void Build ()
		{
			base.Clear ();
			Hashtable categories = categorised? new Hashtable () : null; 
			
			for (int i = 0; i < innerStore.Count; i++) {
				ItemToolboxNode node = (ItemToolboxNode) innerStore [i];
				node.SetParent (null);
				
				if (!node.Filter (filter)) continue;
				
				if (categorised) {
					string cat = node.Category;
					if (cat.Length < 1) cat = "Miscellaneous";
					
					if (!categories.ContainsKey (cat))
						categories.Add (cat, new CategoryToolboxNode (cat));
					
					((CategoryToolboxNode) categories[cat] ).Add (node);
				}
				else
					base.AddNode (node);
			}
			
			if (categorised) {
				ArrayList arrList = new ArrayList (categories.Values);
				arrList.Sort (new SortByName ());
				
				for (int i = 0; i < arrList.Count; i++)
					base.AddNode ((CategoryToolboxNode) arrList [i]);
			}
		}
		
    	private class SortByName : IComparer
		{
			public int Compare (object x, object y)
			{
				return ((BaseToolboxNode) y).Label.CompareTo (((BaseToolboxNode) x).Label);
			}
		}
		
   		#endregion
				
		public enum Columns {
			Icon = 0,
			Label = 1,
			FontWeight = 2,
			BackgroundColour = 3,
			IconVisible = 4,
			ExpanderVisible = 5,
			CanDrag = 6
		}
	}
}

 
