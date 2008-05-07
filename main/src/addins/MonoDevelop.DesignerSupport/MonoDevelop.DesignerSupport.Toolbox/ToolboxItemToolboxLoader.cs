//
// ToolboxItemToolboxLoader.cs: A toolbox loader that loads the standard 
//   .NET Framework ToolboxItems from assemblies.
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
	
	
	public abstract class ToolboxItemToolboxLoader : IToolboxLoader
	{
		static string[] fileTypes = new string[] {"dll"};
		bool initialized;
		
		public bool ShouldIsolate {
			get { return true; }
		}
		
		public string [] FileTypes {
			get { return fileTypes; }
		}
		
		
		public IList<ItemToolboxNode> Load (string filename)
		{		
			System.Reflection.Assembly scanAssem = System.Reflection.Assembly.LoadFile (filename);
			return Load (scanAssem);
		}
		
		public abstract IList<ItemToolboxNode> Load (System.Reflection.Assembly assem);
		
		protected IEnumerable<Type> AccessibleToolboxTypes (System.Reflection.Assembly assem)
		{
			if (!initialized) {
				Gtk.Application.Init ();
				initialized = true;
			}
			
			Type[] types = assem.GetTypes ();

			foreach (Type t in types)
			{
				//skip inaccessible types
				if (t.IsAbstract || !t.IsPublic || !t.IsClass) continue;
				
				//note: toolbox items can create types with non-empty constructors
				//if (t.GetConstructor (new Type[] {}) == null) continue;
				
				//get the ToolboxItemAttribute if present
				object[] atts = t.GetCustomAttributes (typeof (ToolboxItemAttribute), true);
				if (atts == null || atts.Length == 0)
					continue;
				
				ToolboxItemAttribute tba = (ToolboxItemAttribute) atts[0];
				if (!tba.Equals (ToolboxItemAttribute.None))
					yield return t;
			}
		}
		
		//Technically CategoryAttribute shouldn't be used for this purpose (intended for properties in the PropertyGrid)
		//but I can see no harm in doing this.
		protected CategoryAttribute GetCategoryAttribute (Type type)
		{
			object[] atts = type.GetCustomAttributes (typeof (CategoryAttribute), true);
			if (atts != null && atts.Length > 0)
				return (CategoryAttribute)atts[0];
			else
				return null;
		}
		
		protected void SetFullPath (TypeToolboxNode node, System.Reflection.Assembly assem)
		{
			
			//if assembly wasn't loaded from the GAC, record the path too
			if (!assem.GlobalAssemblyCache) {
				string path = assem.Location;
					node.Type.AssemblyLocation = path;
			}
		}
	}
}
