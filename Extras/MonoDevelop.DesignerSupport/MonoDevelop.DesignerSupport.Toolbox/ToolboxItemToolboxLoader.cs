//
// ToolboxItemToolboxLoader.cs: A toolbox loader that loads the standard 
//   .NET Framework ToolboxItems from assemblies.
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
//
//
// This source code is licenced under The MIT License:
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	
	
	public class ToolboxItemToolboxLoader : IToolboxLoader
	{
		static string[] fileTypes = new string[] {"dll"};
		
		public bool ShouldIsolate {
			get { return true; }
		}
		
		public string [] FileTypes {
			get { return fileTypes; }
		}
		
		public IList<ItemToolboxNode> Load (string filename)
		{
			List<ItemToolboxNode> nodes = new List<ItemToolboxNode> ();
			
			System.Reflection.Assembly scanAssem = System.Reflection.Assembly.LoadFile (filename);
			Type[] types = scanAssem.GetTypes ();

			foreach (Type t in types)
			{
				//skip inaccessible types
				if (t.IsAbstract || t.IsNotPublic) continue;
				if (t.GetConstructor (new Type[] {}) == null) continue;
				
				//get the ToolboxItemAttribute if present
				AttributeCollection atts = TypeDescriptor.GetAttributes (t);
				
				bool containsAtt = false;
				foreach (Attribute a in atts)
					 if (a.GetType() == typeof (ToolboxItemAttribute))
					 	containsAtt = true;
				if (!containsAtt) continue;
				
				ToolboxItemAttribute tba = (ToolboxItemAttribute) atts[typeof(ToolboxItemAttribute)];
				if (tba.Equals (ToolboxItemAttribute.None)) continue;
				
				Type toolboxItemType = (tba.ToolboxItemType == null) ? typeof (ToolboxItem) : tba.ToolboxItemType;				
				//FIXME: fix WebControlToolboxItem so that this isn't necessary
				if (typeof (System.Web.UI.Design.WebControlToolboxItem) == toolboxItemType)
					toolboxItemType = typeof (ToolboxItem);
				
				//create the ToolboxItem. The ToolboxItemToolboxNode will destry it, but need to
				//be able to extract data from it first.
				ToolboxItem item = (ToolboxItem) Activator.CreateInstance (toolboxItemType, new object[] {t});
				
				ToolboxItemToolboxNode node = new ToolboxItemToolboxNode (item);
				
				//Technically CategoryAttribute shouldn't be used for this purpose (intended for properties in the PropertyGrid)
				//but I can see no harm in doing this.
				CategoryAttribute ca = atts[typeof(CategoryAttribute)] as CategoryAttribute;
				node.Category = (ca != null)? ca.Category : "General";
				
				//FIXME: this shouldn't be hard coded, remove when we ship a default toolbox with categories predefined
				if (t.IsSubclassOf (typeof (System.Web.UI.WebControls.BaseValidator)))
					node.Category = "Validation";
				else if (t.Namespace == "System.Web.UI.HtmlControls"  && t.IsSubclassOf (typeof (System.Web.UI.HtmlControls.HtmlControl)))
					node.Category = "Html Elements";
				else if (t.IsSubclassOf (typeof (System.Web.UI.WebControls.BaseDataList)))
					node.Category = "Data Controls";
				else if (t.IsSubclassOf (typeof (System.Web.UI.WebControls.WebControl)))
					node.Category = "Web Controls";
				
				nodes.Add (node);
			}
			
			//if assembly wasn't loaded from the GAC, record the path too
			if (!scanAssem.GlobalAssemblyCache) {
				string path = scanAssem.Location;
				foreach (TypeToolboxNode n in nodes)
					n.Type.AssemblyLocation = path;
			}
			
			return (nodes);
		}
	}
}
