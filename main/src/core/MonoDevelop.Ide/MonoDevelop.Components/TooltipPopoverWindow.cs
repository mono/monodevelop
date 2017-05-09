//
// TooltipPopoverWindow.cs
//
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
//       Vsevolod Kukol <sevoku@microsoft.com>
//
// Copyright (c) 2012 Xamarin Inc
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
using MonoDevelop.Core;
using MonoDevelop.Ide.Fonts;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Tasks;
using Xwt;

namespace MonoDevelop.Components
{
	public class TooltipPopoverWindow: XwtThemedPopup
	{
		static readonly Toolkit preferredEngine;
		Label label;
		TaskSeverity? severity;
		bool hasMarkup;
		string text;

		static TooltipPopoverWindow ()
		{
			preferredEngine = Platform.IsWindows? Toolkit.Load (ToolkitType.Gtk) : Toolkit.NativeEngine;
		}

		public static TooltipPopoverWindow Create (bool tryNative = true)
		{
			TooltipPopoverWindow popover = null;
			(tryNative ? preferredEngine : Toolkit.CurrentEngine).Invoke (() => {
				popover = new TooltipPopoverWindow ();
			});
			return popover;
		}

		public TooltipPopoverWindow () : base (PopupType.Tooltip)
		{
			Theme.SetBackgroundColor (Styles.PopoverWindow.DefaultBackgroundColor);
			Theme.Font = Xwt.Drawing.Font.FromName (FontService.SansFontName).WithScaledSize (Styles.FontScale11);
			ShowArrow = true;
		}

		public string Text {
			get {
				return text;
			}
			set {
				hasMarkup = false;
				text = value;
				AddLabel ();
				UpdateLabel ();
				AdjustSize ();
			}
		}

		public string Markup {
			get {
				return text;
			}
			set {
				hasMarkup = true;
				text = value;
				AddLabel ();
				UpdateLabel ();
				AdjustSize ();
			}
		}

		public TaskSeverity? Severity {
			get { return severity; }
			set {
				severity = value;
				AddLabel ();
				UpdateLabel ();

				if (severity.HasValue) {
					Theme.Padding = 3;
					Theme.CornerRadius = 3;

					switch (severity.Value) {
					case TaskSeverity.Information:
					case TaskSeverity.Comment:
						Theme.SetBackgroundColor (Styles.PopoverWindow.InformationBackgroundColor);
						break;

					case TaskSeverity.Error:
						Theme.SetBackgroundColor (Styles.PopoverWindow.ErrorBackgroundColor);
						return;

					case TaskSeverity.Warning:
						Theme.SetBackgroundColor (Styles.PopoverWindow.WarningBackgroundColor);
						return;
					}
				} else
					Theme.SetBackgroundColor (Styles.PopoverWindow.DefaultBackgroundColor);
			}
		}

		void AddLabel ()
		{
			if (label == null) {
				InvokeAsync (() => {
					label = new Label ();
					label.Font = Theme.Font;
					label.Margin = new WidgetSpacing (5, 4, 5, 4);
					Content = label;
				});
			}
		}

		void UpdateLabel ()
		{
			if (label == null)
				return;
			
			string msg = hasMarkup ? text : GLib.Markup.EscapeText (text);
			if (string.IsNullOrEmpty (msg)) {
				label.Text = String.Empty;
				return;
			}

			label.Font = Theme.Font;

			if (severity.HasValue) {
				switch (severity.Value) {
					case TaskSeverity.Information:
					case TaskSeverity.Comment:
						label.TextColor = Styles.PopoverWindow.InformationTextColor;
						break;
					case TaskSeverity.Error:
						label.TextColor = Styles.PopoverWindow.ErrorTextColor;
						break;
					case TaskSeverity.Warning:
						label.TextColor = Styles.PopoverWindow.WarningTextColor;
						break;
				}
			} else
				label.TextColor = Styles.PopoverWindow.DefaultTextColor;

			if (hasMarkup)
				label.Markup = msg;
			else
				label.Text = msg;
		}

		void AdjustSize ()
		{
			// always reset fixed width and wrapping for size calculations
			label.WidthRequest = -1;
			label.Wrap = WrapMode.None;
			var s = label.Surface.GetPreferredSize ();
			if (s.Width > 330) {
				label.Wrap = WrapMode.Word;
				label.WidthRequest = 330;
			} else {
				if (hasMarkup && BackendHost.ToolkitEngine.Type == ToolkitType.XamMac)
					// HACK: Cocoa bug: wrapping needs to be enabled in order to display Attributed string correctly on Mac
					label.Wrap = WrapMode.Word;
				else
					label.Wrap = WrapMode.None;
				label.WidthRequest = -1;
			}
		}
	}
}

