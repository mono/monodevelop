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
using MonoDevelop.Ide.CodeCompletion;
using System.Threading.Tasks;
using System.Threading;

namespace MonoDevelop.Ide.Editor
{
	public sealed class TooltipItem : ISegment
	{
		int offset;
		int length;

		#region ISegment implementation

		public int Offset {
			get {
				return offset;
			}
			internal set {
				offset = value;
			}
		}

		public int Length {
			get {
				return length;
			}
			internal set {
				length = value;
			}
		}

		public int EndOffset {
			get {
				return offset + length;
			}
		}

		#endregion

		public object Item { get; set; }

		public TooltipItem (object item, ISegment itemSegment) 
		{
			if (itemSegment == null)
				throw new ArgumentNullException ("itemSegment");
			Item = item;
			this.offset = itemSegment.Offset;
			this.length = itemSegment.Length;
		}

		public TooltipItem (object item, int offset, int length)
		{
			Item = item;
			this.offset = offset;
			this.length = length;
		}
	}

	// TODO: Improve tooltip API - that really looks messy
	public abstract class TooltipProvider : IDisposable
	{
		public abstract Task<TooltipItem> GetItem (TextEditor editor, DocumentContext ctx, int offset, CancellationToken token = default(CancellationToken));

		public virtual bool IsInteractive (TextEditor editor, Window tipWindow)
		{
			return false;
		}

		public virtual void GetRequiredPosition (TextEditor editor, Window tipWindow, out int requiredWidth, out double xalign)
		{
			if (tipWindow is XwtWindowControl)
				requiredWidth = (int)tipWindow.GetNativeWidget<Xwt.WindowFrame> ().Width;
			else
				requiredWidth = ((Gtk.Window)tipWindow).SizeRequest ().Width;
			xalign = 0.5;
		}

		public virtual Window CreateTooltipWindow (TextEditor editor, DocumentContext ctx, TooltipItem item, int offset, Xwt.ModifierKeys modifierState)
		{
			return null;
		}

		protected Xwt.Rectangle GetAllocation (TextEditor editor)
		{
			return editor.GetContent<ITextEditorImpl> ().GetEditorAllocation ();
		}

		void ShowTipInfoWindow (TextEditor editor, TooltipInformationWindow tipWindow, TooltipItem item, Xwt.ModifierKeys modifierState, int mouseX, int mouseY)
		{
			Gtk.Widget editorWidget = editor;

			var startLoc = editor.OffsetToLocation (item.Offset);
			var endLoc = editor.OffsetToLocation (item.EndOffset);
			var p1 = editor.LocationToPoint (startLoc);
			var p2 = editor.LocationToPoint (endLoc);

			int w = (int)(p2.X - p1.X);

			var caret = new Gdk.Rectangle (
				(int)p1.X,
				(int)p1.Y,
				(int)w,
				(int)editor.GetLineHeight (startLoc.Line)
			);

			tipWindow.ShowPopup (editorWidget, caret, PopupPosition.Top);
		}

		public virtual void ShowTooltipWindow (TextEditor editor, Window tipWindow, TooltipItem item, Xwt.ModifierKeys modifierState, int mouseX, int mouseY)
		{
			if (tipWindow == null)
				return;

			TooltipInformationWindow tipInfoWindow = (tipWindow as XwtWindowControl)?.Window as TooltipInformationWindow;
			if (tipInfoWindow != null) {
				ShowTipInfoWindow (editor, tipInfoWindow, item, modifierState, mouseX, mouseY);
				return;
			}

			var origin = editor.GetContent<ITextEditorImpl> ().GetEditorWindowOrigin ();

			int w;
			double xalign;
			GetRequiredPosition (editor, tipWindow, out w, out xalign);
			w += 10;

			var allocation = GetAllocation (editor);
			int x = (int)(mouseX + origin.X + allocation.X);
			int y = (int)(mouseY + origin.Y + allocation.Y);
			Gtk.Widget widget = editor;
			var geometry = widget.Screen.GetUsableMonitorGeometry (widget.Screen.GetMonitorAtPoint (x, y));
			
			x -= (int) ((double) w * xalign);
			y += 10;
			
			if (x + w >= geometry.X + geometry.Width)
				x = geometry.X + geometry.Width - w;
			if (x < geometry.Left)
				x = geometry.Left;

			var xwtWindow = (Xwt.WindowFrame)tipWindow;
			int h = (int)xwtWindow.Size.Height;
			if (y + h >= geometry.Y + geometry.Height)
				y = geometry.Y + geometry.Height - h;
			if (y < geometry.Top)
				y = geometry.Top;
			
			xwtWindow.Location = new Xwt.Point(x, y);
			var gtkWindow = Xwt.Toolkit.Load (Xwt.ToolkitType.Gtk).GetNativeWindow (xwtWindow) as Gtk.Window;
			if (gtkWindow != null)
				gtkWindow.ShowAll ();
			else
				xwtWindow.Show ();
		}

		protected bool IsDisposed {
			get;
			private set;
		}

		public virtual void Dispose ()
		{
			IsDisposed = true;
		}
	}
}

