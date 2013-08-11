//
// ViStatusArea.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//       Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (c) 2013 Xamarin Inc.
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

namespace Mono.TextEditor.Vi
{
	class ViStatusArea : Gtk.DrawingArea
	{
		TextEditor editor;
		bool showCaret;
		string statusText;

		public ViStatusArea (TextEditor editor)
		{
			this.editor = editor;
			editor.TextViewMargin.CaretBlink += HandleCaretBlink;
			editor.Caret.PositionChanged += HandlePositionChanged;

			editor.AddTopLevelWidget (this, 0, 0);
			((TextEditor.EditorContainerChild)editor[this]).FixedPosition = true;
			Show ();
		}

		void HandlePositionChanged (object sender, DocumentLocationEventArgs e)
		{
			QueueDraw ();
		}

		void HandleCaretBlink (object sender, EventArgs e)
		{
			QueueDraw ();
		}

		public void RemoveFromParentAndDestroy ()
		{
			editor.Remove (this);
			Destroy ();
		}

		protected override void OnDestroyed ()
		{
			editor.Caret.PositionChanged -= HandlePositionChanged;
			editor.TextViewMargin.CaretBlink -= HandleCaretBlink;
			base.OnDestroyed ();
		}

		public void AllocateArea (TextArea textArea, Gdk.Rectangle allocation)
		{
			if (!Visible)
				Show ();
			allocation.Height -= (int)textArea.LineHeight;
			if (textArea.Allocation != allocation)
				textArea.SizeAllocate (allocation);
			SetSizeRequest (allocation.Width, (int)editor.LineHeight);
			editor.MoveTopLevelWidget (this, 0, allocation.Height);
		}

		public bool ShowCaret {
			get { return showCaret; }
			set {
				if (showCaret != value) {
					showCaret = value;
					editor.Caret.IsVisible = !showCaret;
					editor.RequestResetCaretBlink ();
					QueueDraw ();
				}
			}
		}

		public string Message {
			get { return statusText; }
			set {
				if (statusText == value)
					return;
				statusText = value;
				if (showCaret) {
					editor.RequestResetCaretBlink ();
				}
				QueueDraw ();
			}
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window)) {
				cr.Rectangle (evnt.Region.Clipbox.X, evnt.Region.Clipbox.Y, evnt.Region.Clipbox.Width, evnt.Region.Clipbox.Height);
				cr.SetSourceColor (editor.ColorStyle.PlainText.Background);
				cr.Fill ();
				using (var layout = PangoUtil.CreateLayout (editor)) {
					layout.FontDescription = editor.Options.Font;

					layout.SetText ("000,00-00");
					int minstatusw, minstatush;
					layout.GetPixelSize (out minstatusw, out minstatush);

					var line = editor.GetLine (editor.Caret.Line);
					var visColumn = line.GetVisualColumn (editor.GetTextEditorData (), editor.Caret.Column);

					if (visColumn != editor.Caret.Column) {
						layout.SetText (editor.Caret.Line + "," + editor.Caret.Column + "-" + visColumn);
					} else {
						layout.SetText (editor.Caret.Line + "," + editor.Caret.Column);
					}

					int statusw, statush;
					layout.GetPixelSize (out statusw, out statush);

					statusw = System.Math.Max (statusw, minstatusw);

					statusw += 8;
					cr.MoveTo (Allocation.Width - statusw, 0);
					statusw += 8;
					cr.SetSourceColor (editor.ColorStyle.PlainText.Foreground);
					cr.ShowLayout (layout);

					layout.SetText (statusText ?? "");
					int w, h;
					layout.GetPixelSize (out w, out h);
					var x = System.Math.Min (0, -w + Allocation.Width - editor.TextViewMargin.CharWidth - statusw);
					cr.MoveTo (x, 0);
					cr.SetSourceColor (editor.ColorStyle.PlainText.Foreground);
					cr.ShowLayout (layout);
					if (ShowCaret) {
						if (editor.TextViewMargin.caretBlink) {
							cr.Rectangle (w + x, 0, (int)editor.TextViewMargin.CharWidth, (int)editor.LineHeight);
							cr.Fill ();
						}
					}
				}
			}
			return true;
		}
	}
}
