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
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;
using Gtk;

using MonoDevelop.Core.Serialization;
using MonoDevelop.Core;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	[DataItem (Name = "item", FallbackType = typeof(UnknownToolboxNode))]
	[Serializable]
	public abstract class ItemToolboxNode : ICustomDataItem, IComparable, IComparable<ItemToolboxNode>
	{
		[NonSerialized]
		Gdk.Pixbuf icon;
		
		[ItemProperty ("name")]
		string name = "";
		
		[ItemProperty ("category")]
		string category = "";
		
		[ItemProperty ("description")]
		string description = "";
		
		List <ToolboxItemFilterAttribute> itemFilters = new List <ToolboxItemFilterAttribute> ();
		
		//need to be able to create empty instances of derived classes
		//for deserialisation
		public ItemToolboxNode ()
		{
		}
		
		#region properties
		
		[Browsable(false)]
		public virtual Gdk.Pixbuf Icon {
			get { return icon; }
			set { icon = value; }
		}
		
		public virtual string Name {
			get { return name; }
			set { name = value; }
		}
		
		[ReadOnlyAttribute (true)]
		public virtual string Category {
			get { return category; }
			set { category = value; }
		}
		
		public virtual string Description {
			get { return description; }
			set { description = value; }
		}
		
		[Browsable(false)]
		public virtual IList<ToolboxItemFilterAttribute> ItemFilters {
			get { return itemFilters; }
		}
		
		[Browsable(false)]
		public virtual string ItemDomain {
			get { return GettextCatalog.GetString ("Unknown"); }
		}
		
		#endregion
		
		#region Behaviours
		
		public virtual bool Filter (string keyword)
		{
			return ((Name==null)? false :
			        (Name.ToLower ().IndexOf (keyword, StringComparison.InvariantCultureIgnoreCase) >= 0))
			    || ((Description == null)? false :
				(Description.ToLower ().IndexOf (keyword, StringComparison.InvariantCultureIgnoreCase) >= 0));
		}
		
		public virtual bool Equals (object o)
		{
			ItemToolboxNode node = o as ItemToolboxNode;
			return (node != null) && (node.Name == this.Name) && (node.Category == this.Category) && (node.Description == this.Description);
		}
		
		public virtual int GetHashCode ()
		{
			return (string.Empty + Name + Category + Description).GetHashCode ();
		}
		
		public int CompareTo (object other)
		{
			return CompareTo (other as ItemToolboxNode);
		}
		
		public virtual int CompareTo (ItemToolboxNode other)
		{
			if (other == null) return -1;
			if (Category == other.Category)
				return Name.CompareTo (other.Name);
			else
				return Category.CompareTo (other.Category);
		}
		
		#endregion Behaviours
		
		
		protected Gdk.Pixbuf ImageToPixbuf (System.Drawing.Image image)
		{
			using (System.IO.MemoryStream stream = new System.IO.MemoryStream ()) {
				image.Save (stream, System.Drawing.Imaging.ImageFormat.Png);
				stream.Position = 0;
				return new Gdk.Pixbuf (stream);
			}
		}
		
		#region custom serialisation for ToolboxItemFilterAttribute collection
		
		public DataCollection Serialize (ITypeSerializer handler)
		{
			DataCollection dc = handler.Serialize (this);
			
			dc.Extract ("filters");
			dc.Extract ("icon");
			
			DataItem filtersItem = new DataItem ();
			filtersItem.Name = "filters";
			dc.Add (filtersItem);
			
			foreach (ToolboxItemFilterAttribute tbfa in itemFilters) {
				DataItem item = new DataItem ();
				item.Name = "filter";
				item.ItemData.Add (new DataValue ("string", tbfa.FilterString));
				item.ItemData.Add (new DataValue ("type", System.Enum.GetName (typeof (ToolboxItemFilterType), tbfa.FilterType)));
				filtersItem.ItemData.Add (item);
			}
			
			if (icon !=  null) {
				DataItem item = new DataItem ();
				item.Name = "icon";
				dc.Add (item);
				string iconString = Convert.ToBase64String (icon.SaveToBuffer ("png"));
				item.ItemData.Add (new DataValue ("enc", iconString));
			}
			
			return dc;
		}
		
		public void Deserialize (ITypeSerializer handler, DataCollection data)
		{
			DataItem filtersItem = data.Extract ("filters") as DataItem;
			if ((filtersItem != null) && (filtersItem.HasItemData)) {
				foreach (DataItem item in filtersItem.ItemData) {
					string filter = ((DataValue) item.ItemData ["string"]).Value;
					string typeString = ((DataValue) item.ItemData ["type"]).Value;
					ToolboxItemFilterType type = (ToolboxItemFilterType) Enum.Parse (typeof (ToolboxItemFilterType), typeString);
					
					itemFilters.Add (new ToolboxItemFilterAttribute (filter, type));
				}
			}
			
			DataItem iconItem = data.Extract ("icon") as DataItem;
			if (iconItem != null) {
				DataValue iconData = (DataValue) iconItem ["enc"];
				this.icon = new Gdk.Pixbuf (Convert.FromBase64String(iconData.Value));
			}
			
			handler.Deserialize (this, data);
		}
		
		#endregion
	}
}
