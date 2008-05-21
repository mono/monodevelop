
using System;
using Gtk;
using Mono.Addins;
using Mono.Addins.Description;
using MonoDevelop.Projects;

namespace MonoDevelop.AddinAuthoring
{
	public class NodeEditorDialog: Dialog
	{
		NodeEditorWidget editor;
		
		public NodeEditorDialog (DotNetProject project, AddinRegistry reg, ExtensionNodeType ntype, AddinDescription parentAddinDescription, string parentPath, ExtensionNodeDescription node)
		{
			editor = new NodeEditorWidget (project, reg, ntype, parentAddinDescription, parentPath, node);
			editor.BorderWidth = 12;
			this.VBox.PackStart (editor, true, true, 0);
			this.AddButton (Stock.Cancel, ResponseType.Cancel);
			this.AddButton (Stock.Ok, ResponseType.Ok);
			this.DefaultWidth = 400;
			ShowAll ();
		}
		
		public void Save ()
		{
			editor.Save ();
		}
	}
}
