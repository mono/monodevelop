//
// TranslatorCoreService.cs
//
// Author:
//   Rafael 'Monoman' Teixeira
//
// Copyright (C) 2006 Rafael 'Monoman' Teixeira
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
using System.IO;
using System.Xml;
using System.Collections;
using System.CodeDom;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Gettext.Translator
{
	class TranslatorCoreService
	{
		public static void Initialize ()
		{
			IdeApp.ProjectOperations.FileChangedInProject += new ProjectFileEventHandler (OnFileChanged);
			IdeApp.ProjectOperations.CombineOpened += new CombineEventHandler (OnCombineOpened);
		}
		
		public static TranslatorInfo GetTranslatorInfo (Project project)
		{
			if (! (project is DotNetProject))
				return null;

			TranslatorInfo info = TranslatorInfo.GetFrom ((DotNetProject)project);
			if (info == null)
				return null;

			info.Bind ((DotNetProject)project);
			return info;
		}

		public static TranslatorInfo EnableTranslatorSupport (Project project)
		{
			TranslatorInfo info = GetTranslatorInfo (project);
			if (info != null)
				return info;

			info = new TranslatorInfo ((DotNetProject)project);
			info.UpdateTranslatorFolder ();
			return info;
		}

		static void OnFileChanged (object s, ProjectFileEventArgs args)
		{
//			if (IdeApp.ProjectOperations.CurrentOpenCombine == null)
//				return;
			
/*			foreach (Project project in IdeApp.ProjectOperations.CurrentOpenCombine.GetAllProjects ())
			{
				if (! project.IsFileInProject (args.FileName))
					continue;*/
			
			Console.WriteLine ("Update:" + args.ProjectFile.FilePath);
			Project project = args.Project;
			TranslatorInfo info = TranslatorCoreService.GetTranslatorInfo (project);
			if (info == null)
				return;

			info.UpdateTranslatorFolder ();
			
			IdeApp.ProjectOperations.ParserDatabase.UpdateFile (project, args.ProjectFile.FilePath, null);
//			}
		}

		static void OnCombineOpened (object sender, CombineEventArgs e)
		{
			foreach (CombineEntry entry in e.Combine.GetAllProjects ())
			{
				if (entry is Project && ! (entry is TranslationProject))
				{
					Project project = entry as Project;
					Translator.TranslationProjectInfo info =
						project.ExtendedProperties ["MonoDevelop.Gettext.TranslationInfo"] as Translator.TranslationProjectInfo;
					if (info == null)
					{
						info = new Translator.TranslationProjectInfo ();
						project.ExtendedProperties ["MonoDevelop.Gettext.TranslationInfo"] = info;
					}
					info.Project = project;
				}
			}
		}
	}

	public class TranslatorCoreStartupCommand : CommandHandler
	{
		protected override void Run ()
		{
			TranslatorCoreService.Initialize ();
		}
	}
}
