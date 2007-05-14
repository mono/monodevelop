//
// IProject.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Diagnostics;
using System.IO;
using System.Xml;

using MonoDevelop.Ide.Projects.Item;

namespace MonoDevelop.Ide.Projects
{
	public class MSBuildProject : IProject
	{
		string            fileName;
		List<ProjectItem> items   = new List<ProjectItem> ();
		List<string>      imports = new List<string> ();
		
		public string FileName {
			get {
				return fileName;
			}
			set {
				fileName = value;
			}
		}
		
		public string BasePath {
			get {
				return Path.GetDirectoryName (FileName);
			}
		}

		
		public List<ProjectItem> Items {
			get {
				return items;
			}
		}
		
		public MSBuildProject (string fileName)
		{
			this.fileName = fileName;
		}
		
		void ReadPropertyGroup (XmlReader reader)
		{
			ProjectReadHelper.ReadList (reader, "PropertyGroup", delegate() {
				return true;
			});
		}
		
		void ReadItemGroup (XmlReader reader)
		{
			ProjectReadHelper.ReadList (reader, "ItemGroup", delegate() {
				ProjectItem item = ProjectItemFactory.Create (reader.LocalName);
				if (item != null) {
					item.Include = reader.GetAttribute ("Include");
					ProjectReadHelper.ReadList (reader, reader.LocalName, delegate() {
						item.SetMetadata (reader.LocalName, reader.ReadString ());
						return true;
					});
					items.Add (item);
					return true;
				}
				return false;
			});
		}
		
		public static IProject Load (string fileName)
		{
			MSBuildProject result = new MSBuildProject (fileName);
			using (XmlReader reader = XmlTextReader.Create (fileName)) {
				ProjectReadHelper.ReadList (reader, "Project", delegate() {
					switch (reader.LocalName) {
					case "Project":
						// Root node
						return true;
					case "PropertyGroup":
						result.ReadPropertyGroup (reader);
						return true;
					case "ItemGroup":
						result.ReadItemGroup (reader);
						return true;
					case "Import":
						if (!String.IsNullOrEmpty (reader.GetAttribute ("Project"))) {
							result.imports.Add (reader.GetAttribute ("Project"));
						}
						return true;
					}
					return false;
				});
			}
			return result;
		}
	}
}
