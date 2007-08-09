
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
using MonoDevelop.Projects;
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
				AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Ide/FileTemplateTypes", OnExtensionChanged);
			}
			
			foreach (FileTemplateTypeCodon template in templates) {
				if (template.ElementName == element.Name) {
					FileDescriptionTemplate t = (FileDescriptionTemplate) template.CreateInstance (typeof(FileDescriptionTemplate));
					t.Load (element);
					return t;
				}
			}
			throw new InvalidOperationException ("Unknown file template type: " + element.Name);
		}
		
		static void OnExtensionChanged (object s, ExtensionNodeEventArgs args)
		{
			if (args.Change == ExtensionChange.Add)
				templates.Add ((FileTemplateTypeCodon) args.ExtensionNode);
			else
				templates.Remove ((FileTemplateTypeCodon) args.ExtensionNode);
		}
		
		public abstract string Name { get; }
		
		public abstract void Load (XmlElement filenode);
		public abstract void AddToProject (Project project, string language, string directory, string name);
		public abstract void Show ();
		
		public virtual bool IsValidName (string name, string language)
		{
			return (name.Length > 0);
		}
	}
}
