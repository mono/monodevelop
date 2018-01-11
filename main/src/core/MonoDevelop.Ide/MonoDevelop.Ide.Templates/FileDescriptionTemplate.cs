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
using System.Linq;
using System.Xml;
using System.Collections.Generic;

using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Core.StringParsing;
using System.Threading.Tasks;

namespace MonoDevelop.Ide.Templates
{
	public abstract class FileDescriptionTemplate
	{
		static List<FileTemplateTypeCodon> templates;
		
		public static FileDescriptionTemplate CreateTemplate (XmlElement element, FilePath baseDirectory)
		{
			if (templates == null) {
				templates = new List<FileTemplateTypeCodon> ();
				AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Ide/FileTemplateTypes", OnExtensionChanged);
			}
			
			foreach (FileTemplateTypeCodon template in templates) {
				if (template.ElementName == element.Name) {
					var t = (FileDescriptionTemplate) template.CreateInstance (typeof(FileDescriptionTemplate));
					t.Load (element, baseDirectory);
					t.CreateCondition = element.GetAttribute ("if");
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
		
		public abstract void Load (XmlElement filenode, FilePath baseDirectory);

		[Obsolete("Use public abstract Task<bool> AddToProjectAsync (SolutionFolderItem policyParent, Project project, string language, string directory, string name)")]
		public virtual bool AddToProject (SolutionFolderItem policyParent, Project project, string language, string directory, string name)
		{
			return false;
		}

		public abstract Task<bool> AddToProjectAsync (SolutionFolderItem policyParent, Project project, string language, string directory, string name);
		public abstract void Show ();

		internal string CreateCondition { get; private set; }
		
		public virtual bool IsValidName (string name, string language)
		{
			return FileService.IsValidFileName (name);
/*			if (name.Length > 0) {
				if (language != null && language.Length > 0) {
					IDotNetLanguageBinding binding = LanguageBindingService.GetBindingPerLanguageName (language) as IDotNetLanguageBinding;
					if (binding != null) {
						System.CodeDom.Compiler.CodeDomProvider provider = binding.GetCodeDomProvider ();
						if (provider != null)
							return provider.IsValidIdentifier (provider.CreateEscapedIdentifier (name));
					}
				}
				return name.IndexOfAny (Path.GetInvalidFileNameChars ()) == -1;
			}
			else
				return false;*/
		}
		
		public virtual bool SupportsProject (Project project, string projectPath)
		{
			return true;
		}

		protected IStringTagModel ProjectTagModel {
			get;
			private set;
		}

		// FIXME: maybe these should be public/protected, not 100% happy committing to this API right now though
		// AddProjectTags is called before AddToProject, then called with null afterwards
		internal virtual void SetProjectTagModel (IStringTagModel tagModel)
		{
			ProjectTagModel = tagModel;
		}

		internal bool EvaluateCreateCondition ()
		{
			return TemplateConditionEvaluator.EvaluateCondition (ProjectTagModel, CreateCondition);
		}
	}
}
