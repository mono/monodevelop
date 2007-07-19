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
	public class TranslationNodeBuilder : TypeNodeBuilder
	{
		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/Deployment/ProjectBrowser/ContextMenu/Translation"; }
		}
		
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
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
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
					label = IsoCodes.LookupLanguageCode (language).Name + "/" + IsoCodes.LookupCountryCode (country).Name + " (" + translation.IsoCode + ")";
				} else {
					label = IsoCodes.LookupLanguageCode (language).Name +  " (" + translation.IsoCode + ")";
				}
			} else {
				label = "(" + translation.IsoCode + ")";
			}			
			icon = Context.GetIcon ("md-gettext-locale");
		}
		
		class TranslationNodeCommandHandler : NodeCommandHandler
		{
			public override void ActivateItem ()
			{
				TranslationProject project     = CurrentNode.GetParentDataItem (typeof(TranslationProject), false) as TranslationProject;
				Translation        translation = CurrentNode.DataItem as Translation;
				if (project == null || translation == null)
					return;
				IdeApp.Workbench.OpenDocument (Path.Combine (project.BaseDirectory, translation.FileName));
			}
			
			[CommandHandler (MonoDevelop.Ide.Commands.EditCommands.Delete)]
			public void OnDelete ()
			{
				TranslationProject project     = CurrentNode.GetParentDataItem (typeof(TranslationProject), false) as TranslationProject;
				Translation        translation = CurrentNode.DataItem as Translation;
				if (project == null || translation == null)
					return;
				
				bool yes = MonoDevelop.Core.Gui.Services.MessageService.AskQuestion (GettextCatalog.GetString (
					"Do you really want to remove the translation {0} from solution {1}?", translation.IsoCode, project.ParentCombine.Name));

				if (yes) {
					string fileName = Path.Combine (project.BaseDirectory, translation.FileName);
					if (File.Exists (fileName)) {
						Runtime.FileService.DeleteFile (fileName);
					}
					
					project.RemoveTranslation (translation.IsoCode);
					IdeApp.ProjectOperations.SaveCombineEntry (project);
				}
			}
			
		}
	}
}
