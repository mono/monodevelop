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
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Components;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.SourceEditor.Wrappers;
using MonoDevelop.Core.Text;
using System.Threading;

namespace MonoDevelop.SourceEditor
{
	class ExtensibleTextEditor : Mono.TextEditor.MonoTextEditor
	{
		internal object MemoryProbe = Counters.EditorsInMemory.CreateMemoryProbe ();
		
		SourceEditorView view;
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

		public ISyntaxHighlighting SyntaxHighlighting {
			get {
				return Document.SyntaxMode;
			}
			internal set {
				Document.SyntaxMode = value;
			} 
		}


		void UpdateSemanticHighlighting ()
		{
			var oldSemanticHighighting = Document.SyntaxMode as SemanticHighlightingSyntaxMode;
			if (semanticHighlighting == null) {
				if (oldSemanticHighighting != null)
					Document.SyntaxMode = oldSemanticHighighting.UnderlyingSyntaxMode;
			} else {
				if (oldSemanticHighighting == null) {
					Document.SyntaxMode = new SemanticHighlightingSyntaxMode (this, Document.SyntaxMode, semanticHighlighting);
				} else {
					oldSemanticHighighting.UpdateSemanticHighlighting (semanticHighlighting);
				}
			}
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
				var native =(Tuple<Gdk.Key, Gdk.ModifierType>)descriptor.NativeKeyChar;
				ext.SimulateKeyPress (native.Item1, (uint)descriptor.KeyChar, native.Item2);
				if (descriptor.SpecialKey == SpecialKey.Escape)
					return true;
				return false;
			}
		}

		static ExtensibleTextEditor ()
		{
			var icon = Xwt.Drawing.Image.FromResource ("gutter-bookmark-15.png");

			BookmarkMarker.DrawBookmarkFunc = delegate(Mono.TextEditor.MonoTextEditor editor, Cairo.Context cr, DocumentLine lineSegment, double x, double y, double width, double height) {
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

			Document.SyntaxModeChanged += delegate {
				UpdateSemanticHighlighting ();
			};

			UpdateEditMode ();
			this.DoPopupMenu = ShowPopup;
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
	//		if (!(CurrentMode is SimpleEditMode)){
				SimpleEditMode simpleMode = new SimpleEditMode ();
				simpleMode.KeyBindings [Mono.TextEditor.EditMode.GetKeyCode (Gdk.Key.Tab)] = new TabAction (this).Action;
				simpleMode.KeyBindings [Mono.TextEditor.EditMode.GetKeyCode (Gdk.Key.BackSpace)] = EditActions.AdvancedBackspace;
				CurrentMode = simpleMode;
	//		}
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

		internal bool IsDestroyed { get; private set; }

		protected override void OnDestroyed ()
		{
			IsDestroyed = true;
			UnregisterAdjustments ();
			view = null;
			var disposableSyntaxMode = Document.SyntaxMode as IDisposable;
			if (disposableSyntaxMode != null)  {
				disposableSyntaxMode.Dispose ();
				Document.SyntaxMode = null;
			}
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

		protected internal override string GetIdeColorStyleName ()
		{
			var scheme = Ide.Editor.Highlighting.SyntaxHighlightingService.GetEditorTheme (IdeApp.Preferences.ColorScheme);
			if (!scheme.FitsIdeTheme (IdeApp.Preferences.UserInterfaceTheme))
				scheme = Ide.Editor.Highlighting.SyntaxHighlightingService.GetDefaultColorStyle (IdeApp.Preferences.UserInterfaceTheme);
			return scheme.Name;
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
			LoggingService.LogInternalError ("Error in text editor extension chain", ex);
		}

		internal static IEnumerable<char> GetTextWithoutCommentsAndStrings (Mono.TextEditor.TextDocument doc, int start, int end) 
		{
			bool isInString = false, isInChar = false;
			bool isInLineComment = false, isInBlockComment = false;
			int escaping = 0;
			
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
						} else  if (!isInString && !isInChar && pos + 1 < doc.Length) {
							char nextChar = doc.GetCharAt (pos + 1);
							if (nextChar == '/')
								isInLineComment = true;
							if (!isInLineComment && nextChar == '*')
								isInBlockComment = true;
						}
						break;
					case '"':
						if (!(isInChar || isInLineComment || isInBlockComment))
							if (!isInString || escaping != 1)
								isInString = !isInString;
						break;
					case '\'':
						if (!(isInString || isInLineComment || isInBlockComment))
							if (!isInChar || escaping != 1)
								isInChar = !isInChar;
						break;
					case '\\':
						if (escaping != 1)
							escaping = 2;
						break;
					default :
						if (!(isInString || isInChar || isInLineComment || isInBlockComment))
							yield return ch;
						break;
				}
				escaping--;
			}
		}


		protected internal override bool OnIMProcessedKeyPressEvent (Gdk.Key key, uint ch, Gdk.ModifierType state)
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


			var oldMode = Caret.IsInInsertMode;
			bool wasHandled = false;
			var currentSession = this.view.CurrentSession;
			if (currentSession != null) {
				switch (key) {
				case Gdk.Key.Return:
					currentSession.BeforeReturn (out wasHandled);
					break;
				case Gdk.Key.BackSpace:
					currentSession.BeforeBackspace (out wasHandled);
					break;
				case Gdk.Key.Delete:
				case Gdk.Key.KP_Delete:
					currentSession.BeforeDelete (out wasHandled);
					break;
				default:
					currentSession.BeforeType ((char)ch, out wasHandled);
					break;
				}
			}

			if (!wasHandled) {
				if (EditorExtension != null) {
					if (!DefaultSourceEditorOptions.Instance.GenerateFormattingUndoStep) {
						using (var undo = Document.OpenUndoGroup ()) {
							if (ExtensionKeyPress (key, ch, state))
								result = base.OnIMProcessedKeyPressEvent (key, ch, state);
						}
					} else {
						if (ExtensionKeyPress (key, ch, state))
							result = base.OnIMProcessedKeyPressEvent (key, ch, state);
					}
				} else {
					result = base.OnIMProcessedKeyPressEvent (key, ch, state);
				}

				if (currentSession != null) {
					switch (key) {
					case Gdk.Key.Return:
						currentSession.AfterReturn ();
						break;
					case Gdk.Key.BackSpace:
						currentSession.AfterBackspace ();
						break;
					case Gdk.Key.Delete:
					case Gdk.Key.KP_Delete:
						currentSession.AfterDelete ();
						break;
					default:
						currentSession.AfterType ((char)ch);
						break;
					}
				}
			}
			return result;
		}

		internal string GetErrorInformationAt (int offset)
		{
			var location = Document.OffsetToLocation (offset);
			DocumentLine line = Document.GetLine (location.Line);
			if (line == null)
				return null;

			var error = Document.GetTextSegmentMarkersAt (offset).OfType<ErrorMarker> ().FirstOrDefault ();
			
			if (error != null) {
				if (error.Error.ErrorType == MonoDevelop.Ide.TypeSystem.ErrorType.Warning)
					return GettextCatalog.GetString ("<b>Warning</b>: {0}",
						GLib.Markup.EscapeText (error.Error.Message));
				return GettextCatalog.GetString ("<b>Error</b>: {0}",
					GLib.Markup.EscapeText (error.Error.Message));
			}
			return null;
		}
		
		public MonoDevelop.Projects.Project Project {
			get {
				var doc = IdeApp.Workbench.ActiveDocument;
				if (doc != null) 
					return doc.Project;
				return null;
			}
		}
		
		public Microsoft.CodeAnalysis.ISymbol GetLanguageItem (int offset, out MonoDevelop.Ide.Editor.DocumentRegion region)
		{
			region = MonoDevelop.Ide.Editor.DocumentRegion.Empty;

			if (textEditorResolverProvider != null) {
				return textEditorResolverProvider.GetLanguageItem (view.WorkbenchWindow.Document, offset, out region);
			} 
			return null;
		}
		
		public CodeTemplateContext GetTemplateContext ()
		{
			if (IsSomethingSelected) {
				var result = GetLanguageItem (Caret.Offset, Document.GetTextAt (SelectionRange));
				if (result != null)
					return CodeTemplateContext.InExpression;
			}
			return CodeTemplateContext.Standard;
		}
		
		ITextEditorResolverProvider textEditorResolverProvider;
		
		public ITextEditorResolverProvider TextEditorResolverProvider {
			get { return this.textEditorResolverProvider; }
			internal set { this.textEditorResolverProvider = value; }
		}
		
		public Microsoft.CodeAnalysis.ISymbol GetLanguageItem (int offset, string expression)
		{
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
			while (offset < Document.Length && IsIdChar (Document.GetCharAt (offset)))
				offset++;
			start++;
			if (offset - start > 0 && start < Document.Length)
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

		string menuPath = "/MonoDevelop/SourceEditor2/ContextMenu/Editor";

		internal string ContextMenuPath {
			get {
				return menuPath;
			}

			set {
				menuPath = value;
			}
		}

		void ShowPopup (Gdk.EventButton evt)
		{
			view.FireCompletionContextChanged ();
			CompletionWindowManager.HideWindow ();
			ParameterInformationWindowManager.HideWindow (null, view);
			HideTooltip ();
			if (string.IsNullOrEmpty (menuPath))
				return;
			var ctx = view.WorkbenchWindow?.ExtensionContext ?? AddinManager.AddinEngine;
			CommandEntrySet cset = IdeApp.CommandService.CreateCommandEntrySet (ctx, menuPath);

			if (Platform.IsMac) {
				if (evt == null) {
					int x, y;
					var pt = LocationToPoint (this.Caret.Location);
					TranslateCoordinates (Toplevel, pt.X, pt.Y, out x, out y);

					IdeApp.CommandService.ShowContextMenu (this, x, y, cset, this);
				} else {
					IdeApp.CommandService.ShowContextMenu (this, evt, cset, this);
				}
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

					GtkWorkarounds.ShowContextMenu (menu, this, (int)pt.X, (int)pt.Y);
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

		public bool IsTemplateKnown (ExtensibleTextEditor instance)
		{
			string shortcut = CodeTemplate.GetTemplateShortcutBeforeCaret (EditorExtension.Editor);
			bool result = false;
			foreach (CodeTemplate template in CodeTemplateService.GetCodeTemplatesAsync (EditorExtension.Editor).WaitAndGetResult (CancellationToken.None)) {
				if (template.Shortcut == shortcut) {
					result = true;
				} else if (template.Shortcut.StartsWith (shortcut)) {
					result = false;
					break;
				}
			}
			return result;
		}
		
		public bool DoInsertTemplate ()
		{
			string shortcut = CodeTemplate.GetTemplateShortcutBeforeCaret (EditorExtension.Editor);
			foreach (CodeTemplate template in CodeTemplateService.GetCodeTemplatesAsync (EditorExtension.Editor).WaitAndGetResult (CancellationToken.None)) {
				if (template.Shortcut == shortcut) {
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
					Links = l.Links.Select (s => (ISegment)new TextSegment (s.Offset, s.Length)).ToList (),
					IsEditable = l.IsEditable,
					IsIdentifier = l.IsIdentifier,
					GetStringFunc = l.GetStringFunc != null ? (Func<Func<string, string>, Mono.TextEditor.PopupWindow.IListDataProvider<string>>)(arg => new ListDataProviderWrapper (l.GetStringFunc (arg))) : null
				}).ToList ();
				var tle = new TextLinkEditMode (this, result.InsertPosition, links);
				tle.TextLinkMode = TextLinkMode.General;
				if (tle.ShouldStartTextLinkMode) {
					tle.OldMode = CurrentMode;
					tle.StartMode ();
					CurrentMode = tle;
					GLib.Timeout.Add (10, delegate {
						tle.UpdateTextLinks ();
						return false;
					}); 
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
		
#region Key bindings
		[CommandUpdateHandler (MonoDevelop.Ide.Commands.TextEditorCommands.LineEnd)]
		[CommandUpdateHandler (MonoDevelop.Ide.Commands.TextEditorCommands.LineStart)]
		[CommandUpdateHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DeleteLeftChar)]
		[CommandUpdateHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DeleteRightChar)]
		[CommandUpdateHandler (MonoDevelop.Ide.Commands.TextEditorCommands.CharLeft)]
		[CommandUpdateHandler (MonoDevelop.Ide.Commands.TextEditorCommands.CharRight)]
		[CommandUpdateHandler (MonoDevelop.Ide.Commands.TextEditorCommands.LineUp)]
		[CommandUpdateHandler (MonoDevelop.Ide.Commands.TextEditorCommands.LineDown)]
		[CommandUpdateHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DocumentStart)]
		[CommandUpdateHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DocumentEnd)]
		[CommandUpdateHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DeleteLine)]
		[CommandUpdateHandler (MonoDevelop.Ide.Commands.TextEditorCommands.MoveBlockUp)]
		[CommandUpdateHandler (MonoDevelop.Ide.Commands.TextEditorCommands.MoveBlockDown)]
		[CommandUpdateHandler (MonoDevelop.Ide.Commands.TextEditorCommands.GotoMatchingBrace)]
		protected void OnUpdateEditorCommand (CommandInfo info)
		{
			// ignore command if the editor has no focus
			info.Bypass = HasFocus == false;
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.LineEnd)]
		internal void OnLineEnd ()
		{
			RunAction (CaretMoveActions.LineEnd);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.LineStart)]
		internal void OnLineStart ()
		{
			RunAction (CaretMoveActions.LineHome);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DeleteLeftChar)]
		internal void OnDeleteLeftChar ()
		{
			RunAction (DeleteActions.Backspace);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DeleteRightChar)]
		internal void OnDeleteRightChar ()
		{
			RunAction (DeleteActions.Delete);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.CharLeft)]
		internal void OnCharLeft ()
		{
			RunAction (CaretMoveActions.Left);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.CharRight)]
		internal void OnCharRight ()
		{
			RunAction (CaretMoveActions.Right);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.LineUp)]
		internal void OnLineUp ()
		{
			RunAction (CaretMoveActions.Up);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.LineDown)]
		internal void OnLineDown ()
		{
			RunAction (CaretMoveActions.Down);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DocumentStart)]
		internal void OnDocumentStart ()
		{
			RunAction (CaretMoveActions.ToDocumentStart);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DocumentEnd)]
		internal void OnDocumentEnd ()
		{
			RunAction (CaretMoveActions.ToDocumentEnd);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.PageUp)]
		internal void OnPageUp ()
		{
			RunAction (CaretMoveActions.PageUp);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.PageDown)]
		internal void OnPageDown ()
		{
			RunAction (CaretMoveActions.PageDown);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DeleteLine)]
		internal void OnDeleteLine ()
		{
			RunAction (DeleteActions.CaretLine);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DeleteToLineStart)]
		internal void OnDeleteToLineStart ()
		{
			RunAction (DeleteActions.CaretLineToStart);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DeleteToLineEnd)]
		internal void OnDeleteToLineEnd ()
		{
			RunAction (DeleteActions.CaretLineToEnd);
		}
		
 		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.ScrollLineUp)]
		internal void OnScrollLineUp ()
		{
			RunAction (ScrollActions.Up);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.ScrollLineDown)]
		internal void OnScrollLineDown ()
		{
			RunAction (ScrollActions.Down);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.ScrollPageUp)]
		internal void OnScrollPageUp ()
		{
			RunAction (ScrollActions.PageUp);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.ScrollPageDown)]
		internal void OnScrollPageDown ()
		{
			RunAction (ScrollActions.PageDown);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.ScrollTop)]
		internal void OnScrollTop ()
		{
			RunAction (ScrollActions.Top);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.ScrollBottom)]
		internal void OnScrollBottom ()
		{
			RunAction (ScrollActions.Bottom);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionMoveLeft)]
		internal void OnSelectionMoveLeft ()
		{
			RunAction (SelectionActions.MoveLeft);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionMoveRight)]
		internal void OnSelectionMoveRight ()
		{
			RunAction (SelectionActions.MoveRight);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.MovePrevWord)]
		internal void OnMovePrevWord ()
		{
			RunAction (CaretMoveActions.PreviousWord);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.MoveNextWord)]
		internal void OnMoveNextWord ()
		{
			RunAction (CaretMoveActions.NextWord);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionMovePrevWord)]
		internal void OnSelectionMovePrevWord ()
		{
			RunAction (SelectionActions.MovePreviousWord);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionMoveNextWord)]
		internal void OnSelectionMoveNextWord ()
		{
			RunAction (SelectionActions.MoveNextWord);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.MovePrevSubword)]
		internal void OnMovePrevSubword ()
		{
			RunAction (CaretMoveActions.PreviousSubword);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.MoveNextSubword)]
		internal void OnMoveNextSubword ()
		{
			RunAction (CaretMoveActions.NextSubword);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionMovePrevSubword)]
		internal void OnSelectionMovePrevSubword ()
		{
			RunAction (SelectionActions.MovePreviousSubword);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionMoveNextSubword)]
		internal void OnSelectionMoveNextSubword ()
		{
			RunAction (SelectionActions.MoveNextSubword);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionMoveUp)]
		internal void OnSelectionMoveUp ()
		{
			RunAction (SelectionActions.MoveUp);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionMoveDown)]
		internal void OnSelectionMoveDown ()
		{
			RunAction (SelectionActions.MoveDown);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionMoveHome)]
		internal void OnSelectionMoveHome ()
		{
			RunAction (SelectionActions.MoveLineHome);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionMoveEnd)]
		internal void OnSelectionMoveEnd ()
		{
			RunAction (SelectionActions.MoveLineEnd);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionMoveToDocumentStart)]
		internal void OnSelectionMoveToDocumentStart ()
		{
			RunAction (SelectionActions.MoveToDocumentStart);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.ExpandSelectionToLine)]
		internal void OnExpandSelectionToLine ()
		{
			RunAction (SelectionActions.ExpandSelectionToLine);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionMoveToDocumentEnd)]
		internal void OnSelectionMoveToDocumentEnd ()
		{
			RunAction (SelectionActions.MoveToDocumentEnd);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SwitchCaretMode)]
		internal void OnSwitchCaretMode ()
		{
			RunAction (MiscActions.SwitchCaretMode);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.InsertTab)]
		internal void OnInsertTab ()
		{
			RunAction (MiscActions.InsertTab);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.RemoveTab)]
		internal void OnRemoveTab ()
		{
			RunAction (MiscActions.RemoveTab);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.InsertNewLine)]
		internal void OnInsertNewLine ()
		{
			RunAction (MiscActions.InsertNewLine);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.InsertNewLineAtEnd)]
		internal void OnInsertNewLineAtEnd ()
		{
			RunAction (MiscActions.InsertNewLineAtEnd);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.InsertNewLinePreserveCaretPosition)]
		internal void OnInsertNewLinePreserveCaretPosition ()
		{
			RunAction (MiscActions.InsertNewLinePreserveCaretPosition);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.CompleteStatement)]
		internal void OnCompleteStatement ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			var generator = CodeGenerator.CreateGenerator (doc);
			if (generator != null) {
				generator.CompleteStatement (doc);
			}
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DeletePrevWord)]
		internal void OnDeletePrevWord ()
		{
			RunAction (DeleteActions.PreviousWord);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DeleteNextWord)]
		internal void OnDeleteNextWord ()
		{
			RunAction (DeleteActions.NextWord);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DeletePrevSubword)]
		internal void OnDeletePrevSubword ()
		{
			RunAction (DeleteActions.PreviousSubword);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DeleteNextSubword)]
		internal void OnDeleteNextSubword ()
		{
			RunAction (DeleteActions.NextSubword);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionPageDownAction)]
		internal void OnSelectionPageDownAction ()
		{
			RunAction (SelectionActions.MovePageDown);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionPageUpAction)]
		internal void OnSelectionPageUpAction ()
		{
			RunAction (SelectionActions.MovePageUp);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.PulseCaret)]
		internal void OnPulseCaretCommand ()
		{
			StartCaretPulseAnimation ();
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.TransposeCharacters)]
		internal void TransposeCharacters ()
		{
			RunAction (MiscActions.TransposeCharacters);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DuplicateLine)]
		internal void DuplicateLine ()
		{
			RunAction (MiscActions.DuplicateLine);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.RecenterEditor)]
		internal void RecenterEditor ()
		{
			RunAction (MiscActions.RecenterEditor);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.EditCommands.JoinWithNextLine)]
		internal void JoinLines ()
		{
			using (var undo = Document.OpenUndoGroup ()) {
				RunAction (Mono.TextEditor.Vi.ViActions.Join);
			}
		}
#endregion
	
	}
}
