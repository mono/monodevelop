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
using MonoDevelop.Ide.Fonts;

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
			Theme.SetBackgroundColor (Styles.PopoverWindow.DefaultBackgroundColor.ToCairoColor ());
			Theme.Font = FontService.SansFont.CopyModified (Styles.FontScale11);
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

					alignment.SetPadding (4, 5, 4, 4);

					label.ModifyFont (FontService.SansFont.CopyModified (Ide.Gui.Styles.FontScale11));

					switch (severity.Value) {
					case TaskSeverity.Information:
						Theme.SetBackgroundColor (Styles.PopoverWindow.InformationBackgroundColor.ToCairoColor ());
						break;

					case TaskSeverity.Comment:
						Theme.SetBackgroundColor (Styles.PopoverWindow.InformationBackgroundColor.ToCairoColor ());
						break;

					case TaskSeverity.Error:
						Theme.SetBackgroundColor (Styles.PopoverWindow.ErrorBackgroundColor.ToCairoColor ());
						return;

					case TaskSeverity.Warning:
						Theme.SetBackgroundColor (Styles.PopoverWindow.WarningBackgroundColor.ToCairoColor ());
						return;
					}
				} else {
					Theme.SetBackgroundColor (Styles.PopoverWindow.DefaultBackgroundColor.ToCairoColor ());
				}
			}
		}

		void AddLabel ()
		{
			if (label == null) {
				alignment = new Gtk.Alignment (0.5f, 0.5f, 1f, 1f);
				alignment.SetPadding (4, 5, 4, 4);
				label = new Gtk.Label ();
				label.ModifyFont (Theme.Font);
				alignment.Add (label);
				ContentBox.Add (alignment);
				alignment.ShowAll ();
			}
		}

		void UpdateLabel ()
		{
			string msg = hasMarkup ? text : GLib.Markup.EscapeText (text);

			if (severity.HasValue) {
				switch (severity.Value) {
				case TaskSeverity.Information:
					label.Markup = "<span font='" + Theme.Font.ToString () + "' color='" + Styles.ColorGetHex (Styles.PopoverWindow.InformationTextColor) + "'>" + msg + "</span>";
					return;

				case TaskSeverity.Comment:
					label.Markup = "<span font='" + Theme.Font.ToString () + "' color='" + Styles.ColorGetHex (Styles.PopoverWindow.InformationTextColor) + "'>" + msg + "</span>";
					return;

				case TaskSeverity.Error:
					label.Markup = "<span font='" + Theme.Font.ToString () + "' color='" + Styles.ColorGetHex (Styles.PopoverWindow.ErrorTextColor) + "'>" + msg + "</span>";
					return;

				case TaskSeverity.Warning:
					label.Markup = "<span font='" + Theme.Font.ToString () + "' color='" + Styles.ColorGetHex (Styles.PopoverWindow.WarningTextColor) + "'>" + msg + "</span>";
					return;
				}
			}

			label.Markup = "<span font='" + Theme.Font.ToString () + "' color='" + Styles.ColorGetHex (Styles.PopoverWindow.DefaultTextColor) + "'>" + msg + "</span>";
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

