//
// ProjectFolderNodeBuilderExtension.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Components.Commands;
using Mono.Addins.Description;
using MonoDevelop.Ide.Gui.Components;
using Gtk;

namespace MonoDevelop.AddinAuthoring
{
	class ProjectFolderNodeBuilderExtension: NodeBuilderExtension
	{
		public override bool CanBuildNode (Type dataType)
		{
			return typeof(ProjectFolder).IsAssignableFrom (dataType) ||
					typeof(ProjectFile).IsAssignableFrom (dataType) ||
					typeof(DotNetProject).IsAssignableFrom (dataType);
		}
		
		public override Type CommandHandlerType {
			get { return typeof(AddinFolderCommandHandler); }
		}
		
		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			if (dataObject is DotNetProject) {
				AddinData data = AddinData.GetAddinData ((DotNetProject)dataObject);
				if (data != null)
					builder.AddChild (data);
			}
		}
		
		public override void OnNodeAdded (object dataObject)
		{
			if (dataObject is DotNetProject) {
				AddinData data = AddinData.GetAddinData ((DotNetProject)dataObject);
				if (data != null)
					data.Changed += OnProjectChanged;
			}
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			if (dataObject is DotNetProject) {
				AddinData data = AddinData.GetAddinData ((DotNetProject)dataObject);
				if (data != null)
					data.Changed -= OnProjectChanged;
			}
		}

		
		void OnProjectChanged (object s, EventArgs a)
		{
			AddinData data = (AddinData) s;
			ITreeBuilder tb = Context.GetTreeBuilder (data.Project);
			if (tb != null)
				tb.UpdateAll ();
		}

	}
	
	class AddinFolderCommandHandler: NodeCommandHandler
	{
		public override void ActivateItem ()
		{
			if (CurrentNode.DataItem is AddinData) {
				AddinData data = (AddinData) CurrentNode.DataItem;
				IdeApp.Workbench.OpenDocument (data.AddinManifestFileName, true);
			}
		}

		[CommandHandler (Commands.AddExtension)]
		public void OnAddExtension ()
		{
			DotNetProject p = CurrentNode.GetParentDataItem (typeof(Project), true) as DotNetProject;
			AddinData data = AddinData.GetAddinData (p);
			
			AddinDescription desc = data.LoadAddinManifest ();
			ExtensionSelectorDialog dlg = new ExtensionSelectorDialog (data.AddinRegistry, null, desc.IsRoot,  false);
			if (dlg.Run () == (int) ResponseType.Ok) {
				foreach (object ob in dlg.GetSelection ())
					Console.WriteLine ("pp s: " + ob);
			}
			dlg.Destroy ();
		}

		[CommandUpdateHandler (Commands.AddExtension)]
		public void OnUpdateAddExtension (CommandInfo cinfo)
		{
			if (CurrentNode.DataItem is ProjectFolder || CurrentNode.DataItem is DotNetProject) {
				DotNetProject p = CurrentNode.GetParentDataItem (typeof(Project), true) as DotNetProject;
				cinfo.Visible = p != null && AddinData.GetAddinData (p) != null;
			}
		}
	}
}
