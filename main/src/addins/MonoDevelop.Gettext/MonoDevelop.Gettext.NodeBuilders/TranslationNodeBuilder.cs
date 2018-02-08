//
// TranslationNodeBuilder.cs
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
using System.IO;

using MonoDevelop.Core;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide;
using System.Threading.Tasks;

namespace MonoDevelop.Gettext.NodeBuilders
{
	class TranslationNodeBuilder : TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(Translation); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(TranslationNodeCommandHandler); }
		}

		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			Translation translation = dataObject as Translation;
			if (translation == null)
				return "Translation";
			return translation.IsoCode;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			Translation translation = dataObject as Translation;
			if (translation == null)
				return;
			int idx = translation.IsoCode.IndexOf ('_');
			string language;
			string country;
			if (idx > 0) {
				language = translation.IsoCode.Substring (0, idx);
				country  = translation.IsoCode.Substring (idx + 1);
				
			} else {
				language = translation.IsoCode;
				country  = "";
			}
			
			if (IsoCodes.IsKnownLanguageCode (language)) {
				if (IsoCodes.IsKnownCountryCode (country)) {
					nodeInfo.Label = IsoCodes.LookupLanguageCode (language).Name + "/" + IsoCodes.LookupCountryCode (country).Name + " (" + translation.IsoCode + ")";
				} else {
					nodeInfo.Label = IsoCodes.LookupLanguageCode (language).Name +  " (" + translation.IsoCode + ")";
				}
			} else {
				nodeInfo.Label = "(" + translation.IsoCode + ")";
			}			
			nodeInfo.Icon = Context.GetIcon ("md-po-file");
		}
		
		class TranslationNodeCommandHandler : NodeCommandHandler
		{
			public override void ActivateItem ()
			{
				TranslationProject project     = CurrentNode.GetParentDataItem (typeof(TranslationProject), false) as TranslationProject;
				Translation        translation = CurrentNode.DataItem as Translation;
				if (project == null || translation == null)
					return;
				IdeApp.Workbench.OpenDocument (Path.Combine (project.BaseDirectory, translation.FileName), project);
			}
			
			public override void DeleteItem ()
			{
				TranslationProject project     = CurrentNode.GetParentDataItem (typeof(TranslationProject), false) as TranslationProject;
				Translation        translation = CurrentNode.DataItem as Translation;
				if (project == null || translation == null)
					return;
				
				bool yes = MessageService.AskQuestion (GettextCatalog.GetString (
					"Do you really want to remove the translation {0} from solution {1}?", translation.IsoCode, project.ParentFolder.Name), AlertButton.Cancel, AlertButton.Remove) == AlertButton.Remove;

				if (yes) {
					string fileName = Path.Combine (project.BaseDirectory, translation.FileName);
					if (File.Exists (fileName)) {
						FileService.DeleteFile (fileName);
					}
					
					project.RemoveTranslation (translation.IsoCode);
					IdeApp.ProjectOperations.SaveAsync (project);
				}
			}
			
			[CommandHandler (Commands.UpdateTranslation)]
			public void OnUpdateTranslation ()
			{
				TranslationProject project     = CurrentNode.GetParentDataItem (typeof(TranslationProject), false) as TranslationProject;
				Translation        translation = CurrentNode.DataItem as Translation;
				if (project == null || translation == null)
					return;
				UpdateTranslations (project, translation);
			}
			
			static Task currentUpdateTranslationOperation = Task.FromResult (0);
			
			void UpdateTranslationsAsync (ProgressMonitor monitor, TranslationProject project, Translation translation)
			{
				try {
					project.UpdateTranslations (monitor, false, translation);
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

			void UpdateTranslations (TranslationProject project, Translation translation)
			{
				if (currentUpdateTranslationOperation != null && !currentUpdateTranslationOperation.IsCompleted) 
					return;
				ProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetBuildProgressMonitor ();
				currentUpdateTranslationOperation = Task.Run (() => UpdateTranslationsAsync (monitor, project, translation));
			}
		}
	}
}
