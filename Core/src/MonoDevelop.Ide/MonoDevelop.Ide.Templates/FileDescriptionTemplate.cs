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

namespace MonoDevelop.Ide.Templates
{
	internal class FileDescriptionTemplate
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
