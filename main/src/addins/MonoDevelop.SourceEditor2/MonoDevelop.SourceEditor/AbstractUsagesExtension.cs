//
// AbstractUsagesExtension.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using Mono.TextEditor;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.SourceEditor.QuickTasks;
using MonoDevelop.Components;
using Cairo;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;

namespace MonoDevelop.SourceEditor
{
	/// <summary>
	/// Provides a base class for implementing highlighting of usages inside the text editor.
	/// </summary>
	public abstract class AbstractUsagesExtension : TextEditorExtension, IUsageProvider
	{
		public class UsageSegment
		{
			public readonly ReferenceUsageType UsageType;
			public readonly TextSegment TextSegment;

			public UsageSegment (ReferenceUsageType usageType, int offset, int length)
			{
				UsageType = usageType;
				TextSegment = new TextSegment (offset, length);
			}

			public static implicit operator TextSegment (UsageSegment usage)
			{
				return usage.TextSegment;
			}
		}

		public readonly List<UsageSegment> UsagesSegments = new List<UsageSegment> ();

		protected TextEditorData textEditorData;

		public override void Initialize ()
		{
			base.Initialize ();

			textEditorData = Document.Editor;
			textEditorData.Caret.PositionChanged += HandleTextEditorDataCaretPositionChanged;
			textEditorData.Document.TextReplaced += HandleTextEditorDataDocumentTextReplaced;
			textEditorData.SelectionChanged += HandleTextEditorDataSelectionChanged;
		}

		void HandleTextEditorDataSelectionChanged (object sender, EventArgs e)
		{
			RemoveMarkers (false);
		}

		void HandleTextEditorDataDocumentTextReplaced (object sender, DocumentChangeEventArgs e)
		{
			RemoveMarkers (false);
		}

		public override void Dispose ()
		{
			CancelTooltip ();

			textEditorData.SelectionChanged -= HandleTextEditorDataSelectionChanged;
			textEditorData.Caret.PositionChanged -= HandleTextEditorDataCaretPositionChanged;
			textEditorData.Document.TextReplaced -= HandleTextEditorDataDocumentTextReplaced;
			base.Dispose ();
			RemoveTimer ();
		}

		uint popupTimer;

		public bool IsTimerOnQueue {
			get {
				return popupTimer != 0;
			}
		}

		void ResetPopupTimer ()
		{
			popupTimer = 0;
		}

		public void ForceUpdate ()
		{
			RemoveTimer ();
			DelayedTooltipShow ();
		}

		void RemoveTimer ()
		{
			if (popupTimer != 0) {
				GLib.Source.Remove (popupTimer);
				popupTimer = 0;
			}
		}

		void HandleTextEditorDataCaretPositionChanged (object sender, DocumentLocationEventArgs e)
		{
			if (!DefaultSourceEditorOptions.Instance.EnableHighlightUsages)
				return;
			if (!textEditorData.IsSomethingSelected && markers.Values.Any (m => m.Contains (textEditorData.Caret.Offset)))
				return;
			RemoveMarkers (textEditorData.IsSomethingSelected);
			RemoveTimer ();
			if (!textEditorData.IsSomethingSelected)
				popupTimer = GLib.Timeout.Add (1000, DelayedTooltipShow);
		}

		void ClearQuickTasks ()
		{
			UsagesSegments.Clear ();
			if (usages.Count > 0) {
				usages.Clear ();
				OnUsagesUpdated (EventArgs.Empty);
			}
		}

		CancellationTokenSource tooltipCancelSrc = new CancellationTokenSource ();

		void CancelTooltip ()
		{
			tooltipCancelSrc.Cancel ();
			tooltipCancelSrc = new CancellationTokenSource ();
		}

		#region implementor API
		protected static readonly List<MemberReference> EmptyList = new List<MemberReference> ();

		/// <summary>
		/// Tries to resolve inside the current location inside tho document.
		/// </summary>
		/// <returns><c>true</c>, if resolve was successful, <c>false</c> otherwise.</returns>
		/// <param name="resolveResult">The resolve result.</param>
		protected abstract bool TryResolve(out object resolveResult);

		/// <summary>
		/// Gets all references from a given resolve result. Note that this method is called on a background thread.
		/// </summary>
		/// <returns>The references.</returns>
		/// <param name="resolveResult">The resolve result given in 'TryResolve'.</param>
		/// <param name="token">A cancellation token to cancel the operation.</param>
		protected abstract IEnumerable<MemberReference> GetReferences (object resolveResult, CancellationToken token);

		#endregion

		bool DelayedTooltipShow ()
		{
			try {
				object result;

				if (!TryResolve(out result)) {
					ClearQuickTasks ();
					return false;
				}

				CancelTooltip ();
				var token = tooltipCancelSrc.Token;
				Task.Factory.StartNew (delegate {
					var list = GetReferences (result, token).ToList ();
					if (!token.IsCancellationRequested) {
						Gtk.Application.Invoke (delegate {
							if (!token.IsCancellationRequested)
								ShowReferences (list);
						});
					}
				});

			} catch (Exception e) {
				LoggingService.LogError ("Unhandled Exception in HighlightingUsagesExtension", e);
			} finally {
				ResetPopupTimer ();
			}
			return false;
		}

		void ShowReferences (IEnumerable<MemberReference> references)
		{
			RemoveMarkers (false);
			var lineNumbers = new HashSet<int> ();
			usages.Clear ();
			UsagesSegments.Clear ();
			var editor = textEditorData.Parent;
			if (editor != null && editor.TextViewMargin != null) {
				if (references != null) {
					bool alphaBlend = false;
					foreach (var r in references) {
						if (r == null)
							continue;
						var marker = GetMarker (r.Region.BeginLine);

						usages.Add (new Usage (r.Region.Begin, r.ReferenceUsageType));

						int offset = r.Offset;
						int endOffset = offset + r.Length;
						if (!alphaBlend && editor.TextViewMargin.SearchResults.Any (sr => sr.Contains (offset) || sr.Contains (endOffset) ||
							offset < sr.Offset && sr.EndOffset < endOffset)) {
							editor.TextViewMargin.AlphaBlendSearchResults = alphaBlend = true;
						}
						UsagesSegments.Add (new UsageSegment (r.ReferenceUsageType, offset, endOffset - offset));
						marker.Usages.Add (new UsageSegment (r.ReferenceUsageType, offset, endOffset - offset));
						lineNumbers.Add (r.Region.BeginLine);
					}
				}
				foreach (int line in lineNumbers)
					textEditorData.Document.CommitLineUpdate (line);
				UsagesSegments.Sort ((x, y) => x.TextSegment.Offset.CompareTo (y.TextSegment.Offset));
			}
			OnUsagesUpdated (EventArgs.Empty);
		}

		Dictionary<int, UsageMarker> markers = new Dictionary<int, UsageMarker> ();

		public Dictionary<int, UsageMarker> Markers {
			get { return markers; }
		}

		void RemoveMarkers (bool updateLine)
		{
			if (markers.Count == 0)
				return;
			textEditorData.Parent.TextViewMargin.AlphaBlendSearchResults = false;
			foreach (var pair in markers) {
				textEditorData.Document.RemoveMarker (pair.Value, true);
			}
			markers.Clear ();
		}

		UsageMarker GetMarker (int line)
		{
			UsageMarker result;
			if (!markers.TryGetValue (line, out result)) {
				result = new UsageMarker ();
				textEditorData.Document.AddMarker (line, result);
				markers.Add (line, result);
			}
			return result;
		}

		public class UsageMarker : TextLineMarker
		{
			List<UsageSegment> usages = new List<UsageSegment> ();

			public List<UsageSegment> Usages {
				get { return usages; }
			}

			public bool Contains (int offset)
			{
				return usages.Any (u => u.TextSegment.Offset <= offset && offset <= u.TextSegment.EndOffset);
			}

			public override bool DrawBackground (TextEditor editor, Context cr, double y, LineMetrics metrics)
			{
				if (metrics.SelectionStart >= 0 || editor.CurrentMode is TextLinkEditMode || editor.TextViewMargin.SearchResultMatchCount > 0)
					return false;
				foreach (var usage in Usages) {
					int markerStart = usage.TextSegment.Offset;
					int markerEnd = usage.TextSegment.EndOffset;

					if (markerEnd < metrics.TextStartOffset || markerStart > metrics.TextEndOffset) 
						return false; 

					double @from;
					double to;

					if (markerStart < metrics.TextStartOffset && metrics.TextEndOffset < markerEnd) {
						@from = metrics.TextRenderStartPosition;
						to = metrics.TextRenderEndPosition;
					} else {
						int start = metrics.TextStartOffset < markerStart ? markerStart : metrics.TextStartOffset;
						int end = metrics.TextEndOffset < markerEnd ? metrics.TextEndOffset : markerEnd;

						uint curIndex = 0, byteIndex = 0;
						TextViewMargin.TranslateToUTF8Index (metrics.Layout.LineChars, (uint)(start - metrics.TextStartOffset), ref curIndex, ref byteIndex);

						int x_pos = metrics.Layout.Layout.IndexToPos ((int)byteIndex).X;

						@from = metrics.TextRenderStartPosition + (int)(x_pos / Pango.Scale.PangoScale);

						TextViewMargin.TranslateToUTF8Index (metrics.Layout.LineChars, (uint)(end - metrics.TextStartOffset), ref curIndex, ref byteIndex);
						x_pos = metrics.Layout.Layout.IndexToPos ((int)byteIndex).X;

						to = metrics.TextRenderStartPosition + (int)(x_pos / Pango.Scale.PangoScale);
					}

					@from = Math.Max (@from, editor.TextViewMargin.XOffset);
					to = Math.Max (to, editor.TextViewMargin.XOffset);
					if (@from < to) {
						Mono.TextEditor.Highlighting.AmbientColor colorStyle;
						if ((usage.UsageType & ReferenceUsageType.Write) == ReferenceUsageType.Write) {
							colorStyle = editor.ColorStyle.ChangingUsagesRectangle;
						} else {
							colorStyle = editor.ColorStyle.UsagesRectangle;
						}

						using (var lg = new LinearGradient (@from + 1, y + 1, to , y + editor.LineHeight)) {
							lg.AddColorStop (0, colorStyle.Color);
							lg.AddColorStop (1, colorStyle.SecondColor);
							cr.SetSource (lg);
							cr.RoundedRectangle (@from + 0.5, y + 1.5, to - @from - 1, editor.LineHeight - 2, editor.LineHeight / 4);
							cr.FillPreserve ();
						}

						cr.SetSourceColor (colorStyle.BorderColor);
						cr.Stroke ();
					}
				}
				return true;
			}
		}

		#region IUsageProvider implementation
		public event EventHandler UsagesUpdated;

		void OnUsagesUpdated (EventArgs e)
		{
			var handler = UsagesUpdated;
			if (handler != null)
				handler (this, e);
		}

		readonly List<Usage> usages = new List<Usage> ();
		IEnumerable<Usage> IUsageProvider.Usages {
			get {
				return usages;
			}
		}
		#endregion
	}
}