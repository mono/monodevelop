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
		public bool IsEmpty {
			get; 
			set;
		}
		
		public LanguageItemWindow (ExtensibleTextEditor ed, Gdk.ModifierType modifierState, ResolveResult result, string errorInformations, ICompilationUnit unit)
		{
			ProjectDom dom = ed.ProjectDom;
			Ambience ambience = AmbienceService.GetAmbience (ed.Document.MimeType);
			
			string tooltip = null;
			if (result != null && ed.TextEditorResolverProvider != null)
				tooltip = ed.TextEditorResolverProvider.CreateTooltip (dom, unit, result, errorInformations, ambience, modifierState);
			if (string.IsNullOrEmpty (tooltip)) {
				IsEmpty = true;
				return;
			}
			
			MonoDevelop.Components.FixedWidthWrapLabel lab = new MonoDevelop.Components.FixedWidthWrapLabel ();
			Pango.FontDescription font =  new Gtk.Label ("").Style.FontDescription;
			font.Size = DefaultSourceEditorOptions.Instance.Font.Size;
			lab.FontDescription = font;
			lab.Wrap = Pango.WrapMode.WordChar;
			lab.Indent = -20;
			lab.BreakOnCamelCasing = true;
			lab.BreakOnPunctuation = true;
			lab.Markup = tooltip;
			this.BorderWidth = 3;
			Add (lab);
			
			EnableTransparencyControl = true;
		}
		
		//return the real width
		public int SetMaxWidth (int maxWidth)
		{
			MonoDevelop.Components.FixedWidthWrapLabel l = (MonoDevelop.Components.FixedWidthWrapLabel)Child;
			l.MaxWidth = maxWidth;
			return l.RealWidth;
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
