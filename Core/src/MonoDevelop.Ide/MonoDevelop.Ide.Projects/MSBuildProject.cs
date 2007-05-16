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

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide.Projects.Item;

namespace MonoDevelop.Ide.Projects
{
	public class MSBuildProject : IProject
	{

		string            fileName;
		List<string>      imports = new List<string> ();
		List<ProjectItem> items   = new List<ProjectItem> ();
		List<PropertyGroup> propertyGroups = new List<PropertyGroup> ();
		
		public string FileName {
			get {
				return fileName;
			}
			set {
				fileName = value;
			}
		}
		
		public string Name {
			get {
				return Path.GetFileNameWithoutExtension (FileName);
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
		
		public List<PropertyGroup> PropertyGroups {
			get {
				return this.propertyGroups;
			}
		}
		
		public MSBuildProject (string fileName)
		{
			this.fileName = fileName;
		}
		
		public CompilerResult Build (IProgressMonitor monitor)
		{
			Console.WriteLine ("!!!!!!!!!!!");
			string responseFileName = Path.GetTempFileName ();
			Console.WriteLine ("response file:" + responseFileName);
			using (StreamWriter writer = new StreamWriter (responseFileName)) {
				writer.WriteLine("/noconfig");
				writer.WriteLine("/nologo");
				writer.WriteLine("/codepage:utf8");
				writer.WriteLine("/t:exe");
				
				writer.WriteLine("\"/out:{0}\"", "a.exe");
				
				foreach (ProjectItem item in this.Items) {
					if (item is ProjectFile) {
						writer.WriteLine("\"{0}\"", item.Include);
					}
				}
			}
			
			string output = Path.GetTempFileName ();
			string error = Path.GetTempFileName ();
			
			using (StreamWriter outWriter = new StreamWriter (output)) {
				using (StreamWriter errWriter = new StreamWriter (error)) {
					ProcessWrapper pw = Runtime.ProcessService.StartProcess ("gmcs", "\"@" + responseFileName + "\"", BasePath, outWriter, errWriter, delegate {});
					pw.WaitForExit();
				}
			}
			
			return new CompilerResult();
		}
		
		public void Start (IProgressMonitor monitor, ExecutionContext context)
		{
			Runtime.ProcessService.StartConsoleProcess ( Path.Combine(BasePath, "a.exe"),
			                                            "",
			                                            BasePath,
			                                            context.ConsoleFactory.CreateConsole (true),
			                                            delegate {});
		}

#region I/O
		
		class Property
		{
			string name;
			string value;
			Dictionary<string, string> attributes = new Dictionary<string, string> ();
			
			public string Name {
				get { return name; }
				set { name = value; }
			}
			public string Value {
				get { return this.value; }
				set { this.value = value; }
			}
			public Dictionary<string, string> Attributes {
				get { return this.attributes; }
			}
			
			public static Property Read (XmlReader reader)
			{
				Property result = new Property ();
				result.Name = reader.LocalName;
				for (int i = 0; i < reader.AttributeCount; ++i) {
					reader.MoveToAttribute (i);
					result.Attributes[reader.LocalName] = reader.Value;
				}
				reader.MoveToElement();
				result.Value = reader.Value;
				Console.WriteLine (result.Name + " --" + result.Value);
				return result;
			}
			
		}
		
		class PropertyGroup
		{
			string condition;
			List<Property> properties = new List<Property> ();
			
			public string Condition {
				get { return condition; }
				set { condition = value; }
			}
			
			public List<Property> Properties {
				get { return properties; }
			}
			
			public PropertyGroup (string condition)
			{
				this.condition = condtion;
			}
			
			public static PropertyGroup Read (XmlReader reader)
			{
				PropertyGroup result = new PropertyGroup (reader.GetAttribute("Condition"));
				ProjectReadHelper.ReadList (reader, "PropertyGroup", delegate() {
					result.Properties.Add (Property.Read (reader));
					return true;
				});
				return result;
			}
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
						result.PropertyGroups.Add(PropertyGroup.Read (reader));
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
#endregion
	}
}
