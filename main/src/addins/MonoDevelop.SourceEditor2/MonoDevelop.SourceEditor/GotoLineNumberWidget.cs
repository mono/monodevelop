//
// GotoLineNumberWidget.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using Gtk;

using Mono.TextEditor;
using MonoDevelop.Components;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.SourceEditor
{
	partial class GotoLineNumberWidget : Gtk.Bin
	{
		readonly MonoTextEditor textEditor;
		readonly Widget frame;

		double vSave, hSave;
		DocumentLocation caretSave;
		bool cleanExit = false;
		
		void HandleViewTextEditorhandleSizeAllocated (object o, SizeAllocatedArgs args)
		{
			int newX = textEditor.Allocation.Width - this.Allocation.Width - 8;
			var containerChild = ((MonoTextEditor.EditorContainerChild)textEditor [frame]);
			if (newX != containerChild.X) {
				this.entryLineNumber.WidthRequest = textEditor.Allocation.Width / 4;
				containerChild.X = newX;
				textEditor.QueueResize ();
			}
		}

		void CloseWidget ()
		{
			Destroy ();
		}

		public GotoLineNumberWidget (MonoTextEditor textEditor, Widget frame)
		{
			this.textEditor = textEditor;
			this.frame = frame;
			this.Build ();
			
			StoreWidgetState ();
			textEditor.Parent.SizeAllocated += HandleViewTextEditorhandleSizeAllocated;

			this.closeButton.Clicked += delegate {
				RestoreWidgetState ();
				CloseWidget ();
			};
			
			this.buttonGoToLine.Clicked += delegate {
				cleanExit = true;
				GotoLine ();
				CloseWidget ();
			};
			
			foreach (Gtk.Widget child in this.Children) {
				child.KeyPressEvent += delegate (object sender, Gtk.KeyPressEventArgs args) {
					if (args.Event.Key == Gdk.Key.Escape) {
						RestoreWidgetState ();
						CloseWidget ();
					}
				};
			}
			
			Gtk.Widget oldWidget = null;
			this.FocusChildSet += delegate (object sender, Gtk.FocusChildSetArgs args) {
				// only store state when the focus comes from a non child widget
				if (args.Widget != null && oldWidget == null)
					StoreWidgetState ();
				oldWidget = args.Widget;
			};
			
			this.entryLineNumber.Changed += delegate {
				PreviewLine ();
			};
				
			this.entryLineNumber.Activated += delegate {
				cleanExit = true;
				GotoLine ();
				CloseWidget ();
			};
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			textEditor.Parent.SizeAllocated -= HandleViewTextEditorhandleSizeAllocated;
		}

		
		void StoreWidgetState ()
		{
			this.vSave = textEditor.VAdjustment.Value;
			this.hSave = textEditor.HAdjustment.Value;
			this.caretSave = textEditor.Caret.Location;
		}
		
		void RestoreWidgetState ()
		{
			if (cleanExit)
				return;
			textEditor.VAdjustment.Value = this.vSave;
			textEditor.HAdjustment.Value = this.hSave;
			textEditor.Caret.Location    = this.caretSave;
		}
		
		int TargetLine {
			get {
				int line;
				try {
					var lineNumberText = entryLineNumber.Text.Split (',', ':')[0];
					line = Int32.Parse (lineNumberText);
				} catch (OverflowException) {
					line = entryLineNumber.Text.Trim ().StartsWith ("-", StringComparison.Ordinal) ? int.MinValue : int.MaxValue;
				}
				bool isRelativeJump = entryLineNumber.Text.Trim ().StartsWith ("-", StringComparison.Ordinal) || entryLineNumber.Text.Trim ().StartsWith ("+", StringComparison.Ordinal);
				return isRelativeJump ? this.caretSave.Line + line : line;
			}
		}
		
		int TargetColumn {
			get {
				int column;
				try {
					var col = entryLineNumber.Text.Split (',', ':');
					if (col.Length != 2)
						return 0;
					var lineNumberText = col [1];
					column = Int32.Parse (lineNumberText);
				} catch (OverflowException) {
					column = entryLineNumber.Text.Trim ().StartsWith ("-", StringComparison.Ordinal) ? int.MinValue : int.MaxValue;
				}
				bool isRelativeJump = entryLineNumber.Text.Trim ().StartsWith ("-", StringComparison.Ordinal) || entryLineNumber.Text.Trim ().StartsWith ("+", StringComparison.Ordinal);
				return isRelativeJump ? this.caretSave.Column + column : column;
			}
		}

		void GotoLine ()
		{
			try {
				textEditor.Caret.Line = TargetLine;
				var col = TargetColumn;
				if (col > 0)
					textEditor.Caret.Column = col;
				textEditor.CenterToCaret ();
			} catch (System.Exception) { 
			}
		}

		void PreviewLine ()
		{
			if (String.IsNullOrEmpty (entryLineNumber.Text) || entryLineNumber.Text == "+" || entryLineNumber.Text == "-") {
				this.entryLineNumber.ModifyBase (Gtk.StateType.Normal, Style.Base (Gtk.StateType.Normal));
				this.entryLineNumber.ModifyText (Gtk.StateType.Normal, Style.Foreground (Gtk.StateType.Normal));
				RestoreWidgetState ();
				return;
			}
			try {
				int targetLine = TargetLine;
				if (targetLine >= textEditor.Document.LineCount || targetLine < 0) {
					targetLine = Math.Max (1, Math.Min (textEditor.Document.LineCount, targetLine));
					
				} else {
					this.entryLineNumber.ModifyBase (Gtk.StateType.Normal, Style.Base (Gtk.StateType.Normal));
					this.entryLineNumber.ModifyText (Gtk.StateType.Normal, Style.Foreground (Gtk.StateType.Normal));
				}
				textEditor.Caret.Line = targetLine;
				textEditor.CenterToCaret ();
			} catch (System.Exception) {
				this.entryLineNumber.ModifyText (Gtk.StateType.Normal, Ide.Gui.Styles.Editor.SearchErrorForegroundColor.ToGdkColor ());
			}
		}
		
		public void Focus ()
		{
			if (!this.entryLineNumber.IsFocus)
				this.entryLineNumber.IsFocus = true;
			PreviewLine ();
		}
	}
}
