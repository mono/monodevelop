// ReferencesFolderNodeBuilder.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Components.Commands;
using Mono.Addins;
using Mono.Addins.Description;

namespace MonoDevelop.AddinAuthoring
{
	public class ReferencesFolderNodeBuilder: NodeBuilderExtension
	{
		public override bool CanBuildNode (Type dataType)
		{
			return typeof(ProjectReferenceCollection).IsAssignableFrom (dataType);
		}
		
		public override Type CommandHandlerType {
			get { return typeof(ReferencesFolderCommandHandler); }
		}
		
		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			DotNetProject p = builder.GetParentDataItem (typeof(Project), false) as DotNetProject;
			if (p != null) {
				AddinData data = AddinData.GetAddinData (p);
				if (data != null) {
					foreach (Dependency dep in data.CachedAddinManifest.MainModule.Dependencies)
						builder.AddChild (dep);
				}
			}
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			DotNetProject p = builder.GetParentDataItem (typeof(Project), false) as DotNetProject;
			if (p != null) {
				AddinData data = AddinData.GetAddinData (p);
				if (data != null)
					return data.CachedAddinManifest.MainModule.Dependencies.Count > 0;
			}
			return false;
		}

	}
	
	class ReferencesFolderCommandHandler: NodeCommandHandler
	{
		[CommandHandler (Commands.AddAddinDependency)]
		public void AddAddinDependency ()
		{
			DotNetProject p = CurrentNode.GetParentDataItem (typeof(Project), true) as DotNetProject;
			AddinData data = AddinData.GetAddinData (p);
			
			ExtensionSelectorDialog dlg = new ExtensionSelectorDialog (data.AddinRegistry, null, data.CachedAddinManifest.IsRoot,  true);
			if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
				AddinAuthoringService.AddReferences (data, dlg.GetSelection ());
			}
			dlg.Destroy ();
		}

		[CommandUpdateHandler (Commands.AddAddinDependency)]
		public void OnUpdateAddAddinDependency (CommandInfo cinfo)
		{
			DotNetProject p = CurrentNode.GetParentDataItem (typeof(Project), true) as DotNetProject;
			cinfo.Visible = p != null && AddinData.GetAddinData (p) != null;
		}
	}
}
