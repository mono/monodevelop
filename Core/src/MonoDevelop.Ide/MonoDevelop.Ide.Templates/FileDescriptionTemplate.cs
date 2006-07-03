
using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.CodeDom;
using System.CodeDom.Compiler;

using MonoDevelop.Core;
using MonoDevelop.Core.AddIns;
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
				Runtime.AddInService.RegisterExtensionItemListener ("/MonoDevelop/FileTemplateTypes", OnExtensionChanged);
			}
			
			foreach (FileTemplateTypeCodon template in templates) {
				if (template.ElementName == element.Name) {
					Type type = template.ClassType;
					FileDescriptionTemplate t = (FileDescriptionTemplate) Activator.CreateInstance (type);
					t.Load (element);
					return t;
				}
			}
			throw new InvalidOperationException ("Unknown file template type: " + element.Name);
		}
		
		static void OnExtensionChanged (ExtensionAction action, object item)
		{
			if (action == ExtensionAction.Add)
				templates.Add ((FileTemplateTypeCodon) item);
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
