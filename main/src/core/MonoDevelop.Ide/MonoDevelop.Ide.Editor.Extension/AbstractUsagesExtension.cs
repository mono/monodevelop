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
using System.Collections.Immutable;

namespace MonoDevelop.Ide.Editor.Extension
{
	public abstract class UsageProviderEditorExtension : TextEditorExtension
	{
		public abstract ImmutableArray<Usage> Usages {
			get;
		}

		public event EventHandler UsagesUpdated;

		protected void OnUsagesUpdated (EventArgs e)
		{
			var handler = UsagesUpdated;
			if (handler != null)
				handler (this, e);
		}
	}

	/// <summary>
	/// Provides a base class for implementing highlighting of usages inside the text editor.
	/// </summary>
	public abstract class AbstractUsagesExtension<T> : UsageProviderEditorExtension
	{
		[SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes")]
		protected static readonly List<MemberReference> EmptyList = new List<MemberReference> ();

		CancellationTokenSource tooltipCancelSrc = new CancellationTokenSource ();
		List<ITextSegmentMarker> markers = new List<ITextSegmentMarker> ();

		public IList<ITextSegmentMarker> Markers {
			get {
				return markers;
			}
		}

		uint popupTimer;

		protected override void Initialize ()
		{
			Editor.CaretPositionChanged += HandleTextEditorDataCaretPositionChanged;
			Editor.TextChanged += HandleTextEditorDataDocumentTextReplaced;
			Editor.SelectionChanged += HandleTextEditorDataSelectionChanged;
		}

		void HandleTextEditorDataSelectionChanged (object sender, EventArgs e)
		{
			if (Editor.IsSomethingSelected)
				RemoveMarkers ();
		}

		void HandleTextEditorDataDocumentTextReplaced (object sender, TextChangeEventArgs e)
		{
			RemoveMarkers ();
		}

		public override void Dispose ()
		{
			CancelTooltip ();

			Editor.SelectionChanged -= HandleTextEditorDataSelectionChanged;
			Editor.CaretPositionChanged -= HandleTextEditorDataCaretPositionChanged;
			Editor.TextChanged -= HandleTextEditorDataDocumentTextReplaced;
			RemoveMarkers ();
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
		/// <param name="token">A cancellation token to cancel the operation.</param>
		protected abstract Task<T> ResolveAsync (CancellationToken token);
		

		/// <summary>
		/// Gets all references from a given resolve result. Note that this method is called on a background thread.
		/// </summary>
		/// <returns>The references.</returns>
		/// <param name="resolveResult">The resolve result given in 'TryResolve'.</param>
		/// <param name="token">A cancellation token to cancel the operation.</param>
		protected abstract Task<IEnumerable<MemberReference>> GetReferencesAsync (T resolveResult, CancellationToken token);

		async void DelayedTooltipShow ()
		{
			try {
				CancelTooltip ();
				
				var token = tooltipCancelSrc.Token;

				T result = await ResolveAsync (token);
				if (token.IsCancellationRequested) {
					ClearQuickTasks ();
					return;
				}
				var list = await GetReferencesAsync (result, token);
				if (!token.IsCancellationRequested) {
					Gtk.Application.Invoke (delegate {
						if (!token.IsCancellationRequested)
							ShowReferences (list);
					});
				}

			} catch (OperationCanceledException) {
			} catch (Exception e) {
				LoggingService.LogError ("Unhandled Exception in HighlightingUsagesExtension", e);
			} finally {
				popupTimer = 0;
			}
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
			if (!Editor.IsSomethingSelected && markers.Any (m => m.Contains (Editor.CaretOffset)))
				return;
			RemoveMarkers ();
			RemoveTimer ();
			if (!Editor.IsSomethingSelected)
				popupTimer = GLib.Timeout.Add (250, () => { DelayedTooltipShow (); return false; } );
		}

		void ClearQuickTasks ()
		{
			//UsagesSegments.Clear ();
			if (usages.Length > 0) {
				usages = ImmutableArray<Usage>.Empty;
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
				Editor.RemoveMarker (marker);
			}
			markers.Clear ();
		}

		void ShowReferences (IEnumerable<MemberReference> references)
		{
			RemoveMarkers ();
			var lineNumbers = new HashSet<int> ();
			usages = ImmutableArray<Usage>.Empty;
			var editor = Editor;
			if (editor != null /*&& editor.TextViewMargin != null*/) {
				if (references != null) {
					var builder = ImmutableArray<Usage>.Empty.ToBuilder ();
					foreach (var r in references.OrderBy (r => r.Offset)) {
						if (r == null)
							continue;
						var start = r.Offset;
						var end = r.Offset + r.Length;
						if (end > editor.Length)
							continue;
						var usage = new Usage (TextSegment.FromBounds (start, end), r.ReferenceUsageType);
						builder.Add (usage);
						var marker = TextMarkerFactory.CreateUsageMarker (editor, usage);
						markers.Add (marker);
						lineNumbers.Add (editor.OffsetToLineNumber (start));
						editor.AddMarker (marker);
					}
					usages = builder.ToImmutable ();
				}
			}
			OnUsagesUpdated (EventArgs.Empty);
		}

		#region IUsageProvider implementation

		ImmutableArray<Usage> usages = ImmutableArray<Usage>.Empty;
		public override ImmutableArray<Usage> Usages {
			get {
				return usages;
			}
		}
		#endregion
	}
}

