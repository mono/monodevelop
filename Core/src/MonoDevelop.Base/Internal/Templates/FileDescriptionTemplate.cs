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

namespace MonoDevelop.Internal.Templates
{
	internal class FileDescriptionTemplate
	{
		string name;
		string content;
		
		public FileDescriptionTemplate(string name, string content)
		{
			this.name    = name;
			this.content = content;
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
	}
}
