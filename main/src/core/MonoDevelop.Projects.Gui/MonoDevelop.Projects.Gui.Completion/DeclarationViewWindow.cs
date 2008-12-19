//  DeclarationViewWindow.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Reflection;
using System.Collections;
using MonoDevelop.Components;

using Gtk;

namespace MonoDevelop.Projects.Gui.Completion
{
	internal class DeclarationViewWindow : TooltipWindow
	{
		static char[] newline = {'\n'};
		static char[] whitespace = {' '};

		ArrayList overloads;
		int current_overload;
		
		MonoDevelop.Components.FixedWidthWrapLabel headlabel, bodylabel;
		Label helplabel;
		Arrow left, right;
		VBox helpbox;
		
		public string DescriptionMarkup
		{
			get {
			 	if (string.IsNullOrEmpty (bodylabel.Text))
					return headlabel.Text;
				else
					return headlabel.Text + "\n" + bodylabel.Text;
			}
			
			set {
				if (string.IsNullOrEmpty (value)) {
					headlabel.Markup = string.Empty;
					bodylabel.Markup = string.Empty;
					return;
				}

				string[] parts = value.Split (newline, 2);
				headlabel.Markup = "<b>" + parts[0].Trim (whitespace) + "</b>";
				bodylabel.Markup = parts.Length == 2 ?
					"<span size=\"smaller\">" + parts[1].Trim (whitespace) + "</span>" : String.Empty;

				headlabel.Visible = !string.IsNullOrEmpty (headlabel.Text);
				bodylabel.Visible = !string.IsNullOrEmpty (bodylabel.Text);
				//QueueDraw ();
			}
		}

		public bool Multiple
		{
			get {
				return left.Visible;
			}

			set {
				left.Visible = value;
				right.Visible = value;
				helpbox.Visible = value;
				
				//this could go somewhere better, as long as it's after realization
				headlabel.Visible = !string.IsNullOrEmpty (headlabel.Text);
				bodylabel.Visible = !string.IsNullOrEmpty (bodylabel.Text);
			}
		}

		public void AddOverload (string desc)
		{
			overloads.Add (desc);
			if (overloads.Count == 2) {
				Multiple = true;
			}
			ShowOverload ();
		}

		void ShowOverload ()
		{
			DescriptionMarkup = (string)overloads[current_overload];
			helplabel.Markup = String.Format ("<small>{0} of {1} overloads</small>", current_overload + 1, overloads.Count);
		}

		public void OverloadLeft ()
		{
			if (current_overload == 0)
				current_overload = overloads.Count - 1;
			else
				current_overload--;
			ShowOverload ();
		}

		public void OverloadRight ()
		{
			if (current_overload == overloads.Count - 1)
				current_overload = 0;
			else
				current_overload++;
			ShowOverload ();
		}

		public void Clear ()
		{
			overloads.Clear ();
			Multiple = false;
			DescriptionMarkup = String.Empty;
			current_overload = 0;
		}
		
		public void SetFixedWidth (int w)
		{
			if (w != -1) {
				w -= (SizeRequest ().Width - headlabel.SizeRequest ().Width);//  otherWidths ();
				headlabel.MaxWidth = w > 0 ? w : 1;
			} else {
				headlabel.MaxWidth = -1;
			}
			bodylabel.MaxWidth = headlabel.RealWidth > 350 ? headlabel.RealWidth : 350;
			QueueResize ();
		}

		public DeclarationViewWindow () : base ()
		{
			overloads = new ArrayList ();
			this.AllowShrink = false;
			this.AllowGrow = false;
			
			EnableTransparencyControl = true;
			
			headlabel = new MonoDevelop.Components.FixedWidthWrapLabel ();
			headlabel.Indent = -20;
			headlabel.Wrap = Pango.WrapMode.WordChar;
			headlabel.BreakOnCamelCasing = true;
			headlabel.BreakOnPunctuation = true;
			
			bodylabel = new MonoDevelop.Components.FixedWidthWrapLabel ();
			bodylabel.Wrap = Pango.WrapMode.WordChar;
			bodylabel.BreakOnCamelCasing = true;
			bodylabel.BreakOnPunctuation = true;
			
			VBox vb = new VBox (false, 0);
			vb.PackStart (headlabel, true, true, 0);
			vb.PackStart (bodylabel, true, true, 3);

			left = new Arrow (ArrowType.Left, ShadowType.None);
			right = new Arrow (ArrowType.Right, ShadowType.None);

			HBox hb = new HBox (false, 0);
			hb.Spacing = 4;
			hb.PackStart (left, false, true, 0);
			hb.PackStart (vb, true, true, 0);
			hb.PackStart (right, false, true, 0);

			helplabel = new Label (string.Empty);
			helplabel.Xalign = 1;
			
			helpbox = new VBox (false, 0);
			helpbox.PackStart (new HSeparator (), false, true, 0);
			helpbox.PackStart (helplabel, false, true, 0);
			helpbox.BorderWidth = 2;
			
			VBox vb2 = new VBox (false, 0);
			vb2.Spacing = 4;
			vb2.PackStart (hb, true, true, 0);
			vb2.PackStart (helpbox, false, true, 0);
			
			this.Add (vb2);
		}
	}
}
