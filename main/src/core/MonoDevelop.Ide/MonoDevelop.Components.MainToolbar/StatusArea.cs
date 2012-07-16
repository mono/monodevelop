// 
// StatusArea.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Components;
using Cairo;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Tasks;

namespace MonoDevelop.Components.MainToolbar
{
	class StatusArea : EventBox
	{
		HBox contentBox = new HBox (false, 8);

		StatusAreaSeparator statusIconSeparator;
		Gtk.Widget buildResultWidget;

		public StatusArea ()
		{
			VisibleWindow = false;
			WidgetFlags |= Gtk.WidgetFlags.AppPaintable;
			contentBox.PackStart (MonoDevelopStatusBar.messageBox, true, true, 0);
			contentBox.PackEnd (MonoDevelopStatusBar.statusIconBox, false, false, 0);
			contentBox.PackEnd (statusIconSeparator = new StatusAreaSeparator (), false, false, 0);
			contentBox.PackEnd (buildResultWidget = CreateBuildResultsWidget (Orientation.Horizontal), false, false, 0);

			var align = new Alignment (0, 0, 1, 1);
			align.LeftPadding = 4;
			align.RightPadding = 8;
			align.Add (contentBox);
			Add (align);

			this.ButtonPressEvent += delegate {
				MonoDevelopStatusBar.HandleEventMessageBoxButtonPressEvent (null, null);
			};

			MonoDevelopStatusBar.statusIconBox.Shown += delegate {
				UpdateSeparators ();
			};

			MonoDevelopStatusBar.statusIconBox.Hidden += delegate {
				UpdateSeparators ();
			};
			
			ShowAll ();
		}

		void UpdateSeparators ()
		{
			statusIconSeparator.Visible = MonoDevelopStatusBar.statusIconBox.Visible && buildResultWidget.Visible;
		}

		public Widget CreateBuildResultsWidget (Orientation orientation)
		{
			Gtk.Box box;
			if (orientation == Orientation.Horizontal)
				box = new HBox ();
			else
				box = new VBox ();
			box.Spacing = 3;
			
			Gdk.Pixbuf errorIcon = ImageService.GetPixbuf (MonoDevelop.Ide.Gui.Stock.Error, IconSize.Menu);
			Gdk.Pixbuf noErrorIcon = ImageService.MakeGrayscale (errorIcon); // creates a new pixbuf instance
			Gdk.Pixbuf warningIcon = ImageService.GetPixbuf (MonoDevelop.Ide.Gui.Stock.Warning, IconSize.Menu);
			Gdk.Pixbuf noWarningIcon = ImageService.MakeGrayscale (warningIcon); // creates a new pixbuf instance
			
			Gtk.Image errorImage = new Gtk.Image (errorIcon);
			Gtk.Image warningImage = new Gtk.Image (warningIcon);
			
			box.PackStart (errorImage, false, false, 0);
			Label errors = new Gtk.Label ();
			box.PackStart (errors, false, false, 0);
			
			box.PackStart (warningImage, false, false, 0);
			Label warnings = new Gtk.Label ();
			box.PackStart (warnings, false, false, 0);
			
			TaskEventHandler updateHandler = delegate {
				int ec=0, wc=0;
				foreach (Task t in TaskService.Errors) {
					if (t.Severity == TaskSeverity.Error)
						ec++;
					else if (t.Severity == TaskSeverity.Warning)
						wc++;
				}
				errors.Text = ec.ToString ();
				errorImage.Pixbuf = ec > 0 ? errorIcon : noErrorIcon;
				warnings.Text = wc.ToString ();
				warningImage.Pixbuf = wc > 0 ? warningIcon : noWarningIcon;
			};
			
			updateHandler (null, null);
			
			TaskService.Errors.TasksAdded += updateHandler;
			TaskService.Errors.TasksRemoved += updateHandler;
			
			box.Destroyed += delegate {
				noErrorIcon.Dispose ();
				noWarningIcon.Dispose ();
				TaskService.Errors.TasksAdded -= updateHandler;
				TaskService.Errors.TasksRemoved -= updateHandler;
			};

			EventBox ebox = new EventBox ();
			ebox.VisibleWindow = false;
			ebox.Add (box);
			ebox.ShowAll ();
			ebox.ButtonReleaseEvent += delegate {
				var pad = IdeApp.Workbench.GetPad<MonoDevelop.Ide.Gui.Pads.ErrorListPad> ();
				pad.BringToFront ();
			};


			return ebox;
		}

		protected override void OnRealized ()
		{
			base.OnRealized ();
			ModifyText (StateType.Normal, Styles.StatusBarTextColor.ToGdkColor ());
			ModifyFg (StateType.Normal, Styles.StatusBarTextColor.ToGdkColor ());
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			requisition.Height = 22;
			base.OnSizeRequested (ref requisition);
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var context = Gdk.CairoHelper.Create (evnt.Window)) {
				CairoExtensions.RoundedRectangle (context, Allocation.X + 0.5, Allocation.Y + 0.5, Allocation.Width - 1, Allocation.Height - 1, 3);
				using (LinearGradient lg = new LinearGradient (Allocation.X, Allocation.Y, Allocation.X, Allocation.Height)) {
					lg.AddColorStop (0, Styles.StatusBarFill1Color);
					lg.AddColorStop (1, Styles.StatusBarFill2Color);
					context.Pattern = lg;
				}
				context.Fill ();

				CairoExtensions.RoundedRectangle (context, Allocation.X + 1.5, Allocation.Y + 1.5, Allocation.Width - 2.5, Allocation.Height - 2.5, 3);
				context.LineWidth = 1;
				context.Color = Styles.StatusBarInnerColor;
				context.Stroke ();

				CairoExtensions.RoundedRectangle (context, Allocation.X + 0.5, Allocation.Y + 0.5, Allocation.Width - 1, Allocation.Height - 1, 3);
				context.LineWidth = 1;
				context.Color = Styles.StatusBarBorderColor;
				context.Stroke ();
			}
			return base.OnExposeEvent (evnt);
		}
	}

	class StatusAreaSeparator: HBox
	{
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var ctx = Gdk.CairoHelper.Create (this.GdkWindow)) {
				var alloc = Allocation;
				//alloc.Inflate (0, -2);
				ctx.Rectangle (alloc.X, alloc.Y, 1, alloc.Height);
				Cairo.LinearGradient gr = new LinearGradient (alloc.X, alloc.Y, alloc.X, alloc.Y + alloc.Height);
				gr.AddColorStop (0, new Cairo.Color (0, 0, 0, 0));
				gr.AddColorStop (0.5, new Cairo.Color (0, 0, 0, 0.2));
				gr.AddColorStop (1, new Cairo.Color (0, 0, 0, 0));
				ctx.Pattern = gr;
				ctx.Fill ();
			}
			return true;
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			requisition.Width = 1;
		}
	}
}

