//
// TooltipProvider.cs
//
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc
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
using MonoDevelop.Core.Text;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Editor
{
	public sealed class TooltipItem : AbstractSegment
	{
		public object Item { get; set; }

		public TooltipItem (object item, ISegment itemSegment) : base (itemSegment)
		{
			Item = item;
		}

		public TooltipItem (object item, int offset, int length) : base (offset, length)
		{
			Item = item;
		}
	}

	// TODO: Improve tooltip API - that really looks messy
	public abstract class TooltipProvider
	{
		public abstract TooltipItem GetItem (TextEditor editor, int offset);

		public virtual bool IsInteractive (TextEditor editor, Gtk.Window tipWindow)
		{
			return false;
		}

		public virtual void GetRequiredPosition (TextEditor editor, Gtk.Window tipWindow, out int requiredWidth, out double xalign)
		{
			requiredWidth = tipWindow.SizeRequest ().Width;
			xalign = 0.5;
		}

		public virtual Gtk.Window CreateTooltipWindow (TextEditor editor, int offset, Gdk.ModifierType modifierState, TooltipItem item)
		{
			return null;
		}

		protected Xwt.Rectangle GetAllocation (TextEditor editor)
		{
			return editor.TextEditorImpl.GetEditorAllocation ();
		}

		public virtual Gtk.Window ShowTooltipWindow (TextEditor editor, int offset, Gdk.ModifierType modifierState, int mouseX, int mouseY, TooltipItem item)
		{
			var tipWindow = CreateTooltipWindow (editor, offset, modifierState, item);
			if (tipWindow == null)
				return null;


			var origin = editor.TextEditorImpl.GetEditorWindowOrigin ();

			int w;
			double xalign;
			GetRequiredPosition (editor, tipWindow, out w, out xalign);
			w += 10;

			var allocation = GetAllocation (editor);
			int x = (int)(mouseX + origin.X + allocation.X);
			int y = (int)(mouseY + origin.Y + allocation.Y);
			var widget = editor.GetGtkWidget ();
			var geometry = widget.Screen.GetUsableMonitorGeometry (widget.Screen.GetMonitorAtPoint (x, y));
			
			x -= (int) ((double) w * xalign);
			y += 10;
			
			if (x + w >= geometry.X + geometry.Width)
				x = geometry.X + geometry.Width - w;
			if (x < geometry.Left)
				x = geometry.Left;
			
			int h = tipWindow.SizeRequest ().Height;
			if (y + h >= geometry.Y + geometry.Height)
				y = geometry.Y + geometry.Height - h;
			if (y < geometry.Top)
				y = geometry.Top;
			
			tipWindow.Move (x, y);
			
			tipWindow.ShowAll ();

			return tipWindow;
		}
	}
}

