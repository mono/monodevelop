//
// EditorCompareWidgetBase.DiffScrollbar.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corporation. All rights reserved.
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
using Gtk;
using Gdk;
using Mono.TextEditor;
using Mono.TextEditor.Utils;
using MonoDevelop.Components;

namespace MonoDevelop.VersionControl.Views
{
	public abstract partial class EditorCompareWidgetBase
	{
		class DiffScrollbar : DrawingArea
		{
			MonoTextEditor editor;
			EditorCompareWidgetBase widget;
			bool useLeftDiff;
			bool paintInsert;
			Adjustment vAdjustment;

			public DiffScrollbar (EditorCompareWidgetBase widget, MonoTextEditor editor, bool useLeftDiff, bool paintInsert)
			{
				this.editor = editor;
				this.useLeftDiff = useLeftDiff;
				this.paintInsert = paintInsert;
				this.widget = widget;
				vAdjustment = widget.vAdjustment;
				vAdjustment.ValueChanged += HandleValueChanged;
				WidthRequest = 50;

				Events |= EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.ButtonMotionMask;

				Show ();
			}

			protected override void OnDestroyed ()
			{
				base.OnDestroyed ();
				if (vAdjustment != null) {
					vAdjustment.ValueChanged -= HandleValueChanged;
					vAdjustment = null;
				}
			}

			void HandleValueChanged (object sender, EventArgs e)
			{
				QueueDraw ();
			}

			public void MouseMove (double y)
			{
				var adj = widget.vAdjustment;
				double position = (y / Allocation.Height) * adj.Upper - (double)adj.PageSize / 2;
				position = Math.Max (0, Math.Min (position, adj.Upper - adj.PageSize));
				widget.vAdjustment.Value = position;
			}

			protected override bool OnMotionNotifyEvent (EventMotion evnt)
			{
				if (button != 0)
					MouseMove (evnt.Y);
				return base.OnMotionNotifyEvent (evnt);
			}

			uint button;

			protected override bool OnButtonPressEvent (EventButton evnt)
			{
				button |= evnt.Button;
				MouseMove (evnt.Y);
				return base.OnButtonPressEvent (evnt);
			}

			protected override bool OnButtonReleaseEvent (EventButton evnt)
			{
				button &= ~evnt.Button;
				return base.OnButtonReleaseEvent (evnt);
			}

			protected override bool OnExposeEvent (Gdk.EventExpose e)
			{
				if (widget.LeftDiff == null)
					return true;
				var adj = widget.vAdjustment;

				var diff = useLeftDiff ? widget.LeftDiff : widget.RightDiff;

				using (Cairo.Context cr = Gdk.CairoHelper.Create (e.Window)) {
					cr.LineWidth = 1;
					double curY = 0;

					foreach (var hunk in diff) {
						double y, count;
						if (paintInsert) {
							y = hunk.InsertStart / (double)editor.LineCount;
							count = hunk.Inserted / (double)editor.LineCount;
						} else {
							y = hunk.RemoveStart / (double)editor.LineCount;
							count = hunk.Removed / (double)editor.LineCount;
						}

						double start = y * Allocation.Height;
						FillGradient (cr, 0.5 + curY, start - curY);

						curY = start;
						double height = Math.Max (cr.LineWidth, count * Allocation.Height);
						cr.Rectangle (0.5, 0.5 + curY, Allocation.Width, height);
						cr.SetSourceColor (GetColor (hunk, !paintInsert, false, 1.0));
						cr.Fill ();
						curY += height;
					}

					FillGradient (cr, 0.5 + curY, Allocation.Height - curY);

					int barPadding = 3;
					var allocH = Allocation.Height;
					var adjUpper = adj.Upper;
					var barY = allocH * adj.Value / adjUpper + barPadding;
					var barH = allocH * (adj.PageSize / adjUpper) - barPadding - barPadding;
					DrawBar (cr, barY, barH);

					cr.Rectangle (0.5, 0.5, Allocation.Width - 1, Allocation.Height - 1);
					cr.SetSourceColor ((HslColor)Style.Dark (StateType.Normal));
					cr.Stroke ();
				}
				return true;
			}

			void FillGradient (Cairo.Context cr, double y, double h)
			{
				cr.Rectangle (0.5, y, Allocation.Width, h);

				// FIXME: VV: Remove gradient features
				using (var grad = new Cairo.LinearGradient (0, y, Allocation.Width, y)) {
					var col = (HslColor)Style.Base (StateType.Normal);
					col.L *= 0.95;
					grad.AddColorStop (0, col);
					grad.AddColorStop (0.7, (HslColor)Style.Base (StateType.Normal));
					grad.AddColorStop (1, col);
					cr.SetSource (grad);
					cr.Fill ();
				}
			}

			void DrawBar (Cairo.Context cr, double y, double h)
			{
				int barPadding = 3;
				int barWidth = Allocation.Width - barPadding - barPadding;

				MonoDevelop.Components.CairoExtensions.RoundedRectangle (cr,
					barPadding,
					y,
					barWidth,
					h,
					barWidth / 2);

				var color = Ide.Gui.Styles.BaseBackgroundColor;
				color.Light = 0.5;
				cr.SetSourceColor (color.WithAlpha (0.6).ToCairoColor ());
				cr.Fill ();
			}

			static void IncPos (Hunk h, ref int pos)
			{
				pos += System.Math.Max (h.Inserted, h.Removed);
			}
		}
	}

}

