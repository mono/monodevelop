// 
// PinnedWatchWidget.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using Mono.TextEditor;
using MonoDevelop.Debugger;
using MonoDevelop.Ide.Tasks;
using System.Collections.Generic;
using Mono.Debugging.Client;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core;

namespace MonoDevelop.SourceEditor
{
	public class PinnedWatchWidget : Gtk.EventBox
	{
		PinnedWatch watch;
		Gtk.Image image = new Gtk.Image ();
		Gtk.Label label = new Gtk.Label ();
		Gtk.Label valueLabel = new Gtk.Label ();
		Gtk.EventBox unpin = new Gtk.EventBox ();
		
		public PinnedWatch Watch {
			get { return this.watch; }
		}
		
		public ObjectValue ObjectValue {
			get {
				return objectValue;
			}
			set {
				if (this.objectValue == value)
					return;
				this.objectValue = value;
				if (this.objectValue != null) {
					image.Pixbuf = ImageService.GetPixbuf (ObjectValueTreeView.GetIcon (this.objectValue.Flags), Gtk.IconSize.Menu);
					label.Text = this.objectValue.Name;
					valueLabel.Text = GetString (this.objectValue);
					this.QueueResize ();
				} else {
					/*label.Text = "";
					valueLabel.Text = "";*/ 
				}
				
			}
		}
		
		ObjectValue objectValue;
		
		TextEditorContainer container;
		
		TextEditor Editor {
			get {
				return container.TextEditorWidget;
			}
		}
		
		class Divider : Gtk.Widget
		{
			public Divider ()
			{
				WidthRequest = 1;
				WidgetFlags |= Gtk.WidgetFlags.NoWindow;
			}
			
			protected override bool OnExposeEvent (Gdk.EventExpose evnt)
			{
				evnt.Window.DrawLine (Style.BaseGC (Gtk.StateType.Normal), Allocation.X, Allocation.Top, Allocation.X, Allocation.Bottom);
				int border = Allocation.Height / 5;
				evnt.Window.DrawLine (Style.MidGC (Gtk.StateType.Normal), Allocation.X, Allocation.Top + border, Allocation.X, Allocation.Bottom - border - 1);
				
				evnt.Window.DrawLine (Style.MidGC (Gtk.StateType.Normal), Allocation.X, Allocation.Top, Allocation.Right, Allocation.Top);
				evnt.Window.DrawLine (Style.MidGC (Gtk.StateType.Normal), Allocation.X, Allocation.Bottom - 1, Allocation.Right, Allocation.Bottom - 1);
				return false;
			}
		}
		Gtk.Image unpinImage;
		public PinnedWatchWidget (TextEditorContainer container, PinnedWatch watch)
		{
			this.container = container;
			this.watch = watch;
			this.ObjectValue = watch.Value;
			
			Gtk.HBox box = new Gtk.HBox ();
			box.PackStart (image, false, false, 0);
			box.PackStart (label, false, false, 0);
			
			box.PackStart (new Divider (), true, true, 2);
			
			box.PackStart (valueLabel, false, false, 2);
			unpinImage = new Gtk.Image ();
			unpinImage.Pixbuf = ImageService.GetPixbuf ("md-pin-down", Gtk.IconSize.Menu);
			unpin.Child = unpinImage;
			unpin.ButtonPressEvent += delegate {
				DebuggingService.PinnedWatches.Remove (watch);
			};
			box.PackStart (unpin, false, false, 0);
			this.Child = box;
			HandleEditorOptionsChanged (null, null);
			ShowAll ();
			//unpin.Hide ();
			Editor.EditorOptionsChanged += HandleEditorOptionsChanged;
		}

		void HandleEditorOptionsChanged (object sender, EventArgs e)
		{
			this.HeightRequest = Editor.LineHeight;
			fontDescription = Pango.FontDescription.FromString (Editor.Options.FontName);
			fontDescription.Family = "Sans";
			fontDescription.Size = (int)(fontDescription.Size * Editor.Options.Zoom);
			label.ModifyFont (fontDescription);
			valueLabel.ModifyFont (fontDescription);
		}
		
		Pango.FontDescription fontDescription;
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			Editor.EditorOptionsChanged -= HandleEditorOptionsChanged;
			
		}

		static string GetString (ObjectValue val)
		{
			if (val.IsUnknown) 
				return GettextCatalog.GetString ("The name '{0}' does not exist in the current context.", val.Name);
			if (val.IsError) 
				return val.Value;
			if (val.IsNotSupported) 
				return val.Value;
			if (val.IsError) 
				return val.Value;
			if (val.IsEvaluating) 
				return GettextCatalog.GetString ("Evaluating...");
			return val.DisplayValue ?? "(null)";
		}
		
		bool mousePressed = false;
		double originX, originY;
		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			originX = evnt.XRoot;
			originY = evnt.YRoot;
			
			if (!mousePressed) {
				mousePressed = true;
				container.MoveToTop (this);
				Gdk.Pointer.Grab (this.GdkWindow, true, Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonReleaseMask | Gdk.EventMask.PointerMotionMask | Gdk.EventMask.EnterNotifyMask | Gdk.EventMask.LeaveNotifyMask, null, null, Gtk.Global.CurrentEventTime);
				Gtk.Grab.Add (this);
			}
			return base.OnButtonPressEvent (evnt);
		}
		
		protected override bool OnButtonReleaseEvent (Gdk.EventButton evnt)
		{
			if (mousePressed) {
				Gdk.Pointer.Ungrab (Gtk.Global.CurrentEventTime);
				Gtk.Grab.Remove (this);
				mousePressed = false;
			}
			return base.OnButtonReleaseEvent (evnt);
		}
		
		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
		{
			bool result = base.OnEnterNotifyEvent (evnt);
/*			this.unpin.ShowAll ();
			QueueResize ();*/
			return result;
		}
		
		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
		{
			bool result = base.OnLeaveNotifyEvent (evnt);
			/*
			this.unpin.HideAll ();
			QueueResize ();*/
			
			return result;
		}
 
		protected override bool OnMotionNotifyEvent (Gdk.EventMotion evnt)
		{
			if (mousePressed) {
				watch.OffsetX += (int)(evnt.XRoot - originX);
				watch.OffsetY += (int)(evnt.YRoot - originY);
				
				originX = evnt.XRoot;
				originY = evnt.YRoot;
			}
			
			return base.OnMotionNotifyEvent (evnt);
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			int winWidth = Allocation.Width;
			int winHeight = Allocation.Height;
			evnt.Window.DrawRectangle (Style.BaseGC (Gtk.StateType.Normal), true, 0, 0, winWidth - 1, winHeight - 1);
			evnt.Window.DrawRectangle (Style.MidGC (Gtk.StateType.Normal), false, 0, 0, winWidth - 1, winHeight - 1);
			foreach (var child in this.Children)
				this.PropagateExpose (child, evnt);
			return false;
		}

		
	}
}