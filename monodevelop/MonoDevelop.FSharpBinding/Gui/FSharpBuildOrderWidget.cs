using System;
namespace MonoDevelop.FSharp.Gui
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class FSharpBuildOrderWidget : Gtk.Bin
	{
		public FSharpBuildOrderWidget ()
		{
			this.Build ();
		}

    public Gtk.Button ButtonUp { get { return btnUp; } }
    public Gtk.Button ButtonDown { get { return btnDown; } }
    public Gtk.TreeView ListItems { get { return treeItemList; } }
  }
}

