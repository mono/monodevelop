//
// AssemblyReferenceWidgetLibrary.cs
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
using MonoDevelop.Core;
using Mono.Cecil;

namespace MonoDevelop.GtkCore.WidgetLibrary
{
	public class AssemblyReferenceWidgetLibrary: BaseWidgetLibrary
	{
		string assemblyReference;
		string fileName;
		XmlDocument objects;
		XmlDocument steticGui;
		DateTime timestamp;
		
		public AssemblyReferenceWidgetLibrary (string assemblyReference, string assemblyName)
		{
			this.assemblyReference = assemblyReference;
			
			if (!assemblyReference.EndsWith (".dll") && !assemblyReference.EndsWith (".exe")) {
				try {
					fileName = Runtime.SystemAssemblyService.GetAssemblyLocation (assemblyReference);
				} catch (Exception ex) {
					Runtime.LoggingService.Error (ex);
				}
			} else {
				fileName = assemblyReference;
			}
			LoadInfo ();
		}
		
		public override string Name {
			get { return fileName; }
		}
		
		public override bool CanReload {
			get { return true; }
		}
		
		public override bool NeedsReload {
			get {
				if (!File.Exists (fileName))
					return false;
				return File.GetLastWriteTime (fileName) != timestamp;
			}
		}
		
		public bool ExportsWidgets {
			get { return objects != null; }
		}
				
		public string AssemblyReference {
			get { return assemblyReference; }
		}
		
		protected override XmlDocument GetObjectsDocument ()
		{
			return objects;
		}
		
		public override void Reload ()
		{
			LoadInfo ();
			base.Reload ();
		}

		public void LoadInfo ()
		{
			objects = null;
			steticGui = null;
			
			if (fileName == null || !File.Exists (fileName))
				return;
			
			timestamp = File.GetLastWriteTime (fileName);
			
			IAssemblyDefinition asm = AssemblyFactory.GetAssembly (fileName);
			foreach (Resource res in asm.MainModule.Resources) {
				EmbeddedResource eres = res as EmbeddedResource;
				if (eres == null) continue;
				
				if (eres.Name == "objects.xml") {
					MemoryStream ms = new MemoryStream (eres.Data);
					objects = new XmlDocument ();
					objects.Load (ms);
				}
				
				if (eres.Name == "gui.stetic") {
					MemoryStream ms = new MemoryStream (eres.Data);
					steticGui = new XmlDocument ();
					steticGui.Load (ms);
				}
			}
		}

		protected override Stetic.ClassDescriptor LoadClassDescriptor (XmlElement element)
		{
			ProjectClassDescriptor desc = base.LoadClassDescriptor (element) as ProjectClassDescriptor;
			if (desc == null)
				return null;

			// If this widget is being designed using stetic in this project,
			// then add the design to the class info
			
			if (steticGui != null) {
				XmlElement elem = (XmlElement) steticGui.DocumentElement.SelectSingleNode ("widget[@id='" + desc.WrappedTypeName + "']");
				if (elem != null)
					desc.ClassInfo.WidgetDesc = elem;
			}
			
			return desc;
		}
		
		protected override IParserContext GetParserContext ()
		{
			if (!File.Exists (fileName))
				return null;
			else
				return IdeApp.ProjectOperations.ParserDatabase.GetAssemblyParserContext (fileName);
		}
		
		public override string AssemblyPath {
			get { return assemblyReference; }
		}
	}	
}

