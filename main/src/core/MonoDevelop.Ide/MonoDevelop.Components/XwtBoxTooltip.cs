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
		TooltipPopoverWindow tooltipWindow;
		bool mouseOver;

		public string ToolTip {
			get { return tip; }
			set {
				tip = value;
				if (tooltipWindow != null) {
					if (!string.IsNullOrEmpty (tip))
						tooltipWindow.Markup = value;
					else
						HideTooltip ();
				} else if (!string.IsNullOrEmpty (tip) && mouseOver)
					ShowTooltip ();
			}
		}

		public TaskSeverity? Severity { get; set; }
		public PopupPosition Position { get; set; }

		public XwtBoxTooltip (Widget child)
		{
			if (child == null)
				throw new ArgumentNullException (nameof (child));
			
			Content = child;
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
				HideTooltip ();
				tooltipWindow = new TooltipPopoverWindow ();
				tooltipWindow.ShowArrow = true;
				tooltipWindow.Markup = tip;
				tooltipWindow.Severity = Severity;
				var rect = new Rectangle (0, 0, Content.Size.Width, Content.Size.Height);
				tooltipWindow.ShowPopup (Content, rect, Position);
			}
			return false;
		}

		void HideTooltip ()
		{
			if (tooltipWindow != null) {
				tooltipWindow.Destroy ();
				tooltipWindow = null;
			}
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing) {
				HideTooltip ();
			}
			base.Dispose (disposing);
		}
	}
}
