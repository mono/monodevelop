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
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.SourceEditor
{
	public class LanguageItemWindow: MonoDevelop.Components.TooltipWindow
	{
		Pango.FontDescription fontDescription;
		
		public bool IsEmpty { get; set; }
		
		public LanguageItemWindow (ExtensibleTextEditor ed, Gdk.ModifierType modifierState, ResolveResult result, string errorInformations, ICompilationUnit unit)
		{
			ProjectDom dom = ed.ProjectDom;
			Ambience ambience = AmbienceService.GetAmbience (ed.Document.MimeType);
			
			string tooltip = null;
			if (result != null && ed.TextEditorResolverProvider != null) {
				tooltip = ed.TextEditorResolverProvider.CreateTooltip (dom, unit, result, errorInformations, ambience, modifierState);
				if (result.ResolveErrors.Count > 0) {
					StringBuilder sb = new StringBuilder ();
					sb.Append (tooltip);
					sb.AppendLine ();
					sb.AppendLine ();
					sb.AppendLine (GettextCatalog.GetPluralString ("Error:", "Errors:", result.ResolveErrors.Count));
					for (int i = 0; i < result.ResolveErrors.Count; i++) {
						sb.Append ('\t');
						sb.Append (result.ResolveErrors[i]);
						if (i + 1 < result.ResolveErrors.Count) 
							sb.AppendLine ();
					}
					tooltip = sb.ToString ();
				}
			} else {
				tooltip = errorInformations;
			}
			if (string.IsNullOrEmpty (tooltip)) {
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
			if (fontDescription != null) {
				fontDescription.Dispose ();
			}
			fontDescription = new Gtk.Label ("").Style.FontDescription.Copy ();
			fontDescription.Size = DefaultSourceEditorOptions.Instance.Font.Size;
			label.FontDescription = fontDescription;
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			
			if (fontDescription != null) {
				fontDescription.Dispose ();
				fontDescription = null;
			}
		}
		
		
/*		static string GetCref (string cref)
		{
			if (cref == null)
				return "";
			
			if (cref.Length < 2)
				return cref;
			
			if (cref.Substring(1, 1) == ":")
				return cref.Substring (2, cref.Length - 2);
			
			return cref;
		}*/
	}
}
