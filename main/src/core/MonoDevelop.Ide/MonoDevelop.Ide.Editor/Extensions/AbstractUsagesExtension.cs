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
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Ide.FindInFiles;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using System.Diagnostics.CodeAnalysis;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Core.Text;

namespace MonoDevelop.Ide.Editor
{
	/// <summary>
	/// Provides a base class for implementing highlighting of usages inside the text editor.
	/// </summary>
	public abstract class AbstractUsagesExtension<T> : TextEditorExtension, IUsageProvider
	{
		[SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes")]
		protected static readonly List<MemberReference> EmptyList = new List<MemberReference> ();

		protected TextEditor TextEditorData;

		CancellationTokenSource tooltipCancelSrc = new CancellationTokenSource ();
		List<ITextSegmentMarker> markers = new List<ITextSegmentMarker> ();
		uint popupTimer;

		public override void Initialize ()
		{
			base.Initialize ();

			TextEditorData = Document.Editor;
			TextEditorData.CaretPositionChanged += HandleTextEditorDataCaretPositionChanged;
			TextEditorData.TextChanged += HandleTextEditorDataDocumentTextReplaced;
			TextEditorData.SelectionChanged += HandleTextEditorDataSelectionChanged;
		}

		void HandleTextEditorDataSelectionChanged (object sender, EventArgs e)
		{
			RemoveMarkers ();
		}

		void HandleTextEditorDataDocumentTextReplaced (object sender, TextChangeEventArgs e)
		{
			RemoveMarkers ();
		}

		public override void Dispose ()
		{
			CancelTooltip ();

			TextEditorData.SelectionChanged -= HandleTextEditorDataSelectionChanged;
			TextEditorData.CaretPositionChanged -= HandleTextEditorDataCaretPositionChanged;
			TextEditorData.TextChanged -= HandleTextEditorDataDocumentTextReplaced;
			base.Dispose ();
			RemoveTimer ();
		}

		public bool IsTimerOnQueue {
			get {
				return popupTimer != 0;
			}
		}

		public void ForceUpdate ()
		{
			RemoveTimer ();
			DelayedTooltipShow ();
		}

		/// <summary>
		/// Tries to resolve inside the current location inside tho document.
		/// </summary>
		/// <returns><c>true</c>, if resolve was successful, <c>false</c> otherwise.</returns>
		/// <param name="resolveResult">The resolve result.</param>
		protected abstract bool TryResolve(out T resolveResult);

		/// <summary>
		/// Gets all references from a given resolve result. Note that this method is called on a background thread.
		/// </summary>
		/// <returns>The references.</returns>
		/// <param name="resolveResult">The resolve result given in 'TryResolve'.</param>
		/// <param name="token">A cancellation token to cancel the operation.</param>
		protected abstract IEnumerable<MemberReference> GetReferences (T resolveResult, CancellationToken token);

		bool DelayedTooltipShow ()
		{
			try {
				T result;

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
				popupTimer = 0;
			}
			return false;
		}

		void RemoveTimer ()
		{
			if (popupTimer != 0) {
				GLib.Source.Remove (popupTimer);
				popupTimer = 0;
			}
		}

		void HandleTextEditorDataCaretPositionChanged (object sender, EventArgs e)
		{
			if (!DefaultSourceEditorOptions.Instance.EnableHighlightUsages)
				return;
			if (!TextEditorData.IsSomethingSelected && markers.Any (m => m.Contains (TextEditorData.CaretOffset)))
				return;
			RemoveMarkers ();
			RemoveTimer ();
			if (!TextEditorData.IsSomethingSelected)
				popupTimer = GLib.Timeout.Add (1000, DelayedTooltipShow);
		}

		void ClearQuickTasks ()
		{
			//UsagesSegments.Clear ();
			if (usages.Count > 0) {
				usages.Clear ();
				OnUsagesUpdated (EventArgs.Empty);
			}
		}

		void CancelTooltip ()
		{
			tooltipCancelSrc.Cancel ();
			tooltipCancelSrc = new CancellationTokenSource ();
		}

		void RemoveMarkers ()
		{
			if (markers.Count == 0)
				return;
			// TextEditorData.Parent.TextViewMargin.AlphaBlendSearchResults = false;
			foreach (var marker in markers) {
				TextEditorData.RemoveMarker (marker);
			}
			markers.Clear ();
		}

		void ShowReferences (IEnumerable<MemberReference> references)
		{
			RemoveMarkers ();
			var lineNumbers = new HashSet<int> ();
			usages.Clear ();
			var editor = TextEditorData;
			if (editor != null /*&& editor.TextViewMargin != null*/) {
				if (references != null) {
					bool alphaBlend = false;
					foreach (var r in references) {
						if (r == null)
							continue;
						var start = TextEditorData.LocationToOffset (r.Region.BeginLine, r.Region.BeginColumn);
						var end   = TextEditorData.LocationToOffset (r.Region.EndLine, r.Region.EndColumn);
						var usage = new Usage (TextSegment.FromBounds (start, end), r.ReferenceUsageType);
						usages.Add (usage);
						var marker = TextEditorData.SegmentMarkerHost.CreateUsageMarker (usage);
						markers.Add (marker);
						lineNumbers.Add (r.Region.BeginLine);
						TextEditorData.AddMarker (marker);
					}
				}
				/*	foreach (int line in lineNumbers)
					TextEditorData.CommitLineUpdate (line);
				UsagesSegments.Sort ((x, y) => x.TextSegment.Offset.CompareTo (y.TextSegment.Offset));*/
			}
			OnUsagesUpdated (EventArgs.Empty);
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

