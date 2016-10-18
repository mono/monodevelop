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

namespace MonoDevelop.AnalysisCore.Gui
{
	class ResultTooltipProvider : TooltipProvider
	{
		#region ITooltipProvider implementation 
		public override Task<TooltipItem> GetItem (TextEditor editor, DocumentContext ctx, int offset, CancellationToken token = default (CancellationToken))
		{
			var results = new List<Result> ();
			int markerOffset = -1, markerEndOffset = -1;
			foreach (var marker in editor.GetTextSegmentMarkersAt (offset)) {
				var result = marker.Tag as Result;
				if (result != null) {
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
			if (results != null)
				return Task.FromResult (new TooltipItem (results, markerOffset, markerEndOffset - markerOffset));

			return Task.FromResult<TooltipItem> (null);

		}

		public override Control CreateTooltipWindow (TextEditor editor, DocumentContext ctx, TooltipItem item, int offset, Xwt.ModifierKeys modifierState)
		{
			var result = item.Item as List<Result>;

			var sb = new StringBuilder ();
			foreach (var r in result) {
				var escapedMessage = Ambience.EscapeText (r.Message);
				if (result.Count > 0) {
					string severity;
					HslColor color;
					switch (r.Level) {
					case Microsoft.CodeAnalysis.DiagnosticSeverity.Info:
						severity = GettextCatalog.GetString ("Info");
						color = editor.Options.GetColorStyle ().UnderlineHint.Color;
						break;
					case Microsoft.CodeAnalysis.DiagnosticSeverity.Warning:
						severity = GettextCatalog.GetString ("Warning");
						color = editor.Options.GetColorStyle ().UnderlineWarning.Color;
						break;
					case Microsoft.CodeAnalysis.DiagnosticSeverity.Error:
						severity = GettextCatalog.GetString ("Error");
						color = editor.Options.GetColorStyle ().UnderlineError.Color;
						break;
					default:
						severity = "?";
						color = editor.Options.GetColorStyle ().UnderlineSuggestion.Color;
						break;
					}

					sb.AppendLine (string.Format ("<span foreground ='{2}'font_weight='bold'>{0}</span>: {1}", severity, escapedMessage, color.ToPangoString ()));
				} else {
					sb.AppendLine (escapedMessage);
				}
			}
			var window = new LanguageItemWindow (CompileErrorTooltipProvider.GetExtensibleTextEditor (editor), modifierState, null, sb.ToString (), null);
			if (window.IsEmpty)
				return null;
			return window;
		}

		public override void GetRequiredPosition (TextEditor editor, Control tipWindow, out int requiredWidth, out double xalign)
		{
			var win = (LanguageItemWindow) tipWindow;
			requiredWidth = win.SetMaxWidth (win.Screen.Width / 4);
			xalign = 0.5;
		}
		#endregion


	}
}
