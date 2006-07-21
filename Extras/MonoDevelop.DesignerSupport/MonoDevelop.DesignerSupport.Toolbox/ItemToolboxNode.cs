 /* 
 * ItemToolboxNode.cs - A base class for ToolboxNodes that represent a 
 *						selectable/usable item
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
	public abstract class ItemToolboxNode : BaseToolboxNode
	{
		//we expect to serialise/deserialise these
		//TODO: getters, setters
		//TODO: serialisation
		protected Gdk.Pixbuf Icon;
		public string Name = "";
		public string Category = "";
		public string Description = "";
		protected ICollection ItemFilters; //ToolboxItemFilterAttribute
		
		//need to be able to create empty instances of derived classes
		//for deserialisation
		public ItemToolboxNode ()
		{
		}
		
		#region Behaviours
		
		public override bool Filter (string keyword)
		{
			return ((Name==null)? false : (Name.ToLower ().IndexOf (keyword) >= 0))
				   || ((Description == null)? false : (Description.ToLower ().IndexOf (keyword) >= 0));
		}
		
		//Runs when item is clicked.
		public abstract void Activate (object host);
		
		public virtual bool IsSupportedBy (object rootDesigner)
		{
			//if something has no filters it is more useful and efficient
			//to show it for everything than to show it for nothing
			if (ItemFilters == null || ItemFilters.Count == 0)
				return true;
			
			//get the designer's filter attributes
			//TODO: This may be worth caching at some point
			Type desType = rootDesigner.GetType ();
			ToolboxItemFilterAttribute[] hostFilters =
				(ToolboxItemFilterAttribute[]) desType.GetCustomAttributes (typeof (ToolboxItemFilterAttribute), true);
			
			//check all of host's filters
			foreach (ToolboxItemFilterAttribute desFa in hostFilters)
			{
				if (!FilterPermitted (desFa, ItemFilters, rootDesigner))
					return false;
			}
			
			//check all of item's filters
			foreach (ToolboxItemFilterAttribute itemFa in ItemFilters)
			{
				if (!FilterPermitted (itemFa, hostFilters, rootDesigner))
					return false;
			}
			
			//we assume permitted, so only return false when blocked by a filter
			return true;
		}
		
		//evaluate a filter attribute against a list, and check whther permitted
		private bool FilterPermitted (ToolboxItemFilterAttribute desFa, ICollection filterAgainst, object rootDesigner)
		{
			switch (desFa.FilterType) {
				case ToolboxItemFilterType.Allow:
					//this is really for matching some other filter string against
					return true;
				
				case ToolboxItemFilterType.Custom:
					IToolboxUser tbUser = rootDesigner as IToolboxUser;
					if (tbUser == null)
						throw new ArgumentException ("Host's root designer does not support IToolboxUser interface.");
					return EvaluateCustomFilter (tbUser);
					
				case ToolboxItemFilterType.Prevent:
					//if host and toolboxitem have same filterstring, then not permitted
					foreach (ToolboxItemFilterAttribute itemFa in filterAgainst)
						if (desFa.Match (itemFa))
							return false;
					return true;
				
				case ToolboxItemFilterType.Require:
					//if host and toolboxitem have same filterstring, then permitted, unless one is prevented
					foreach (ToolboxItemFilterAttribute itemFa in filterAgainst)
						if (desFa.Match (itemFa) && (desFa.FilterType != ToolboxItemFilterType.Prevent))
							return true;
					return false;
			}
			throw new InvalidOperationException ("Unexpected ToolboxItemFilterType value.");
		}
		
		//utility, gets a root designer from an IDesignerHost and calls IsSupportedBy (object) on it
		public virtual bool IsSupportedBy (IDesignerHost host)
		{
			if (host == null)
				throw new ArgumentException ("IDesignerHost must not be null.");
			IComponent comp = host.RootComponent;
			if (comp == null)
				throw new ArgumentException ("Host does not have a root component.");
			IDesigner des = host.GetDesigner (comp);
			if (des == null)
				throw new ArgumentException ("Host does not have a root designer.");
			
			return IsSupportedBy (des);
		}
		
		//if derived class supports it, we have to load the real toolboxitem
		protected virtual bool EvaluateCustomFilter (IToolboxUser user)
		{
			throw new InvalidOperationException ("Custom filters are only supported with ToolboxItems.");
		}
		
		//TODO: drag source
		
		#endregion Behaviours
		
		
		#region Tree columns

		public override Gdk.Pixbuf ViewIcon {
			get {
				if (Icon != null)
					return Icon;
				else
					return base.ViewIcon;
			}
		}
		
		public override string Label {
			get { 
				return Name;
			}
		}
		
		#endregion Tree columns
		
		
		protected Gdk.Pixbuf ImageToPixbuf (System.Drawing.Image image)
		{
			using (System.IO.MemoryStream stream = new System.IO.MemoryStream ()) {
				image.Save (stream, System.Drawing.Imaging.ImageFormat.Png);
				stream.Position = 0;
				return new Gdk.Pixbuf (stream);
			}
		}
	}	
}
