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
		bool initialized;
		
		public bool ShouldIsolate {
			get { return true; }
		}

		public string[] FileTypes {
			get { return new string[] {"dll", "exe"}; }
		}
		
		public IList<ItemToolboxNode> Load (string filename)
		{
			List<ItemToolboxNode> list = new List<ItemToolboxNode> ();
			System.Reflection.Assembly scanAssem;
			try {
				scanAssem = System.Reflection.Assembly.LoadFile (filename);
			} catch (Exception ex) {
				MonoDevelop.Core.LoggingService.LogError ("ToolboxItemToolboxLoader: Could not load assembly '"
				    + filename + "'", ex);
				return list;
			}
			
			MonoDevelop.Core.SystemPackage package
				= MonoDevelop.Core.Runtime.SystemAssemblyService.GetPackageFromPath (filename);
			
			//need to initialise if this if out of the main process
			//in order to be able to load icons etc.
			if (!initialized) {
				Gtk.Application.Init ();
				initialized = true;
			}
			
			//detect the runtime version
			MonoDevelop.Core.ClrVersion clrVersion = MonoDevelop.Core.ClrVersion.Default;
			byte[] corlibKey = new byte[] { 0xb7, 0x7a, 0x5c, 0x56, 0x19, 0x34, 0xe0, 0x89 };      
			//the other system.{...} key: 	
			//{ 0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a };
			foreach (System.Reflection.AssemblyName an in scanAssem.GetReferencedAssemblies ()) {
				if (an.Name == "mscorlib" && byteArraysEqual (corlibKey, an.GetPublicKeyToken ())) {
					if (an.Version == new Version (2, 0, 0, 0)) {
						clrVersion = MonoDevelop.Core.ClrVersion.Net_2_0;
						break;
					} else if (an.Version == new Version (1, 0, 5000, 0)) {
						clrVersion = MonoDevelop.Core.ClrVersion.Net_1_1;
						break;
					}
				}
			}
			
			if (clrVersion == MonoDevelop.Core.ClrVersion.Default) {
				MonoDevelop.Core.LoggingService.LogError ("ToolboxItemToolboxLoader: assembly '"
				    + filename + "' references unknown runtime version.");
				return list;
			}
			
			Type[] types = scanAssem.GetTypes ();

			foreach (Type t in types) {
				//skip inaccessible types
				if (t.IsAbstract || !t.IsPublic || !t.IsClass) continue;
				
				//get the ToolboxItemAttribute if present
				object[] atts = t.GetCustomAttributes (typeof (ToolboxItemAttribute), true);
				if (atts == null || atts.Length == 0)
					continue;
				
				ToolboxItemAttribute tba = (ToolboxItemAttribute) atts[0];
				if (tba.Equals (ToolboxItemAttribute.None) || tba.ToolboxItemType == null)
					continue;
				
				// Technically CategoryAttribute shouldn't be used for this purpose (intended for properties in 
				// the PropertyGrid) but I can see no harm in doing this.
				string cat = null;
				atts = t.GetCustomAttributes (typeof (CategoryAttribute), true);
				if (atts != null && atts.Length > 0)
					cat = ((CategoryAttribute)atts[0]).Category;
				
				try {
					ItemToolboxNode node = GetNode (t, tba, cat, package != null? filename : null, clrVersion);
					if (node != null)
						list.Add (node);
				} catch (Exception ex) {
					MonoDevelop.Core.LoggingService.LogError (
					    "Unhandled error in toolbox node loader '" + GetType ().FullName 
					    + "' with type '" + t.FullName
					    + "' in assembly '" + scanAssem.FullName + "'",
					    ex);
				}
			}
			
			return list;// Load (scanAssem);
		}
		
		bool byteArraysEqual (byte[] a, byte[] b)
		{
			if (a == null)
				return b == null;
			if (b == null)
				return a == null;
			if (a.Length != b.Length)
				return false;
			for (int i = 0; i < a.Length; i++) {
				if (a[i] != b[i])
					return false;
			}
			return true;
		}
		
		//TODO: this method is public so that the toolbox service can special-case subclasses of this
		//to unify them into a single type-walk in a single remote process
		/// <param name="type">The <see cref="Type"/> of the item for which a node should be created.</param>
		/// <param name="attribute">The <see cref="ToolboxItemAttribute"/> that was applied to the type.</param>
		/// <param name="attributeCategory"> The node's category as detected from the <see cref="CategoryAttribute"/>. 
		/// If it's null or empty, the method will need to infer a value.</param>
		/// <param name="assemblyPath"> If the assembly is a system package, this value will be null. Else, the method will 
		/// need to record the full path in the node.</param>
		public abstract ItemToolboxNode GetNode (
		    Type type,
		    ToolboxItemAttribute attribute,
		    string attributeCategory,
		    string assemblyPath,
		    MonoDevelop.Core.ClrVersion referencedRuntime
		    );
	}
}
