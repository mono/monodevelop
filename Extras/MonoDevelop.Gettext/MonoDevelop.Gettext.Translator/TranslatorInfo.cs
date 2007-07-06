//
// TranslatorInfo.cs
//
// Author:
//   Rafael 'Monoman' Teixeira
//   David Makovský <yakeen@sannyas-on.net>
//
// Copyright (C) 2006 Rafael 'Monoman' Teixeira
// Copyright (C) 2007 David Makovský
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
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Collections;
using System.Collections.Specialized;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.Serialization;

namespace MonoDevelop.Gettext.Translator
{
	class TranslatorInfo : IDisposable
	{
		Project project;
		TranslationProject builderProject;
		string basename;

		public TranslatorInfo ()
		{
		}

		public TranslatorInfo (Project project)
		{
			IExtendedDataItem item = (IExtendedDataItem)project;
			item.ExtendedProperties["TranslatorInfo"] = this;
			Bind (project);
		}

		public static TranslatorInfo GetFrom (Project project)
		{
			IExtendedDataItem item = (IExtendedDataItem)project;
			return (TranslatorInfo)item.ExtendedProperties["TranslatorInfo"];
		}

		public void Bind (Project project)
		{
			this.project = project;
			basename = project.Name.Trim ().ToLower ().Replace (' ', '_');
			//binding = Services.Languages.GetBindingPerLanguageName (project.LanguageName) as IDotNetLanguageBinding;
		}

		public void Dispose ()
		{
			if (builderProject != null)
				builderProject.Dispose ();
		}

		public TranslationProject TranslationProject
		{
			get
			{
				if (builderProject == null)
				{
					UpdateTranslatorFolder ();
					builderProject = TranslationProject.FindTranslationProject (project.ParentCombine);
				}
				return builderProject;
			}
		}
		
		public string GeneratedPotFile
		{
			get { return Path.Combine (TranslatorFolder, basename + ".pot" ); }
		}
		
		public string PotFiles
		{
			get { return Path.Combine (TranslatorFolder, "POTFILES" ); }
		}
				
		public string TranslatorFolder
		{
			get { return Path.Combine (project.BaseDirectory, "po"); }
		}
		
		public void UpdateTranslatorFolder ()
		{
			if (project == null)	// This happens when deserializing
				return;

			// This method synchronizes the current project configuration info
			// with the needed support files in the translator folder.

			Directory.CreateDirectory (TranslatorFolder);
				
			// Create the translator file if not found
			if (! File.Exists (PotFiles))
			{
				StreamWriter sw = new StreamWriter (PotFiles);
				sw.WriteLine ("[encoding: UTF-8]");
				sw.Close ();
			}
			
			// The the translator file to the project
			if (! project.IsFileInProject (PotFiles))
				project.AddFile (PotFiles, BuildAction.Nothing);
			
			if (! File.Exists (GeneratedPotFile))
			{
				// TODO Generate an empty .pot file
				StreamWriter swg = new StreamWriter (GeneratedPotFile);
				swg.WriteLine ("");
				swg.Close ();
			}

			// Add the generated file to the project, if not already there
			if (! project.IsFileInProject (GeneratedPotFile))
				project.AddFile (GeneratedPotFile, BuildAction.Nothing);

			// Add catalog references, if not already added.
			bool catalog = false;
			foreach (ProjectReference r in project.ProjectReferences)
			{
				if (r.Reference.StartsWith ("Mono.Posix") && r.ReferenceType == ReferenceType.Gac)
					catalog = true;
			}
			if (! catalog)
				project.ProjectReferences.Add (new ProjectReference (ReferenceType.Gac, typeof (Mono.Unix.Catalog).Assembly.FullName));
		}

//		CodeDomProvider GetCodeDomProvider ()
//		{
//			IDotNetLanguageBinding binding = Services.Languages.GetBindingPerLanguageName (project.LanguageName) as IDotNetLanguageBinding;
//			CodeDomProvider provider = binding.GetCodeDomProvider ();
//			if (provider == null)
//				throw new UserException ("Code generation not supported in language: " + project.LanguageName);
//			return provider;
//		}
	}
}
