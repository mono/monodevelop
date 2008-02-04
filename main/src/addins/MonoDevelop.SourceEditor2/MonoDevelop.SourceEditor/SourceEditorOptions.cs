// SourceEditorOptions.cs
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

using Pango;

using Mono.TextEditor;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.SourceEditor
{
	public enum EditorFontType {
		/// <summary>
		/// Default Monospace font as set in the user's GNOME font properties
		/// </summary>
		DefaultMonospace,
		
		/// <summary>
		/// Custom font, will need to get the FontName property for more specifics
		/// </summary>
		UserSpecified
	}
	
	public class SourceEditorOptions : TextEditorOptions
	{
		public new static SourceEditorOptions Options {
			get {
				return (SourceEditorOptions)TextEditorOptions.Options;
			}
		}
		
		static SourceEditorOptions ()
		{
			Init ();
		}
		
		static bool initialized = false;
		public static void Init ()
		{
			if (!initialized) {
				Mono.TextEditor.TextEditorOptions.Options = new SourceEditorOptions ();
				initialized = true;
			}
		}
		
		public override bool TabsToSpaces {
			get {
				return PropertyService.Get ("TabsToSpaces", base.TabsToSpaces);
			}
			set {
				if (value != TabsToSpaces) {
					PropertyService.Set ("TabsToSpaces", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}

		public override int IndentationSize {
			get {
				return PropertyService.Get ("TabIndent", base.IndentationSize);
			}
			set {
				if (value != IndentationSize) {
					PropertyService.Set ("TabIndent", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}

		public override int TabSize {
			get {
				return IndentationSize;
			}
			set {
				IndentationSize = value;
			}
		}

//		public override bool ShowIconMargin {
//			get {
//				return showIconMargin;
//			}
//			set {
//				showIconMargin = value;
//			}
//		}

		public override bool ShowLineNumberMargin {
			get {
				return PropertyService.Get ("ShowLineNumberMargin", base.ShowLineNumberMargin);
			}
			set {
				if (value != ShowLineNumberMargin) {
					PropertyService.Set ("ShowLineNumberMargin", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}

		public override bool ShowFoldMargin {
			get {
				return PropertyService.Get ("ShowFoldMargin", base.ShowFoldMargin);
			}
			set {
				if (value != ShowFoldMargin) {
					PropertyService.Set ("ShowFoldMargin", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}

		public override bool ShowInvalidLines {
			get {
				return PropertyService.Get ("ShowInvalidLines", base.ShowInvalidLines);
			}
			set {
				if (value != ShowInvalidLines) {
					PropertyService.Set ("ShowInvalidLines", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}

		public override bool ShowTabs {
			get {
				return PropertyService.Get ("ShowTabs", base.ShowTabs);
			}
			set {
				if (value != ShowTabs) {
					PropertyService.Set ("ShowTabs", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}

		public override bool ShowEolMarkers {
			get {
				return PropertyService.Get ("ShowEolMarkers", base.ShowEolMarkers);
			}
			set {
				if (value != ShowEolMarkers) {
					PropertyService.Set ("ShowEolMarkers", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}

		public override bool HighlightCaretLine {
			get {
				return PropertyService.Get ("HighlightCaretLine", base.HighlightCaretLine);
			}
			set {
				if (value != HighlightCaretLine) {
					PropertyService.Set ("HighlightCaretLine", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}

		public override bool ShowSpaces {
			get {
				return PropertyService.Get ("ShowSpaces", base.ShowSpaces);
			}
			set {
				if (value != ShowSpaces) {
					PropertyService.Set ("ShowSpaces", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		public override bool EnableSyntaxHighlighting {
			get {
				return PropertyService.Get ("EnableSyntaxHighlighting", true);
			}
			set {
				if (value != EnableSyntaxHighlighting) {
					PropertyService.Set ("EnableSyntaxHighlighting", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		public bool AutoInsertTemplates {
			get {
				return PropertyService.Get ("AutoInsertTemplates", false);
			}
			set {
				if (value != AutoInsertTemplates) {
					PropertyService.Set ("AutoInsertTemplates", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		public bool AutoInsertMatchingBracket {
			get {
				return PropertyService.Get ("AutoInsertMatchingBracket", false);
			}
			set {
				if (value != AutoInsertMatchingBracket) {
					PropertyService.Set ("AutoInsertMatchingBracket", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		public bool EnableCodeCompletion {
			get {
				return PropertyService.Get ("EnableCodeCompletion", true);
			}
			set {
				if (value != EnableCodeCompletion) {
					PropertyService.Set ("EnableCodeCompletion", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		public bool EnableQuickFinder {
			get {
				return PropertyService.Get ("EnableQuickFinder", true);
			}
			set {
				if (value != EnableQuickFinder) {
					PropertyService.Set ("EnableQuickFinder", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		public bool UnderlineErrors {
			get {
				return PropertyService.Get ("UnderlineErrors", true);
			}
			set {
				if (value != UnderlineErrors) {
					PropertyService.Set ("UnderlineErrors", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		public override bool HighlightMatchingBracket {
			get {
				return PropertyService.Get ("HighlightMatchingBracket", true);
			}
			set {
				if (value != HighlightMatchingBracket) {
					PropertyService.Set ("HighlightMatchingBracket", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		public override int RulerColumn {
			get {
				return PropertyService.Get ("RulerColumn", base.RulerColumn);
			}
			set {
				if (value != RulerColumn) {
					PropertyService.Set ("RulerColumn", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}

		public override bool ShowRuler {
			get {
				return PropertyService.Get ("ShowRuler", base.ShowRuler);
			}
			set {
				if (value != ShowRuler) {
					PropertyService.Set ("ShowRuler", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}

		public override bool AutoIndent {
			get {
				return IndentStyle != MonoDevelop.Ide.Gui.Content.IndentStyle.None;
			}
			set {
				throw new NotSupportedException ("Use property 'IndentStyle' instead.");
			}
		}
		
		public IndentStyle IndentStyle {
			get {
				return PropertyService.Get ("IndentStyle", MonoDevelop.Ide.Gui.Content.IndentStyle.Smart);
			}
			set {
				if (value != IndentStyle) {
					PropertyService.Set ("IndentStyle", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		public EditorFontType EditorFontType {
			get {
				return PropertyService.Get ("EditorFontType", MonoDevelop.SourceEditor.EditorFontType.DefaultMonospace);
			}
			set {
				if (value != EditorFontType) {
					PropertyService.Set ("EditorFontType", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		public override string FontName {
			get {
				return PropertyService.Get ("FontName", "Mono 10");
			}
			set {
				if (value != FontName) {
					PropertyService.Set ("FontName", !String.IsNullOrEmpty (value) ? value : "Mono 10");
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		public string ColorSheme {
			get {
				return PropertyService.Get ("ColorSheme", "Default");
			}
			set {
				if (value != ColorSheme) {
					PropertyService.Set ("ColorSheme", !String.IsNullOrEmpty (value) ? value : "Default");
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		public override FontDescription Font {
			get {
				switch (EditorFontType) {
				case EditorFontType.DefaultMonospace:
					try {
						return FontDescription.FromString (IdeApp.Services.PlatformService.DefaultMonospaceFont);
					} catch (Exception ex) {
						LoggingService.LogWarning ("Could not load platform's default monospace font.", ex);
						goto default;
					}
//				case "__default_sans":
//					return new Gtk.Label ("").Style.FontDescription;
				default:
					return FontDescription.FromString (FontName);
				}
			}
		}
	}
}
