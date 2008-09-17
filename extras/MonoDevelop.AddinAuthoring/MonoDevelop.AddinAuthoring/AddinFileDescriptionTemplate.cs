//
// WidgetFileDescriptionTemplate.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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
using System.Xml;
using System.IO;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Templates;

namespace MonoDevelop.AddinAuthoring
{
	public class AddinFileDescriptionTemplate: FileDescriptionTemplate
	{
		XmlElement addinTemplate;
		
		public override string Name {
			get { return "Addin"; }
		}
		
		public override void Load (XmlElement filenode)
		{
			addinTemplate = filenode;
		}
		
		public override bool AddToProject (Project project, string language, string directory, string name)
		{
			// Replace template variables
			
			string cname = Path.GetFileNameWithoutExtension (name);
			string[,] tags = { 
				{"Name", cname},
			};
			
			string content = addinTemplate.OuterXml;
			content = StringParserService.Parse (content, tags);
			
			// Create the manifest
			
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (content);

			string file = Path.Combine (directory, "manifest.addin.xml");
			doc.Save (file);
			
			project.AddFile (file, BuildAction.EmbedAsResource);
			
			AddinData.EnableAddinAuthoringSupport ((DotNetProject)project);
			return true;
		}
		
		public override void Show ()
		{
		}
	}
}
