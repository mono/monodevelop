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
using MonoDevelop.Ide.Templates;
using MonoDevelop.GtkCore.GuiBuilder;
using MonoDevelop.Ide;
using MonoDevelop.Ide.TypeSystem;


namespace MonoDevelop.GtkCore
{
	public class WidgetFileDescriptionTemplate: FileDescriptionTemplate
	{
		SingleFileDescriptionTemplate fileTemplate;
		XmlElement steticTemplate;
		
		public override string Name {
			get { return "Widget"; }
		}
		
		public override void Load (XmlElement filenode, FilePath baseDirectory)
		{
			foreach (XmlNode node in filenode.ChildNodes) {
				XmlElement elem = node as XmlElement;
				if (elem == null) continue;
				
				if (elem.Name == "SteticTemplate") {
					if (steticTemplate != null)
						throw new InvalidOperationException ("Widget templates can't contain more than one SteticTemplate element");
					steticTemplate = elem;
				} else if (fileTemplate == null) {
					fileTemplate = FileDescriptionTemplate.CreateTemplate (elem, baseDirectory) as SingleFileDescriptionTemplate;
					if (fileTemplate == null)
						throw new InvalidOperationException ("Widget templates can only contain single-file and stetic templates.");
				}
			}
			if (fileTemplate == null)
				throw new InvalidOperationException ("File template not found in widget template.");
			if (steticTemplate == null)
				throw new InvalidOperationException ("Stetic template not found in widget template.");
		}
		
		public override bool SupportsProject (Project project, string projectPath)
		{
			return (project is DotNetProject) && GtkDesignInfo.SupportsDesigner (project);
		}
		
		public override bool AddToProject (SolutionFolderItem policyParent, Project project, string language, string directory, string name)
		{
			if (!GtkDesignInfo.SupportsDesigner (project, false)) {
				ReferenceManager mgr = new ReferenceManager (project as DotNetProject);
				mgr.GtkPackageVersion = mgr.DefaultGtkVersion;
				mgr.Dispose ();
			}

			GtkDesignInfo info = GtkDesignInfo.FromProject ((DotNetProject) project);
				
			GuiBuilderProject gproject = info.GuiBuilderProject;
			
			string fileName = fileTemplate.GetFileName (policyParent, project, language, directory, name);
			fileTemplate.AddToProject (policyParent, project, language, directory, name);

			FileService.NotifyFileChanged (fileName);

			DotNetProject netProject = project as DotNetProject;
			string ns = netProject != null ? netProject.GetDefaultNamespace (fileName) : "";
			string cname = Path.GetFileNameWithoutExtension (fileName);
			string fullName = ns.Length > 0 ? ns + "." + cname : cname;
			string[,] tags = { 
				{"Name", cname},
				{"Namespace", ns},
				{"FullName", fullName}
			};

			XmlElement widgetElem = steticTemplate ["widget"];
			if (widgetElem != null) {
				string content = widgetElem.OuterXml;
				content = StringParserService.Parse (content, tags);
				
				XmlDocument doc = new XmlDocument ();
				doc.LoadXml (content);
				
				gproject.AddNewComponent (doc.DocumentElement);
				gproject.SaveAll (false);
				IdeApp.ProjectOperations.SaveAsync (project);
				return true;
			}
			
			widgetElem = steticTemplate ["action-group"];
			if (widgetElem != null) {
				string content = widgetElem.OuterXml;
				content = StringParserService.Parse (content, tags);
				
				XmlDocument doc = new XmlDocument ();
				doc.LoadXml (content);
				
				gproject.SteticProject.AddNewActionGroup (doc.DocumentElement);
				gproject.SaveAll (false);
				IdeApp.ProjectOperations.SaveAsync (project);
				return true;
			}
			
			throw new InvalidOperationException ("<widget> or <action-group> element not found in widget template.");
		}
		
		public override void Show ()
		{
			fileTemplate.Show ();
		}
	}
}
