//  FileSyntaxModeProvider.cs
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
using System.Reflection;
using System.Xml;

namespace MonoDevelop.TextEditor.Document
{
	public class FileSyntaxModeProvider : ISyntaxModeFileProvider
	{
		string    directory;
		ArrayList syntaxModes = null;
		
		public ArrayList SyntaxModes {
			get {
				return syntaxModes;
			}
		}
		
		public FileSyntaxModeProvider(string directory)
		{
			this.directory = directory;
			string syntaxModeFile = Path.Combine(directory, "SyntaxModes.xml");
			if (File.Exists(syntaxModeFile)) {
				Stream s = File.OpenRead(syntaxModeFile);
				syntaxModes = SyntaxMode.GetSyntaxModes(s);
				s.Close();
			} else {
				syntaxModes = ScanDirectory(directory);
			}
		}
		
		public XmlTextReader GetSyntaxModeFile(SyntaxMode syntaxMode)
		{
			string syntaxModeFile = Path.Combine(directory, syntaxMode.FileName);
			if (!File.Exists(syntaxModeFile)) {
				//MessageBox.Show("Can't load highlighting definition " + syntaxModeFile + " (file not found)!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1);
				return null;
			}
			return new XmlTextReader(File.OpenRead(syntaxModeFile));
		}
		
		ArrayList ScanDirectory(string directory)
		{
			string[] files = Directory.GetFiles(directory);
			ArrayList modes = new ArrayList();
			foreach (string file in files) {
				if (Path.GetExtension(file).ToUpper() == ".XSHD") {
					XmlTextReader reader = new XmlTextReader(file);
					while (reader.Read()) {
						if (reader.NodeType == XmlNodeType.Element) {
							switch (reader.Name) {
								case "SyntaxDefinition":
									string name       = reader.GetAttribute("name");
									string extensions = reader.GetAttribute("extensions");
									modes.Add(new SyntaxMode(Path.GetFileName(file),
									                               name,
									                               extensions));
									goto bailout;
								default:
									//MessageBox.Show("Unknown root node in syntax highlighting file :" + reader.Name, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1);
									goto bailout;
							}
						}
					}
					bailout:
					reader.Close();
			
				}
			}
			return modes;
		}
	}
}
