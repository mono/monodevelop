// ExtendibleTextEditor.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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

using System;
using System.Collections.Generic;
using System.Linq;

using Gtk;

using Mono.TextEditor;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Components.Commands;
using Mono.TextEditor.Highlighting;
using MonoDevelop.Ide.CodeTemplates;
using Mono.Addins;
using MonoDevelop.Projects.Text;
using MonoDevelop.Ide;
using MonoDevelop.Ide.CodeFormatting;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using MonoDevelop.Components;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.SourceEditor.Wrappers;

namespace MonoDevelop.SourceEditor
{
	public class ExtensibleTextEditor : Mono.TextEditor.TextEditor
	{
		internal object MemoryProbe = Counters.EditorsInMemory.CreateMemoryProbe ();
		
		SourceEditorView view;
		ExtensionContext extensionContext;
		Adjustment cachedHAdjustment, cachedVAdjustment;
		
		TextEditorExtension editorExtension;
		bool needToAddLastExtension;

		public TextEditorExtension EditorExtension {
			get {
				return editorExtension;
			}
			set {
				editorExtension = value;
				needToAddLastExtension = true;
			}
		}

		SemanticHighlighting semanticHighlighting;
		public SemanticHighlighting SemanticHighlighting {
			get {
				return semanticHighlighting;
			}
			set {
				semanticHighlighting = value;
				UpdateSemanticHighlighting ();
			}
		}

		void UpdateSemanticHighlighting ()
		{
			if (Document.SyntaxMode is SemanticHighlightingSyntaxMode)
				return;
			if (semanticHighlighting == null) {
				Document.MimeType = Document.MimeType;
				return;
			}
			Document.SyntaxMode = new SemanticHighlightingSyntaxMode (this, Document.SyntaxMode, semanticHighlighting);
		}

		static Gdk.ModifierType ConvertModifiers (ModifierKeys s)
		{
			Gdk.ModifierType m = Gdk.ModifierType.None;
			if ((s & ModifierKeys.Shift) != 0)
				m |= Gdk.ModifierType.ShiftMask;
			if ((s & ModifierKeys.Control) != 0)
				m |= Gdk.ModifierType.ControlMask;
			if ((s & ModifierKeys.Alt) != 0)
				m |= Gdk.ModifierType.Mod1Mask;
			if ((s & ModifierKeys.Command) != 0)
				m |= Gdk.ModifierType.Mod2Mask;
			return m;
		}

		class LastEditorExtension : TextEditorExtension
		{
			readonly ExtensibleTextEditor ext;
			public LastEditorExtension (ExtensibleTextEditor ext)
			{
				if (ext == null)
					throw new ArgumentNullException ("ext");
				this.ext = ext;
			}
			
			public override bool KeyPress (KeyDescriptor descriptor)
			{
				ext.SimulateKeyPress ((Gdk.Key)descriptor.SpecialKey, (uint)descriptor.KeyChar, ConvertModifiers (descriptor.ModifierKeys));
				if (descriptor.SpecialKey == SpecialKey.Escape)
					return true;
				return false;
			}
		}

		static ExtensibleTextEditor ()
		{
			var icon = Xwt.Drawing.Image.FromResource ("gutter-bookmark-light-15.png");

			BookmarkMarker.DrawBookmarkFunc = delegate(Mono.TextEditor.TextEditor editor, Cairo.Context cr, DocumentLine lineSegment, double x, double y, double width, double height) {
				if (!lineSegment.IsBookmarked)
					return;
				cr.DrawImage (
					editor, 
					icon, 
					Math.Floor (x + (width - icon.Width) / 2), 
					Math.Floor (y + (height - icon.Height) / 2)
				);
			};

		}
		
		public ExtensibleTextEditor (SourceEditorView view, Mono.TextEditor.ITextEditorOptions options, Mono.TextEditor.TextDocument doc) : base(doc, options)
		{
			Initialize (view);
		}

		public ExtensibleTextEditor (SourceEditorView view)
		{
			base.Options = new StyledSourceEditorOptions (DefaultSourceEditorOptions.Instance);
			Initialize (view);
		}
		
		internal SourceEditorView View {
			get { return view; }
		}
		
		void Initialize (SourceEditorView view)
		{
			this.view = view;

			Document.TextReplaced += HandleSkipCharsOnReplace;
			TypeSystemService.ParseOperationFinished += HandleParseOperationFinished;
			Document.SyntaxModeChanged += delegate {
				UpdateSemanticHighlighting ();
			};

			UpdateEditMode ();
			this.DoPopupMenu = ShowPopup;
		}
		
		void HandleSkipCharsOnReplace (object sender, DocumentChangeEventArgs args)
		{
			var skipChars = GetTextEditorData ().SkipChars;
			for (int i = 0; i < skipChars.Count; i++) {
				var sc = skipChars [i];
				if (args.Offset > sc.Offset) {
					skipChars.RemoveAt (i);
					i--;
					continue;
				}
				if (args.Offset <= sc.Offset) {
					sc.Offset += args.ChangeDelta;
				}
			}
		}
		

		static bool? testNewViMode = null;
		static bool TestNewViMode {
			get {
				if (!testNewViMode.HasValue)
					testNewViMode = System.Environment.GetEnvironmentVariable ("TEST_NEW_VI_MODE") != null;
				return testNewViMode.Value;
			}
		}
		
		void UpdateEditMode ()
		{
			if (MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.UseViModes) {
				if (TestNewViMode) {
					if (!(CurrentMode is NewIdeViMode))
					CurrentMode = new NewIdeViMode (this);
				} else {
					if (!(CurrentMode is IdeViMode))
						CurrentMode = new IdeViMode (this);
				}
			} else {
		//		if (!(CurrentMode is SimpleEditMode)){
					SimpleEditMode simpleMode = new SimpleEditMode ();
					simpleMode.KeyBindings [Mono.TextEditor.EditMode.GetKeyCode (Gdk.Key.Tab)] = new TabAction (this).Action;
					simpleMode.KeyBindings [Mono.TextEditor.EditMode.GetKeyCode (Gdk.Key.BackSpace)] = EditActions.AdvancedBackspace;
					CurrentMode = simpleMode;
		//		}
			}
		}

		void UnregisterAdjustments ()
		{
			if (cachedHAdjustment != null)
				cachedHAdjustment.ValueChanged -= HAdjustment_ValueChanged;
			if (cachedVAdjustment != null)
				cachedVAdjustment.ValueChanged -= VAdjustment_ValueChanged;
			cachedHAdjustment = null;
			cachedVAdjustment = null;
		}

		protected override void OnDestroyed ()
		{
			UnregisterAdjustments ();
			Extension = null;
			ExtensionContext = null;
			view = null;
			base.OnDestroyed ();
			if (Options != null) {
				Options.Dispose ();
				base.Options = null;
			}
		}

		public void FireOptionsChange ()
		{
			this.OptionsChanged (null, null);
		}
		
		protected override void OptionsChanged (object sender, EventArgs args)
		{
			if (view != null && view.Control != null) {
				if (!Options.ShowFoldMargin)
					this.Document.ClearFoldSegments ();
			}
			UpdateEditMode ();
			base.OptionsChanged (sender, args);
		}
		
		bool isInKeyStroke = false;
		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			isInKeyStroke = true;
			try {
				// Handle keyboard toolip popup
	/*			if ((evnt.Key == Gdk.Key.F1 && (evnt.State & Gdk.ModifierType.ControlMask) == Gdk.ModifierType.ControlMask)) {
					Gdk.Point p = this.TextViewMargin.LocationToDisplayCoordinates (this.Caret.Location);
					this.mx = p.X;
					this.my = p.Y;
					this.ShowTooltip ();
					return true;
				}
	*/			
				return base.OnKeyPressEvent (evnt);
			} finally {
				isInKeyStroke = false;
			}
		}
		
		bool ExtensionKeyPress (Gdk.Key key, uint ch, Gdk.ModifierType state)
		{
			isInKeyStroke = true;
			try {
				if (needToAddLastExtension) {
					var ext = EditorExtension;
					while (ext.Next != null)
						ext = ext.Next;
					ext.Next = new LastEditorExtension (this);
					needToAddLastExtension = false;
				}
				return EditorExtension.KeyPress (KeyDescriptor.FromGtk (key, (char)ch, state));
			} catch (Exception ex) {
				ReportExtensionError (ex);
			} finally {
				isInKeyStroke = false;
			}
			return false;
		}
		
		void ReportExtensionError (Exception ex) 
		{
			MonoDevelop.Core.LoggingService.LogError ("Error in text editor extension chain", ex);
			MessageService.ShowException (ex, "Error in text editor extension chain");
		}
		
		IEnumerable<char> TextWithoutCommentsAndStrings {
			get {
				return from p in GetTextWithoutCommentsAndStrings (Document, 0, Document.TextLength) select p.Key;
			}
		}
		
		static IEnumerable<KeyValuePair <char, int>> GetTextWithoutCommentsAndStrings (Mono.TextEditor.TextDocument doc, int start, int end) 
		{
			bool isInString = false, isInChar = false;
			bool isInLineComment = false, isInBlockComment = false;
			
			for (int pos = start; pos < end; pos++) {
				char ch = doc.GetCharAt (pos);
				switch (ch) {
					case '\r':
					case '\n':
						isInLineComment = false;
						break;
					case '/':
						if (isInBlockComment) {
							if (pos > 0 && doc.GetCharAt (pos - 1) == '*') 
								isInBlockComment = false;
						} else  if (!isInString && !isInChar && pos + 1 < doc.TextLength) {
							char nextChar = doc.GetCharAt (pos + 1);
							if (nextChar == '/')
								isInLineComment = true;
							if (!isInLineComment && nextChar == '*')
								isInBlockComment = true;
						}
						break;
					case '"':
						if (!(isInChar || isInLineComment || isInBlockComment)) 
							isInString = !isInString;
						break;
					case '\'':
						if (!(isInString || isInLineComment || isInBlockComment)) 
							isInChar = !isInChar;
						break;
					default :
						if (!(isInString || isInChar || isInLineComment || isInBlockComment))
							yield return new KeyValuePair<char, int> (ch, pos);
						break;
				}
			}
		}
		
		
		protected override bool OnIMProcessedKeyPressEvent (Gdk.Key key, uint ch, Gdk.ModifierType state)
		{
			bool result = true;
			if (key == Gdk.Key.Escape) {
				bool b = EditorExtension != null ? ExtensionKeyPress (key, ch, state) : base.OnIMProcessedKeyPressEvent (key, ch, state);
				if (b) {
					view.SourceEditorWidget.RemoveSearchWidget ();
					return true;
				}
				return false; 
			}

			if (Document == null)
				return true;

			bool inStringOrComment = false;
			bool isString = false;
			DocumentLine line = Document.GetLine (Caret.Line);
			if (line == null)
				return true;
			//			string escape = "\"";
			var stack = line.StartSpan.Clone ();
			var sm = Document.SyntaxMode as SyntaxMode;
			if (sm != null)
				Mono.TextEditor.Highlighting.SyntaxModeService.ScanSpans (Document, sm, sm, stack, line.Offset, Caret.Offset);

			foreach (Span span in stack) {
				if (string.IsNullOrEmpty (span.Color))
					continue;
				if (span.Color.StartsWith ("String", StringComparison.Ordinal) ||
				    span.Color.StartsWith ("Comment", StringComparison.Ordinal) ||
				    span.Color.StartsWith ("Xml Attribute Value", StringComparison.Ordinal)) {
					//Treat "Xml Attribute Value" as "String" so quotes in SkipChars works in Xml
					if (span.Color.StartsWith ("Comment", StringComparison.Ordinal)) {
						isString = false;
					} else {
						isString = true;
					}
					inStringOrComment = true;
					break;
				}
			}
			// insert template when space is typed (currently disabled - it's annoying).
			bool templateInserted = false;
			//!inStringOrComment && (key == Gdk.Key.space) && DoInsertTemplate ();
			bool returnBetweenBraces = key == Gdk.Key.Return && (state & (Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask)) == Gdk.ModifierType.None && Caret.Offset > 0 && Caret.Offset < Document.TextLength && Document.GetCharAt (Caret.Offset - 1) == '{' && Document.GetCharAt (Caret.Offset) == '}' && !inStringOrComment;
//			int initialOffset = Caret.Offset;
			const string openBrackets = "{[('\"";
			const string closingBrackets = "}])'\"";
			int braceIndex = openBrackets.IndexOf ((char)ch);
			var skipChars = GetTextEditorData ().SkipChars;
			var skipChar = skipChars.Find (sc => sc.Char == (char)ch && sc.Offset == Caret.Offset);
//			bool startedAtomicOperation = false;

			// special handling for escape chars inside ' and "
			if (Caret.Offset > 0) {
				char charBefore = Document.GetCharAt (Caret.Offset - 1);
				if (ch == '"') {
					if (!inStringOrComment && charBefore == '"' || 
						isString && charBefore == '\\' ) {
						skipChar = null;
						braceIndex = -1;
					}
				}

			}
			char insertionChar = '\0';
			bool insertMatchingBracket = false;
			IDisposable undoGroup = null;
			if (skipChar == null && MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.AutoInsertMatchingBracket && braceIndex >= 0 && !IsSomethingSelected) {
				if (!inStringOrComment) {
					char closingBrace = closingBrackets [braceIndex];
					char openingBrace = openBrackets [braceIndex];

					int count = 0;
					foreach (char curCh in TextWithoutCommentsAndStrings) {
						if (curCh == openingBrace) {
							count++;
						} else if (curCh == closingBrace) {
							count--;
						}
					}

					if (count >= 0) {
						insertMatchingBracket = true;
						insertionChar = closingBrace;
					}
				} else {
					char charBefore = Document.GetCharAt (Caret.Offset - 1);
					if (!inStringOrComment && ch == '"' && charBefore != '\\') {
						insertMatchingBracket = true;
						insertionChar = '"';
					}
				}
			}
			
			//Console.WriteLine (Caret.Offset + "/" + insOff);
			if (insertMatchingBracket)
				undoGroup = Document.OpenUndoGroup ();

			var oldMode = Caret.IsInInsertMode;
			if (skipChar != null) {
				Caret.IsInInsertMode = false;
				skipChars.Remove (skipChar);
			}
			if (EditorExtension != null) {
				if (!MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.GenerateFormattingUndoStep) {
					using (var undo = Document.OpenUndoGroup ()) {
						if (ExtensionKeyPress (key, ch, state))
							result = base.OnIMProcessedKeyPressEvent (key, ch, state);
					}
				} else {
					if (ExtensionKeyPress (key, ch, state))
						result = base.OnIMProcessedKeyPressEvent (key, ch, state);
				}
				if (returnBetweenBraces)
					HitReturn ();
			} else {
				result = base.OnIMProcessedKeyPressEvent (key, ch, state);
				if (returnBetweenBraces)
					HitReturn ();
			}
			if (skipChar != null) {
				Caret.IsInInsertMode = oldMode;
			}

			if (insertMatchingBracket) {
				GetTextEditorData ().EnsureCaretIsNotVirtual ();
				int offset = Caret.Offset;
				Caret.AutoUpdatePosition = false;
				Insert (offset, insertionChar.ToString ());
				Caret.AutoUpdatePosition = true;
				GetTextEditorData ().SetSkipChar (offset, insertionChar);
				undoGroup.Dispose ();
			}
			return templateInserted || result;
		}
		
		void HitReturn ()
		{
			int o = Caret.Offset - 1;
			while (o > 0 && char.IsWhiteSpace (GetCharAt (o - 1))) 
				o--;
			Caret.Offset = o;
			ExtensionKeyPress (Gdk.Key.Return, (char)0, Gdk.ModifierType.None);			
		}
		
		internal string GetErrorInformationAt (int offset)
		{
			var location = Document.OffsetToLocation (offset);
			DocumentLine line = Document.GetLine (location.Line);
			if (line == null)
				return null;

			var error = Document.GetTextSegmentMarkersAt (offset).OfType<ErrorMarker> ().FirstOrDefault ();
			
			if (error != null) {
				if (error.Error.ErrorType == ErrorType.Warning)
					return GettextCatalog.GetString ("<b>Parser Warning</b>: {0}",
						GLib.Markup.EscapeText (error.Error.Message));
				return GettextCatalog.GetString ("<b>Parser Error</b>: {0}",
					GLib.Markup.EscapeText (error.Error.Message));
			}
			return null;
		}
		
		internal ParsedDocument ParsedDocument {
			get {
				var doc = IdeApp.Workbench.ActiveDocument;
				if (doc != null) 
					return doc.ParsedDocument;
				return null;
			}
		}
		
		public MonoDevelop.Projects.Project Project {
			get {
				var doc = IdeApp.Workbench.ActiveDocument;
				if (doc != null) 
					return doc.Project;
				return null;
			}
		}
		
		int           oldOffset = -1;

		public ResolveResult GetLanguageItem (int offset, out DomRegion region)
		{
			oldOffset = offset;
			region = DomRegion.Empty;

			if (textEditorResolverProvider != null) {
				return textEditorResolverProvider.GetLanguageItem (view.WorkbenchWindow.Document, offset, out region);
			} 
			return null;
		}
		
		public CodeTemplateContext GetTemplateContext ()
		{
			if (IsSomethingSelected) {
				var result = GetLanguageItem (Caret.Offset, Document.GetTextAt (SelectionRange));
				if (result != null && !result.IsError)
					return CodeTemplateContext.InExpression;
			}
			return CodeTemplateContext.Standard;
		}
		
		ITextEditorResolverProvider textEditorResolverProvider;
		
		public ITextEditorResolverProvider TextEditorResolverProvider {
			get { return this.textEditorResolverProvider; }
			internal set { this.textEditorResolverProvider = value; }
		}
		
		public ResolveResult GetLanguageItem (int offset, string expression)
		{
			oldOffset = offset;
			
			if (textEditorResolverProvider != null) {
				return textEditorResolverProvider.GetLanguageItem (view.WorkbenchWindow.Document, offset, expression);
			}
	
			return null;
		}

//		public string GetExpression (int offset)
//		{
//			if (textEditorResolverProvider != null) 
//				return textEditorResolverProvider.GetExpression (view.WorkbenchWindow.Document, offset);
//			return string.Empty;
//		}
		
		string GetExpressionBeforeOffset (int offset)
		{
			int start = offset;
			while (start > 0 && IsIdChar (Document.GetCharAt (start)))
				start--;
			while (offset < Document.TextLength && IsIdChar (Document.GetCharAt (offset)))
				offset++;
			start++;
			if (offset - start > 0 && start < Document.TextLength)
				return Document.GetTextAt (start, offset - start);
			else
				return string.Empty;
		}
		
		bool IsIdChar (char c)
		{
			return char.IsLetterOrDigit (c) || c == '_';
		}
		
		protected override bool OnFocusOutEvent (Gdk.EventFocus evnt)
		{
			CompletionWindowManager.HideWindow ();
			ParameterInformationWindowManager.HideWindow (null, view);
			return base.OnFocusOutEvent (evnt); 
		}
		
		void ShowPopup (Gdk.EventButton evt)
		{
			view.FireCompletionContextChanged ();
			CompletionWindowManager.HideWindow ();
			ParameterInformationWindowManager.HideWindow (null, view);
			HideTooltip ();
			const string menuPath = "/MonoDevelop/SourceEditor2/ContextMenu/Editor";
			var ctx = view.WorkbenchWindow.ExtensionContext ?? AddinManager.AddinEngine;

			CommandEntrySet cset = IdeApp.CommandService.CreateCommandEntrySet (ctx, menuPath);

			if (Platform.IsMac) {
				IdeApp.CommandService.ShowContextMenu (this, evt, cset, this);
			} else {
				Gtk.Menu menu = IdeApp.CommandService.CreateMenu (cset);
				var imMenu = CreateInputMethodMenuItem (GettextCatalog.GetString ("_Input Methods"));
				if (imMenu != null) {
					menu.Append (new SeparatorMenuItem ());
					menu.Append (imMenu);
				}

				menu.Hidden += HandleMenuHidden;
				if (evt != null) {
					GtkWorkarounds.ShowContextMenu (menu, this, evt);
				} else {
					var pt = LocationToPoint (this.Caret.Location);
					GtkWorkarounds.ShowContextMenu (menu, this, new Gdk.Rectangle (pt.X, pt.Y, 1, (int)LineHeight));
				}
			}
		}

		void HandleMenuHidden (object sender, EventArgs e)
		{	
			var menu = sender as Gtk.Menu;
			menu.Hidden -= HandleMenuHidden; 
			GLib.Timeout.Add (10, delegate {
				menu.Destroy ();
				return false;
			});
		}
		
#region Templates
		int FindPrevWordStart (int offset)
		{
			while (--offset >= 0 && !Char.IsWhiteSpace (Document.GetCharAt (offset))) 
				;
			return ++offset;
		}

		public string GetWordBeforeCaret ()
		{
			int offset = this.Caret.Offset;
			int start  = FindPrevWordStart (offset);
			return Document.GetTextAt (start, offset - start);
		}
		
		public bool IsTemplateKnown ()
		{
			string word = GetWordBeforeCaret ();
			bool result = false;
			foreach (CodeTemplate template in CodeTemplateService.GetCodeTemplates (Document.MimeType)) {
				if (template.Shortcut == word) {
					result = true;
				} else if (template.Shortcut.StartsWith (word)) {
					result = false;
					break;
				}
			}
			return result;
		}
		
		public bool DoInsertTemplate ()
		{
			string word = GetWordBeforeCaret ();
			foreach (CodeTemplate template in CodeTemplateService.GetCodeTemplates (Document.MimeType)) {
				if (template.Shortcut == word) {
					InsertTemplate (template, view.WorkbenchWindow.Document.Editor, view.WorkbenchWindow.Document);
					return true;
				}
			}
			return false;
		}
		

		internal void InsertTemplate (CodeTemplate template, MonoDevelop.Ide.Editor.TextEditor editor, MonoDevelop.Ide.Editor.DocumentContext context)
		{
			using (var undo = editor.OpenUndoGroup ()) {
				var result = template.InsertTemplateContents (editor, context);

				var links = result.TextLinks.Select (l => new Mono.TextEditor.TextLink (l.Name) {
					Links = l.Links.Select (s => new TextSegment (s.Offset, s.Length)).ToList (),
					IsEditable = l.IsEditable,
					IsIdentifier = l.IsIdentifier
				}).ToList ();
				var tle = new TextLinkEditMode (this, result.InsertPosition, links);
				tle.TextLinkMode = TextLinkMode.General;
				if (tle.ShouldStartTextLinkMode) {
					tle.OldMode = CurrentMode;
					tle.StartMode ();
					CurrentMode = tle;
				}
			}
		}

		protected override void OnScrollAdjustmentsSet()
		{
			UnregisterAdjustments ();
			if (HAdjustment != null) {
				cachedHAdjustment = HAdjustment;
				HAdjustment.ValueChanged += HAdjustment_ValueChanged;
			}
			if (VAdjustment != null) {
				cachedVAdjustment = VAdjustment;
				VAdjustment.ValueChanged += VAdjustment_ValueChanged;
			}
		}

		void VAdjustment_ValueChanged (object sender, EventArgs e)
		{
			CompletionWindowManager.HideWindow ();
			ParameterInformationWindowManager.HideWindow (null, view);
		}

		void HAdjustment_ValueChanged (object sender, EventArgs e)
		{
			if (!isInKeyStroke) {
				CompletionWindowManager.HideWindow ();
				ParameterInformationWindowManager.HideWindow (null, view);
			} else {
				CompletionWindowManager.RepositionWindow ();
				ParameterInformationWindowManager.RepositionWindow (null, view);
			}
		}
		
#endregion
	
	}
}
