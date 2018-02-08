//
// TranslationProjectNodeBuilder.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide;
using System.Threading.Tasks;

namespace MonoDevelop.Gettext.NodeBuilders
{
	class TranslationProjectNodeBuilder : TypeNodeBuilder
	{
		public override Type CommandHandlerType {
			get { return typeof(TranslationProjectNodeCommandHandler); }
		}
		
		public override Type NodeDataType {
			get { return typeof(TranslationProject); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			TranslationProject project = dataObject as TranslationProject;
			if (project == null)
				return "TranslationProject";
			return project.Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			TranslationProject project = dataObject as TranslationProject;
			if (project == null)
				return;
			nodeInfo.Label = project.Name;
			nodeInfo.Icon = Context.GetIcon ("md-gettext-project");
		}
		
		public override void OnNodeAdded (object dataObject)
		{
			TranslationProject project = dataObject as TranslationProject;
			if (project == null)
				return;
			project.TranslationAdded += new EventHandler (UpdateTranslationNodes);
			project.TranslationRemoved += new EventHandler (UpdateTranslationNodes);
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			TranslationProject project = dataObject as TranslationProject;
			if (project == null)
				return;
			project.TranslationRemoved -= new EventHandler (UpdateTranslationNodes);
			project.TranslationAdded -= new EventHandler (UpdateTranslationNodes);
		}
		
		void UpdateTranslationNodes (object sender, EventArgs e)
		{
			ITreeBuilder treeBuilder = Context.GetTreeBuilder (sender);
			if (treeBuilder != null) 
				treeBuilder.UpdateAll ();
		}
		
		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			TranslationProject project = dataObject as TranslationProject;
			if (project == null)
				return;

			builder.AddChildren (project.Translations);
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			TranslationProject project = dataObject as TranslationProject;
			if (project == null)
				return false;
			return project.Translations.Count > 0;
		}
		
		class TranslationProjectNodeCommandHandler : NodeCommandHandler
		{
			public override void DeleteItem ()
			{
				TranslationProject project = CurrentNode.DataItem as TranslationProject;
				if (project == null)
					return;
				IdeApp.ProjectOperations.RemoveSolutionItem (project);
			}
					
			[CommandHandler (MonoDevelop.Ide.Commands.ProjectCommands.Options)]
			public void OnOptions ()
			{
				TranslationProject project = CurrentNode.DataItem as TranslationProject;
				if (project == null)
					return;
				
				using (var dlg = new TranslationProjectOptionsDialog (project))
					MessageService.ShowCustomDialog (dlg);
				IdeApp.Workspace.SaveAsync ();
			}
			
			[CommandUpdateHandler (Commands.AddTranslation)]
			public void OnUpdateAddTranslation (CommandInfo info)
			{
				info.Enabled = currentUpdateTranslationOperation == null  ||  currentUpdateTranslationOperation.IsCompleted;
			}
			
			[CommandHandler (Commands.AddTranslation)]
			public void OnAddTranslation ()
			{
				TranslationProject project = CurrentNode.DataItem as TranslationProject;
				if (project == null)
					return;
				
				string monitorTitle = GettextCatalog.GetString ("Translator Output");
				Translator.LanguageChooserDialog chooser = new Translator.LanguageChooserDialog ();
				try {
					if (MessageService.RunCustomDialog (chooser) == (int)ResponseType.Ok) {
						string language = chooser.Language + (chooser.HasCountry ? "_" + chooser.Country : "");
					
						using (ProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor (monitorTitle, "md-package", true, true)) {
							project.AddNewTranslation (language, monitor);
							UpdateTranslations (project);
						}
					}
					
				} finally {
					chooser.Destroy ();
					chooser.Dispose ();
				}
			}
			static Task currentUpdateTranslationOperation = Task.FromResult (0);
			
			void UpdateTranslationsAsync (ProgressMonitor monitor, TranslationProject project)
			{
				try {
					project.UpdateTranslations (monitor, false);
					Gtk.Application.Invoke ((o, args) => {
						POEditorWidget.ReloadWidgets ();
					});
				} catch (Exception ex) {
					monitor.ReportError (GettextCatalog.GetString ("Translation update failed."), ex);
				} finally {
					monitor.Log.WriteLine ();
					monitor.Log.WriteLine (GettextCatalog.GetString ("---------------------- Done ----------------------"));
					monitor.Dispose ();
				}
			}

			void UpdateTranslations (TranslationProject project)
			{
				if (currentUpdateTranslationOperation != null && !currentUpdateTranslationOperation.IsCompleted) 
					return;
				ProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetBuildProgressMonitor ();
				currentUpdateTranslationOperation = Task.Run (() => UpdateTranslationsAsync (monitor, project));
			}
			
			[CommandHandler (Commands.UpdateTranslations)]
			public void OnUpdateTranslations ()
			{
				TranslationProject project = CurrentNode.DataItem as TranslationProject;
				if (project == null)
					return;
				UpdateTranslations (project);
			}
			
		}
	}
}
