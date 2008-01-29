using Gtk;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using MonoDevelop.Components.Commands;
using MonoDevelop.Core.Gui;
using MonoDevelop.SourceEditor.Gui.Dialogs;
using GtkSourceView;
using MonoDevelop.SourceEditor;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Search;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui;
using Stock = MonoDevelop.Core.Gui.Stock;

namespace MonoDevelop.SourceEditor.Gui
{
	public class SourceEditor : ScrolledWindow
	{
		public SourceEditorBuffer Buffer;
		public SourceEditorView View;
		public SourceEditorDisplayBindingWrapper DisplayBinding;
		Dictionary<string,Gdk.Pixbuf> markers = new Dictionary<string,Gdk.Pixbuf> ();
		
		static Gdk.Pixbuf dragIconPixbuf;
		static Gdk.Pixbuf executionMarkerPixbuf;
		static Gdk.Pixbuf breakPointPixbuf;
		
		static SourceEditor ()
		{
			dragIconPixbuf = Gdk.Pixbuf.LoadFromResource ("bookmark.png");
			executionMarkerPixbuf = Services.Resources.GetIcon (Stock.ExecutionMarker);
			breakPointPixbuf = Services.Resources.GetIcon (Stock.BreakPoint);
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
			
			AddMarker ("SourceEditorBookmark", dragIconPixbuf);
			AddMarker ("ExecutionMark", executionMarkerPixbuf);
			AddMarker ("BreakpointMark", breakPointPixbuf);
			
			Add (View);
		}
		
		public override void Dispose ()
		{
			Buffer.Dispose ();
			base.Dispose ();
		}
/* FIXME GTKSV2
		public void ExecutingAt (int linenumber)
		{
			View.ExecutingAt (linenumber);
		}

		public void ClearExecutingAt (int linenumber)
		{
			View.ClearExecutingAt (linenumber);
		}
*/
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
			
			if (selectedText != null && selectedText != String.Empty)
				SearchReplaceManager.SearchOptions.SearchPattern = selectedText.Split ('\n')[0];
		}
		
		public void AddMarker (string name, Gdk.Pixbuf image)
		{
			// Keep track of markers in a dictionary. It is used in case the
			// line height of the editor changes.
			markers.Add (name, image);
			//FIXME GTKSV2
			//View.SetMarkerPixbuf (name, FixMarkerSize (image));
		}
		
		internal void UpdateMarkerSize ()
		{
			// Makes sure markers are not bigger than the line height
			//foreach (KeyValuePair<string, Gdk.Pixbuf> entry in markers) {
				//FIXME GTKSV2
				//View.SetMarkerPixbuf (entry.Key, FixMarkerSize (entry.Value));
			//}
		}
		
		Gdk.Pixbuf FixMarkerSize (Gdk.Pixbuf pix)
		{
			// Returns a marker image with a size that fits the line height
			
			TextIter iter = View.Buffer.GetIterAtMark (View.Buffer.InsertMark);
			int markerSize = View.GetIterLocation (iter).Height;
			
			if (pix.Height <= markerSize)
				return pix;
				
			int width = pix.Width;
			int height = markerSize;
			
			if ((pix.Width - width) > (pix.Height - height)) {
				if (pix.Width != width) {
					float prop = (float) pix.Height / (float) pix.Width;
					return pix.ScaleSimple (width, (int)(width * prop), Gdk.InterpType.Bilinear);
				}
			} else {
				if (pix.Height != height) {
					float prop = (float) pix.Width / (float) pix.Height;
					return pix.ScaleSimple ((int)(height * prop), height, Gdk.InterpType.Bilinear);
				}
			}
			return pix;
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
		
		protected static string StrMiddleTruncate (string str, int truncLen)
		{
			if (str == null) return String.Empty;
			if (str.Length <= truncLen) return str;
			
			string delimiter = "...";
			int leftOffset = (truncLen - delimiter.Length) / 2;
			int rightOffset = str.Length - truncLen + leftOffset + delimiter.Length;
			
			return str.Substring (0, leftOffset) + delimiter + str.Substring (rightOffset);
		}
/* FIXME GTKSV2	
		[CommandHandler (DebugCommands.ToggleBreakpoint)]
		public void ToggleBreakpoint ()
		{
			if (Services.DebuggingService != null && DisplayBinding.ContentName != null) {
				int line = Buffer.GetIterAtMark (Buffer.InsertMark).Line + 1;
				Services.DebuggingService.ToggleBreakpoint (DisplayBinding.ContentName, line);
			}
		}
		
		[CommandUpdateHandler (DebugCommands.ToggleBreakpoint)]
		public void UpdateToggleBreakpoint (CommandInfo info)
		{
			if (Services.DebuggingService == null)
				info.Visible = false;
			else
				info.Enabled = DisplayBinding.ContentName != null;
		}*/
	}
}
