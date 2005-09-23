// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Drawing;
using System.Reflection;
using System.Collections;

using MonoDevelop.TextEditor;

using Gtk;
using GtkSharp;

namespace MonoDevelop.TextEditor.Gui.CompletionWindow
{
	public class CompletionWindow : Window
	{
		const  int  DeclarationIndent  = 1;
		static GLib.GType type;
		Gtk.TreeViewColumn complete_column;
		
		ICompletionDataProvider completionDataProvider;
		TextEditorControl       control;
		Gtk.TreeView            listView;
		Gtk.TreeStore		store;
		DeclarationViewWindow   declarationviewwindow = new DeclarationViewWindow();
		Gdk.Pixbuf[]		imgList;		
		int    insertLength = 0;
		
		string GetTypedString()
		{
			return control.Document.GetText(control.ActiveTextAreaControl.Caret.Offset - insertLength, insertLength);
		}
		
		void DeleteInsertion()
		{
			if (insertLength > 0) {
				int startOffset = control.ActiveTextAreaControl.Caret.Offset - insertLength;
				control.Document.Remove(startOffset, insertLength);
				control.ActiveTextAreaControl.Caret.Position = control.Document.OffsetToPosition(startOffset);
				control.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.SingleLine, new Point(0, control.Document.GetLineNumberForOffset(control.ActiveTextAreaControl.Caret.Offset))));
				control.Document.CommitUpdate();
			}
		}
		
		// Lame fix. The backspace press event is not being caught. The release event yes, though
		// ???
		void ListKeyreleaseEvent(object sender, KeyReleaseEventArgs ex) {
			if (ex.Event.Key == Gdk.Key.BackSpace) {
				new MonoDevelop.TextEditor.Actions.Backspace().Execute(control.ActiveTextAreaControl.TextArea);
				if (insertLength > 0) {
					--insertLength;
				} else {
					// no need to delete here (insertLength <= 0)
					LostFocusListView(null, null);
				}
			}
		}
		void ListKeypressEvent(object sender, KeyPressEventArgs ex)
		{
			Gdk.Key key = ex.Event.Key;
			char val = (char) key;
			switch (key) {
				case Gdk.Key.Shift_L:
				case Gdk.Key.Shift_R:
				case Gdk.Key.Control_L:
				case Gdk.Key.Control_R:
					ex.RetVal = true;
					return;
					
				case Gdk.Key.Escape:
					LostFocusListView(null, null);
					ex.RetVal = true;
					return;
					
				case Gdk.Key.BackSpace:
					new MonoDevelop.TextEditor.Actions.Backspace().Execute(control.ActiveTextAreaControl.TextArea);
					if (insertLength > 0) {
						--insertLength;
					} else {
						// no need to delete here (insertLength <= 0)
						LostFocusListView(null, null);
					}
					break;
					
				default:
					if (val != '_' && !Char.IsLetterOrDigit(val)) {
						if (listView.Selection.CountSelectedRows() > 0) {
							ActivateItem(null, null);
						} else {
							LostFocusListView(null, null);
						}
						
						control.ActiveTextAreaControl.TextArea.SimulateKeyPress(key);
						ex.RetVal = true;
						return;
					} else {
						control.ActiveTextAreaControl.TextArea.InsertChar(val);
						++insertLength;
					}
					break;
			}
			
			// select the current typed word
			int lastSelected = -1;
			int capitalizationIndex = -1;
			
			string typedString = GetTypedString();
			TreeIter iter;
			int i = 0;
			for (store.GetIterFirst(out iter); store.IterNext(out iter) == true; i++) {
				string text = (string)store.GetValue(iter, 0);
				
				if (text.ToUpper().StartsWith(typedString.ToUpper())) {
					int currentCapitalizationIndex = 0;
					for (int j = 0; j < typedString.Length && j < text.Length; ++j) {
						if (typedString[j] == text[j]) {
							++currentCapitalizationIndex;
						}
					}
					
					if (currentCapitalizationIndex > capitalizationIndex) {
						lastSelected = i;
						capitalizationIndex = currentCapitalizationIndex;
					}
				}
			}
			
			listView.Selection.UnselectAll();
			if (lastSelected != -1) {
				TreePath path = new TreePath("" + (lastSelected + 1));
				listView.Selection.SelectPath(path);
				listView.SetCursor (path, complete_column, false);
				listView.ScrollToCell(path, null, false, 0, 0);
			}
			
			ex.RetVal =  true;
		}
		
		void InitializeControls()
		{
			RequestSize = new Size (340, 210 - 85);
			Decorated = false;
			SkipPagerHint = true;
			SkipTaskbarHint = true;
			TypeHint = Gdk.WindowTypeHint.Dialog;
			
			store = new Gtk.TreeStore (typeof (string), typeof (Gdk.Pixbuf), typeof(ICompletionData));
			listView = new Gtk.TreeView (store);

			listView.HeadersVisible = false;

			complete_column = new Gtk.TreeViewColumn ();
			complete_column.Title = "completion";

			Gtk.CellRendererPixbuf pix_render = new Gtk.CellRendererPixbuf ();
			complete_column.PackStart (pix_render, false);
			complete_column.AddAttribute (pix_render, "pixbuf", 1);
			
			Gtk.CellRendererText text_render = new Gtk.CellRendererText ();
			complete_column.PackStart (text_render, true);
			complete_column.AddAttribute (text_render, "text", 0);
	
			listView.AppendColumn (complete_column);

			Gtk.ScrolledWindow scroller = new Gtk.ScrolledWindow ();
			scroller.Add (listView);

			Gtk.Frame frame = new Gtk.Frame ();
			frame.Add (scroller);
			this.Add(frame);

			imgList = completionDataProvider.ImageList;
			listView.KeyPressEvent += new KeyPressEventHandler(ListKeypressEvent);
			listView.KeyReleaseEvent += new KeyReleaseEventHandler(ListKeyreleaseEvent);
			listView.FocusOutEvent += new FocusOutEventHandler(LostFocusListView);
			listView.RowActivated += new RowActivatedHandler(ActivateItem);
			listView.AddEvents ((int) (Gdk.EventMask.KeyPressMask));

			/*
			Panel buttonPanel = new Panel();
			buttonPanel.Dock = DockStyle.Bottom;
			buttonPanel.Size = new Size(100, 30);
			
			this.Controls.Add(buttonPanel);
			*/
		}
	
		/// <remarks>
		/// Shows the filled completion window, if it has no items it isn't shown.
		/// </remarks>
		public void ShowCompletionWindow(char firstChar)
		{
			FillList(true, firstChar);

			TreeIter iter;
			if (store.GetIterFirst(out iter) == false) {
				control.GrabFocus();
				return;
			}

			Point caretPos  = control.ActiveTextAreaControl.Caret.Position;
			Point visualPos = new Point(control.ActiveTextAreaControl.TextArea.TextView.GetDrawingXPos(caretPos.Y, caretPos.X) + control.ActiveTextAreaControl.TextArea.TextView.DrawingPosition.X,
			          (int)((1 + caretPos.Y) * control.ActiveTextAreaControl.TextArea.TextView.FontHeight) - control.ActiveTextAreaControl.TextArea.VirtualTop.Y - 1 + control.ActiveTextAreaControl.TextArea.TextView.DrawingPosition.Y);

			int tx, ty;
			control.ActiveTextAreaControl.TextArea.GdkWindow.GetOrigin(out tx, out ty);
			Move(tx + visualPos.X, ty + visualPos.Y);
			listView.Selection.Changed += new EventHandler (RowActivated);
			ShowAll ();
		}
		string fileName;
		
		static CompletionWindow ()
		{
			type = RegisterGType (typeof (CompletionWindow));
		}
		
		/// <remarks>
		/// Creates a new Completion window and puts it location under the caret
		/// </remarks>
		public CompletionWindow (TextEditorControl control, string fileName, ICompletionDataProvider completionDataProvider) : base (type)
		{
			this.fileName = fileName;
			this.completionDataProvider = completionDataProvider;
			this.control                = control;

			InitializeControls();
		}
		
		/// <remarks>
		/// Creates a new Completion window at a given location
		/// </remarks>
		CompletionWindow (TextEditorControl control, Point location, ICompletionDataProvider completionDataProvider) : base (type)
		{
			this.completionDataProvider = completionDataProvider;
			this.control                = control;

			InitializeControls();
		}
		
		void ActivateItem(object sender, RowActivatedArgs e)
		{
			if (listView.Selection.CountSelectedRows() > 0) {
				TreeModel foo;
				TreeIter iter;
				listView.Selection.GetSelected(out foo, out iter);
				ICompletionData data = (ICompletionData) store.GetValue(iter, 2);
				DeleteInsertion();
				data.InsertAction(control);
				LostFocusListView(null, null);
			}
		}
		
		void LostFocusListView(object sender, FocusOutEventArgs e)
		{
			control.HasFocus = true;
			declarationviewwindow.HideAll ();
			Hide();
		}
		
		void FillList(bool firstTime, char ch)
		{
			ICompletionData[] completionData = completionDataProvider.GenerateCompletionData(fileName, control.ActiveTextAreaControl.TextArea, ch);
			if (completionData == null || completionData.Length == 0) {
				return;
			}

			foreach (ICompletionData data in completionData) {
				store.AppendValues (data.Text[0], imgList[data.ImageIndex], data);
			}
			// sort here
			store.SetSortColumnId (0, SortType.Ascending);
		}
		
		void RowActivated  (object sender, EventArgs a)
		{
			Gtk.TreeIter iter;
			Gtk.TreeModel model;
	
			if (listView.Selection.GetSelected (out model, out iter)){
				ICompletionData data = (ICompletionData) store.GetValue (iter, 2);
				
				//FIXME: This code is buggy, and generates a bad placement sometimes when you jump a lot. but it is better than 0,0
				
				Gtk.TreePath path = store.GetPath (iter);
				Gdk.Rectangle rect;
				rect = listView.GetCellArea (path, (Gtk.TreeViewColumn)listView.Columns[0]);

				int x, y;
				listView.TreeToWidgetCoords (rect.x, rect.y, out x, out y);
				
				int listpos_x, listpos_y;
				GetPosition (out listpos_x, out listpos_y);
				int vert = listpos_y + rect.y;

				if (vert > listpos_y + listView.GdkWindow.Size.Height) {
					vert = listpos_y + listView.GdkWindow.Size.Height - rect.height;
				} else if (vert < listpos_y) {
					vert = listpos_y;
				}

				//FIXME: This is a bad calc, its always on the right, it needs to test if thats too big, and if so, place on the left;
				int horiz = listpos_x + listView.GdkWindow.Size.Width + 30;
				ICompletionDataWithMarkup wMarkup = data as ICompletionDataWithMarkup;
				declarationviewwindow.Destroy ();
				if (wMarkup != null) {
					declarationviewwindow = new DeclarationViewWindow ();
					declarationviewwindow.DescriptionMarkup = wMarkup.DescriptionPango;
				} else {
					declarationviewwindow = new DeclarationViewWindow ();
					declarationviewwindow.DescriptionMarkup = data.Description;
				}
				
				declarationviewwindow.ShowAll ();
				if (listView.Screen.Width <= horiz + declarationviewwindow.GdkWindow.FrameExtents.Width) {
					horiz = listpos_x - declarationviewwindow.GdkWindow.FrameExtents.Width - 10;
				}
				declarationviewwindow.Move (horiz, vert);
			}
		}
	}
}
