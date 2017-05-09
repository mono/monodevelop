//
// OverlayMessageWindow.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using Mono.TextEditor;
using Gtk;
using MonoDevelop.Components;
using Gdk;
using MonoDevelop.Ide.Editor.Highlighting;

namespace MonoDevelop.SourceEditor
{
	class OverlayMessageWindow : Gtk.EventBox
	{
		const int border = 8;

		private Func<int> sizeFunc;

		ExtensibleTextEditor textEditor;

		public Func<int> SizeFunc {
			get {
				return sizeFunc;
			}

			set {
				sizeFunc = value;
				QueueResize ();
			}
		}

		public OverlayMessageWindow ()
		{
			AppPaintable = true;
		}

		public void ShowOverlay (ExtensibleTextEditor textEditor)
		{
			this.textEditor = textEditor;
			this.ShowAll (); 
			textEditor.AddTopLevelWidget (this, 0, 0); 
			textEditor.SizeAllocated += HandleSizeAllocated;
			var child = (MonoTextEditor.EditorContainerChild)textEditor [this];
			child.FixedPosition = true;
		}

		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			if (textEditor != null) {
				textEditor.SizeAllocated -= HandleSizeAllocated;
				textEditor = null;
			}
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);

			if (SizeFunc != null) {
				requisition.Width = Math.Min (SizeFunc (), textEditor.Allocation.Width - border * 2);
			}

		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			AdjustPositionInEditor (allocation);
		}

		void HandleSizeAllocated (object o, Gtk.SizeAllocatedArgs args)
		{
			AdjustPositionInEditor (Allocation);
		}

		void AdjustPositionInEditor (Gdk.Rectangle alloc)
		{
			textEditor.MoveTopLevelWidget (this, (textEditor.Allocation.Width - alloc.Width) / 2, textEditor.Allocation.Height - alloc.Height - 8);
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var cr = CairoHelper.Create (evnt.Window)) {
				cr.LineWidth = 1;
				cr.Rectangle (0, 0, Allocation.Width, Allocation.Height);

				cr.SetSourceColor (SyntaxHighlightingService.GetColor (textEditor.EditorTheme, EditorThemeColors.NotificationTextBackground));
				cr.Fill ();
				cr.RoundedRectangle (0, 0, Allocation.Width, Allocation.Height, 3);
				cr.SetSourceColor (SyntaxHighlightingService.GetColor (textEditor.EditorTheme, EditorThemeColors.NotificationTextBackground));
				cr.FillPreserve ();

				cr.SetSourceColor (SyntaxHighlightingService.GetColor (textEditor.EditorTheme, EditorThemeColors.NotificationBorder));
				cr.Stroke();
			}

			return base.OnExposeEvent (evnt);
		}

	}
}

