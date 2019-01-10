//
// ResultTooltipProvider.FloatingQuickFixIconWidget.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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
using MonoDevelop.Ide.Editor;
using MonoDevelop.SourceEditor;
using MonoDevelop.Components;
using MonoDevelop.CodeActions;
using Gdk;
using System.Windows.Input;
using MonoDevelop.Ide;

namespace MonoDevelop.AnalysisCore.Gui
{
	partial class ResultTooltipProvider
	{
		partial class FloatingQuickFixIconWidget : Gtk.Window
		{
			readonly CodeActionEditorExtension ext;
			readonly LanguageItemWindow window;
			readonly SourceEditorView sourceEditorView;
			readonly CodeActionContainer fixes;
			readonly Cairo.Point point;
			uint destroyTimeout;

			public FloatingQuickFixIconWidget (
				CodeActionEditorExtension codeActionEditorExtension,
				LanguageItemWindow window,
				SourceEditorView sourceEditorView,
				CodeActionContainer fixes,
				Cairo.Point point) : base (Gtk.WindowType.Popup)
			{
				this.ext = codeActionEditorExtension;
				this.window = window;
				this.sourceEditorView = sourceEditorView;
				this.fixes = fixes;
				this.point = point;
				this.Decorated = false;
				this.Events |= EventMask.ButtonPressMask | EventMask.LeaveNotifyMask | EventMask.EnterNotifyMask;
				TypeHint = Gdk.WindowTypeHint.Utility;
				var fr = new Gtk.HBox ();
				fr.BorderWidth = 2;
				var view = new ImageView (SmartTagMarginMarker.GetIconId (fixes.GetSmartTagSeverity ()), Gtk.IconSize.Menu);
				fr.PackStart (view, false, false, 0);
				fr.PackEnd (new RectangleMarker (), false, false, 0);
				Add (fr);
				ext.FixesMenuClosed += Ext_FixesMenuClosed;

				ShowAll ();
			}

			void Ext_FixesMenuClosed (object sender, EventArgs e)
			{
				Destroy ();
			}

			protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
			{
				CancelDestroy ();
				return base.OnEnterNotifyEvent (evnt);
			}

			protected override bool OnLeaveNotifyEvent (EventCrossing evnt)
			{
				if (ext.smartTagPopupTimeoutId == 0) {
					if (!this.IsMouseOver ()) {
						QueueDestroy ();
					}
				}
				return base.OnLeaveNotifyEvent (evnt);
			}

			protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
			{
				ext.CancelSmartTagPopupTimeout ();
				ext.smartTagPopupTimeoutId = GLib.Timeout.Add (150, delegate {
					ext.PopupQuickFixMenu (null, fixes, menu => { }, new Xwt.Point (
						point.X,
						point.Y + Allocation.Height + 10));
					ext.smartTagPopupTimeoutId = 0;
					return false;
				});
				return base.OnButtonPressEvent (evnt);
			}

			protected override void OnDestroyed ()
			{
				ext.FixesMenuClosed -= Ext_FixesMenuClosed;
				CancelDestroy ();
				window.Destroy ();
				base.OnDestroyed ();
			}

			internal void CancelDestroy ()
			{
				if (destroyTimeout > 0) {
					GLib.Source.Remove (destroyTimeout);
					destroyTimeout = 0;
				}
			}

			internal void QueueDestroy (uint timer = 500)
			{
				if (timer == 0) {
					CancelDestroy ();
					Destroy ();
					return;
				}
				if (destroyTimeout != 0)
					return;
				destroyTimeout = GLib.Timeout.Add (timer, delegate {
					Destroy ();
					destroyTimeout = 0;
					return false;
				});
			}

			internal bool IsMouseNear ()
			{
				GetPointer (out int x, out int y);
				return x >= -Allocation.Width && y >= -Allocation.Height && x <= Allocation.Width && y <= Allocation.Height;
			}
		}
	}
}
