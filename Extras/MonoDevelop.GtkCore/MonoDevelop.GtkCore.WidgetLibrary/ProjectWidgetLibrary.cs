//
// ProjectWidgetLibrary.cs
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
using System.Collections;
using System.IO;
using System.Xml;
using System.Runtime.Serialization.Formatters.Binary;

using MonoDevelop.Projects;	
using MonoDevelop.Projects.Parser;	
using MonoDevelop.Ide.Gui;
using MonoDevelop.GtkCore.GuiBuilder;	

namespace MonoDevelop.GtkCore.WidgetLibrary
{
	public class ProjectWidgetLibrary: BaseWidgetLibrary
	{
		Hashtable classes = new Hashtable ();
		bool fromCache;
		DotNetProject project;
		
		public ProjectWidgetLibrary (DotNetProject project)
		{
			this.project = project;
			LoadProjectInfo ();
		}
		
		protected override XmlDocument GetObjectsDocument ()
		{
			GtkDesignInfo info = GtkCoreService.GetGtkInfo (project);
			if (info == null) return null;
			
			string objectsFile;
			if (fromCache)
				objectsFile = GetObjectsCache (info);
			else
				objectsFile = info.ObjectsFile;
				
			if (!File.Exists (objectsFile))
				return null;

			XmlDocument doc = new XmlDocument ();
			doc.Load (objectsFile);
			return doc;
		}
		
		public override void Load ()
		{
			base.Load ();
			SaveProjectInfo ();
		}
		
		public void ClearCachedInfo ()
		{
			fromCache = false;
			classes.Clear ();
		}
		
		public void LoadProjectInfo ()
		{
			GtkDesignInfo info = GtkCoreService.GetGtkInfo (project);
			if (info == null) return;
			
			string cacheFile = GetInfoCache (info);
			if (!File.Exists (cacheFile))
				return;

			BinaryFormatter bf = new BinaryFormatter ();
			FileStream file = null;
			try {
				file = File.OpenRead (cacheFile);
				classes = (Hashtable) bf.Deserialize (file);
				fromCache = true;
			} catch {
				// If the cached info can't be read, just discard it.
			}
			
			if (file != null)
				file.Close ();
				
			if (classes == null)
				classes = new Hashtable ();
		}
		
		public void SaveProjectInfo ()
		{
			GtkDesignInfo info = GtkCoreService.GetGtkInfo (project);
			if (info == null) return;
			
			if (File.Exists (info.ObjectsFile))
				File.Copy (info.ObjectsFile, GetObjectsCache (info), true);

			string cacheFile = GetInfoCache (info);
			BinaryFormatter bf = new BinaryFormatter ();
			FileStream file = File.Create (cacheFile);
			try {
				bf.Serialize (file, classes);
			} finally {
				file.Close ();
			}
		}

		protected override Stetic.ClassDescriptor LoadClassDescriptor (XmlElement element)
		{
			string name = element.GetAttribute ("type");
			ProjectClassInfo cinfo;
			
			if (fromCache) {
				cinfo = (ProjectClassInfo) classes [name];
				if (cinfo != null) {
					Console.WriteLine ("READ CLASS " + name);
					return new ProjectClassDescriptor (element, cinfo);
				} else
					return null;
			}
			
			ProjectClassDescriptor desc = base.LoadClassDescriptor (element) as ProjectClassDescriptor;
			if (desc == null)
				return null;

			classes [name] = desc.ClassInfo;
			
			// If this widget is being designed using stetic in this project,
			// then add the design to the class info
			
			GtkDesignInfo info = GtkCoreService.GetGtkInfo (project);
			GuiBuilderWindow win = info.GuiBuilderProject.GetWindowForClass (desc.ClassInfo.Name);
			if (win != null)
				desc.ClassInfo.WidgetDesc = Stetic.WidgetUtils.ExportWidget (win.RootWidget.Wrapped);
			
			return desc;
		}
		
		protected override IParserContext GetParserContext ()
		{
			return IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (project);
		}
		
		string GetObjectsCache (GtkDesignInfo info)
		{
			return Path.Combine (info.GtkGuiFolder, "objects.xml.dat");
		}
		
		string GetInfoCache (GtkDesignInfo info)
		{
			return Path.Combine (info.GtkGuiFolder, "library.dat");
		}
		
		public override string AssemblyPath {
			get { return project.GetOutputFileName (); }
		}
	}
	
}
