 /* 
 * ToolboxItemToolboxNode.cs - A ToolboxNode that wraps a System.Drawing.Design.ToolboxItem
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
using System.IO;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.Runtime.Serialization.Formatters.Binary;

using MonoDevelop.Projects.Serialization;
using MonoDevelop.Core;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	[Serializable]
	public class ToolboxItemToolboxNode : TypeToolboxNode
	{
		[ItemProperty ("itemcontents")]
		string serializedToolboxItem;
		
		[ItemProperty ("itemtype")]
		TypeReference toolboxItemType;
		
		public ToolboxItemToolboxNode (ToolboxItem item)
			: base (item.TypeName, item.AssemblyName.FullName)
		{
			base.Name = item.DisplayName;
			if (item.Bitmap != null)
				base.Icon = ImageToPixbuf (item.Bitmap);
			
			foreach (ToolboxItemFilterAttribute tbfa in item.Filter)
				base.ItemFilters.Add (tbfa);
			
			//we only need to serialise the ToolboxItem if it is non-standard, because we can reliably recreate the two built-in types
			if (item.GetType () == typeof (ToolboxItem))
				toolboxItemType = null; //no-op, but this has consequences 	
			else if (item.GetType () == typeof (System.Web.UI.Design.WebControlToolboxItem))
				toolboxItemType = new TypeReference (typeof (System.Web.UI.Design.WebControlToolboxItem));
			else {
				serializedToolboxItem = SerializeToolboxItem (item);
				toolboxItemType = new TypeReference (item.GetType ());
			} 
		}
		
		//ToolboxItems don't handle assembly locations, so hack around this
		public ToolboxItemToolboxNode (ToolboxItem item, string assemblyLocation)
		  : base (item.TypeName, item.AssemblyName.FullName)
		{
			base.Type.AssemblyLocation = assemblyLocation;
		}	
		
		//for deserialisation
		public ToolboxItemToolboxNode ()
		{
		}
		
		public override bool Equals (object obj)
		{
			ToolboxItemToolboxNode other = obj as ToolboxItemToolboxNode;
			return (other != null)
			    && (this.toolboxItemType == null?
				    other.toolboxItemType == null
				    : this.toolboxItemType.Equals (other.toolboxItemType))
			    && base.Equals (other);
		}
		
		public override int GetHashCode ()
		{
			int code = base.GetHashCode ();
			if (toolboxItemType != null)
				code += toolboxItemType.GetHashCode ();
			return code;
		}
		
		public ToolboxItem GetToolboxItem ()
		{
			//get the type of the toolboxitem, and make sure it's loaded
			Type tbiType = typeof (ToolboxItem);
			if ((toolboxItemType != null) && (!string.IsNullOrEmpty (toolboxItemType.TypeName)))
				tbiType = toolboxItemType.Load ();
			
			if ((serializedToolboxItem != null) && (serializedToolboxItem.Length > 0))
				return DeserializeToolboxItem (serializedToolboxItem);
			
			//built-in type, no need to deserialise; we can recreate
			Type clsType = base.Type.Load ();
			ToolboxItem item = (ToolboxItem) Activator.CreateInstance (tbiType, new object[] {clsType});
			return item;
		}
		
		#region private utility methods
		
		ToolboxItem DeserializeToolboxItem (string serializedObject)
		{
			byte[] bytes = Convert.FromBase64String (serializedObject);
			
			MemoryStream ms = new MemoryStream(bytes);
			BinaryFormatter BF = new BinaryFormatter ();
			
			object obj = BF.Deserialize (ms);
   			ms.Close ();
   			
   			if (! (obj is ToolboxItem))
   				throw new Exception ("Could not deserialise ToolboxItem for " + base.Name);
   			
   			return (ToolboxItem) obj;
		}
		
		string SerializeToolboxItem (ToolboxItem toolboxItem)
		{
			MemoryStream ms = new MemoryStream ();
			BinaryFormatter BF = new BinaryFormatter ();
			
			BF.Serialize (ms, toolboxItem);
			byte[] bytes = ms.ToArray ();
			
			
   			ms.Close ();
   			return Convert.ToBase64String(bytes);
		}
		
		public override string ItemDomain {
			get { return GettextCatalog.GetString ("Web and Windows Forms Components"); }
		}
		
		#endregion
	}	
}
