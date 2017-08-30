//
// XwtBoxTooltip.cs
//
// Author:
//       Vsevolod Kukol <sevoku@microsoft.com>
//
// Copyright (c) 2016 Microsoft Corporation
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
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Tasks;
using Xwt;
namespace MonoDevelop.Components
{
	public class XwtBoxTooltip : Widget
	{
		string tip;
		TaskSeverity? severity;
		TooltipPopoverWindow tooltipWindow;
		Xwt.Popover xwtPopover;

		public string ToolTip {
			get { return tip; }
			set {
				tip = value;
				if (!string.IsNullOrEmpty (tip)) {
					if (tooltipWindow != null)
						tooltipWindow.Markup = value;
					if (xwtPopover != null)
						((Label)xwtPopover.Content).Markup = value;
				} else {
					if (tooltipWindow != null)
						tooltipWindow.Markup = string.Empty;
					if (xwtPopover != null) 
						((Label)xwtPopover.Content).Markup = string.Empty;
					HideTooltip ();
				}
			}
		}

		public TaskSeverity? Severity {
			get {
				return severity;
			}
			set {
				severity = value;
				if (tooltipWindow != null)
					tooltipWindow.Severity = Severity;
				if (xwtPopover != null) {
					((Label)xwtPopover.Content).Font = Xwt.Drawing.Font.SystemFont.WithScaledSize (Styles.FontScale11);

					switch (severity.Value) {
					case TaskSeverity.Information:
						xwtPopover.BackgroundColor = Styles.PopoverWindow.InformationBackgroundColor;
						break;

					case TaskSeverity.Comment:
						xwtPopover.BackgroundColor = Styles.PopoverWindow.InformationBackgroundColor;
						break;

					case TaskSeverity.Error:
						xwtPopover.BackgroundColor = Styles.PopoverWindow.ErrorBackgroundColor;
						return;

					case TaskSeverity.Warning:
						xwtPopover.BackgroundColor = Styles.PopoverWindow.WarningBackgroundColor;
						return;
					}
				}
			}
		}

		PopupPosition position;

		public PopupPosition Position {
			get {
				return position;
			}
			set {
				if (position != value) {
					position = value;
					if (tooltipWindow?.Visible == true || xwtPopover != null) {
						HideTooltip ();
						ShowTooltip ();
					}
				}
			}
		}

		public XwtBoxTooltip (Widget child)
		{
			if (child == null)
				throw new ArgumentNullException (nameof (child));
			
			Content = child;
			// FIXME: WPF blocks the main Gtk loop and makes TooltipPopoverWindow unusable.
			//        We use the Xwt.Popover as a workaround for now.
			if (Surface.ToolkitEngine.Type == ToolkitType.Wpf) {
				xwtPopover = new Popover ();
				xwtPopover.BackgroundColor = Styles.PopoverWindow.DefaultBackgroundColor;
				xwtPopover.Content = new Label { Wrap = WrapMode.Word };
				xwtPopover.Padding = 3;
			} else {
				tooltipWindow = TooltipPopoverWindow.Create ();
				tooltipWindow.ShowArrow = true;
			}
			Position = PopupPosition.Top;
			Severity = TaskSeverity.Information;
		}

		protected override void OnMouseEntered (EventArgs args)
		{
			base.OnMouseEntered (args);
			ShowTooltip ();
		}

		protected override void OnMouseExited (EventArgs args)
		{
			base.OnMouseExited (args);
			HideTooltip ();
		}

		public bool ShowTooltip ()
		{
			if (hideTooltipTimer?.Enabled == true)
				hideTooltipTimer.Stop ();
			if (!string.IsNullOrEmpty (tip)) {
				var rect = new Rectangle (0, 0, Content.Size.Width, Content.Size.Height);
				if (tooltipWindow != null)
					tooltipWindow.ShowPopup (Content, rect, Position);
				if (xwtPopover != null)
					xwtPopover.Show (GetXwtPosition(Position), Content, rect);
			}
			return false;
		}

		static Popover.Position GetXwtPosition (PopupPosition position)
		{
			switch (position) {
				case PopupPosition.Bottom:
					return Popover.Position.Bottom;
				default: 
					return Popover.Position.Top;
			}
		}

		Timer hideTooltipTimer;

		public void HideTooltip ()
		{
			// we delay hiding using a timer to avoid tooltip flickering in case of focus stealing
			// due to weird toolkit behaviour.
			if (hideTooltipTimer == null) {
				hideTooltipTimer = new Timer (50) {
					AutoReset = false,
					SynchronizingObject = this,
				};
				hideTooltipTimer.Elapsed += (sender, e) => {
					if (tooltipWindow?.Visible == true)
						tooltipWindow.Hide ();
				};
			}
			hideTooltipTimer.Start ();
		}

		public new bool Visible {
			get {
				return base.Visible;
			}
			set {
				base.Visible = value;
				if (!value)
					HideTooltip ();
			}
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing) {
				HideTooltip ();
				hideTooltipTimer?.Dispose ();
				tooltipWindow?.Dispose ();
				xwtPopover?.Dispose ();
			}
			hideTooltipTimer = null;
			tooltipWindow = null;
			xwtPopover = null;
			base.Dispose (disposing);
		}
	}
}
