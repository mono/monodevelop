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
using MonoDevelop.Ide.Tasks;
using Xwt;
namespace MonoDevelop.Components
{
	public class XwtBoxTooltip : Widget
	{
		string tip;
		TaskSeverity? severity;
		TooltipPopoverWindow tooltipWindow;
		bool mouseOver, mouseOverTooltip;

		public string ToolTip {
			get { return tip; }
			set {
				tip = value;
				if (!string.IsNullOrEmpty (tip))
					tooltipWindow.Markup = value;
				else {
					tooltipWindow.Markup = string.Empty;
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
				tooltipWindow.Severity = Severity;
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
					if (tooltipWindow.Visible) {
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
			tooltipWindow = new TooltipPopoverWindow ();
			tooltipWindow.ShowArrow = true;
			Position = PopupPosition.Top;
			Severity = TaskSeverity.Information;
		}

		protected override void OnMouseEntered (EventArgs args)
		{
			base.OnMouseEntered (args);
			mouseOver = true;
			ShowTooltip ();
		}

		protected override void OnMouseExited (EventArgs args)
		{
			base.OnMouseExited (args);
			mouseOver = false;
			HideTooltip ();
		}

		bool ShowTooltip ()
		{
			if (!string.IsNullOrEmpty (tip)) {
				var rect = new Rectangle (0, 0, Content.Size.Width, Content.Size.Height);
				tooltipWindow.ShowPopup (Content, rect, Position);
			}
			return false;
		}

		void HideTooltip ()
		{
			if (tooltipWindow != null) {
				tooltipWindow.Hide ();
			}
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing) {
				HideTooltip ();
				tooltipWindow?.Dispose ();
			}
			base.Dispose (disposing);
		}
	}
}
