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
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.ProgressMonitoring;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Components;
using MonoDevelop.Components.Commands;
using MonoDevelop.Projects;

namespace MonoDevelop.Gettext.NodeBuilders
{
	public class TranslationProjectNodeBuilder : TypeNodeBuilder
	{
		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/Deployment/ProjectBrowser/ContextMenu/TranslationProject"; }
		}
		
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
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			TranslationProject project = dataObject as TranslationProject;
			if (project == null)
				return;
			label = project.Name;
			icon = Context.GetIcon ("md-gettext-project");
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
				
			foreach (Translation translation in project.Translations)
				builder.AddChild (translation);
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
				
				bool yes = MonoDevelop.Core.Gui.MessageService.AskQuestion (GettextCatalog.GetString (
					"Do you really want to remove the translations from solution {0}?", project.ParentFolder.Name), AlertButton.Cancel, AlertButton.Remove) == AlertButton.Remove;

				if (yes) {
					Solution sol = project.ParentSolution;
					project.ParentFolder.Items.Remove (project);
					project.Dispose ();
					IdeApp.ProjectOperations.Save (sol);
				}
			}
					
			[CommandHandler (MonoDevelop.Ide.Commands.ProjectCommands.Options)]
			public void OnOptions ()
			{
				TranslationProject project = CurrentNode.DataItem as TranslationProject;
				if (project == null)
					return;
				TranslationProjectOptionsDialog options = new TranslationProjectOptionsDialog (project);
				options.Run ();
				IdeApp.Workspace.Save ();
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
					int response = 0;
					chooser.Response += delegate(object o, ResponseArgs args) {
						response = (int)args.ResponseId;
					};
					chooser.Run ();
					
					if (response == (int)ResponseType.Ok) {
						string language = chooser.Language + (chooser.HasCountry ? "_" + chooser.Country : "");
					
						using (IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor (monitorTitle, "md-package", true, true)) {
							project.AddNewTranslation (language, monitor);
							UpdateTranslations (project);
						}
					}
					
				} finally {
					chooser.Destroy ();
				}
			}
			static IAsyncOperation currentUpdateTranslationOperation = MonoDevelop.Core.ProgressMonitoring.NullAsyncOperation.Success;
			
			void UpdateTranslationsAsync (object ob)
			{
				object[] data = (object[]) ob;
				IProgressMonitor monitor = (IProgressMonitor) data [0];
				TranslationProject project = (TranslationProject) data [1];
				
				try {
					project.UpdateTranslations (monitor);
					Gtk.Application.Invoke (delegate {
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
				IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetBuildProgressMonitor ();
				currentUpdateTranslationOperation = monitor.AsyncOperation;
				DispatchService.BackgroundDispatch (new StatefulMessageHandler (UpdateTranslationsAsync), new object[] {monitor, project});
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
