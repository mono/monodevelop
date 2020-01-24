//
// EditorCompareWidgetBase.MiddleArea.cs
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
using System.Linq;
using Gtk;
using Gdk;
using System.Collections.Generic;
using Mono.TextEditor;
using Mono.TextEditor.Utils;
using MonoDevelop.Core;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;

namespace MonoDevelop.VersionControl.Views
{
	public abstract partial class EditorCompareWidgetBase
	{
		class MiddleArea : DrawingArea
		{
			EditorCompareWidgetBase widget;
			MonoTextEditor fromEditor, toEditor;
			bool useLeft;

			public Adjustment VAdjustment { get { return fromEditor.VAdjustment; } }

			IEnumerable<Hunk> Diff {
				get {
					return useLeft ? widget.LeftDiff : widget.RightDiff;
				}
			}

			public MiddleArea (EditorCompareWidgetBase widget, MonoTextEditor fromEditor, MonoTextEditor toEditor, bool useLeft)
			{
				this.widget = widget;
				this.Events |= EventMask.PointerMotionMask | EventMask.ButtonPressMask;
				this.fromEditor = fromEditor;
				this.toEditor = toEditor;
				this.useLeft = useLeft;
				this.toEditor.EditorOptionsChanged += HandleToEditorhandleEditorOptionsChanged;
				this.widget.DiffChanged += Widget_DiffChanged;
				this.fromEditor.VAdjustment.ValueChanged += VAdjustment_Changed;
				this.toEditor.VAdjustment.ValueChanged += VAdjustment_Changed;
				Accessible.SetRole (AtkCocoa.Roles.AXGroup);
				Accessible.SetTitle (GettextCatalog.GetString ("Revert changes margin"));
			}

			void VAdjustment_Changed (object sender, EventArgs e)
			{
				UpdateAccessiblity ();
			}

			void Widget_DiffChanged (object sender, EventArgs e)
			{
				UpdateAccessiblity ();
			}

			protected override void OnDestroyed ()
			{
				this.widget.DiffChanged -= Widget_DiffChanged;
				this.fromEditor.VAdjustment.ValueChanged -= VAdjustment_Changed;
				this.toEditor.VAdjustment.ValueChanged -= VAdjustment_Changed;
				this.toEditor.EditorOptionsChanged -= HandleToEditorhandleEditorOptionsChanged;
				this.ClearAccessibleButtons ();
				base.OnDestroyed ();
			}

			void HandleToEditorhandleEditorOptionsChanged (object sender, EventArgs e)
			{
				QueueDraw ();
			}

			Hunk selectedHunk = Hunk.Empty;
			protected override bool OnMotionNotifyEvent (EventMotion evnt)
			{
				bool hideButton = widget.MainEditor.Document.IsReadOnly;
				Hunk selectedHunk = Hunk.Empty;
				if (!hideButton) {
					int delta = widget.MainEditor.Allocation.Y - Allocation.Y;
					if (Diff != null) {
						foreach (var hunk in Diff) {
							double z1 = delta + fromEditor.LineToY (hunk.RemoveStart) - fromEditor.VAdjustment.Value;
							double z2 = delta + fromEditor.LineToY (hunk.RemoveStart + hunk.Removed) - fromEditor.VAdjustment.Value;
							if (z1 == z2)
								z2 = z1 + 1;

							double y1 = delta + toEditor.LineToY (hunk.InsertStart) - toEditor.VAdjustment.Value;
							double y2 = delta + toEditor.LineToY (hunk.InsertStart + hunk.Inserted) - toEditor.VAdjustment.Value;

							if (y1 == y2)
								y2 = y1 + 1;
							double x, y, w, h;
							GetButtonPosition (hunk, y1, y2, z1, z2, out x, out y, out w, out h);

							if (evnt.X >= x && evnt.X < x + w && evnt.Y >= y && evnt.Y < y + h) {
								selectedHunk = hunk;
								TooltipText = GettextCatalog.GetString ("Revert this change");
								QueueDrawArea ((int)x, (int)y, (int)w, (int)h);
								break;
							}
						}
					}
				} else {
					selectedHunk = Hunk.Empty;
				}

				if (selectedHunk.IsEmpty)
					TooltipText = null;

				if (this.selectedHunk != selectedHunk) {
					this.selectedHunk = selectedHunk;
					QueueDraw ();
				}
				return base.OnMotionNotifyEvent (evnt);
			}

			protected override bool OnButtonPressEvent (EventButton evnt)
			{
				if (!evnt.TriggersContextMenu () && evnt.Button == 1 && !selectedHunk.IsEmpty) {
					PerformRevert (selectedHunk);
					return true;
				}
				return base.OnButtonPressEvent (evnt);
			}

			void PerformRevert (Hunk hunk)
			{
				try {
					var selectedBounds = GetFocusedButton ()?.Bounds;

					widget.UndoChange (fromEditor, toEditor, hunk);
					UpdateAccessiblity ();

					if (selectedBounds != null) {
						var nearestButton = GetNearestButton (selectedBounds.Value);
						if (nearestButton != null) {
							nearestButton.Accessible.Focused = true;
						} else {
							Accessible.SetCurrentFocus ();
						}
					}
				} catch (Exception e) {
					LoggingService.LogInternalError ("Error while undoing widget change.", e);
				}
			}

			ButtonAccessible GetNearestButton (Rectangle selectedBounds)
			{
				static int GetDelta (Rectangle rect1, Rectangle rect2)
				{
					int d1 = Math.Abs (rect1.Top - rect2.Bottom);
					int d2 = Math.Abs (rect1.Bottom - rect2.Top);
					return Math.Min (d1, d2);
				}

				int curDelta = int.MaxValue;
				ButtonAccessible nearestButton = null;

				foreach (var button in accessibleButtons) {
					int delta = GetDelta (button.Value.Bounds, selectedBounds);
					if (delta < curDelta) {
						nearestButton = button.Value;
					}
				}
				return nearestButton;
			}

			ButtonAccessible GetFocusedButton ()
			{
				foreach (var buttons in accessibleButtons) {
					if (buttons.Value.Accessible.Focused)
						return buttons.Value;
				}
				return null;
			}

			protected override bool OnLeaveNotifyEvent (EventCrossing evnt)
			{
				selectedHunk = Hunk.Empty;
				TooltipText = null;
				QueueDraw ();
				return base.OnLeaveNotifyEvent (evnt);
			}

			const int buttonSize = 16;

			public bool GetButtonPosition (Hunk hunk, double y1, double y2, double z1, double z2, out double x, out double y, out double w, out double h)
			{
				if (hunk.Removed > 0) {
					var b1 = z1;
					var b2 = z2;
					x = useLeft ? 0 : Allocation.Width - buttonSize;
					y = b1;
					w = buttonSize;
					h = b2 - b1;
					return hunk.Inserted > 0;
				} else {
					var b1 = y1;
					var b2 = y2;

					x = useLeft ? Allocation.Width - buttonSize : 0;
					y = b1;
					w = buttonSize;
					h = b2 - b1;
					return hunk.Removed > 0;
				}
			}

			void DrawArrow (Cairo.Context cr, double x, double y)
			{
				if (useLeft) {
					cr.MoveTo (x - 2, y - 3);
					cr.LineTo (x + 2, y);
					cr.LineTo (x - 2, y + 3);
				} else {
					cr.MoveTo (x + 2, y - 3);
					cr.LineTo (x - 2, y);
					cr.LineTo (x + 2, y + 3);
				}
			}

			static void DrawCross (Cairo.Context cr, double x, double y)
			{
				cr.MoveTo (x - 2, y - 3);
				cr.LineTo (x + 2, y + 3);
				cr.MoveTo (x + 2, y - 3);
				cr.LineTo (x - 2, y + 3);
			}

			protected override bool OnExposeEvent (EventExpose evnt)
			{
				bool hideButton = widget.MainEditor.Document.IsReadOnly;
				using (Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window)) {
					cr.Rectangle (evnt.Region.Clipbox.X, evnt.Region.Clipbox.Y, evnt.Region.Clipbox.Width, evnt.Region.Clipbox.Height);
					cr.Clip ();
					int delta = widget.MainEditor.Allocation.Y - Allocation.Y;
					if (Diff != null) {
						foreach (Hunk hunk in Diff) {
							double z1 = delta + fromEditor.LineToY (hunk.RemoveStart) - fromEditor.VAdjustment.Value;
							double z2 = delta + fromEditor.LineToY (hunk.RemoveStart + hunk.Removed) - fromEditor.VAdjustment.Value;
							if (z1 == z2)
								z2 = z1 + 1;

							double y1 = delta + toEditor.LineToY (hunk.InsertStart) - toEditor.VAdjustment.Value;
							double y2 = delta + toEditor.LineToY (hunk.InsertStart + hunk.Inserted) - toEditor.VAdjustment.Value;

							if (y1 == y2)
								y2 = y1 + 1;

							if (!useLeft) {
								var tmp = z1;
								z1 = y1;
								y1 = tmp;

								tmp = z2;
								z2 = y2;
								y2 = tmp;
							}

							int x1 = 0;
							int x2 = Allocation.Width;

							if (!hideButton) {
								if (useLeft && hunk.Removed > 0 || !useLeft && hunk.Removed == 0) {
									x1 += 16;
								} else {
									x2 -= 16;
								}
							}

							if (z1 == z2)
								z2 = z1 + 1;

							cr.MoveTo (x1, z1);

							cr.CurveTo (x1 + (x2 - x1) / 4, z1,
								x1 + (x2 - x1) * 3 / 4, y1,
								x2, y1);

							cr.LineTo (x2, y2);
							cr.CurveTo (x1 + (x2 - x1) * 3 / 4, y2,
								x1 + (x2 - x1) / 4, z2,
								x1, z2);
							cr.ClosePath ();
							cr.SetSourceColor (GetColor (hunk, this.useLeft, false, 1.0));
							cr.Fill ();

							cr.SetSourceColor (GetColor (hunk, this.useLeft, true, 1.0));
							cr.MoveTo (x1, z1);
							cr.CurveTo (x1 + (x2 - x1) / 4, z1,
								x1 + (x2 - x1) * 3 / 4, y1,
								x2, y1);
							cr.Stroke ();

							cr.MoveTo (x2, y2);
							cr.CurveTo (x1 + (x2 - x1) * 3 / 4, y2,
								x1 + (x2 - x1) / 4, z2,
								x1, z2);
							cr.Stroke ();

							if (!hideButton) {
								bool isButtonSelected = hunk == selectedHunk;

								double x, y, w, h;
								bool drawArrow = useLeft ? GetButtonPosition (hunk, y1, y2, z1, z2, out x, out y, out w, out h) :
									GetButtonPosition (hunk, z1, z2, y1, y2, out x, out y, out w, out h);

								cr.Rectangle (x, y, w, h);
								if (isButtonSelected) {
									int mx, my;
									GetPointer (out mx, out my);
									//	mx -= (int)x;
									//	my -= (int)y;
									using (var gradient = new Cairo.RadialGradient (mx, my, h, mx, my, 2)) {
										var color = (MonoDevelop.Components.HslColor)Style.Mid (StateType.Normal);
										color.L *= 1.05;
										gradient.AddColorStop (0, color);
										color.L *= 1.07;
										gradient.AddColorStop (1, color);
										cr.SetSource (gradient);
									}
								} else {
									cr.SetSourceColor ((MonoDevelop.Components.HslColor)Style.Mid (StateType.Normal));
								}
								cr.FillPreserve ();

								cr.SetSourceColor ((MonoDevelop.Components.HslColor)Style.Dark (StateType.Normal));
								cr.Stroke ();
								cr.LineWidth = 1;
								cr.SetSourceColor (MonoDevelop.Ide.Gui.Styles.BaseForegroundColor.ToCairoColor ());
								if (drawArrow) {
									DrawArrow (cr, x + w / 1.5, y + h / 2);
									DrawArrow (cr, x + w / 2.5, y + h / 2);
								} else {
									DrawCross (cr, x + w / 2, y + (h) / 2);
								}
								cr.Stroke ();
							}
						}
					}
				}
				return true;
			}

			internal void Refresh ()
			{
				QueueDraw ();
				UpdateAccessiblity ();
			}

			Dictionary<Hunk, ButtonAccessible> accessibleButtons = new Dictionary<Hunk, ButtonAccessible> ();

			void UpdateAccessiblity ()
			{
				if (Accessible == null)
					return;
				bool hideButton = widget.MainEditor.Document.IsReadOnly;
				int delta = widget.MainEditor.Allocation.Y - Allocation.Y;

				if (Diff != null) {
					foreach (var hunk in Diff) {
						double z1 = delta + fromEditor.LineToY (hunk.RemoveStart) - fromEditor.VAdjustment.Value;
						double z2 = delta + fromEditor.LineToY (hunk.RemoveStart + hunk.Removed) - fromEditor.VAdjustment.Value;
						if (z1 == z2)
							z2 = z1 + 1;

						double y1 = delta + toEditor.LineToY (hunk.InsertStart) - toEditor.VAdjustment.Value;
						double y2 = delta + toEditor.LineToY (hunk.InsertStart + hunk.Inserted) - toEditor.VAdjustment.Value;

						if (y1 == y2)
							y2 = y1 + 1;

						if (!useLeft) {
							var tmp = z1;
							z1 = y1;
							y1 = tmp;

							tmp = z2;
							z2 = y2;
							y2 = tmp;
						}

						int x1 = 0;
						int x2 = Allocation.Width;

						if (!hideButton) {
							if (useLeft && hunk.Removed > 0 || !useLeft && hunk.Removed == 0) {
								x1 += 16;
							} else {
								x2 -= 16;
							}
						}

						if (z1 == z2)
							z2 = z1 + 1;

						if (!hideButton) {
							bool isButtonSelected = hunk == selectedHunk;

							double x, y, w, h;
							bool drawArrow = useLeft ? GetButtonPosition (hunk, y1, y2, z1, z2, out x, out y, out w, out h) :
								GetButtonPosition (hunk, z1, z2, y1, y2, out x, out y, out w, out h);

							var button = SetAccessibleProxyButton (hunk, x, y, w, h);
							button.Visible = y + h > 0 && y < Allocation.Height;
						}
					}
				}
				foreach (var kv in accessibleButtons.ToArray ()) {
					if (!Diff.Contains (kv.Key)) {
						accessibleButtons.Remove (kv.Key);
						kv.Value.Dispose ();
					}
				}
				Accessible.SetAccessibleChildren (accessibleButtons.Where(a => a.Value.Visible).Select (a => a.Value.Accessible).ToArray ());
			}

			ButtonAccessible SetAccessibleProxyButton (Hunk hunk, double x, double y, double w, double h)
			{
				if (!accessibleButtons.TryGetValue (hunk, out var button)) {
					button = new ButtonAccessible (this, hunk);
					accessibleButtons [hunk] = button;
				}

				button.SetBounds ((int)x, (int)y, (int)w, (int)h);
				return button;
			}

			void ClearAccessibleButtons ()
			{

				foreach (var button in accessibleButtons) {
					button.Value.Dispose ();
				}
				accessibleButtons.Clear ();
				Accessible.SetAccessibleChildren (Array.Empty<AccessibilityElementProxy> ());

			}

			class ButtonAccessible : IDisposable
			{
				MiddleArea widget;
				Hunk hunk;

				public AccessibilityElementProxy Accessible { get; private set; }
				public bool Visible { get; internal set; }
				public Rectangle Bounds { get; private set; }

				public ButtonAccessible (MiddleArea widget, Hunk hunk)
				{
					this.widget = widget;
					this.hunk = hunk;

					Accessible = AccessibilityElementProxy.ButtonElementProxy ();
					Accessible.GtkParent = widget;
					Accessible.PerformPress += PerformPress;

					Accessible.SetRole (AtkCocoa.Roles.AXButton);

					if (hunk.Inserted > 0) {
						Accessible.Label = GettextCatalog.GetPluralString ("Revert {0} inserted line starting at {1}", "Revert {0} inserted lines starting at {1}", hunk.Inserted, hunk.Inserted, hunk.InsertStart);
					} else if (hunk.Removed > 0) {
						Accessible.Label = GettextCatalog.GetPluralString ("Revert {0} removed line starting at {1}", "Revert {0} removed lines starting at {1}", hunk.Removed, hunk.Removed, hunk.RemoveStart);
					} else {
						Accessible.Label = GettextCatalog.GetPluralString ("Revert {0} replaced line starting at {1}", "Revert {0} replaced lines starting at {1}", hunk.Removed, hunk.Removed, hunk.InsertStart);
					}
				}

				public void SetBounds (int x, int y, int w, int h)
				{
					Accessible.FrameInGtkParent = Bounds = new Rectangle (x, y, w, h);

					var cocoaY = widget.Allocation.Height - y - h;

					Accessible.FrameInParent = new Rectangle (x, cocoaY, w, h);
				}

				void PerformPress (object sender, EventArgs e)
				{
					widget.PerformRevert (hunk);
				}

				public void Dispose ()
				{
					if (Accessible == null)
						return;
					Accessible.PerformPress -= PerformPress;
					Accessible = null;
					widget = null;
				}
			}
		}
	}

}

