// FileDescriptionTemplate.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2006 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//


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
		public abstract bool AddToProject (SolutionItem policyParent, Project project, string language, string directory, string name);
		public abstract void Show ();
		
		public virtual bool IsValidName (string name, string language)
		{
			if (name.Length > 0) {
				if (language != null && language.Length > 0) {
					IDotNetLanguageBinding binding = MonoDevelop.Projects.Services.Languages.GetBindingPerLanguageName (language) as IDotNetLanguageBinding;
					if (binding != null) {
						System.CodeDom.Compiler.CodeDomProvider provider = binding.GetCodeDomProvider ();
						if (provider != null)
							return provider.IsValidIdentifier (provider.CreateEscapedIdentifier (name));
					}
				}
				return name.IndexOfAny (Path.GetInvalidFileNameChars ()) == -1;
			}
			else
				return false;
		}
		
		public virtual bool SupportsProject (Project project, string projectPath)
		{
			return true;
		}
	}
}
