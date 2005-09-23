// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;

using MonoDevelop.Services;

namespace MonoDevelop.Internal.Templates
{
	/// <summary>
	/// This class defines and holds text templates
	/// they're a bit similar than code templates, but they're
	/// not inserted automaticaly
	/// </summary>
	internal class TextTemplate
	{
		public static ArrayList TextTemplates = new ArrayList();
		
		string    name    = null;
		ArrayList entries = new ArrayList();
		
		public string Name {
			get {
				return name;
			}
		}
		
		public ArrayList Entries {
			get {
				return entries;
			}
		}
		
		public class Entry 
		{
			public string Display;
			public string Value;
			
			public Entry(XmlElement el)
			{
				this.Display = el.Attributes["display"].InnerText;
				this.Value   = el.Attributes["value"].InnerText;
			}
			
			public override string ToString()
			{
				return Display;
			}
		}
		
		public TextTemplate(string filename)
		{
			try {
				XmlDocument doc = new XmlDocument();
				doc.Load(filename);
				
				name = doc.DocumentElement.Attributes["name"].InnerText;
				
				XmlNodeList nodes = doc.DocumentElement.ChildNodes;
				foreach (XmlElement entrynode in nodes) {
					entries.Add(new Entry(entrynode));
				}
			} catch (Exception e) {
				throw new System.IO.FileLoadException("Can't load standard sidebar template file", filename, e);
			}
		}
		
		static void LoadTextTemplate(string filename)
		{
			TextTemplates.Add(new TextTemplate(filename));
		}
		
		static TextTemplate()
		{
			StringCollection files = Runtime.FileUtilityService.SearchDirectory (Runtime.Properties.DataDirectory + 
			                            Path.DirectorySeparatorChar + "options" + 
			                            Path.DirectorySeparatorChar + "textlib", "*.xml");
			foreach (string file in files) {
				LoadTextTemplate(file);
			}
		}
	}
}

