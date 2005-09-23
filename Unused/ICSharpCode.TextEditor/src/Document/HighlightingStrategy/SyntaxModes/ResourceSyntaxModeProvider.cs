// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Reflection;
using System.Xml;
using System.IO;

namespace MonoDevelop.TextEditor.Document
{
	public class ResourceSyntaxModeProvider : ISyntaxModeFileProvider
	{
		ArrayList syntaxModes = null;
		
		public ArrayList SyntaxModes {
			get {
				return syntaxModes;
			}
		}
		
		public ResourceSyntaxModeProvider()
		{
			Assembly assembly = this.GetType().Assembly;
			Stream syntaxModeStream = assembly.GetManifestResourceStream("SyntaxModes.xml");
			if (syntaxModeStream == null) throw new ApplicationException("DAMN RESOURCES!");
			syntaxModes = SyntaxMode.GetSyntaxModes(syntaxModeStream);
		}
		
		public XmlTextReader GetSyntaxModeFile(SyntaxMode syntaxMode)
		{
			Assembly assembly = typeof(SyntaxMode).Assembly;
			return new XmlTextReader(assembly.GetManifestResourceStream(syntaxMode.FileName));
		}
	}
}
