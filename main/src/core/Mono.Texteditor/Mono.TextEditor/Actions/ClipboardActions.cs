// 
// ClipboardActions.cs
// 
// Author:
//   Mike Krüger <mkrueger@novell.com>
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2007-2008 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Gtk;
using Mono.TextEditor.Highlighting;
using Mono.TextEditor.Utils;

namespace Mono.TextEditor
{
	public static class ClipboardActions
	{
		public static void Cut (TextEditorData data)
		{
			if (data.IsSomethingSelected && data.CanEditSelection) {
				Copy (data);
				DeleteActions.Delete (data);
			} else {
				Copy (data);
				DeleteActions.CaretLine (data);
			}
		}
		
		public static void Copy (TextEditorData data)
		{
			CopyOperation operation = new CopyOperation ();
			
			Clipboard clipboard = Clipboard.Get (CopyOperation.CLIPBOARD_ATOM);
			operation.CopyData (data);
			
			clipboard.SetWithData ((Gtk.TargetEntry[])CopyOperation.targetList, operation.ClipboardGetFunc,
			                       operation.ClipboardClearFunc);
		}
	
		public class CopyOperation
		{
			public const int TextType     = 1;
			public const int HTMLTextType = 2;
			public const int RichTextType = 3;

			public const int MonoTextType = 99;

			const int UTF8_FORMAT = 8;
			
			public static readonly Gdk.Atom CLIPBOARD_ATOM        = Gdk.Atom.Intern ("CLIPBOARD", false);
			public static readonly Gdk.Atom PRIMARYCLIPBOARD_ATOM = Gdk.Atom.Intern ("PRIMARY", false);
			public static readonly Gdk.Atom RTF_ATOM;
			public static readonly Gdk.Atom MD_ATOM  = Gdk.Atom.Intern ("text/monotext", false);
			public static readonly Gdk.Atom HTML_ATOM;

			public CopyOperation ()	
			{
			}
			
			public void SetData (SelectionData selection_data, uint info)
			{
				if (selection_data == null)
					return;
				switch (info) {
				case TextType:
					// Windows specific hack to work around bug: Bug 661973 - copy operation in TextEditor braks text lines with duplicate line endings when the file has CRLF
					// Remove when https://bugzilla.gnome.org/show_bug.cgi?id=640439 is fixed.
					if (Platform.IsWindows) {
						selection_data.Text = copiedDocument.Text.Replace ("\r\n", "\n");
					} else {
						selection_data.Text = copiedDocument.Text;
					}
					break;
				case RichTextType:
					selection_data.Set (RTF_ATOM, UTF8_FORMAT, System.Text.Encoding.UTF8.GetBytes (RtfWriter.GenerateRtf (copiedDocument, mode, docStyle, options)));
					break;
				case HTMLTextType:
					selection_data.Set (HTML_ATOM, UTF8_FORMAT, System.Text.Encoding.UTF8.GetBytes (HtmlWriter.GenerateHtml (copiedDocument, mode, docStyle, options)));
					break;
				case MonoTextType:
					byte[] rawText = System.Text.Encoding.UTF8.GetBytes (monoDocument.Text);
					byte[] data = new byte [rawText.Length + 1];
					rawText.CopyTo (data, 1);
					data [0] = 0;
					if (isBlockMode)
						data [0] |= 1;
					if (isLineSelectionMode)
						data [0] |= 2;
					selection_data.Set (MD_ATOM, UTF8_FORMAT, data);
					break;
				}
			}
			bool isLineSelectionMode = false;
			bool isBlockMode         = false;
			
			public void ClipboardGetFunc (Clipboard clipboard, SelectionData selection_data, uint info)
			{
				SetData (selection_data, info);
			}
			
			public void ClipboardClearFunc (Clipboard clipboard)
			{
				// NOTHING ?
			}
	
			public TextDocument copiedDocument;
			public TextDocument monoDocument; // has a slightly different format !!!
			public Mono.TextEditor.Highlighting.ColorScheme docStyle;
			ITextEditorOptions options;
			Mono.TextEditor.Highlighting.ISyntaxMode mode;

			public static Gtk.TargetList targetList;

			static CopyOperation ()
			{
				if (Platform.IsMac) {
					RTF_ATOM = Gdk.Atom.Intern ("NSRTFPboardType", false); //TODO: use public.rtf when dep on MacOS 10.6
					const string NSHTMLPboardType = "Apple HTML pasteboard type";
					HTML_ATOM = Gdk.Atom.Intern (NSHTMLPboardType, false);
				} else {
					RTF_ATOM = Gdk.Atom.Intern ("text/rtf", false);
					HTML_ATOM = Gdk.Atom.Intern ("text/html", false);
				}

				targetList = new Gtk.TargetList ();
				targetList.Add (HTML_ATOM, /* FLAGS */0, HTMLTextType);
				targetList.Add (RTF_ATOM, /* FLAGS */0, RichTextType);
				targetList.Add (MD_ATOM, /* FLAGS */0, MonoTextType);
				targetList.AddTextTargets (TextType);

				//HACK: work around gtk_selection_data_set_text causing crashes on Mac w/ QuickSilver, Clipbard History etc.
				if (Platform.IsMac) {
					targetList.Remove ("COMPOUND_TEXT");
					targetList.Remove ("TEXT");
					targetList.Remove ("STRING");
				}
			}
			
			void CopyData (TextEditorData data, Selection selection)
			{
				copiedDocument = null;
				monoDocument = null;
				if (selection != null && data != null && data.Document != null) {
					copiedDocument = new TextDocument ();
					monoDocument = new TextDocument ();
					this.docStyle = data.ColorStyle;
					this.options = data.Options;
					this.mode = SyntaxModeService.GetSyntaxMode (monoDocument, data.MimeType);
					switch (selection.SelectionMode) {
					case SelectionMode.Normal:
						isBlockMode = false;
						var segment = selection.GetSelectionRange (data);
						var text = data.GetTextAt (segment);
						copiedDocument.Text = text;
						monoDocument.Text = text;
						var line = data.Document.GetLineByOffset (segment.Offset);
						var spanStack = line.StartSpan.Clone ();
						SyntaxModeService.ScanSpans (data.Document, this.mode as SyntaxMode, this.mode as SyntaxMode, spanStack, line.Offset, segment.Offset);
						this.copiedDocument.GetLine (DocumentLocation.MinLine).StartSpan = spanStack;
						break;
					case SelectionMode.Block:
						isBlockMode = true;
						DocumentLocation visStart = data.LogicalToVisualLocation (selection.Anchor);
						DocumentLocation visEnd = data.LogicalToVisualLocation (selection.Lead);
						int startCol = System.Math.Min (visStart.Column, visEnd.Column);
						int endCol = System.Math.Max (visStart.Column, visEnd.Column);
						for (int lineNr = selection.MinLine; lineNr <= selection.MaxLine; lineNr++) {
							DocumentLine curLine = data.Document.GetLine (lineNr);
							int col1 = curLine.GetLogicalColumn (data, startCol) - 1;
							int col2 = System.Math.Min (curLine.GetLogicalColumn (data, endCol) - 1, curLine.Length);
							if (col1 < col2) {
								copiedDocument.Insert (copiedDocument.TextLength, data.Document.GetTextAt (curLine.Offset + col1, col2 - col1));
								monoDocument.Insert (monoDocument.TextLength, data.Document.GetTextAt (curLine.Offset + col1, col2 - col1));
							}
							if (lineNr < selection.MaxLine) {
								// Clipboard line end needs to be system dependend and not the document one.
								copiedDocument.Insert (copiedDocument.TextLength, Environment.NewLine);
								// \r in mono document stands for block selection line end.
								monoDocument.Insert (monoDocument.TextLength, "\r");
							}
						}
						line = data.Document.GetLine (selection.MinLine);
						spanStack = line.StartSpan.Clone ();
						SyntaxModeService.ScanSpans (data.Document, this.mode as SyntaxMode, this.mode as SyntaxMode, spanStack, line.Offset, line.Offset + startCol);
						this.copiedDocument.GetLine (DocumentLocation.MinLine).StartSpan = spanStack;
						break;
					}
				} else {
					copiedDocument = null;
				}
			}
			
			public void CopyData (TextEditorData data)
			{
				Selection selection;
				isLineSelectionMode = !data.IsSomethingSelected;
				if (data.IsSomethingSelected) {
					selection = data.MainSelection;
				} else {
					var start = DeleteActions.GetStartOfLineOffset (data, data.Caret.Location);
					var end = DeleteActions.GetEndOfLineOffset (data, data.Caret.Location, false);
					selection = new Selection (data.OffsetToLocation (start), data.OffsetToLocation (end));
				}
				CopyData (data, selection);
				
				if (Copy != null)
					Copy (copiedDocument != null ? copiedDocument.Text : null);
			}
		
			public delegate void CopyDelegate (string text);
			public static event CopyDelegate Copy;
		}
		
		public static void CopyToPrimary (TextEditorData data)
		{
			if (Platform.IsWindows) // disable middle click on windows.
				return;
			Clipboard clipboard = Clipboard.Get (CopyOperation.PRIMARYCLIPBOARD_ATOM);
			clipboard.Text = data.SelectedText;
		}
		
		public static void ClearPrimary ()
		{
			Clipboard clipboard = Clipboard.Get (CopyOperation.PRIMARYCLIPBOARD_ATOM);
			clipboard.Clear ();
		}
		
		static int PasteFrom (Clipboard clipboard, TextEditorData data, bool preserveSelection, int insertionOffset)
		{
			return PasteFrom (clipboard, data, preserveSelection, insertionOffset, false);
		}
		
		static int PasteFrom (Clipboard clipboard, TextEditorData data, bool preserveSelection, int insertionOffset, bool preserveState)
		{
			int result = -1;
			if (!data.CanEdit (data.Document.OffsetToLineNumber (insertionOffset)))
				return result;
			if (clipboard.WaitIsTargetAvailable (CopyOperation.MD_ATOM)) {
				clipboard.RequestContents (CopyOperation.MD_ATOM, delegate(Clipboard clp, SelectionData selectionData) {
					if (selectionData.Length > 0) {
						byte[] selBytes = selectionData.Data;
	
						string text = System.Text.Encoding.UTF8.GetString (selBytes, 1, selBytes.Length - 1);
						bool pasteBlock = (selBytes [0] & 1) == 1;
						bool pasteLine = (selBytes [0] & 2) == 2;
						
//						var clearSelection = data.IsSomethingSelected ? data.MainSelection.SelectionMode != SelectionMode.Block : true;
						if (pasteBlock) {
							using (var undo = data.OpenUndoGroup ()) {
								var version = data.Document.Version;
								if (!preserveSelection)
									data.DeleteSelectedText (!data.IsSomethingSelected || data.MainSelection.SelectionMode != SelectionMode.Block);
								data.EnsureCaretIsNotVirtual ();
								insertionOffset = version.MoveOffsetTo (data.Document.Version, insertionOffset);

								data.Caret.PreserveSelection = true;
							
								string[] lines = text.Split ('\r');
								int lineNr = data.Document.OffsetToLineNumber (insertionOffset);
								int col = insertionOffset - data.Document.GetLine (lineNr).Offset;
								int visCol = data.Document.GetLine (lineNr).GetVisualColumn (data, col);
								DocumentLine curLine;
								int lineCol = col;
								result = 0;
								for (int i = 0; i < lines.Length; i++) {
									while (data.Document.LineCount <= lineNr + i) {
										data.Insert (data.Document.TextLength, Environment.NewLine);
										result += Environment.NewLine.Length;
									}
									curLine = data.Document.GetLine (lineNr + i);
									if (lines [i].Length > 0) {
										lineCol = curLine.GetLogicalColumn (data, visCol);
										if (curLine.Length + 1 < lineCol) {
											result += lineCol - curLine.Length;
											data.Insert (curLine.Offset + curLine.Length, new string (' ', lineCol - curLine.Length));
										}
										data.Insert (curLine.Offset + lineCol, lines [i]);
										result += lines [i].Length;
									}
									if (!preserveState)
										data.Caret.Offset = curLine.Offset + lineCol + lines [i].Length;
								}
								if (!preserveState)
									data.ClearSelection ();
								data.Caret.PreserveSelection = false;
							}
						} else if (pasteLine) {
							using (var undo = data.OpenUndoGroup ()) {
								if (!preserveSelection)
									data.DeleteSelectedText (!data.IsSomethingSelected || data.MainSelection.SelectionMode != SelectionMode.Block);
								data.EnsureCaretIsNotVirtual ();

								data.Caret.PreserveSelection = true;
								result = text.Length;
								DocumentLine curLine = data.Document.GetLine (data.Caret.Line);
								data.Insert (curLine.Offset, text + data.EolMarker);
								if (!preserveState)
									data.ClearSelection ();
								data.Caret.PreserveSelection = false;
							}
						} else {
							result = PastePlainText (data, insertionOffset, text, preserveSelection);
						}
					}
				});
				// we got MD_ATOM text - no need to request text. (otherwise buffer may get copied twice).
				return result;
			}
			
			if (result < 0 && clipboard.WaitIsTextAvailable ()) {
				clipboard.RequestText (delegate(Clipboard clp, string text) {
					if (string.IsNullOrEmpty (text))
						return;
					using (var undo = data.OpenUndoGroup ()) {
						result = PastePlainText (data, insertionOffset, text, preserveSelection);
					}
				});
			}
			
			return result;
		}

		static int PastePlainText (TextEditorData data, int offset, string text, bool preserveSelection = false)
		{
			int inserted;
			using (var undo = data.OpenUndoGroup ()) {
				var version = data.Document.Version;
				if (!preserveSelection)
					data.DeleteSelectedText (!data.IsSomethingSelected || data.MainSelection.SelectionMode != SelectionMode.Block);
				data.EnsureCaretIsNotVirtual ();
				offset = version.MoveOffsetTo (data.Document.Version, offset);
				inserted = data.Insert (offset, text);
			}
			data.PasteText (offset, text, inserted);
			return inserted;
		}
		
		public static int PasteFromPrimary (TextEditorData data, int insertionOffset)
		{
			var result = PasteFrom (Clipboard.Get (CopyOperation.PRIMARYCLIPBOARD_ATOM), data, true, insertionOffset, true);
			data.Document.CommitLineUpdate (data.GetLineByOffset (insertionOffset));
			return result;
		}
		
		public static void Paste (TextEditorData data)
		{
			if (!data.CanEditSelection)
				return;
			PasteFrom (Clipboard.Get (CopyOperation.CLIPBOARD_ATOM), data, false, data.IsSomethingSelected ? data.SelectionRange.Offset : data.Caret.Offset);
		}
	}
}
