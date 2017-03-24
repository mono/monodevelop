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
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Components;

namespace Mono.TextEditor
{
	abstract class TooltipProvider
	{
		public abstract Task<MonoDevelop.Ide.Editor.TooltipItem> GetItem (MonoTextEditor editor, int offset, CancellationToken token = default(CancellationToken));

		public virtual bool IsInteractive (MonoTextEditor editor, Xwt.WindowFrame tipWindow)
		{
			return false;
		}

		protected virtual void GetRequiredPosition (MonoTextEditor editor, Xwt.WindowFrame tipWindow, out int requiredWidth, out double xalign)
		{
			requiredWidth = (int)tipWindow.Width;
			xalign = 0.5;
		}

		public virtual Xwt.WindowFrame CreateTooltipWindow (MonoTextEditor editor, int offset, Gdk.ModifierType modifierState, MonoDevelop.Ide.Editor.TooltipItem item)
		{
			return null;
		}

		public virtual Xwt.WindowFrame ShowTooltipWindow (MonoTextEditor editor, Xwt.WindowFrame tipWindow, int offset, Gdk.ModifierType modifierState, int mouseX, int mouseY, MonoDevelop.Ide.Editor.TooltipItem item)
		{
			int w;
			double xalign;
			GetRequiredPosition (editor, tipWindow, out w, out xalign);

			ShowAndPositionTooltip (editor, tipWindow, mouseX, mouseY, w, xalign);

			return tipWindow;
		}

		internal static void ShowAndPositionTooltip (MonoTextEditor editor, Xwt.WindowFrame tipWindow, int mouseX, int mouseY, int width, double xalign)
		{
			int ox = 0, oy = 0;
			if (editor.GdkWindow != null)
				editor.GdkWindow.GetOrigin (out ox, out oy);

			width += 10;

			int x = mouseX + ox + editor.Allocation.X;
			int y = mouseY + oy + editor.Allocation.Y;
			Gdk.Rectangle geometry = editor.Screen.GetUsableMonitorGeometry (editor.Screen.GetMonitorAtPoint (x, y));

			x -= (int)((double)width * xalign);
			y += 10;

			if (x + width >= geometry.X + geometry.Width)
				x = geometry.X + geometry.Width - width;
			if (x < geometry.Left)
				x = geometry.Left;

			int h = (int)tipWindow.Height;
			if (y + h >= geometry.Y + geometry.Height)
				y = geometry.Y + geometry.Height - h;
			if (y < geometry.Top)
				y = geometry.Top;

			tipWindow.Location = new Xwt.Point (x, y);

			tipWindow.Show ();
		}
	}
}
