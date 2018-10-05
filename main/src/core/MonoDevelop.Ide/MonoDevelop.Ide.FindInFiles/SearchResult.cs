//
// SearchResult.cs
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

using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;
using System.Collections.Generic;
using MonoDevelop.Projects;
using System;
using MonoDevelop.Components;
using MonoDevelop.Ide.TypeSystem;
using System.Text;
using MonoDevelop.Core;
using System.Threading;
using System.Threading.Tasks;

namespace MonoDevelop.Ide.FindInFiles
{
	public class SearchResult
	{
		public virtual FileProvider FileProvider { get; private set; }

		public int Offset { get; set; }
		public int Length { get; set; }

		public virtual string FileName {
			get {
				return FileProvider.FileName;
			}
		}

		#region Cached data
		private List<Project> projects;
		public List<Project> Projects {
			get {
				if (projects == null) {
					projects = new List<Project> (IdeApp.Workspace.GetProjectsContainingFile (FileName));
				}
				return projects;
			}
		}

		DocumentLocation? location;
		string copyData;
		string markup, selectedMarkup;

		internal int GetLineNumber (SearchResultWidget widget)
		{
			if (!location.HasValue)
				FillCache (widget);
			return location.Value.Line;
		}

		internal DocumentLocation GetLocation (SearchResultWidget widget)
		{
			if (!location.HasValue)
				FillCache (widget);
			return location.Value;
		}

		internal string GetCopyData (SearchResultWidget widget)
		{
			if (copyData == null)
				FillCache (widget);
			return copyData;
		}

		internal string GetMarkup(SearchResultWidget widget, bool isSelected)
		{
			if (markup == null)
				FillCache (widget);
			return isSelected ? selectedMarkup : markup;
		}

		void FillCache (SearchResultWidget widget)
		{
			location = DocumentLocation.Empty;
			copyData = "";
			markup = selectedMarkup = "";

			var doc = GetDocument ();
			if (doc == null) {
				return;
			}
			try {
				int lineNr = doc.OffsetToLineNumber (Offset);
				var line = doc.GetLine (lineNr);
				if (line != null) {
					location = new DocumentLocation (lineNr, Offset - line.Offset + 1);
					copyData = $"{FileName} ({location.Value.Line}, {location.Value.Column}):{doc.GetTextAt (line.Offset, line.Length)}";
					CreateMarkup (widget, doc, line);
				}
			} catch (Exception e) {
				LoggingService.LogError ("Error while getting search result", e);
			}
		}

		const int maximumMarkupLength = 78;

		void CreateMarkup (SearchResultWidget widget, TextEditor doc, Editor.IDocumentLine line)
		{
			int startIndex = 0, endIndex = 0;

			int indent = line.GetIndentation (doc).Length;
			string lineText;
			int col = Offset - line.Offset;
			int markupStartOffset = 0;
			bool trimStart = false, trimEnd = false;
			int length;
			if (col < indent) {
				trimEnd = line.Length > maximumMarkupLength;
				length = Math.Min (maximumMarkupLength, line.Length);
				// search result contained part of the indent.
				lineText = doc.GetTextAt (line.Offset, length);
			} else {
				// if not crop the indent
				length = line.Length - indent;
				if (length > maximumMarkupLength) {
					markupStartOffset = Math.Min (Math.Max (0, col - indent - maximumMarkupLength / 2), line.Length - maximumMarkupLength);
					trimEnd = markupStartOffset + maximumMarkupLength < line.Length;
					trimStart = markupStartOffset > 0;
					length = maximumMarkupLength;
				}
				lineText = doc.GetTextAt (line.Offset + markupStartOffset + indent, length);
				col -= indent;
			}
			if (col >= 0) {
				uint start;
				uint end;
				try {
					start = (uint)TranslateIndexToUTF8 (lineText, col - markupStartOffset);
					end = (uint)TranslateIndexToUTF8 (lineText, Math.Min (lineText.Length, col - markupStartOffset + Length));
				} catch (Exception e) {
					LoggingService.LogError ("Exception while translating index to utf8 (column was:" + col + " search result length:" + Length + " line text:" + lineText + ")", e);
					return;
				}
				startIndex = (int)start;
				endIndex = (int)end;
			}

			var tabSize = doc.Options.TabSize;
			this.markup = this.selectedMarkup = markup = Ambience.EscapeText (lineText);

			var searchColor = GetBackgroundMarkerColor (widget.HighlightStyle);

			var selectedSearchColor = widget.Style.Base (Gtk.StateType.Selected);
			selectedSearchColor = searchColor.AddLight (-0.2);
			double b1 = HslColor.Brightness (searchColor);
			double b2 = HslColor.Brightness (SearchResultWidget.AdjustColor (widget.Style.Base (Gtk.StateType.Normal), SyntaxHighlightingService.GetColor (widget.HighlightStyle, EditorThemeColors.Foreground)));
			// selected
			markup = FormatMarkup (PangoHelper.ColorMarkupBackground (selectedMarkup, (int)startIndex, (int)endIndex, searchColor), trimStart, trimEnd, tabSize);
			selectedMarkup = FormatMarkup (PangoHelper.ColorMarkupBackground (selectedMarkup, (int)startIndex, (int)endIndex, selectedSearchColor), trimStart, trimEnd, tabSize);

			Task.Run (delegate {
				var newMarkup = doc.GetMarkup (line.Offset + markupStartOffset + indent, length, new MarkupOptions (MarkupFormat.Pango));
				Runtime.RunInMainThread (delegate {
					newMarkup = widget.AdjustColors (newMarkup);

					try {
						double delta = Math.Abs (b1 - b2);
						if (delta < 0.1) {
							var color1 = SyntaxHighlightingService.GetColor (widget.HighlightStyle, EditorThemeColors.FindHighlight);
							if (color1.L + 0.5 > 1.0) {
								color1.L -= 0.5;
							} else {
								color1.L += 0.5;
							}
							searchColor = color1;
						}
						if (startIndex != endIndex) {
							newMarkup = PangoHelper.ColorMarkupBackground (newMarkup, (int)startIndex, (int)endIndex, searchColor);
						}
					} catch (Exception e) {
						LoggingService.LogError ("Error while setting the text renderer markup to: " + newMarkup, e);
					}

					newMarkup = FormatMarkup (newMarkup, trimStart, trimEnd, tabSize);

					this.markup = newMarkup;
					widget.QueueDraw ();
				});
			});
		}

		static string FormatMarkup (string str, bool trimeStart, bool trimEnd, int tabSize)
		{
			var result = StringBuilderCache.Allocate ();
			var tab = new string (' ', tabSize);
			if (trimeStart)
				result.Append ("…");
			foreach (var ch in str) {
				if (ch == '\n' || ch == '\r')
					continue;
				if (ch == '\t') {
					result.Append (tab);
					continue;
				}
				result.Append (ch);
			}
			if (trimEnd)
				result.Append ("…");
			return StringBuilderCache.ReturnAndFree (result);
		}

		static int TranslateIndexToUTF8 (string text, int index)
		{
			byte [] bytes = Encoding.UTF8.GetBytes (text);
			return Encoding.UTF8.GetCharCount (bytes, 0, index);
		}
		#endregion

		protected SearchResult (int offset, int length)
		{
			Offset = offset;
			Length = length;
		}

		public SearchResult (FileProvider fileProvider, int offset, int length)
		{
			FileProvider = fileProvider;
			Offset = offset;
			Length = length;
		}

		public override string ToString ()
		{
			return string.Format("[SearchResult: FileProvider={0}, Offset={1}, Length={2}]", FileProvider, Offset, Length);
		}

		public virtual Components.HslColor GetBackgroundMarkerColor (EditorTheme style)
		{
			return SyntaxHighlightingService.GetColor (style, EditorThemeColors.FindHighlight);;
		}

		static TextEditor cachedEditor;
		static FileProvider cachedEditorFileProvider;

		TextEditor GetDocument ()
		{
			if (cachedEditor == null || cachedEditor.FileName != FileName || cachedEditorFileProvider != FileProvider) {
				var content = FileProvider.ReadString ();
				cachedEditor?.Dispose ();
				cachedEditor = TextEditorFactory.CreateNewEditor (TextEditorFactory.CreateNewReadonlyDocument (new Core.Text.StringTextSource (content.ReadToEnd ()), FileName, DesktopService.GetMimeTypeForUri (FileName)));
				cachedEditorFileProvider = FileProvider;
			}
			return cachedEditor;
		}

	}
}