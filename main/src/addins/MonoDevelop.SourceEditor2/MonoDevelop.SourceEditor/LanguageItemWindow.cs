// LanguageItemWindow.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

using Gtk;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using MonoDevelop.Ide.Fonts;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.SourceEditor
{
	class LanguageItemWindow: MonoDevelop.Components.TooltipWindow
	{
		public bool IsEmpty { get; set; }
		public object Tag { get; set; }

		public LanguageItemWindow (ExtensibleTextEditor ed, Xwt.ModifierKeys modifierState, object result, string errorInformations, object unit)
		{
			string tooltip = errorInformations;
			if (string.IsNullOrEmpty (tooltip)|| tooltip == "?") {
				IsEmpty = true;
				return;
			}

			var label = new MonoDevelop.Components.FixedWidthWrapLabel () {
				Wrap = Pango.WrapMode.WordChar,
				Indent = -20,
				BreakOnCamelCasing = true,
				BreakOnPunctuation = true,
				Markup = tooltip,
			};
			this.BorderWidth = 3;
			Add (label);
			UpdateFont (label);
			
			EnableTransparencyControl = true;
		}
		
		//return the real width
		public int SetMaxWidth (int maxWidth)
		{
			var label = Child as MonoDevelop.Components.FixedWidthWrapLabel;
			if (label == null)
				return Allocation.Width;
			label.MaxWidth = maxWidth;
			return label.RealWidth;
		}
		
		protected override void OnStyleSet (Style previous_style)
		{
			base.OnStyleSet (previous_style);
			UpdateFont (Child as MonoDevelop.Components.FixedWidthWrapLabel);
		}
		
		void UpdateFont (MonoDevelop.Components.FixedWidthWrapLabel label)
		{
			if (label == null)
				return;
			label.FontDescription = FontService.GetFontDescription ("Pad");
			
		}
	}
}
