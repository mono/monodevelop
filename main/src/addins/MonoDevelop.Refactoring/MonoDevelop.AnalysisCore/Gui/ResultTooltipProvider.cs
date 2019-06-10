
// 
// ResultTooltipProvider.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using System.Text;
using System.Collections.Generic;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Editor;
using MonoDevelop.SourceEditor;
using System.Linq;
using MonoDevelop.Components;
using System.Threading.Tasks;
using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Fonts;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Reflection;
using MonoDevelop.CodeActions;
using System.Windows.Input;
using MonoDevelop.Ide;

namespace MonoDevelop.AnalysisCore.Gui
{
	[Obsolete ("Old editor")]
	partial class ResultTooltipProvider : TooltipProvider
	{
		#region ITooltipProvider implementation 
		public override async Task<TooltipItem> GetItem (Ide.Editor.TextEditor editor, DocumentContext ctx, int offset, CancellationToken token = default (CancellationToken))
		{
			var results = new List<Result> ();
			int markerOffset = -1, markerEndOffset = -1;
			foreach (var marker in editor.GetTextSegmentMarkersAt (offset)) {
				if (marker.Tag is Result result) {
					if (result.Level == Microsoft.CodeAnalysis.DiagnosticSeverity.Hidden && result.InspectionMark == IssueMarker.WavedLine)
						continue;
					if (markerOffset < 0) {
						markerOffset = marker.Offset;
						markerEndOffset = marker.EndOffset;
					} else {
						markerOffset = Math.Max (markerOffset, marker.Offset);
						markerEndOffset = Math.Min (markerEndOffset, marker.EndOffset);
					}
					results.Add (result);
				}
			}
			if (results.Count == 0)
				return null;
			var sb = StringBuilderCache.Allocate ();
			sb.Append ("<span font='");
			sb.Append (IdeServices.FontService.SansFontName);
			sb.Append ("' size='small'>");
			int minOffset = int.MaxValue;
			int maxOffset = -1;
			bool floatingWidgetShown = false;
			for (int i = 0; i < results.Count; i++) {
				var r = results [i];
				var escapedMessage = Ambience.EscapeText (r.Message);
				if (i > 0)
					sb.AppendLine ();
				minOffset = Math.Min (minOffset, r.Region.Start);
				maxOffset = Math.Max (maxOffset, r.Region.End);
				floatingWidgetShown |= r.InspectionMark == IssueMarker.WavedLine;

				if (results.Count > 1) {
					string severity;
					HslColor color;
					switch (r.Level) {
					case Microsoft.CodeAnalysis.DiagnosticSeverity.Info:
						severity = GettextCatalog.GetString ("Info");
						editor.Options.GetEditorTheme ().TryGetColor (EditorThemeColors.UnderlineSuggestion, out color);
						break;
					case Microsoft.CodeAnalysis.DiagnosticSeverity.Warning:
						severity = GettextCatalog.GetString ("Warning");
						editor.Options.GetEditorTheme ().TryGetColor (EditorThemeColors.UnderlineWarning, out color);
						break;
					case Microsoft.CodeAnalysis.DiagnosticSeverity.Error:
						severity = GettextCatalog.GetString ("Error");
						editor.Options.GetEditorTheme ().TryGetColor (EditorThemeColors.UnderlineError, out color);
						break;
					default:
						severity = "?";
						editor.Options.GetEditorTheme ().TryGetColor (EditorThemeColors.UnderlineSuggestion, out color);
						break;
					}
					sb.AppendFormat ("<span foreground ='{2}'font_weight='bold'>{0}</span>: {1}", severity, escapedMessage, color.ToPangoString ());
				} else {
					sb.Append (escapedMessage);
				}
			}

			sb.Append ("</span>");
			CodeActions.CodeActionContainer tag = null;
			try {
				var ad = ctx.AnalysisDocument;
				var root = await ad.GetSyntaxRootAsync (token);
				if (root.Span.End < offset) {
					LoggingService.LogError ($"Error in ResultTooltipProvider.GetItem offset {offset} not inside syntax root {root.Span.End} document length {editor.Length}.");
				} else {
					var codeFixService = Ide.Composition.CompositionManager.Instance.GetExportedValue<ICodeFixService> ();
					var span = new TextSpan (offset, 0);
					var fixes = await codeFixService.GetFixesAsync (ad, span, true, token);
					var codeRefactoringService = Ide.Composition.CompositionManager.Instance.GetExportedValue<Microsoft.CodeAnalysis.CodeRefactorings.ICodeRefactoringService> ();
					var refactorings = await codeRefactoringService.GetRefactoringsAsync (ad, span, token);
					tag = new CodeActions.CodeActionContainer (fixes, refactorings) {
						Span = new TextSpan (minOffset, Math.Max (0,  maxOffset - minOffset)),
						FloatingWidgetShown = floatingWidgetShown
					};
				}
			} catch (AggregateException ae) {
				ae.Flatten ().Handle (aex => aex is OperationCanceledException);
			} catch (OperationCanceledException) {
			} catch (TargetInvocationException ex) {
				if (!(ex.InnerException is OperationCanceledException))
					throw;
			}
			if (tag == null)
				return null;
			var tooltipInfo = new TaggedTooltipInformation<CodeActions.CodeActionContainer> {
				SignatureMarkup = StringBuilderCache.ReturnAndFree (sb),
				Tag = tag
			};
			return new TooltipItem (tooltipInfo, markerOffset, markerEndOffset - markerOffset);
		}

		public override Window CreateTooltipWindow (Ide.Editor.TextEditor editor, DocumentContext ctx, TooltipItem item, int offset, Xwt.ModifierKeys modifierState)
		{
			var result = item.Item as TooltipInformation;
			if (result == null)
				return null;
			var window = new LanguageItemWindow (CompileErrorTooltipProvider.GetExtensibleTextEditor (editor), modifierState, null, result.SignatureMarkup, null);
			window.Destroyed += delegate {
				if (window.Tag is FloatingQuickFixIconWidget widget) {
					widget.QueueDestroy ();
				}
			};
			if (window.IsEmpty)
				return null;
			return window;
		}

		public override void GetRequiredPosition (Ide.Editor.TextEditor editor, Window tipWindow, out int requiredWidth, out double xalign)
		{
			var win = (LanguageItemWindow)tipWindow;
			requiredWidth = win.SetMaxWidth (win.Screen.Width / 4);
			xalign = 0.5;
		}

		const int yPadding = 4;
		const int xPadding = 4;
		const int windowSize = 36;

		protected override Xwt.Point CalculateWindowLocation (Ide.Editor.TextEditor editor, TooltipItem item, Xwt.WindowFrame xwtWindow, int mouseX, int mouseY, Xwt.Point origin)
		{
			int w;
			double xalign;
			GetRequiredPosition (editor, xwtWindow, out w, out xalign);
			w += 10;
			var allocation = GetAllocation (editor);

			var info = (TaggedTooltipInformation<CodeActions.CodeActionContainer>)item.Item;
			var loc = editor.OffsetToLocation (info.Tag.Span.Start);
			var p = editor.LocationToPoint (loc);
			var view = editor.GetContent<SourceEditorView> ();
			int x = (int)(p.X +  origin.X + allocation.X + xPadding);
			int y = (int)(p.Y + view.TextEditor.GetLineHeight (loc.Line) + origin.Y + allocation.Y + yPadding);

			Gtk.Widget widget = editor;
			var geometry = widget.Screen.GetUsableMonitorGeometry (widget.Screen.GetMonitorAtPoint (x, y));

			if (x + w >= geometry.X + geometry.Width)
				x = geometry.X + geometry.Width - w;
			if (x < geometry.Left)
				x = geometry.Left;

			if (info.Tag?.FloatingWidgetShown == true) {
				x += windowSize;
			}

			int h = (int)xwtWindow.Size.Height;
			if (y + h >= geometry.Y + geometry.Height)
				y = geometry.Y + geometry.Height - h;
			if (y < geometry.Top)
				y = geometry.Top;

			return new Xwt.Point (x, y);
		}

		public override void ShowTooltipWindow (Ide.Editor.TextEditor editor, Components.Window tipWindow, TooltipItem item, Xwt.ModifierKeys modifierState, int mouseX, int mouseY)
		{
			base.ShowTooltipWindow (editor, tipWindow, item, modifierState, mouseX, mouseY);
			var info = (TaggedTooltipInformation<CodeActions.CodeActionContainer>)item.Item;
			var sourceEditorView = editor.GetContent<SourceEditorView> ();
			var codeActionEditorExtension = editor.GetContent<CodeActionEditorExtension> ();
			var loc = editor.OffsetToLocation (info.Tag.Span.Start);
			if (info.Tag?.FloatingWidgetShown == true) {
				var point = sourceEditorView.TextEditor.TextArea.LocationToPoint (loc.Line, loc.Column);
				point.Y += (int)editor.GetLineHeight (loc.Line);
				var window = (LanguageItemWindow)tipWindow;
				if (floatingWidget != null) {
					floatingWidget.Destroy ();
					floatingWidget = null;
				}
				floatingWidget = new FloatingQuickFixIconWidget (codeActionEditorExtension, window, sourceEditorView, info.Tag, point);
				sourceEditorView.TextEditor.GdkWindow.GetOrigin (out int ox, out int oy);
				floatingWidget.Move (ox + (int)point.X, oy + (int)(point.Y + 4));
				window.Tag = floatingWidget;
				window.EnterNotifyEvent += delegate {
					floatingWidget.CancelDestroy ();
				};
				window.LeaveNotifyEvent += delegate {
					floatingWidget.QueueDestroy ();
				};
			}
		}

		static FloatingQuickFixIconWidget floatingWidget;
		public override bool TryCloseTooltipWindow (Window tipWindow, TooltipCloseReason reason)
		{
			var window = (LanguageItemWindow)tipWindow;
			if (window.Tag is FloatingQuickFixIconWidget iconWidget) {
				if (reason != TooltipCloseReason.Force && iconWidget.IsMouseNear ()) {
					return false;
				}
				iconWidget.QueueDestroy (reason == TooltipCloseReason.Force ? 0u : 500);
			} else {
				window.Destroy ();
			}
			return true;
		}

		#endregion
	}
}
