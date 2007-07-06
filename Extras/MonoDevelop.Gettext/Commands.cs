//
// Commands.cs
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
using System.Collections;
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.ProgressMonitoring;
using MonoDevelop.Projects;
using MonoDevelop.Components;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using Gtk;

namespace MonoDevelop.Gettext
{
	public enum Commands
	{
		AddTranslation,
		IncludeInTranslation
	}

	class TranslatorNodeExtension : NodeBuilderExtension
	{
		public override bool CanBuildNode (Type dataType)
		{
			return dataType == typeof (Translator.TranslationProject);
		}

		public override Type CommandHandlerType
		{
			get { return typeof (TranslatorCommandHandler); }
		}
	}

	class FileNodeExtension : NodeBuilderExtension
	{
		public override bool CanBuildNode (Type dataType)
		{
			return typeof (ProjectFile).IsAssignableFrom (dataType);
		}

		public override Type CommandHandlerType
		{
			get { return typeof (FileCommandHandler); }
		}
	}

	// This Extension is hiding References and Resources folders in project pad
	class HiddenFoldersExtension : NodeBuilderExtension
	{
		public override bool CanBuildNode (Type dataType)
		{
			return
				typeof (ProjectReferenceCollection).IsAssignableFrom (dataType) ||
				typeof (ResourceFolder).IsAssignableFrom (dataType);
		}

		public override void GetNodeAttributes (ITreeNavigator parentNode, object dataObject, ref NodeAttributes attributes)
		{
			if (parentNode.GetParentDataItem (typeof (Translator.TranslationProject), true) != null)
				attributes |= NodeAttributes.Hidden;
		}

	}
	
	public class TranslatorCommandHandler : NodeCommandHandler
	{
//		[CommandHandler (Commands.GenerateFiles)]
//		public void OnGenerateFiles ()
//		{
//			if (CurrentNode.DataItem is Combine || CurrentNode.DataItem is Project)
//			{
//				string msg = GettextCatalog.GetString ("Translation files already exist for this solution/project. Would you like to overwrite them?");
//				string monitorTitle = GettextCatalog.GetString ("Translator Output");
//
//				CombineEntry combine = (CombineEntry)CurrentNode.DataItem;
//				
//				if (Translator.TranslationManager.HasTranslationFiles (combine))
//				{
//					if (! MonoDevelop.Core.Gui.Services.MessageService.AskQuestion (msg))
//						return;
//				}
//
//				using (IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor (monitorTitle, "md-package", true, true))	
//				{
//					Translator.TranslationManager.GenerateFiles (combine, monitor);
//				}
//			}
//		}	


		[CommandHandler (Commands.AddTranslation)]
		public void OnAddTranslation (CommandInfo cinfo)
		{
			ProjectFile file = (ProjectFile)CurrentNode.DataItem;
			if (file.Project is Translator.TranslationProject)
				cinfo.Visible = true;
			else
				cinfo.Visible = false;
		}
	}

	public class FileCommandHandler : NodeCommandHandler
	{
		[CommandHandler (Commands.IncludeInTranslation)]
		public void IncludeInTranslation ()
		{
			ProjectFile file = (ProjectFile)CurrentNode.DataItem;
			Project project = file.Project;
			if (project != null)
			{
				Translator.TranslationProjectInfo info =
					project.ExtendedProperties ["MonoDevelop.Gettext.TranslationInfo"] as Translator.TranslationProjectInfo;
				if (info != null)
				{
					info.SetFileExcluded (file.FilePath, ! info.IsFileExcluded (file.FilePath));
					IdeApp.ProjectOperations.SaveProject (project);
				}
			}
		}

		[CommandUpdateHandler (Commands.IncludeInTranslation)]
		public void IncludeInTranslation (CommandInfo cinfo)
		{
			if (CurrentNode.DataItem is ProjectFile &&
				! (((ProjectFile)CurrentNode.DataItem).Project is Translator.TranslationProject) &&
				Translator.TranslationProject.HasTranslationFiles (((ProjectFile)CurrentNode.DataItem).Project))
			{
				ProjectFile file = (ProjectFile)CurrentNode.DataItem;
				Project project = file.Project;

				cinfo.Visible = true;
				cinfo.Enabled = true;
				Translator.TranslationProjectInfo info =
					project.ExtendedProperties ["MonoDevelop.Gettext.TranslationInfo"] as Translator.TranslationProjectInfo;

				if (info != null)
				{
					cinfo.Checked = ! info.IsFileExcluded (file.FilePath);
				}
				return;
			}
			cinfo.Visible = false;
		}
	}
}
