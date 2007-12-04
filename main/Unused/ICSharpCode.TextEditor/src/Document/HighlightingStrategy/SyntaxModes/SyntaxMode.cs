//  SyntaxMode.cs
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
using System.Collections;
using System.Xml;

namespace MonoDevelop.TextEditor.Document
{
	public class SyntaxMode
	{
		string   fileName;
		string   name;
		string[] extensions;
		
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
				return name;
			}
			set {
				name = value;
			}
		}
		
		public string[] Extensions {
			get {
				return extensions;
			}
			set {
				extensions = value;
			}
		}
		
		public SyntaxMode(string fileName, string name, string extensions)
		{
			this.fileName   = fileName;
			this.name       = name;
			this.extensions = extensions.Split(';', '|', ',');
		}
		
		public SyntaxMode(string fileName, string name, string[] extensions)
		{
			this.fileName = fileName;
			this.name = name;
			this.extensions = extensions;
		}
		
		public static ArrayList GetSyntaxModes(Stream xmlSyntaxModeStream)
		{
			XmlTextReader reader = new XmlTextReader(xmlSyntaxModeStream);
			ArrayList syntaxModes = new ArrayList();
			while (reader.Read()) {
				switch (reader.NodeType) {
					case XmlNodeType.Element:
						switch (reader.Name) {
							case "SyntaxModes":
								string version = reader.GetAttribute("version");
								if (version != "1.0") {
									//MessageBox.Show("Unknown syntax mode file defininition with version " + version , "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1);
									return syntaxModes;
								}
								break;
							case "Mode":
								syntaxModes.Add(new SyntaxMode(reader.GetAttribute("file"), 
								                               reader.GetAttribute("name"),
								                               reader.GetAttribute("extensions")));
								break;
							default:
								//MessageBox.Show("Unknown node in syntax mode file :" + reader.Name, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1);
								return syntaxModes;
						}
						break;
				}
			}
			reader.Close();
			return syntaxModes;
		}
		public override string ToString() 
		{
			return String.Format("[SyntaxMode: FileName={0}, Name={1}, Extensions=({2})]", fileName, name, String.Join(",", extensions));
		}
	}
}
