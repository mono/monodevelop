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
using System.Diagnostics;
using System.CodeDom;
using System.CodeDom.Compiler;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Projects.CodeGeneration;

namespace MonoDevelop.Ide.Templates
{
	public class FileDescriptionTemplate
	{
		string name;
		string content;
		XmlElement domContent;
		
		public FileDescriptionTemplate (string name, string content)
		{
			this.name    = name;
			this.content = content;
		}
		
		public FileDescriptionTemplate (string name, XmlElement domContent)
		{
			this.name    = name;
			this.domContent = domContent;
		}
		
		public string Name {
			get {
				return name;
			}
		}
		
		public string Content {
			get {
				return content;
			}
		}
		
		public XmlElement CodeDomContent {
			get {
				return domContent;
			}
		}
	}
}
