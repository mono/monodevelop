using Gtk;

using System;
using System.Runtime.InteropServices;

using MonoDevelop.TextEditor.Document;
using MonoDevelop.Commands;
using MonoDevelop.Gui;
using MonoDevelop.Gui.Dialogs;
using GtkSourceView;
using MonoDevelop.DefaultEditor;
using MonoDevelop.Services;
using MonoDevelop.Gui.Search;
using Stock = MonoDevelop.Gui.Stock;

namespace MonoDevelop.SourceEditor.Gui
{
	public class SourceEditor : ScrolledWindow
	{	
		public SourceEditorBuffer Buffer;
		public SourceEditorView View;
		public SourceEditorDisplayBindingWrapper DisplayBinding;
		
		static Gdk.Pixbuf dragIconPixbuf;
		static Gdk.Pixbuf executionMarkerPixbuf;
		static Gdk.Pixbuf breakPointPixbuf;
		
		static SourceEditor ()
		{
			dragIconPixbuf = new Gdk.Pixbuf (drag_icon_xpm);
			executionMarkerPixbuf = Runtime.Gui.Resources.GetIcon (Stock.ExecutionMarker);
			breakPointPixbuf = Runtime.Gui.Resources.GetIcon (Stock.BreakPoint);
		}
		
		protected SourceEditor (IntPtr ptr): base (ptr)
		{
		}
		
		public SourceEditor (SourceEditorDisplayBindingWrapper bind)
		{
			ShadowType = Gtk.ShadowType.In;
			DisplayBinding = bind;
			Buffer = new SourceEditorBuffer ();	
			View = new SourceEditorView (Buffer, this);
			Buffer.View = View;
			this.VscrollbarPolicy = PolicyType.Automatic;
			this.HscrollbarPolicy = PolicyType.Automatic;
			
			View.SetMarkerPixbuf ("SourceEditorBookmark", dragIconPixbuf);
			View.SetMarkerPixbuf ("ExecutionMark", executionMarkerPixbuf);
			View.SetMarkerPixbuf ("BreakpointMark", breakPointPixbuf);
			
			Add (View);
		}
		
		public new void Dispose ()
		{
			Buffer.Dispose ();
			Buffer = null;
			Remove (View);
			View.Dispose ();
			View = null;
			DisplayBinding = null;
			base.Dispose ();
		}

		public void ExecutingAt (int linenumber)
		{
			View.ExecutingAt (linenumber);
		}

		public void ClearExecutingAt (int linenumber)
		{
			View.ClearExecutingAt (linenumber);
		}

		public string Text
		{
			get { return Buffer.Text; }
			set { Buffer.Text = value; }
		}

		public void Replace (int offset, int length, string pattern)
		{
			Buffer.Replace (offset, length, pattern);
		}
		
		
		public void SetSearchPattern ()
		{
			string selectedText = Buffer.GetSelectedText ();
			if (selectedText != null && selectedText.Length > 0)
				SearchReplaceManager.SearchOptions.SearchPattern = selectedText.Split ('\n')[0];
		}
		
		[CommandHandler (SearchCommands.Find)]
		public void Find()
		{
			SetSearchPattern();
			SearchReplaceManager.ShowFindWindow ();
		}
		
		[CommandHandler (SearchCommands.FindNext)]
		public void FindNext ()
		{
			SearchReplaceManager.FindNext ();
		}
	
		[CommandHandler (SearchCommands.FindPrevious)]
		public void FindPrevious ()
		{
			SearchReplaceManager.FindPrevious ();
		}
	
		[CommandHandler (SearchCommands.FindNextSelection)]
		public void FindNextSelection ()
		{
			SetSearchPattern();
			SearchReplaceManager.FindNext ();
		}
	
		[CommandHandler (SearchCommands.FindPreviousSelection)]
		public void FindPreviousSelection ()
		{
			SetSearchPattern();
			SearchReplaceManager.FindPrevious ();
		}
	
		[CommandHandler (SearchCommands.Replace)]
		public void Replace ()
		{ 
			SetSearchPattern ();
			SearchReplaceManager.ShowFindReplaceWindow ();
			
		}
		
		[CommandHandler (EditorCommands.GotoLineNumber)]
		public void GotoLineNumber ()
		{
			if (!GotoLineNumberDialog.IsVisible)
				using (GotoLineNumberDialog gnd = new GotoLineNumberDialog ())
					gnd.Run ();
		}
		
		[CommandHandler (EditorCommands.GotoMatchingBrace)]
		public void GotoMatchingBrace ()
		{
			TextIter iter = Buffer.GetIterAtMark (Buffer.InsertMark);
			if (Source.IterFindMatchingBracket (ref iter)) {
				iter.ForwardChar ();
				Buffer.PlaceCursor (iter);
				View.ScrollMarkOnscreen (Buffer.InsertMark);
			}
		}

		[CommandHandler (EditorCommands.ToggleBookmark)]
		public void ToggleBookmark ()
		{
			Buffer.ToggleBookmark ();
		}
		
		[CommandHandler (EditorCommands.PrevBookmark)]
		public void PrevBookmark ()
		{
			Buffer.PrevBookmark ();
			View.ScrollMarkOnscreen (Buffer.InsertMark);
		}
		
		[CommandHandler (EditorCommands.NextBookmark)]
		public void NextBookmark ()
		{
			Buffer.NextBookmark ();
			View.ScrollMarkOnscreen (Buffer.InsertMark);
		}
		
		[CommandHandler (EditorCommands.ClearBookmarks)]
		public void ClearBookmarks ()
		{
			Buffer.ClearBookmarks ();
		}
		
		[CommandHandler (DebugCommands.ToggleBreakpoint)]
		public void ToggleBreakpoint ()
		{
			if (Runtime.DebuggingService != null && DisplayBinding.ContentName != null) {
				int line = Buffer.GetIterAtMark (Buffer.InsertMark).Line + 1;
				Runtime.DebuggingService.ToggleBreakpoint (DisplayBinding.ContentName, line);
			}
		}
		
		[CommandUpdateHandler (DebugCommands.ToggleBreakpoint)]
		public void UpdateToggleBreakpoint (CommandInfo info)
		{
			if (Runtime.DebuggingService == null)
				info.Visible = false;
			else
				info.Enabled = DisplayBinding.ContentName != null;
		}

		private static readonly string [] drag_icon_xpm = new string [] {
			"36 48 9 1",
			" 	c None",
			".	c #020204",
			"+	c #8F8F90",
			"@	c #D3D3D2",
			"#	c #AEAEAC",
			"$	c #ECECEC",
			"%	c #A2A2A4",
			"&	c #FEFEFC",
			"*	c #BEBEBC",
			"               .....................",
			"              ..&&&&&&&&&&&&&&&&&&&.",
			"             ...&&&&&&&&&&&&&&&&&&&.",
			"            ..&.&&&&&&&&&&&&&&&&&&&.",
			"           ..&&.&&&&&&&&&&&&&&&&&&&.",
			"          ..&&&.&&&&&&&&&&&&&&&&&&&.",
			"         ..&&&&.&&&&&&&&&&&&&&&&&&&.",
			"        ..&&&&&.&&&@&&&&&&&&&&&&&&&.",
			"       ..&&&&&&.*$%$+$&&&&&&&&&&&&&.",
			"      ..&&&&&&&.%$%$+&&&&&&&&&&&&&&.",
			"     ..&&&&&&&&.#&#@$&&&&&&&&&&&&&&.",
			"    ..&&&&&&&&&.#$**#$&&&&&&&&&&&&&.",
			"   ..&&&&&&&&&&.&@%&%$&&&&&&&&&&&&&.",
			"  ..&&&&&&&&&&&.&&&&&&&&&&&&&&&&&&&.",
			" ..&&&&&&&&&&&&.&&&&&&&&&&&&&&&&&&&.",
			"................&$@&&&@&&&&&&&&&&&&.",
			".&&&&&&&+&&#@%#+@#@*$%$+$&&&&&&&&&&.",
			".&&&&&&&+&&#@#@&&@*%$%$+&&&&&&&&&&&.",
			".&&&&&&&+&$%&#@&#@@#&#@$&&&&&&&&&&&.",
			".&&&&&&@#@@$&*@&@#@#$**#$&&&&&&&&&&.",
			".&&&&&&&&&&&&&&&&&&&@%&%$&&&&&&&&&&.",
			".&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&.",
			".&&&&&&&&$#@@$&&&&&&&&&&&&&&&&&&&&&.",
			".&&&&&&&&&+&$+&$&@&$@&&$@&&&&&&&&&&.",
			".&&&&&&&&&+&&#@%#+@#@*$%&+$&&&&&&&&.",
			".&&&&&&&&&+&&#@#@&&@*%$%$+&&&&&&&&&.",
			".&&&&&&&&&+&$%&#@&#@@#&#@$&&&&&&&&&.",
			".&&&&&&&&@#@@$&*@&@#@#$#*#$&&&&&&&&.",
			".&&&&&&&&&&&&&&&&&&&&&$%&%$&&&&&&&&.",
			".&&&&&&&&&&$#@@$&&&&&&&&&&&&&&&&&&&.",
			".&&&&&&&&&&&+&$%&$$@&$@&&$@&&&&&&&&.",
			".&&&&&&&&&&&+&&#@%#+@#@*$%$+$&&&&&&.",
			".&&&&&&&&&&&+&&#@#@&&@*#$%$+&&&&&&&.",
			".&&&&&&&&&&&+&$+&*@&#@@#&#@$&&&&&&&.",
			".&&&&&&&&&&$%@@&&*@&@#@#$#*#&&&&&&&.",
			".&&&&&&&&&&&&&&&&&&&&&&&$%&%$&&&&&&.",
			".&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&.",
			".&&&&&&&&&&&&&&$#@@$&&&&&&&&&&&&&&&.",
			".&&&&&&&&&&&&&&&+&$%&$$@&$@&&$@&&&&.",
			".&&&&&&&&&&&&&&&+&&#@%#+@#@*$%$+$&&.",
			".&&&&&&&&&&&&&&&+&&#@#@&&@*#$%$+&&&.",
			".&&&&&&&&&&&&&&&+&$+&*@&#@@#&#@$&&&.",
			".&&&&&&&&&&&&&&$%@@&&*@&@#@#$#*#&&&.",
			".&&&&&&&&&&&&&&&&&&&&&&&&&&&$%&%$&&.",
			".&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&.",
			".&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&.",
			".&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&.",
			"...................................."
		};
	}
}
