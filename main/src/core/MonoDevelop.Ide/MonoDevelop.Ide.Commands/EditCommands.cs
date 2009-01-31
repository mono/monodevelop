// EditCommands.cs
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
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

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
		ToggleCodeComment,
		IndentSelection,
		UnIndentSelection,
		UppercaseSelection,
		LowercaseSelection,
		JoinWithNextLine,
		WordCount,
		MonodevelopPreferences,
		DefaultPolicies,
		InsertStandardHeader,
		
		ToggleFolding,
		ToggleAllFoldings,
		FoldDefinitions
	}
	
	internal class MonodevelopPreferencesHandler: CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.Workbench.ShowGlobalPreferencesDialog (null);
		}
	}
	
	internal class DefaultPoliciesHandler: CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.Workbench.ShowDefaultPoliciesDialog (null);
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
					tv.Buffer.Delete (ref cm, ref cme);
					tv.Buffer.EndUserAction ();
					return;
				}
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			object focus = IdeApp.Workbench.RootWindow.Focus;
			if (focus is Gtk.Editable)
				info.Enabled = ((Gtk.Editable)focus).IsEditable;
			else if (focus is Gtk.TextView)
				info.Enabled =  ((Gtk.TextView)focus).Editable;
			else
				info.Enabled = false;
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
			if (focus is Gtk.Editable)
				info.Enabled = ((Gtk.Editable)focus).IsEditable;
			else if (focus is Gtk.TextView)
				info.Enabled =  ((Gtk.TextView)focus).Editable;
			else
				info.Enabled = false;
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
			if (focus is Gtk.Editable)
				info.Enabled = ((Gtk.Editable)focus).IsEditable;
			else if (focus is Gtk.TextView)
				info.Enabled =  ((Gtk.TextView)focus).Editable;
			else
				info.Enabled = false;
		}
	}
	
	internal class InsertStandardHeaderHandler: CommandHandler
	{
		protected override void Run ()
		{
			Document doc = IdeApp.Workbench.ActiveDocument;
			ILanguageBinding lang = MonoDevelop.Projects.Services.Languages.GetBindingPerFileName (doc.Name);
			string header = MonoDevelop.Ide.StandardHeader.StandardHeaderService.GetHeader (doc.Project, lang.Language, doc.Name, false);
			doc.TextEditor.InsertText (0, header + "\n");
		}
		
		protected override void Update (CommandInfo info)
		{
			Document doc = IdeApp.Workbench.ActiveDocument;
			if (doc != null && doc.Name != null && doc.TextEditor != null) {
				ILanguageBinding lang = MonoDevelop.Projects.Services.Languages.GetBindingPerFileName (doc.Name);
				info.Enabled = lang != null;
			} else
				info.Enabled = false;
		}
	}	
	
	internal class DefaultSelectAllHandler: CommandHandler
	{
		protected override void Run ()
		{
			if (IdeApp.Workbench.RootWindow.HasToplevelFocus) {
				Gtk.Editable editable = IdeApp.Workbench.RootWindow.Focus as Gtk.Editable;
				if (editable != null) {
					editable.SelectRegion (0, -1);
					return;
				}
				Gtk.TextView tv = IdeApp.Workbench.RootWindow.Focus as Gtk.TextView;
				if (tv != null) {
					tv.Buffer.SelectRange (tv.Buffer.StartIter, tv.Buffer.EndIter);
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
