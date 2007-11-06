//  TextTemplate.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;

using MonoDevelop.Core;

namespace MonoDevelop.Ide.Templates
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
				foreach (XmlNode entrynode in nodes) {
					XmlElement entryelem = entrynode as XmlElement;
					if (entryelem == null)
						continue;
					entries.Add(new Entry(entryelem));
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
			string[] files = Directory.GetFiles (Path.Combine (Path.Combine (PropertyService.DataPath, "options"), "textlib"),
			                                     "*.xml",
			                                     SearchOption.AllDirectories);
			foreach (string file in files) {
				LoadTextTemplate(file);
			}
		}
	}
}

