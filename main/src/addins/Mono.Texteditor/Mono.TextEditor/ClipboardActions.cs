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

namespace Mono.TextEditor
{
	
	
	public static class ClipboardActions
	{
		public static void Cut (TextEditorData data)
		{
			if (data.IsSomethingSelected && data.CanEditSelection) {
				Copy (data);
				DeleteActions.Delete (data);
			}
		}
		
		public static void Copy (TextEditorData data)
		{
			if (data.IsSomethingSelected) {
				CopyOperation operation = new CopyOperation ();
				
				Clipboard clipboard = Clipboard.Get (CopyOperation.CLIPBOARD_ATOM);
				operation.CopyData (data);
				
				clipboard.SetWithData ((Gtk.TargetEntry[])CopyOperation.TargetList, operation.ClipboardGetFunc,
				                       operation.ClipboardClearFunc);
			}
		}
	
		public class CopyOperation
		{
			public const int TextType     = 1;		
			public const int RichTextType = 2;
			
			const int UTF8_FORMAT = 8;
			
			public static readonly Gdk.Atom CLIPBOARD_ATOM        = Gdk.Atom.Intern ("CLIPBOARD", false);
			public static readonly Gdk.Atom PRIMARYCLIPBOARD_ATOM = Gdk.Atom.Intern ("PRIMARY", false);
			static readonly Gdk.Atom RTF_ATOM = Gdk.Atom.Intern ("text/rtf", false);
			
			TextEditorData data;
			ISegment selection;
			
			public CopyOperation ()	
			{
			}
			
			public CopyOperation (TextEditorData data, ISegment selection)	
			{
				this.data = data;
				this.selection = selection;
			}
			
			public void SetData (SelectionData selection_data, uint info)
			{
				if (selection_data == null)
					return;
				switch (info) {
				case TextType:
					selection_data.Text = copiedDocument.Text;
					break;
				case RichTextType:
					selection_data.Set (RTF_ATOM, UTF8_FORMAT, System.Text.Encoding.UTF8.GetBytes (GenerateRtf (copiedDocument, mode, docStyle, options)));
					break;
				}
			}
				
			public void ClipboardGetFunc (Clipboard clipboard, SelectionData selection_data, uint info)
			{
				SetData (selection_data, info);
			}
			
			public void ClipboardClearFunc (Clipboard clipboard)
			{
				// NOTHING ?
			}
	
			public Document copiedDocument;
			public Mono.TextEditor.Highlighting.Style docStyle;
			TextEditorOptions options;
			Mono.TextEditor.Highlighting.SyntaxMode mode;
			
			static string GenerateRtf (Document doc, Mono.TextEditor.Highlighting.SyntaxMode mode, Mono.TextEditor.Highlighting.Style style, TextEditorOptions options)
			{
				StringBuilder rtfText = new StringBuilder ();
				List<Gdk.Color> colorList = new List<Gdk.Color> ();
	
				ISegment selection = new Segment (0, doc.Length);
				LineSegment line    = doc.GetLineByOffset (selection.Offset);
				LineSegment endLine = doc.GetLineByOffset (selection.EndOffset);
				
				RedBlackTree<LineSegmentTree.TreeNode>.RedBlackTreeIterator iter = line.Iter;
				bool isItalic = false;
				bool isBold   = false;
				int curColor  = -1;
				do {
					line = iter.Current;
					for (Chunk chunk = mode.GetChunks (doc, style, line, line.Offset, line.Offset + line.EditableLength); chunk != null; chunk = chunk.Next) {
						int start = System.Math.Max (selection.Offset, chunk.Offset);
						int end   = System.Math.Min (chunk.EndOffset, selection.EndOffset);
						if (start < end) {
							bool appendSpace = false;
							if (isBold != chunk.Style.Bold) {
								rtfText.Append (chunk.Style.Bold ? @"\b" : @"\b0");
								isBold = chunk.Style.Bold;
								appendSpace = true;
							}
							if (isItalic != chunk.Style.Italic) {
								rtfText.Append (chunk.Style.Italic ? @"\i" : @"\i0");
								isItalic = chunk.Style.Italic;
								appendSpace = true;
							}
							if (!colorList.Contains (chunk.Style.Color)) 
								colorList.Add (chunk.Style.Color);
							int color = colorList.IndexOf (chunk.Style.Color);
							if (curColor != color) {
								curColor = color;
								rtfText.Append (@"\cf" + (curColor + 1));
								appendSpace = true;
							}
							for (int i = start; i < end; i++) {
								char ch = chunk.GetCharAt (doc, i);
								if (appendSpace && ch != '\t') {
									rtfText.Append (' ');
									appendSpace = false;
								}							
								switch (ch) {
								case '\\':
									rtfText.Append (@"\\");
									break;
								case '{':
									rtfText.Append (@"\{");
									break;
								case '}':
									rtfText.Append (@"\}");
									break;
								case '\t':
									rtfText.Append (@"\tab");
									appendSpace = true;
									break;
								default:
									rtfText.Append (ch);
									break;
								}
							}
						}
					}
					if (line == endLine)
						break;
					rtfText.Append (@"\par");
				} while (iter.MoveNext ());
				
				// color table
				StringBuilder colorTable = new StringBuilder ();
				colorTable.Append (@"{\colortbl ;");
				for (int i = 0; i < colorList.Count; i++) {
					Gdk.Color color = colorList[i];
					colorTable.Append (@"\red");
					colorTable.Append (color.Red / 256);
					colorTable.Append (@"\green");
					colorTable.Append (color.Green / 256); 
					colorTable.Append (@"\blue");
					colorTable.Append (color.Blue / 256);
					colorTable.Append (";");
				}
				colorTable.Append ("}");
				
				
				StringBuilder rtf = new StringBuilder();
				rtf.Append (@"{\rtf1\ansi\deff0\adeflang1025");
				
				// font table
				rtf.Append (@"{\fonttbl");
				rtf.Append (@"{\f0\fnil\fprq1\fcharset128 " + options.Font.Family + ";}");
				rtf.Append ("}");
				
				rtf.Append (colorTable.ToString ());
				
				rtf.Append (@"\viewkind4\uc1\pard");
				rtf.Append (@"\f0");
				try {
					string fontName = options.Font.ToString ();
					double fontSize = Double.Parse (fontName.Substring (fontName.LastIndexOf (' ')  + 1), System.Globalization.CultureInfo.InvariantCulture) * 2;
					rtf.Append (@"\fs");
					rtf.Append (fontSize);
				} catch (Exception) {};
				rtf.Append (@"\cf1");
				rtf.Append (rtfText.ToString ());
				rtf.Append("}");
		//		System.Console.WriteLine(rtf);
				return rtf.ToString ();
			}
			
			public static Gtk.TargetList TargetList {
				get {
					Gtk.TargetList list = new Gtk.TargetList ();
					list.Add (RTF_ATOM, /* FLAGS */ 0, RichTextType);
					list.AddTextTargets (TextType);
					return list;
				}
			}
			
			void CopyData (TextEditorData data, ISegment segment)
			{
				if (copiedDocument != null) {
					copiedDocument.Dispose ();
					copiedDocument = null;
				}
				if (segment != null && data != null && data.Document != null) {
					copiedDocument = new Document ();
					
					
					this.docStyle = data.ColorStyle;
					this.options  = data.Options;
					this.mode = data.Document.SyntaxMode != null && data.Options.EnableSyntaxHighlighting ? data.Document.SyntaxMode : Mono.TextEditor.Highlighting.SyntaxMode.Default;
					copiedDocument.Text = segment != null && segment.Length > 0 ? this.mode.GetTextWithoutMarkup (data.Document, data.ColorStyle, segment.Offset, segment.Length) : "";
					
					LineSegment line    = data.Document.GetLineByOffset (segment.Offset);
					Stack<Span> spanStack = line.StartSpan != null ? new Stack<Span> (line.StartSpan) : new Stack<Span> ();
					SyntaxModeService.ScanSpans (data.Document, this.mode, spanStack, line.Offset, segment.Offset);
	
					this.copiedDocument.GetLine (0).StartSpan = spanStack.ToArray ();
					
	
					/*
					try {
						text = segment.Length > 0 ? data.Document.GetTextAt (segment) : "";
					} catch (Exception) {
						System.Console.WriteLine("Copy data failed - unable to get text at:" + segment);
						throw;
					}
					
					try {
						rtf  = GenerateRtf (data);
					} catch (Exception) {
						System.Console.WriteLine("Copy data failed - unable to generate rtf for text at:" + segment);
						throw;
					}*/
				} else {
					copiedDocument = null;
				}
			}
			
			public void CopyData (TextEditorData data)
			{
				CopyData (data, data.SelectionRange);
				
				if (Copy != null)
					Copy (copiedDocument != null ? copiedDocument.Text : null);
			}
			
			public void ClipboardGetFuncLazy (Clipboard clipboard, SelectionData selection_data, uint info)
			{
				// data may have disposed
				if (data.Document == null)
					return;
				CopyData (data, selection);
				ClipboardGetFunc (clipboard, selection_data, info);
			}
			
			public delegate void CopyDelegate (string text);
			public static event CopyDelegate Copy;
		}
		
		public static void CopyToPrimary (TextEditorData data)
		{
			CopyOperation operation = new CopyOperation (data, data.SelectionRange);
			Clipboard clipboard = Clipboard.Get (CopyOperation.PRIMARYCLIPBOARD_ATOM);
			clipboard.SetWithData ((Gtk.TargetEntry[])CopyOperation.TargetList, operation.ClipboardGetFuncLazy,
			                       operation.ClipboardClearFunc);
		}
		
		public static void ClearPrimary ()
		{
			Clipboard clipboard = Clipboard.Get (CopyOperation.PRIMARYCLIPBOARD_ATOM);
			clipboard.Clear ();
		}
		
		static int PasteFrom (Clipboard clipboard, TextEditorData data, bool preserveSelection, int insertionOffset)
		{
			int result = -1;
			if (!data.CanEdit (data.Document.OffsetToLineNumber (insertionOffset)))
				return result;
			clipboard.RequestText (delegate (Clipboard clp, string text) {
				data.Document.BeginAtomicUndo ();
				if (preserveSelection && data.IsSomethingSelected) 
					data.DeleteSelectedText ();
				
				data.Caret.PreserveSelection = true;
				
				data.Document.Insert (insertionOffset, text);
				//int oldLine = data.Caret.Line;
				int textLength = text != null ? text.Length : 0;
				result = textLength;
				if (data.Caret.Offset >= insertionOffset) 
					data.Caret.Offset += textLength;
				if (data.IsSomethingSelected && data.SelectionRange.Offset >= insertionOffset) 
					data.SelectionRange.Offset += textLength;
				if (data.IsSomethingSelected && data.SelectionAnchor >= insertionOffset) 
					data.SelectionAnchor += textLength;
				data.Caret.PreserveSelection = false;
				data.Document.EndAtomicUndo ();
			});
			return result;
		}
		
		public static int PasteFromPrimary (TextEditorData data, int insertionOffset)
		{
			return PasteFrom (Clipboard.Get (CopyOperation.PRIMARYCLIPBOARD_ATOM), data, false, insertionOffset);
		}
		
		public static void Paste (TextEditorData data)
		{
			if (!data.CanEditSelection)
				return;
			LineSegment line = data.Document.GetLine (data.Caret.Line);
			if (data.Caret.Column > line.EditableLength) {
				int offset = data.Caret.Offset;
				string text = data.GetVirtualSpaces (data.Caret.Line, data.Caret.Column);
				data.Document.Insert (data.Caret.Offset, text);
				data.Caret.Offset = offset + text.Length;
			}
			
			PasteFrom (Clipboard.Get (CopyOperation.CLIPBOARD_ATOM), data, true, data.IsSomethingSelected ? data.SelectionRange.Offset : data.Caret.Offset);
		}
	}
}
