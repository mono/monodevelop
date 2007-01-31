
using System;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Commands
{
	public enum EditCommands
	{
		Copy,
		Cut,
		Paste,
		Delete,
		Rename,
		Undo,
		Redo,
		SelectAll,
		CommentCode,
		UncommentCode,
		IndentSelection,
		UnIndentSelection,
		UppercaseSelection,
		LowercaseSelection,
		WordCount,
		MonodevelopPreferences
	}
	
	internal class MonodevelopPreferencesHandler: CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.Workbench.ShowGlobalPreferencesDialog (null);
		}
	}
	
	internal class DefaultDeleteHandler: CommandHandler
	{
		protected override void Run ()
		{
			if (IdeApp.Workbench.RootWindow.HasToplevelFocus) {
				Gtk.Editable editable = IdeApp.Workbench.RootWindow.Focus as Gtk.Editable;
				if (editable != null) {
					int cm;
					int cme;
					if (!editable.GetSelectionBounds (out cm, out cme)) {
						cm = editable.Position;
						cme = cm + 1;
					}
					editable.DeleteText (cm, cme);
					return;
				}
				Gtk.TextView tv = IdeApp.Workbench.RootWindow.Focus as Gtk.TextView;
				if (tv != null) {
					tv.Buffer.BeginUserAction ();
					Gtk.TextIter cm;
					Gtk.TextIter cme;
					if (!tv.Buffer.GetSelectionBounds (out cm, out cme)) {
						cm = tv.Buffer.GetIterAtMark (tv.Buffer.InsertMark);
						cme = cm;
						cme.ForwardCursorPosition ();
					}
					tv.Buffer.Delete (cm, cme);
					tv.Buffer.EndUserAction ();
					return;
				}
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			object focus = IdeApp.Workbench.RootWindow.Focus;
			info.Enabled = (focus is Gtk.Editable || focus is Gtk.TextView); 
		}
	}
	
	internal class DefaultCopyHandler: CommandHandler
	{
		protected override void Run ()
		{
			if (IdeApp.Workbench.RootWindow.HasToplevelFocus) {
				Gtk.Editable editable = IdeApp.Workbench.RootWindow.Focus as Gtk.Editable;
				if (editable != null) {
					editable.CopyClipboard ();
					return;
				}
				Gtk.TextView tv = IdeApp.Workbench.RootWindow.Focus as Gtk.TextView;
				if (tv != null) {
					Gtk.Clipboard clipboard = Gtk.Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
					tv.Buffer.CopyClipboard (clipboard);
					return;
				}
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			object focus = IdeApp.Workbench.RootWindow.HasToplevelFocus ? IdeApp.Workbench.RootWindow.Focus : null;
			info.Enabled = (focus is Gtk.Editable || focus is Gtk.TextView); 
		}
	}	
	
	internal class DefaultCutHandler: CommandHandler
	{
		protected override void Run ()
		{
			if (IdeApp.Workbench.RootWindow.HasToplevelFocus) {
				Gtk.Editable editable = IdeApp.Workbench.RootWindow.Focus as Gtk.Editable;
				if (editable != null) {
					editable.CutClipboard ();
					return;
				}
				Gtk.TextView tv = IdeApp.Workbench.RootWindow.Focus as Gtk.TextView;
				if (tv != null) {
					Gtk.Clipboard clipboard = Gtk.Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
					tv.Buffer.CutClipboard (clipboard, true);
					return;
				}
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			object focus = IdeApp.Workbench.RootWindow.HasToplevelFocus ? IdeApp.Workbench.RootWindow.Focus : null;
			info.Enabled = (focus is Gtk.Editable || focus is Gtk.TextView); 
		}
	}	
	
	internal class DefaultPasteHandler: CommandHandler
	{
		protected override void Run ()
		{
			if (IdeApp.Workbench.RootWindow.HasToplevelFocus) {
				Gtk.Editable editable = IdeApp.Workbench.RootWindow.Focus as Gtk.Editable;
				if (editable != null) {
					editable.PasteClipboard ();
					return;
				}
				Gtk.TextView tv = IdeApp.Workbench.RootWindow.Focus as Gtk.TextView;
				if (tv != null) {
					Gtk.Clipboard clipboard = Gtk.Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
					tv.Buffer.PasteClipboard (clipboard);
					return;
				}
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			object focus = IdeApp.Workbench.RootWindow.HasToplevelFocus ? IdeApp.Workbench.RootWindow.Focus : null;
			info.Enabled = (focus is Gtk.Editable || focus is Gtk.TextView); 
		}
	}	
}
