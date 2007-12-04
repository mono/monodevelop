 /* 
 * ToolboxService.cs - used to add/remove/find/select toolbox items
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

using System;
using System.Drawing.Design;
using System.Collections;
using System.IO;
using System.Xml;
using System.Reflection;
using System.ComponentModel;
using System.Runtime.Serialization.Formatters.Binary;

namespace AspNetEdit.Editor.ComponentModel
{
	
	public class ToolboxService : IToolboxService
	{
		Hashtable categories = new Hashtable ();
		private string selectedCategory;
		private ToolboxItem selectedItem = null;
		
		public event EventHandler ToolboxChanged;
		
		protected void OnToolboxChanged ()
		{
			ToolboxChanged (this, new EventArgs ());
		} 

		#region IToolboxService Members

		public void AddCreator (ToolboxItemCreatorCallback creator, string format, System.ComponentModel.Design.IDesignerHost host)
		{
			throw new NotImplementedException ();
		}

		public void AddCreator (ToolboxItemCreatorCallback creator, string format)
		{
			throw new NotImplementedException ();
		}

		public void AddLinkedToolboxItem (ToolboxItem toolboxItem, string category, System.ComponentModel.Design.IDesignerHost host)
		{
			throw new NotImplementedException ();
		}

		public void AddLinkedToolboxItem (ToolboxItem toolboxItem, System.ComponentModel.Design.IDesignerHost host)
		{
			throw new NotImplementedException ();
		}

		public void AddToolboxItem (ToolboxItem toolboxItem)
		{
			AddToolboxItem (toolboxItem, "General");
		}

		public void AddToolboxItem (ToolboxItem toolboxItem, string category)
		{
			if (!categories.ContainsKey (category))
				categories[category] = new ArrayList ();
			
			System.Diagnostics.Trace.WriteLine ("Adding ToolboxItem: " + toolboxItem.DisplayName + ", " + category);
			((ArrayList) categories[category]).Add (toolboxItem);
		}

		public CategoryNameCollection CategoryNames
		{
			get {
				string[] cats = new string[categories.Keys.Count];
				categories.Keys.CopyTo (cats, 0);
				return new CategoryNameCollection (cats);
			}
		}

		public ToolboxItem DeserializeToolboxItem(object serializedObject, System.ComponentModel.Design.IDesignerHost host)
		{
			throw new NotImplementedException ();
		}

		public ToolboxItem DeserializeToolboxItem (object serializedObject)
		{		
			if ( !(serializedObject is byte[])) return null;
			
			MemoryStream ms = new MemoryStream( (byte[]) serializedObject);
			object obj = BF.Deserialize (ms);
   			ms.Close ();
   			
   			if (! (obj is ToolboxItem)) return null;
   			
   			return (ToolboxItem) obj;
		}

		public ToolboxItem GetSelectedToolboxItem (System.ComponentModel.Design.IDesignerHost host)
		{
			IToolboxUser toolboxUser = (IToolboxUser) host.GetDesigner (host.RootComponent);
			if (toolboxUser.GetToolSupported (selectedItem))
				return selectedItem;
			else
				return null;
		}

		public ToolboxItem GetSelectedToolboxItem ()
		{
			return selectedItem;
		}

		public ToolboxItemCollection GetToolboxItems (string category, System.ComponentModel.Design.IDesignerHost host)
		{
			if (!categories.ContainsKey (category))
				return null;

			ArrayList tools = new ArrayList ();

			foreach(ToolboxItem tool in ((ArrayList) categories[category]))
				if (((IToolboxUser) host.GetDesigner (host.RootComponent)).GetToolSupported (tool))
					tools.Add (tool);

			return new ToolboxItemCollection ((ToolboxItem[]) tools.ToArray (typeof (ToolboxItem)));
		}

		public ToolboxItemCollection GetToolboxItems (string category)
		{
			if (!categories.ContainsKey (category))
				return null;
			
			ArrayList tools = (ArrayList) categories[category];

			return new ToolboxItemCollection ((ToolboxItem[]) tools.ToArray (typeof (ToolboxItem)));
		}

		public ToolboxItemCollection GetToolboxItems (System.ComponentModel.Design.IDesignerHost host)
		{
			ArrayList tools = new ArrayList();
			IToolboxUser toolboxUser = (IToolboxUser) host.GetDesigner (host.RootComponent);
			
			foreach (ArrayList arr in categories.Values)
				foreach (ToolboxItem tool in arr)
					if (toolboxUser.GetToolSupported (tool))
						tools.Add (tool);

			return new ToolboxItemCollection ((ToolboxItem[]) tools.ToArray (typeof (ToolboxItem)));
		}

		public ToolboxItemCollection GetToolboxItems ()
		{
			ArrayList tools = new ArrayList ();

			foreach (ArrayList arr in categories.Values)
				tools.AddRange (arr);

			return new ToolboxItemCollection ((ToolboxItem[]) tools.ToArray (typeof (ToolboxItem)));
		}

		public bool IsSupported (object serializedObject, System.ComponentModel.Design.IDesignerHost host)
		{
			throw new NotImplementedException ();
		}

		public bool IsSupported (object serializedObject, System.Collections.ICollection filterAttributes)
		{
			throw new NotImplementedException ();
		}

		public bool IsToolboxItem (object serializedObject, System.ComponentModel.Design.IDesignerHost host)
		{
			throw new NotImplementedException ();
		}

		public bool IsToolboxItem (object serializedObject)
		{
			throw new NotImplementedException ();
		}

		public void Refresh ()
		{
			throw new NotImplementedException ();
		}

		public void RemoveCreator (string format, System.ComponentModel.Design.IDesignerHost host)
		{
			throw new NotImplementedException ();
		}

		public void RemoveCreator (string format)
		{
			throw new NotImplementedException ();
		}

		public void RemoveToolboxItem (ToolboxItem toolboxItem, string category)
		{
			throw new NotImplementedException ();
		}

		public void RemoveToolboxItem (ToolboxItem toolboxItem)
		{
			throw new NotImplementedException ();
		}

		public string SelectedCategory {
			get {return selectedCategory;}
			set {
				if (categories.ContainsKey (value))
					selectedCategory  = value;
			}
		}

		public void SelectedToolboxItemUsed ()
		{
			//throw new NotImplementedException ();
		}

		public object SerializeToolboxItem (ToolboxItem toolboxItem)
		{
			MemoryStream ms = new MemoryStream ();
			
			BF.Serialize (ms, toolboxItem);
   			byte[] retval = ms.ToArray ();
   			
   			ms.Close ();
   			return retval;
		}

		public bool SetCursor ()
		{
			throw new NotImplementedException ();
		}

		public void SetSelectedToolboxItem (ToolboxItem toolboxItem)
		{
			this.selectedItem = toolboxItem;
		}

		#endregion
		
		#region Save/load routines
		
		public void Persist (Stream stream)
		{
			StreamWriter strw = new StreamWriter (stream);
			
			XmlTextWriter xw = new XmlTextWriter (strw);
			xw.WriteStartDocument (true);
			
			xw.WriteStartElement ("Toolbox");
			
			foreach(string key in categories.Keys) {
				xw.WriteStartElement ("ToolboxCategory");
				xw.WriteAttributeString ("name", key);
				
				foreach (ToolboxItem item in ((ArrayList)categories[key])) {
					xw.WriteStartElement ("ToolboxItem");
					xw.WriteAttributeString ("DisplayName", item.DisplayName);
					//xw.WriteAttributeString ("AssemblyName", item.AssemblyName.ToString());
					xw.WriteAttributeString ("TypeName", item.TypeName);
					byte[] serItem = (byte[]) SerializeToolboxItem(item);
					xw.WriteString (ToBinHexString(serItem));
					xw.WriteEndElement ();
				}
				xw.WriteEndElement ();
			}
			
			xw.WriteEndElement ();
			xw.Close ();
			strw.Close ();
		}
		
		//temporary method until we get a UI and some form of persistence)
		public void PopulateFromAssembly (Assembly assembly)
		{
			Type[] types = assembly.GetTypes ();

			foreach (Type t in types)
			{
				if (t.IsAbstract || t.IsNotPublic) continue;
				
				if (t.GetConstructor (new Type[] {}) == null) continue;
				
				AttributeCollection atts = TypeDescriptor.GetAttributes (t);
				
				bool containsAtt = false;
				foreach (Attribute a in atts)
					 if (a.GetType() == typeof (ToolboxItemAttribute))
					 	containsAtt = true;
				if (!containsAtt) continue;
				
				ToolboxItemAttribute tba = (ToolboxItemAttribute) atts[typeof(ToolboxItemAttribute)];
				if (tba.Equals (ToolboxItemAttribute.None)) continue;
				//FIXME: fix WebControlToolboxItem
				Type toolboxItemType = typeof (ToolboxItem);//(tba.ToolboxItemType == null) ? typeof (ToolboxItem) : tba.ToolboxItemType;

				string category = "General";
				
				if (t.IsSubclassOf (typeof (System.Web.UI.WebControls.BaseValidator)))
					category = "Validation";
				else if (t.Namespace == "System.Web.UI.HtmlControls"  && t.IsSubclassOf (typeof (System.Web.UI.HtmlControls.HtmlControl)))
					category = "Html Elements";
				else if (t.IsSubclassOf (typeof (System.Web.UI.WebControls.BaseDataList)))
					category = "Data Controls";
				else if (t.IsSubclassOf (typeof (System.Web.UI.WebControls.WebControl)))
					category = "Web Controls";
				
				AddToolboxItem ((ToolboxItem) Activator.CreateInstance (toolboxItemType, new object[] {t}), category);
			}
			OnToolboxChanged ();
		}
		
		#endregion
		
		
		private BinaryFormatter bf = null;
		
		private BinaryFormatter BF {
			get {
				if (bf == null)
					bf = new BinaryFormatter ();
					
				return bf;
			}
		}
		
		#region Borrowed from System.Xml.XmlConvert. If only the methods were public as documented...
		// Authors:	Dwivedi, Ajay kumar (Adwiv@Yahoo.com), Gonzalo Paniagua Javier (gonzalo@ximian.com)
		//      	Alan Tam Siu Lung (Tam@SiuLung.com), Atsushi Enomoto (ginga@kit.hi-ho.ne.jp)
		// License:	MIT X11 (same as file)
		// Copyright:	(C) 2002 Ximian, Inc (http://www.ximian.com
		
		
		// LAMESPEC: It has been documented as public, but is marked as internal.
		private string ToBinHexString (byte [] buffer)
		{
			StringWriter w = new StringWriter ();
			WriteBinHex (buffer, 0, buffer.Length, w);
			return w.ToString ();
		}

		internal static void WriteBinHex (byte [] buffer, int index, int count, TextWriter w)
		{
			if (buffer == null)
				throw new ArgumentNullException ("buffer");
			if (index < 0)
				throw new ArgumentOutOfRangeException ("index", index, "index must be non negative integer.");
			if (count < 0)
				throw new ArgumentOutOfRangeException ("count", count, "count must be non negative integer.");
			if (buffer.Length < index + count)
				throw new ArgumentOutOfRangeException ("index and count must be smaller than the length of the buffer.");

			// Copied from XmlTextWriter.WriteBinHex ()
			int end = index + count;
			for (int i = index; i < end; i++) {
				int val = buffer [i];
				int high = val >> 4;
				int low = val & 15;
				if (high > 9)
					w.Write ((char) (high + 55));
				else
					w.Write ((char) (high + 0x30));
				if (low > 9)
					w.Write ((char) (low + 55));
				else
					w.Write ((char) (low + 0x30));
			}
		}
		
		// It is documented as public method, but in fact it is not.
		private  byte [] FromBinHexString (string s)
		{
			char [] chars = s.ToCharArray ();
			byte [] bytes = new byte [chars.Length / 2 + chars.Length % 2];
			FromBinHexString (chars, 0, chars.Length, bytes);
			return bytes;
		}

		private int FromBinHexString (char [] chars, int offset, int charLength, byte [] buffer)
		{
			int bufIndex = offset;
			for (int i = 0; i < charLength - 1; i += 2) {
				buffer [bufIndex] = (chars [i] > '9' ?
						(byte) (chars [i] - 'A' + 10) :
						(byte) (chars [i] - '0'));
				buffer [bufIndex] <<= 4;
				buffer [bufIndex] += chars [i + 1] > '9' ?
						(byte) (chars [i + 1] - 'A' + 10) : 
						(byte) (chars [i + 1] - '0');
				bufIndex++;
			}
			if (charLength %2 != 0)
				buffer [bufIndex++] = (byte)
					((chars [charLength - 1] > '9' ?
						(byte) (chars [charLength - 1] - 'A' + 10) :
						(byte) (chars [charLength - 1] - '0'))
					<< 4);

			return bufIndex - offset;
		}
		
		#endregion
			
	}
}
