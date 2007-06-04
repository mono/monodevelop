
using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.CodeDom;
using System.CodeDom.Compiler;

using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Ide.Codons;

namespace MonoDevelop.Ide.Templates
{
	public abstract class FileDescriptionTemplate
	{
		static List<FileTemplateTypeCodon> templates;
		
		public static FileDescriptionTemplate CreateTemplate (XmlElement element)
		{
			if (templates == null) {
				templates = new List<FileTemplateTypeCodon> ();
				AddinManager.AddExtensionNodeHandler ("/MonoDevelop/FileTemplateTypes", OnExtensionChanged);
			}
			
			foreach (FileTemplateTypeCodon template in templates) {
				if (template.ElementName == element.Name) {
					FileDescriptionTemplate t = (FileDescriptionTemplate) template.CreateInstance (typeof(FileDescriptionTemplate));
					t.Load (element);
					return t;
				}
			}
			return null;
			//throw new InvalidOperationException ("Unknown file template type: " + element.Name);
		}
		
		static void OnExtensionChanged (object s, ExtensionNodeEventArgs args)
		{
			if (args.Change == ExtensionChange.Add)
				templates.Add ((FileTemplateTypeCodon) args.ExtensionNode);
		}
		
		public abstract string Name { get; }
		
		public abstract void Load (XmlElement filenode);
		public abstract void AddToProject (IProject project, string language, string directory, string name);
		
		/// <returns>Returns a list of all files created.</returns>
		public abstract string[] Create (string language, string directory, string name);
		
		public abstract void Show ();
		
		public virtual bool IsValidName (string name, string language)
		{
			return (name.Length > 0);
		}
	}
}
