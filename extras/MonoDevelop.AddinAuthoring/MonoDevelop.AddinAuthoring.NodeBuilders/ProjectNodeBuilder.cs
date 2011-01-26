//
// ProjectNodeBuilder.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.Collections.Generic;
using System.Text;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Components;
using Mono.Addins.Description;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.AddinAuthoring.NodeBuilders
{
	public class ProjectNodeBuilder: TypeNodeBuilder
	{
		SolutionItemRenamedEventHandler projectNameChanged;

		public ProjectNodeBuilder ()
		{
			projectNameChanged = (SolutionItemRenamedEventHandler) DispatchService.GuiDispatch (new SolutionItemRenamedEventHandler (OnProjectRenamed));
		}

		protected override void Initialize ()
		{
		}
		public override void Dispose ()
		{
		}
		
		public override Type NodeDataType {
			get { return typeof(DotNetProject); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/AddinAuthoring/ContextMenu/ExtensionModelPad/Project"; }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(ProjectCommandHandler); }
		}
		
		public override void OnNodeAdded (object dataObject)
		{
			DotNetProject project = (DotNetProject) dataObject;
			project.NameChanged += projectNameChanged;
			
			AddinData data = project.GetAddinData ();
			if (data != null) {
				data.Changed += HandleDataChanged;
			}
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			DotNetProject project = (DotNetProject) dataObject;
			project.NameChanged -= projectNameChanged;
			
			AddinData data = project.GetAddinData ();
			if (data != null)
				data.Changed -= HandleDataChanged;
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((Project)dataObject).Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			Project p = dataObject as Project;
			label = p.Name;
			icon = Context.GetIcon (p.StockIcon);
		}

		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			DotNetProject project = (DotNetProject) dataObject;
			AddinData data = project.GetAddinData ();
			if (data != null && data.CachedAddinManifest != null) {
				AddinDescription adesc = data.CachedAddinManifest;
				HashSet<string> localPoints = new HashSet<string> ();
				foreach (ExtensionPoint ep in adesc.ExtensionPoints) {
					builder.AddChild (ep);
					localPoints.Add (ep.Path);
				}
				foreach (Extension ext in adesc.MainModule.Extensions) {
					if (!localPoints.Contains (ext.Path))
						builder.AddChild (ext);
				}
			}
		}
		
		public static IEnumerable<object> BuildChildNodes (ITreeBuilder treeBuilder, AddinData data, Func<AddinDescription,bool> includeAddin)
		{
			AddinDescription adesc = data.CachedAddinManifest;
			HashSet<string> localPoints = new HashSet<string> ();
			if (includeAddin (adesc)) {
				foreach (ExtensionPoint ep in adesc.ExtensionPoints) {
					yield return ep;
					localPoints.Add (ep.Path);
				}
			}
			foreach (Extension ext in adesc.MainModule.Extensions) {
				if (!localPoints.Contains (ext.Path)) {
					if (includeAddin != null) {
						object ob = ext.GetExtendedObject ();
						while (!(ob is ExtensionPoint) && ob != null) {
							if (ob is Extension)
								ob = ((Extension)ob).GetExtendedObject ();
							else if (ob is ExtensionNodeDescription)
								ob = ((ExtensionNodeDescription)ob).Parent;
							else
								ob = null;
						}
						if (ob != null && includeAddin (((ExtensionPoint)ob).ParentAddinDescription))
							yield return ext;
					} else {
						yield return ext;
					}
				}
			}
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}

		void HandleDataChanged (object sender, EventArgs e)
		{
			AddinData data = (AddinData) sender;
			ITreeBuilder tb = Context.GetTreeBuilder (data.Project);
			if (tb != null) tb.UpdateAll ();
		}
		
		void OnProjectRenamed (object sender, SolutionItemRenamedEventArgs e)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (e.SolutionItem);
			if (tb != null) tb.Update ();
		}
		
	}

	class ProjectCommandHandler: NodeCommandHandler
	{
		[CommandHandler (Commands.AddExtensionPoint)]
		public void OnAddExtensionPoint ()
		{
			DotNetProject project = (DotNetProject) CurrentNode.DataItem;
			if (project == null)
				return;
			AddinData data = project.GetAddinData ();
			if (project == null)
				return;
			
			ExtensionPoint ep = new ExtensionPoint ();
			NewExtensionPointDialog dlg = new NewExtensionPointDialog (project, data.AddinRegistry, data.CachedAddinManifest, ep);
			try {
				if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
					data.CachedAddinManifest.ExtensionPoints.Add (ep);
					data.SaveAddinManifest ();
					data.NotifyChanged (false);
				}
			} finally {
				dlg.Destroy ();
			}
		}
		
		[CommandHandler (Commands.AddExtension)]
		public void OnAddExtension ()
		{
			DotNetProject project = (DotNetProject) CurrentNode.DataItem;
			if (project == null)
				return;
			AddinData data = project.GetAddinData ();
			if (project == null)
				return;
			
			AddinDescription adesc = data.CachedAddinManifest;
			
			ExtensionSelectorDialog dlg = new ExtensionSelectorDialog (data.AddinRegistry, adesc, adesc.IsRoot, false);
			if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
				foreach (object ob in dlg.GetSelection ()) {
					AddinDescription desc = null;
					if (ob is ExtensionPoint) {
						ExtensionPoint ep = (ExtensionPoint) ob;
						Extension ext = new Extension (ep.Path);
						adesc.MainModule.Extensions.Add (ext);
						desc = (AddinDescription) ep.Parent;
					}
					else if (ob is ExtensionNodeDescription) {
						ExtensionNodeDescription node = (ExtensionNodeDescription) ob;
						desc = node.ParentAddinDescription;
						string path = "";
						while (node != null && !(node.Parent is Extension)) {
							if (!node.IsCondition)
								path = "/" + node.Id + path;
							node = node.Parent as ExtensionNodeDescription;
						}
						Extension eext = (Extension) node.Parent;
						Extension ext = new Extension (eext.Path + "/" + node.Id + path);
						adesc.MainModule.Extensions.Add (ext);
					}
					if (adesc.AddinId != desc.AddinId && !adesc.MainModule.DependsOnAddin (desc.AddinId))
						adesc.MainModule.Dependencies.Add (new AddinDependency (desc.AddinId));
				}
				adesc.Save ();
			}
			dlg.Destroy ();
		}
	}
}
