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
using System.Collections.Generic;

using Pango;

using Mono.TextEditor;
using Mono.TextEditor.Highlighting;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Gui.Completion;

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
			if (initialized) 
				return;
			Mono.TextEditor.TextEditorOptions.Options = new SourceEditorOptions ();
			initialized = true;
			Mono.TextEditor.TextEditorOptions.Options.Changed += delegate {
				if (SourceEditorOptions.Options.EnableSemanticHighlighting) {
					SyntaxModeService.GetSyntaxMode ("text/x-csharp").AddSemanticRule (new HighlightPropertiesRule ());
				} else {
					SyntaxModeService.GetSyntaxMode ("text/x-csharp").RemoveSemanticRule (typeof (HighlightPropertiesRule));
				}
			};
			
		}
		
		public SourceEditorOptions ()
		{
			this.enableSemanticHighlighting = PropertyService.Get ("EnableSemanticHighlighting", false);
			this.autoInsertTemplates        = PropertyService.Get ("AutoInsertTemplates", false);
			this.autoInsertMatchingBracket  = PropertyService.Get ("AutoInsertMatchingBracket", false);
			this.enableCodeCompletion       = PropertyService.Get ("EnableCodeCompletion", true);
			this.enableQuickFinder          = PropertyService.Get ("EnableQuickFinder", true);
			this.underlineErrors            = PropertyService.Get ("UnderlineErrors", true);
			this.indentStyle                = PropertyService.Get ("IndentStyle", MonoDevelop.Ide.Gui.Content.IndentStyle.Smart);
			this.editorFontType             = PropertyService.Get ("EditorFontType", MonoDevelop.SourceEditor.EditorFontType.DefaultMonospace);
			this.tabIsReindent              = PropertyService.Get ("TabIsReindent", false);
			base.TabsToSpaces          = PropertyService.Get ("TabsToSpaces", false);
			base.IndentationSize       = PropertyService.Get ("TabIndent", 4);
			base.ShowLineNumberMargin  = PropertyService.Get ("ShowLineNumberMargin", true);
			base.ShowFoldMargin        = PropertyService.Get ("ShowFoldMargin", true);
			base.ShowInvalidLines      = PropertyService.Get ("ShowInvalidLines", true);
			base.ShowTabs              = PropertyService.Get ("ShowTabs", false);
			base.ShowEolMarkers        = PropertyService.Get ("ShowEolMarkers", false);
			base.HighlightCaretLine    = PropertyService.Get ("HighlightCaretLine", false);
			base.ShowSpaces            = PropertyService.Get ("ShowSpaces", false);
			base.EnableSyntaxHighlighting = PropertyService.Get ("EnableSyntaxHighlighting", true);
			base.HighlightMatchingBracket = PropertyService.Get ("HighlightMatchingBracket", true);
			base.RulerColumn              = PropertyService.Get ("RulerColumn", 80);
			base.ShowRuler                = PropertyService.Get ("ShowRuler", false);
			base.FontName                 = PropertyService.Get ("FontName", "Mono 10");
			base.ColorScheme               =  PropertyService.Get ("ColorScheme", "Default");
			this.DefaultRegionsFolding      =  PropertyService.Get ("DefaultRegionsFolding", false);
			this.DefaultCommentFolding      =  PropertyService.Get ("DefaultCommentFolding", true);
			base.RemoveTrailingWhitespaces = PropertyService.Get ("RemoveTrailingWhitespaces", true);
		}
		
		#region new options
		bool defaultRegionsFolding;
		public bool DefaultRegionsFolding {
			get {
				return defaultRegionsFolding;
			}
			set {
				if (value != this.defaultRegionsFolding) {
					this.defaultRegionsFolding = value;
					PropertyService.Set ("DefaultRegionsFolding", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		bool defaultCommentFolding;
		public bool DefaultCommentFolding {
			get {
				return defaultCommentFolding;
			}
			set {
				if (value != this.defaultCommentFolding) {
					this.defaultCommentFolding = value;
					PropertyService.Set ("DefaultCommentFolding", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		bool enableSemanticHighlighting;
		public bool EnableSemanticHighlighting {
			get {
				return enableSemanticHighlighting;
			}
			set {
				if (value != this.enableSemanticHighlighting) {
					this.enableSemanticHighlighting = value;
					PropertyService.Set ("EnableSemanticHighlighting", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		bool autoInsertTemplates;
		public bool AutoInsertTemplates {
			get {
				return autoInsertTemplates;
			}
			set {
				if (value != this.autoInsertTemplates) {
					this.autoInsertTemplates = value;
					PropertyService.Set ("AutoInsertTemplates", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		bool tabIsReindent;
		public bool TabIsReindent {
			get {
				return tabIsReindent;
			}
			set {
				if (value != this.tabIsReindent) {
					this.tabIsReindent = value;
					PropertyService.Set ("TabIsReindent", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		bool autoInsertMatchingBracket;
		public bool AutoInsertMatchingBracket {
			get {
				return autoInsertMatchingBracket;
			}
			set {
				if (value != this.autoInsertMatchingBracket) {
					this.autoInsertMatchingBracket = value;
					PropertyService.Set ("AutoInsertMatchingBracket", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		bool enableCodeCompletion;
		public bool EnableCodeCompletion {
			get {
				return enableCodeCompletion;
			}
			set {
				if (value != this.enableCodeCompletion) {
					this.enableCodeCompletion = value;
					PropertyService.Set ("EnableCodeCompletion", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		bool enableQuickFinder;
		public bool EnableQuickFinder {
			get {
				return enableQuickFinder;
			}
			set {
				if (value != this.enableQuickFinder) {
					this.enableQuickFinder = value;
					PropertyService.Set ("EnableQuickFinder", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		bool underlineErrors;
		public bool UnderlineErrors {
			get {
				return underlineErrors; 
			}
			set {
				if (value != this.underlineErrors) {
					this.underlineErrors = value;
					PropertyService.Set ("UnderlineErrors", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		IndentStyle indentStyle;
		public IndentStyle IndentStyle {
			get {
				return indentStyle;
			}
			set {
				if (value != this.indentStyle) {
					this.indentStyle = value;
					PropertyService.Set ("IndentStyle", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		EditorFontType editorFontType;
		public EditorFontType EditorFontType {
			get {
				return editorFontType;
			}
			set {
				if (value != this.editorFontType) {
					this.editorFontType = value;
					PropertyService.Set ("EditorFontType", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		#endregion
		
		#region old options
		public override bool TabsToSpaces {
			set {
				PropertyService.Set ("TabsToSpaces", value);
				base.TabsToSpaces = value;
			}
		}
		
		public override int IndentationSize {
			set {
				PropertyService.Set ("TabIndent", value);
				base.IndentationSize = value;
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
		
		public override bool RemoveTrailingWhitespaces {
			set {
				PropertyService.Set ("RemoveTrailingWhitespaces", value);
				base.RemoveTrailingWhitespaces = value;
			}
		}

		
		public override bool ShowLineNumberMargin {
			set {
				PropertyService.Set ("ShowLineNumberMargin", value);
				base.ShowLineNumberMargin = value;
			}
		}
		
		public override bool ShowFoldMargin {
			set {
				PropertyService.Set ("ShowFoldMargin", value);
				base.ShowFoldMargin = value;
			}
		}
		
		public override bool ShowInvalidLines {
			set {
				PropertyService.Set ("ShowInvalidLines", value);
				base.ShowInvalidLines = value;
			}
		}
		
		public override bool ShowTabs {
			set {
				PropertyService.Set ("ShowTabs", value);
				base.ShowTabs = value;
			}
		}
		
		public override bool ShowEolMarkers {
			set {
				PropertyService.Set ("ShowEolMarkers", value);
				base.ShowEolMarkers = value;
			}
		}
		
		public override bool HighlightCaretLine {
			set {
				PropertyService.Set ("HighlightCaretLine", value);
				base.HighlightCaretLine = value;
			}
		}
		
		public override bool ShowSpaces {
			set {
				PropertyService.Set ("ShowSpaces", value);
				base.ShowSpaces = value;
			}
		}
		
		public override bool EnableSyntaxHighlighting {
			set {
				PropertyService.Set ("EnableSyntaxHighlighting", value);
				base.EnableSyntaxHighlighting = value;
			}
		}
		
		public override bool HighlightMatchingBracket {
			set {
				PropertyService.Set ("HighlightMatchingBracket", value);
				base.HighlightMatchingBracket = value;
			}
		}
		
		public override int RulerColumn {
			set {
				PropertyService.Set ("RulerColumn", value);
				base.RulerColumn = value;
			}
		}
		
		public override bool ShowRuler {
			set {
				PropertyService.Set ("ShowRuler", value);
				base.ShowRuler = value;
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
		
		public override string FontName {
			get {
				if (EditorFontType == EditorFontType.DefaultMonospace) {
					string font = IdeApp.Services.PlatformService.DefaultMonospaceFont;
					if (String.IsNullOrEmpty (font))
						return DEFAULT_FONT;
					else
						return font;
				}
				return base.FontName;
			}
			set {
				string newName = !String.IsNullOrEmpty (value) ? value : DEFAULT_FONT;
				PropertyService.Set ("FontName", newName);
				base.FontName = newName;
			}
		}
		
		public override string ColorScheme {
			set {
				string newColorScheme = !String.IsNullOrEmpty (value) ? value : "Default";
				PropertyService.Set ("ColorScheme", newColorScheme);
				base.ColorScheme =  newColorScheme;
			}
		}
		
		#endregion
	}
}
