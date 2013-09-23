// 
// QuickFixEditorExtension.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Ide.Gui.Content;
using Gtk;
using Mono.TextEditor;
using System.Collections.Generic;
using MonoDevelop.Components.Commands;
using MonoDevelop.SourceEditor.QuickTasks;
using System.Linq;
using MonoDevelop.Refactoring;
using ICSharpCode.NRefactory;
using System.Threading;
using MonoDevelop.Core;
using ICSharpCode.NRefactory.CSharp;

namespace MonoDevelop.CodeActions
{
	class CodeActionEditorExtension : TextEditorExtension 
	{
		CodeActionWidget widget;
		uint quickFixTimeout;

		const int menuTimeout = 250;
		uint smartTagPopupTimeoutId;
		uint menuCloseTimeoutId;
		Menu codeActionMenu;

		public IEnumerable<CodeAction> Fixes {
			get;
			private set;
		}

		void CancelSmartTagPopupTimeout ()
		{
			if (smartTagPopupTimeoutId != 0) {
				GLib.Source.Remove (smartTagPopupTimeoutId);
				smartTagPopupTimeoutId = 0;
			}
		}

		void CancelMenuCloseTimer ()
		{
			if (menuCloseTimeoutId != 0) {
				GLib.Source.Remove (menuCloseTimeoutId);
				menuCloseTimeoutId = 0;
			}
		}
		
		void RemoveWidget ()
		{
			if (widget != null) {
				widget.Destroy ();
				widget = null;
			}
			if (currentSmartTag != null) {
				document.Editor.Document.RemoveMarker (currentSmartTag);
				currentSmartTag = null;
				currentSmartTagBegin = DocumentLocation.Empty;
			}
			CancelSmartTagPopupTimeout ();

		}
		
		public override void Dispose ()
		{
			CancelMenuCloseTimer ();
			CancelQuickFixTimer ();
			document.Editor.SelectionChanged -= HandleSelectionChanged;
			document.DocumentParsed -= HandleDocumentDocumentParsed;
			document.Editor.Parent.BeginHover -= HandleBeginHover;
			RemoveWidget ();
			Fixes = null;
			base.Dispose ();
		}
		//TextLocation loc;
		void CreateWidget (IEnumerable<CodeAction> fixes, TextLocation loc)
		{
			//this.loc = loc;
			var editor = document.Editor;
			var container = editor.Parent;
			var point = editor.Parent.LocationToPoint (loc);
			point.Y += (int)editor.LineHeight;
			if (widget == null) {
				widget = new CodeActionWidget (this, Document);
				container.AddTopLevelWidget (
					widget,
					point.X,
					point.Y
				);
				widget.Show ();
			} else {
				if (!widget.Visible)
					widget.Show ();
				container.MoveTopLevelWidget (
					widget,
					point.X,
					point.Y
				);
			}
			widget.SetFixes (fixes, loc);
		}

		public void CancelQuickFixTimer ()
		{
			if (quickFixCancellationTokenSource != null)
				quickFixCancellationTokenSource.Cancel ();
			if (quickFixTimeout != 0) {
				GLib.Source.Remove (quickFixTimeout);
				quickFixTimeout = 0;
			}
		}

		CancellationTokenSource quickFixCancellationTokenSource;

		public override void CursorPositionChanged ()
		{
			CancelQuickFixTimer ();
			if (QuickTaskStrip.EnableFancyFeatures &&  Document.ParsedDocument != null && !Debugger.DebuggingService.IsDebugging) {
				quickFixCancellationTokenSource = new CancellationTokenSource ();
				var token = quickFixCancellationTokenSource.Token;
				quickFixTimeout = GLib.Timeout.Add (100, delegate {
					var loc = Document.Editor.Caret.Location;
					RefactoringService.QueueQuickFixAnalysis (Document, loc, token, delegate(List<CodeAction> fixes) {
						if (!fixes.Any ()) {
							ICSharpCode.NRefactory.Semantics.ResolveResult resolveResult;
							AstNode node;
							if (ResolveCommandHandler.ResolveAt (document, out resolveResult, out node, token)) {
								var possibleNamespaces = ResolveCommandHandler.GetPossibleNamespaces (document, node, ref resolveResult);
								if (!possibleNamespaces.Any ()) {
									if (currentSmartTag != null)
										Application.Invoke (delegate { RemoveWidget (); });
									return;
								}
							} else {
								if (currentSmartTag != null)
									Application.Invoke (delegate { RemoveWidget (); });
								return;
							}
						}
						Application.Invoke (delegate {
							if (token.IsCancellationRequested)
								return;
							CreateSmartTag (fixes, loc);
							quickFixTimeout = 0;
						});
					});
					return false;
				});
			} else {
				RemoveWidget ();
			}
			base.CursorPositionChanged ();
		}

		class SmartTagMarker : TextSegmentMarker, IActionTextLineMarker
		{
			CodeActionEditorExtension codeActionEditorExtension;
			internal List<CodeAction> fixes;
			DocumentLocation loc;

			public SmartTagMarker (int offset, CodeActionEditorExtension codeActionEditorExtension, List<CodeAction> fixes, DocumentLocation loc) : base (offset, 0)
			{
				this.codeActionEditorExtension = codeActionEditorExtension;
				this.fixes = fixes;
				this.loc = loc;
			}

			public SmartTagMarker (int offset) : base (offset, 0)
			{
			}
			const double tagMarkerWidth = 8;
			const double tagMarkerHeight = 2;
			public override void Draw (TextEditor editor, Cairo.Context cr, Pango.Layout layout, bool selected, int startOffset, int endOffset, double y, double startXPos, double endXPos)
			{
				var line = editor.GetLine (loc.Line);
				var x = editor.ColumnToX (line, loc.Column) - editor.HAdjustment.Value + editor.TextViewMargin.XOffset + editor.TextViewMargin.TextStartPosition;

				cr.Rectangle (Math.Floor (x) + 0.5, Math.Floor (y) + 0.5 + (line == editor.GetLineByOffset (startOffset) ? editor.LineHeight - tagMarkerHeight - 1 : 0), tagMarkerWidth * cr.LineWidth, tagMarkerHeight * cr.LineWidth);

				if (HslColor.Brightness (editor.ColorStyle.PlainText.Background) < 0.5) {
					cr.SetSourceRGBA (0.8, 0.8, 1, 0.9);
				} else {
					cr.SetSourceRGBA (0.2, 0.2, 1, 0.9);
				}
				cr.Stroke ();
			}

			#region IActionTextLineMarker implementation

			bool IActionTextLineMarker.MousePressed (TextEditor editor, MarginMouseEventArgs args)
			{
				return false;
			}

			void IActionTextLineMarker.MouseHover (TextEditor editor, MarginMouseEventArgs args, TextLineMarkerHoverResult result)
			{
				if (args.Button != 0)
					return;
				var line = editor.GetLine (loc.Line);
				if (line == null)
					return;
				var x = editor.ColumnToX (line, loc.Column) - editor.HAdjustment.Value + editor.TextViewMargin.TextStartPosition;
				var y = editor.LineToY (line.LineNumber + 1) - editor.VAdjustment.Value;
				if (args.X - x >= 0 * editor.Options.Zoom && 
				    args.X - x < tagMarkerWidth * editor.Options.Zoom && 
				    args.Y - y < (editor.LineHeight / 2) * editor.Options.Zoom) {
					result.Cursor = null;
					Popup ();
				} else {
					codeActionEditorExtension.CancelSmartTagPopupTimeout ();
				}
			}

			public void Popup ()
			{
				codeActionEditorExtension.smartTagPopupTimeoutId = GLib.Timeout.Add (menuTimeout, delegate {
					codeActionEditorExtension.CreateWidget (fixes, loc);
					codeActionEditorExtension.widget.PopupQuickFixMenu (menu => {
						codeActionEditorExtension.codeActionMenu = menu;
						menu.MotionNotifyEvent += (o, args) => {
							if (args.Event.Window == codeActionEditorExtension.Editor.Parent.TextArea.GdkWindow) {
								codeActionEditorExtension.StartMenuCloseTimer ();
							} else {
								codeActionEditorExtension.CancelMenuCloseTimer ();
							}
						};
					});
					codeActionEditorExtension.widget.Destroy ();
					codeActionEditorExtension.smartTagPopupTimeoutId = 0;
					return false;
				});
			}
			#endregion
		}

		SmartTagMarker currentSmartTag;
		DocumentLocation currentSmartTagBegin;
		void CreateSmartTag (List<CodeAction> fixes, DocumentLocation loc)
		{
			Fixes = fixes;
			if (!QuickTaskStrip.EnableFancyFeatures) {
				RemoveWidget ();
				return;
			}
			var editor = document.Editor;
			if (editor == null || editor.Parent == null || !editor.Parent.IsRealized) {
				RemoveWidget ();
				return;
			}
			if (document.ParsedDocument == null || document.ParsedDocument.IsInvalid) {
				RemoveWidget ();
				return;
			}

			var container = editor.Parent;
			if (container == null) {
				RemoveWidget ();
				return;
			}
			bool first = true;
			DocumentLocation smartTagLocBegin = loc;
			foreach (var fix in fixes) {
				if (fix.DocumentRegion.IsEmpty)
					continue;
				if (first || loc < fix.DocumentRegion.Begin) {
					smartTagLocBegin = fix.DocumentRegion.Begin;
				}
				first = false;
			}
			if (smartTagLocBegin.Line != loc.Line)
				smartTagLocBegin = new DocumentLocation (loc.Line, 1);
			// got no fix location -> try to search word start
			if (first) {
				int offset = document.Editor.LocationToOffset (smartTagLocBegin);
				while (offset > 0) {
					char ch = document.Editor.GetCharAt (offset - 1);
					if (!char.IsLetterOrDigit (ch) && ch != '_')
						break;
					offset--;
				}
				smartTagLocBegin = document.Editor.OffsetToLocation (offset);
			}

			if (currentSmartTag != null && currentSmartTagBegin == smartTagLocBegin) {
				currentSmartTag.fixes = fixes;
				return;
			}
			currentSmartTagBegin = smartTagLocBegin;

			RemoveWidget ();
			var line = document.Editor.GetLine (smartTagLocBegin.Line);
			currentSmartTag = new SmartTagMarker ((line.NextLine ?? line).Offset, this, fixes, smartTagLocBegin);
			document.Editor.Document.AddMarker (currentSmartTag);
		}
		
		public override void Initialize ()
		{
			base.Initialize ();
			document.DocumentParsed += HandleDocumentDocumentParsed;
			document.Editor.SelectionChanged += HandleSelectionChanged;
			document.Editor.Parent.BeginHover += HandleBeginHover;
		}

		void HandleBeginHover (object sender, EventArgs e)
		{
			CancelSmartTagPopupTimeout ();
			CancelMenuCloseTimer ();
		}

		void StartMenuCloseTimer ()
		{
			CancelMenuCloseTimer ();
			menuCloseTimeoutId = GLib.Timeout.Add (menuTimeout, delegate {
				if (codeActionMenu != null) {
					codeActionMenu.Destroy ();
					codeActionMenu = null;
				}
				menuCloseTimeoutId = 0;
				return false;
			});
		}

		void HandleSelectionChanged (object sender, EventArgs e)
		{
			CursorPositionChanged ();
		}
		
		void HandleDocumentDocumentParsed (object sender, EventArgs e)
		{
			CursorPositionChanged ();
		}
		
		[CommandUpdateHandler(RefactoryCommands.QuickFix)]
		public void UpdateQuickFixCommand (CommandInfo ci)
		{
			if (QuickTaskStrip.EnableFancyFeatures) {
				ci.Enabled = currentSmartTag != null;
			} else {
				ci.Enabled = true;
			}
		}
		
		[CommandHandler(RefactoryCommands.QuickFix)]
		void OnQuickFixCommand ()
		{
			if (!QuickTaskStrip.EnableFancyFeatures) {
				var w = new CodeActionWidget (this, Document);
				w.SetFixes (RefactoringService.GetValidActions (Document, Document.Editor.Caret.Location).Result, Document.Editor.Caret.Location);
				w.PopupQuickFixMenu ();
				w.Destroy ();

				return;
			}
			if (currentSmartTag == null)
				return;
			currentSmartTag.Popup ();
		}

		internal List<CodeAction> GetCurrentFixes ()
		{
			if (currentSmartTag == null)
				return RefactoringService.GetValidActions (document, document.Editor.Caret.Location).Result.ToList ();
			return currentSmartTag.fixes;
		}
	}
}

