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

namespace MonoDevelop.SourceEditor
{
	public class OverlayMessageWindow : Gtk.EventBox
	{
		ExtensibleTextEditor textEditor;

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
			var child = (TextEditor.EditorContainerChild)textEditor [this];
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

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			Resize (allocation);
		}

		void HandleSizeAllocated (object o, Gtk.SizeAllocatedArgs args)
		{
			Resize (Allocation);
		}

		void Resize (Gdk.Rectangle alloc)
		{
			textEditor.MoveTopLevelWidget (this, (textEditor.Allocation.Width - alloc.Width) / 2, textEditor.Allocation.Height - alloc.Height - 8);
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var cr = CairoHelper.Create (evnt.Window)) {
				cr.Rectangle (0, 0, Allocation.Width, Allocation.Height);
				cr.SetSourceColor (textEditor.ColorStyle.TooltipText.Background);
				cr.FillPreserve ();

				cr.SetSourceColor (textEditor.ColorStyle.TooltipBorder.Color);
				cr.Stroke();
			}

			return base.OnExposeEvent (evnt);
		}

	}
}

