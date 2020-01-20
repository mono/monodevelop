//
// InformationPopoverWidget.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc (http://www.xamarin.com)
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
using System.Timers;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Tasks;
using Xwt;

namespace MonoDevelop.Components
{
	public class InformationPopoverWidget: Widget
	{
		TaskSeverity severity;
		Xwt.ImageView imageView;
		string message;
		bool markup;
		TooltipPopoverWindow popover;
		PopupPosition popupPosition = PopupPosition.Top;

		public InformationPopoverWidget ()
		{
			severity = TaskSeverity.Information;
			imageView = new Xwt.ImageView ();
			Accessible.Role = Xwt.Accessibility.Role.Image;
			imageView.Accessible.Role = Xwt.Accessibility.Role.Filler;
			UpdateIcon ();
			UpdateAccessibility ();
			Content = imageView;
			CanGetFocus = true;
		}

		public TaskSeverity Severity {
			get {
				return severity;
			}
			set {
				severity = value;
				UpdateIcon ();
				UpdateAccessibility ();
				UpdatePopover ();
			}
		}

		public string Message {
			get {
				return message;
			}
			set {
				message = value;
				UpdateAccessibility ();
				UpdatePopover ();
			}
		}

		public bool UseMarkup {
			get { return markup; }
			set {
				markup = value;
				UpdateAccessibility ();
				UpdatePopover ();
			}
		}

		public PopupPosition PopupPosition {
			get { return popupPosition; }
			set {
				popupPosition = value;
				UpdatePopover ();
			}
		}

		void UpdateIcon ()
		{
			imageView.Image = GetSeverityIcon ();
		}

		void UpdateAccessibility ()
		{
			Accessible.RoleDescription = GetAccessibilityDescription ();
			var text = message ?? string.Empty;
			if (UseMarkup) {
				var cleanText = FormattedText.FromMarkup (text);
				Accessible.Title = cleanText.Text;
			} else {
				Accessible.Title = message ?? string.Empty;
			}
		}

		Xwt.Drawing.Image GetSeverityIcon ()
		{
			switch (severity) {
			case TaskSeverity.Error:
				return ImageService.GetIcon ("md-error", Gtk.IconSize.Menu);
			case TaskSeverity.Warning:
				return ImageService.GetIcon ("md-warning", Gtk.IconSize.Menu);
			}
			return ImageService.GetIcon ("md-information", Gtk.IconSize.Menu);
		}

		string GetAccessibilityDescription ()
		{
			switch (severity) {
			case TaskSeverity.Error:
				return GettextCatalog.GetString ("Error Icon");
			case TaskSeverity.Warning:
				return GettextCatalog.GetString ("Warning Icon");
			}
			return GettextCatalog.GetString ("Information Icon");
		}

		protected override void OnGotFocus (EventArgs args)
		{
			base.OnGotFocus (args);
			ShowPopover ();
		}

		protected override void OnMouseEntered (EventArgs args)
		{
			base.OnMouseEntered (args);
			ShowPopover ();
		}

		bool WorkaroundNestedDialogFlickering ()
		{
			// There seems to be a problem with Gdk.Window focus events when the parent
			// window is transient for another modal window (i.e. dialogs on top of the Ide preferences window).
			// A native tooltip seems to confuse Gdk in this case and it rapidly fires LeaveNotify/EnterNotify
			// events leading to fast flickering of the tooltip.
			if (ParentWindow != null && Surface.ToolkitEngine.GetNativeWindow (ParentWindow) is Gtk.Window gtkWindow) {
				if (gtkWindow.TransientFor?.TransientFor != null)
					return true;
			}
			return false;
		}

		void ShowPopover ()
		{
			if (hideTooltipTimer?.Enabled == true)
				hideTooltipTimer.Stop ();
			if (popover == null)
				popover = TooltipPopoverWindow.Create (!WorkaroundNestedDialogFlickering ());
			popover.ShowArrow = true;
			if (markup)
				popover.Markup = message;
			else
				popover.Text = message;
			popover.Severity = severity;
			popover.ShowPopup (this, popupPosition);
		}

		void UpdatePopover ()
		{
			if (popover?.Visible == true)
				ShowPopover ();
		}

		protected override void OnLostFocus (EventArgs args)
		{
			base.OnLostFocus (args);
			HidePopover ();
		}

		protected override void OnMouseExited (EventArgs args)
		{
			base.OnMouseExited (args);
			HidePopover (true);
		}

		protected override void OnPreferredSizeChanged ()
		{
			base.OnPreferredSizeChanged ();
			if (!Visible)
				HidePopover ();
		}

		Timer hideTooltipTimer;

		void HidePopover (bool delayed = false)
		{
			if (delayed) {
				// we delay hiding using a timer to avoid tooltip flickering in case of focus stealing
				// due to weird toolkit behaviour.
				if (hideTooltipTimer == null) {
					hideTooltipTimer = new Timer (50) {
						AutoReset = false,
						SynchronizingObject = this,
					};
					hideTooltipTimer.Elapsed += (sender, e) => {
						if (popover?.Visible == true)
							popover.Hide ();
					};
				}
				hideTooltipTimer.Start ();
			} else {
				if (hideTooltipTimer?.Enabled == true)
					hideTooltipTimer.Stop ();
				if (popover?.Visible == true)
					popover.Hide ();
			}
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing) {
				hideTooltipTimer?.Dispose ();
				if (popover?.Visible == true)
					popover.Hide ();
				popover?.Dispose ();
			}
			hideTooltipTimer = null;
			popover = null;
			base.Dispose (disposing);
		}
	}
}

