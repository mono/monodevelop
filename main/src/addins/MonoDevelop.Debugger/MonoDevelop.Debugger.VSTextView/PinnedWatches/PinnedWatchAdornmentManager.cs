//
// PinnedWatchAdornmentManager.cs
//
// Author:
//       Jeffrey Stedfast <jestedfa@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corp.
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

using AppKit;
using CoreGraphics;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Adornments;

using MonoDevelop.Core;

namespace MonoDevelop.Debugger.VSTextView.PinnedWatches
{
	sealed class PinnedWatchAdornmentManager : IDisposable
	{
		readonly Dictionary<PinnedWatch, NSView> adornments = new Dictionary<PinnedWatch, NSView> ();
		readonly ICocoaViewFactory cocoaViewFactory;
		readonly IXPlatAdornmentLayer layer;
		readonly ICocoaTextView textView;
		readonly string path;
		bool debugging;

		public PinnedWatchAdornmentManager (ICocoaViewFactory cocoaViewFactory, ICocoaTextView textView)
		{
			path = textView.TextDataModel.DocumentBuffer.GetFilePathOrNull ();

			if (path == null)
				return;

			DebuggingService.PinnedWatches.WatchAdded += OnWatchAdded;
			DebuggingService.PinnedWatches.WatchChanged += OnWatchChanged;
			DebuggingService.PinnedWatches.WatchRemoved += OnWatchRemoved;
			DebuggingService.DebugSessionStarted += OnDebugSessionStarted;
			DebuggingService.StoppedEvent += OnDebuggingSessionStopped;
			DebuggingService.VariableChanged += OnVariableChanged;

			this.layer = textView.GetXPlatAdornmentLayer ("PinnedWatch");
			this.cocoaViewFactory = cocoaViewFactory;
			this.textView = textView;

			this.textView.LayoutChanged += OnTextViewLayoutChanged;

			if (DebuggingService.IsDebugging) {
				RenderAllAdornments ();
				debugging = true;
			}
		}

		void OnWatchAdded (object sender, PinnedWatchEventArgs e)
		{
			if (!debugging || e.Watch.File != path)
				return;

			RenderAdornment (e.Watch);
		}

		void OnWatchChanged (object sender, PinnedWatchEventArgs e)
		{
			if (!debugging || e.Watch.File != path)
				return;

			if (!adornments.TryGetValue (e.Watch, out var adornment))
				return;

			var view = (PinnedWatchView) ((ICocoaMaterialView) adornment).ContentView;

			view.SetObjectValue (e.Watch.Value);
		}

		void OnWatchRemoved (object sender, PinnedWatchEventArgs e)
		{
			if (!debugging || e.Watch.File != path)
				return;

			layer.RemoveAdornmentsByTag (e.Watch);
			adornments.Remove (e.Watch);
		}

		private void OnVariableChanged (object sender, EventArgs e)
		{
			if (!debugging)
				return;

			foreach (var watch in adornments) {
				var view = (PinnedWatchView)((ICocoaMaterialView)watch.Value).ContentView;
				view.Refresh ();
			}
		}

		SnapshotSpan GetSnapshotSpan (PinnedWatch watch)
		{
			var newSpan = textView.TextSnapshot.SpanFromMDColumnAndLine (watch.Line, watch.Column, watch.EndLine, watch.EndColumn);
			var trackingSpan = textView.TextSnapshot.CreateTrackingSpan (newSpan, SpanTrackingMode.EdgeInclusive);
			return trackingSpan.GetSpan (textView.TextSnapshot);
		}

		void RenderAdornment (PinnedWatch watch)
		{
			var span = GetSnapshotSpan (watch);

			if (textView.TextViewLines == null)
				return;

			if (!textView.TextViewLines.FormattedSpan.Contains (span.End))
				return;

			var pinnedWatchView = new PinnedWatchView (watch, DebuggingService.CurrentFrame);
			var materialView = cocoaViewFactory.CreateMaterialView ();
			materialView.Material = NSVisualEffectMaterial.WindowBackground;
			materialView.ContentView = pinnedWatchView;
			materialView.CornerRadius = 3;

			var view = (NSView) materialView;
			view.WantsLayer = true;

			UpdateAdornmentLayout (watch, view, span);

			adornments [watch] = view;
		}

		void UpdateAdornmentLayout (PinnedWatch watch, NSView view, SnapshotSpan span)
		{
			try {
				if (!textView.TextViewLines.IntersectsBufferSpan (span)) {
					layer.RemoveAdornment (view);
					return;
				}

				var charBound = textView.TextViewLines.GetCharacterBounds (span.End);
				var origin = new CGPoint (
					Math.Round (charBound.Left),
					Math.Round (charBound.TextTop + charBound.TextHeight / 2 - view.Frame.Height / 2));
				view.SetFrameOrigin (origin);

				if (view.Superview == null || view.VisibleRect () == CGRect.Empty) {
					layer.RemoveAdornment (view);
					layer.AddAdornment (XPlatAdornmentPositioningBehavior.TextRelative, span, watch, view, null);
				}
			} catch (Exception ex) {
				view.SetFrameOrigin (default);
				LoggingService.LogInternalError ("https://vsmac.dev/923058", ex);
			}
		}

		void RenderAllAdornments ()
		{
			foreach (var watch in DebuggingService.PinnedWatches.GetWatchesForFile (path))
				RenderAdornment (watch);
		}

		void OnDebugSessionStarted (object sender, EventArgs e)
		{
			if (debugging || !DebuggingService.IsDebugging)
				return;

			RenderAllAdornments ();
			debugging = true;
		}

		void OnDebuggingSessionStopped (object sender, EventArgs e)
		{
			if (DebuggingService.IsDebugging)
				return;

			layer.RemoveAllAdornments ();
			adornments.Clear ();
			debugging = false;
		}

		void OnTextViewLayoutChanged (object sender, TextViewLayoutChangedEventArgs e)
		{
			if (!DebuggingService.IsDebugging)
				return;

			foreach (var adornmentPair in adornments)
				UpdateAdornmentLayout (adornmentPair.Key, adornmentPair.Value, GetSnapshotSpan(adornmentPair.Key));
		}

		public void Dispose ()
		{
			if (path == null)
				return;

			DebuggingService.PinnedWatches.WatchAdded -= OnWatchAdded;
			DebuggingService.PinnedWatches.WatchChanged -= OnWatchChanged;
			DebuggingService.PinnedWatches.WatchRemoved -= OnWatchRemoved;
			DebuggingService.DebugSessionStarted -= OnDebugSessionStarted;
			DebuggingService.StoppedEvent -= OnDebuggingSessionStopped;
			DebuggingService.VariableChanged -= OnVariableChanged;

			textView.LayoutChanged -= OnTextViewLayoutChanged;
		}
	}
}
