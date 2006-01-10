
using System;
using Gtk;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components.Commands;

namespace GladeAddIn.Gui
{
	public class GladeProjectPad: AbstractPadContent
	{
		Gladeui.ProjectView pview;
		
		public GladeProjectPad (): base ("")
		{
			pview = new Gladeui.ProjectView (Gladeui.ProjectViewType.Tree);
			GladeService.App.AddProjectView (pview);
			pview.ShowAll ();
		}
		
		public override Gtk.Widget Control {
			get { return pview; }
		}
		
		[CommandHandler (EditCommands.Undo)]
		protected void OnUndo ()
		{
			GladeService.App.CommandUndo ();
		}
		
		[CommandHandler (EditCommands.Redo)]
		protected void OnRedo ()
		{
			GladeService.App.CommandRedo ();
		}
		
		[CommandHandler (EditCommands.Copy)]
		protected void OnCopy ()
		{
			GladeService.App.CommandCopy ();
		}
		
		[CommandHandler (EditCommands.Cut)]
		protected void OnCut ()
		{
			GladeService.App.CommandCut ();
		}
		
		[CommandHandler (EditCommands.Paste)]
		protected void OnPaste ()
		{
			GladeService.App.CommandPaste ();
		}
		
		[CommandHandler (EditCommands.Delete)]
		protected void OnDelete ()
		{
			GladeService.App.CommandDelete ();
		}
	}
}
