//
// TooltipPopoverWindow.cs
//
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
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
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Components
{
	public class TooltipPopoverWindow: PopoverWindow
	{
		Gtk.Label label;
		TaskSeverity? severity;
		bool hasMarkup;
		string text;
		Gtk.Alignment alignment;

		public TooltipPopoverWindow ()
		{
			Theme.SetFlatColor (Styles.PopoverWindow.DefaultBackgroundColor);
			Theme.BorderColor = Styles.PopoverWindow.DefaultBorderColor;
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

				UpdateLabel ();

				if (severity.HasValue) {
					Theme.Padding = 3;
					Theme.CornerRadius = 3;
					Theme.BorderColor = new Cairo.Color (0, 0, 0, 0);

					alignment.SetPadding (4, 4, 6, 6);

					var f = Style.FontDescription.Copy ();
					f.Size = ((f.Size / (int)Pango.Scale.PangoScale) - 1) * (int)Pango.Scale.PangoScale;
					label.ModifyFont (f);

					switch (severity.Value) {
					case TaskSeverity.Information:
						Theme.SetFlatColor (Styles.PopoverWindow.InformationBackgroundColor);
						Theme.BorderColor = Styles.PopoverWindow.InformationBorderColor;
						break;

					case TaskSeverity.Comment:
						Theme.SetFlatColor (Styles.PopoverWindow.InformationBackgroundColor);
						Theme.BorderColor = Styles.PopoverWindow.InformationBorderColor;
						break;

					case TaskSeverity.Error:
						Theme.SetFlatColor (Styles.PopoverWindow.ErrorBackgroundColor);
						Theme.BorderColor = Styles.PopoverWindow.ErrorBorderColor;
						return;

					case TaskSeverity.Warning:
						Theme.SetFlatColor (Styles.PopoverWindow.WarningBackgroundColor);
						Theme.BorderColor = Styles.PopoverWindow.WarningBorderColor;
						return;
					}
				} else {
					Theme.SetFlatColor (Styles.PopoverWindow.DefaultBackgroundColor);
					Theme.BorderColor = Styles.PopoverWindow.DefaultBorderColor;
				}
			}
		}

		void AddLabel ()
		{
			if (label == null) {
				alignment = new Gtk.Alignment (0.5f, 0.5f, 1f, 1f);
				alignment.SetPadding (6, 6, 6, 6);
				label = new Gtk.Label ();
				alignment.Add (label);
				ContentBox.Add (alignment);
				alignment.ShowAll ();
			}
		}

		void UpdateLabel ()
		{
			if (severity.HasValue) {
				string msg = hasMarkup ? text : GLib.Markup.EscapeText (text);

				switch (severity.Value) {
				case TaskSeverity.Information:
					label.Markup = "<b><span color='" + CairoExtensions.ColorGetHex (Styles.PopoverWindow.InformationTextColor) + "'>" + msg + "</span></b>";
					return;

				case TaskSeverity.Comment:
					label.Markup = "<b><span color='" + CairoExtensions.ColorGetHex (Styles.PopoverWindow.InformationTextColor) + "'>" + msg + "</span></b>";
					return;

				case TaskSeverity.Error:
					label.Markup = "<b><span color='" + CairoExtensions.ColorGetHex (Styles.PopoverWindow.ErrorTextColor) + "'>" + msg + "</span></b>";
					return;

				case TaskSeverity.Warning:
					label.Markup = "<b><span color='" + CairoExtensions.ColorGetHex (Styles.PopoverWindow.WarningTextColor) + "'>" + msg + "</span></b>";
					return;
				}
			}

			if (hasMarkup)
				label.Markup = text;
			else
				label.Text = text;
		}

		void AdjustSize ()
		{
			if (label.SizeRequest ().Width > 330) {
				label.Wrap = true;
				label.WidthRequest = 330;
			} else {
				label.Wrap = false;
				label.WidthRequest = -1;
			}
			RepositionWindow ();
		}
	}
}

