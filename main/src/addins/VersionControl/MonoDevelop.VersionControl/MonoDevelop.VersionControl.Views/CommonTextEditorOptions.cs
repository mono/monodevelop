
// 
// ComparisonWidget.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using Gtk;
using Gdk;
using System.Collections.Generic;
using Mono.TextEditor;
using MonoDevelop.Ide;
using MonoDevelop.Components;
using System.ComponentModel;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Fonts;
using MonoDevelop.Ide.Editor.Extension;

namespace MonoDevelop.VersionControl.Views
{
	// Code snatched from SourceEditor
	class CommonTextEditorOptions : TextEditorOptions, Mono.TextEditor.ITextEditorOptions
	{
		static CommonTextEditorOptions instance;
		//static TextStylePolicy defaultPolicy;
		static bool inited;

		public static CommonTextEditorOptions Instance {
			get { return instance; }
		}

		static CommonTextEditorOptions ()
		{
			Init ();
		}

		public static void Init ()
		{
			if (inited)
				return;
			inited = true;

			TextStylePolicy policy = MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<TextStylePolicy> ("text/plain");
			instance = new CommonTextEditorOptions (policy);
			MonoDevelop.Projects.Policies.PolicyService.DefaultPolicies.PolicyChanged += instance.HandlePolicyChanged;
		}

		void HandlePolicyChanged (object sender, MonoDevelop.Projects.Policies.PolicyChangedEventArgs args)
		{
			TextStylePolicy pol = MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<TextStylePolicy> ("text/plain");
			UpdateStylePolicy (pol);
		}

		CommonTextEditorOptions (MonoDevelop.Ide.Gui.Content.TextStylePolicy currentPolicy)
		{
			UpdateStylePolicy (currentPolicy);
			MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.Changed += delegate(object sender, EventArgs e) {
				OnChanged (e);
			};
		}

		public override void Dispose()
		{
			FontService.RemoveCallback (UpdateFont);
		}

		void UpdateFont ()
		{
			base.FontName = FontName;
			base.GutterFontName = GutterFontName;
			this.OnChanged (EventArgs.Empty);

		}

		void UpdateStylePolicy (MonoDevelop.Ide.Gui.Content.TextStylePolicy currentPolicy)
		{
			this.defaultEolMarker = TextStylePolicy.GetEolMarker (currentPolicy.EolMarker);
			base.TabsToSpaces          = currentPolicy.TabsToSpaces; // PropertyService.Get ("TabsToSpaces", false);
			base.IndentationSize       = currentPolicy.TabWidth; //PropertyService.Get ("TabIndent", 4);
			base.RulerColumn           = currentPolicy.FileWidth; //PropertyService.Get ("RulerColumn", 80);
			base.AllowTabsAfterNonTabs = !currentPolicy.NoTabsAfterNonTabs; //PropertyService.Get ("AllowTabsAfterNonTabs", true);
			base.RemoveTrailingWhitespaces = currentPolicy.RemoveTrailingWhitespace; //PropertyService.Get ("RemoveTrailingWhitespaces", true);
		}

		#region new options

		public bool EnableAutoCodeCompletion {
			get { return CompletionTextEditorExtension.EnableAutoCodeCompletion; }
			set { CompletionTextEditorExtension.EnableAutoCodeCompletion.Set (value); }
		}

		public bool DefaultRegionsFolding {
			get {
				return MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.DefaultRegionsFolding;
			}
			set {
				MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.DefaultRegionsFolding = value;
			}
		}

		public bool DefaultCommentFolding {
			get {
				return MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.DefaultCommentFolding;
			}
			set {
				MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.DefaultCommentFolding = value;
			}
		}

		public bool EnableSemanticHighlighting {
			get {
				return MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.EnableSemanticHighlighting;
			}
			set {
				MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.EnableSemanticHighlighting = value;
			}
		}

		public bool TabIsReindent {
			get {
				return MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.TabIsReindent;
			}
			set {
				MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.TabIsReindent = value;
			}
		}

		public bool AutoInsertMatchingBracket {
			get {
				return MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.AutoInsertMatchingBracket;
			}
			set {
				MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.AutoInsertMatchingBracket = value;
			}
		}

		public bool SmartSemicolonPlacement {
			get {
				return MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.SmartSemicolonPlacement;
			}
			set {
				MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.SmartSemicolonPlacement = value;
			}
		}

		public bool UnderlineErrors {
			get {
				return MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.UnderlineErrors; 
			}
			set {
				MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.UnderlineErrors = value;
			}
		}

		public override IndentStyle IndentStyle {
			get {
				return (IndentStyle)MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.IndentStyle;
			}
			set {
				MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.IndentStyle = (MonoDevelop.Ide.Editor.IndentStyle)value;
			}
		}

		public bool EnableHighlightUsages {
			get {
				return MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.EnableHighlightUsages;
			}
			set {
				MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.EnableHighlightUsages = value;
			}
		}

		MonoDevelop.Ide.Editor.LineEndingConversion lineEndingConversion;
		public MonoDevelop.Ide.Editor.LineEndingConversion LineEndingConversion {
			get {
				return MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.LineEndingConversion;
			}
			set {
				MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.LineEndingConversion = value;
			}
		}


		#endregion
		public bool UseViModes {
			get {
				return MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.UseViModes;
			}
			set {
				MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.UseViModes = value;
			}
		}

		public bool OnTheFlyFormatting {
			get {
				return MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.OnTheFlyFormatting;
			}
			set {
				MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.OnTheFlyFormatting = value;
			}
		}

		#region old options
		string defaultEolMarker;
		public override string DefaultEolMarker {
			get { return defaultEolMarker; }
		}

		public MonoDevelop.Ide.Editor.ControlLeftRightMode ControlLeftRightMode {
			get {
				return MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.ControlLeftRightMode;
			}
			set {
				MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.ControlLeftRightMode = value;
			}
		}

		IWordFindStrategy wordFindStrategy = null;
		public override IWordFindStrategy WordFindStrategy {
			get {
				if (wordFindStrategy == null)
					SetWordFindStrategy ();
				return this.wordFindStrategy;
			}
			set {
				throw new System.NotImplementedException ();
			}
		}

		void SetWordFindStrategy ()
		{
			if (UseViModes) {
				this.wordFindStrategy = new Mono.TextEditor.Vi.ViWordFindStrategy ();
				return;
			}

			switch (ControlLeftRightMode) {
			case MonoDevelop.Ide.Editor.ControlLeftRightMode.MonoDevelop:
				this.wordFindStrategy = new EmacsWordFindStrategy (true);
				break;
			case MonoDevelop.Ide.Editor.ControlLeftRightMode.Emacs:
				this.wordFindStrategy = new EmacsWordFindStrategy (false);
				break;
			case MonoDevelop.Ide.Editor.ControlLeftRightMode.SharpDevelop:
				this.wordFindStrategy = new SharpDevelopWordFindStrategy ();
				break;
			}
		}

		public override bool ShowLineNumberMargin {
			get {
				return MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.ShowLineNumberMargin;
			}
			set {
				MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.ShowLineNumberMargin = value;
			}
		}

		public override bool ShowFoldMargin {
			get {
				return MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.ShowFoldMargin;
			}
			set {
				MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.ShowFoldMargin = value;
			}
		}

		public override bool HighlightCaretLine {
			get {
				return MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.HighlightCaretLine;
			}
			set {
				MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.HighlightCaretLine = value;
			}
		}

		public override bool EnableSyntaxHighlighting {
			get {
				return MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.EnableSyntaxHighlighting;
			}
			set {
				MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.EnableSyntaxHighlighting = value;
			}
		}

		public override bool HighlightMatchingBracket {
			get {
				return MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.HighlightMatchingBracket;
			}
			set {
				MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.HighlightMatchingBracket = value;
			}
		}

		public override bool ShowRuler {
			get {
				return MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.ShowRuler;
			}
			set {
				MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.ShowRuler = value;
			}
		}

		public override bool EnableAnimations {
			get {
				return MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.EnableAnimations;
			}
			set {
				MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.EnableAnimations = value;
			}
		}

		public override bool DrawIndentationMarkers {
			get {
				return MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.DrawIndentationMarkers;
			}
			set {
				MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.DrawIndentationMarkers = value;
			}
		}

		PropertyWrapper<ShowWhitespaces> showWhitespaces = new PropertyWrapper<ShowWhitespaces> ("ShowWhitespaces", ShowWhitespaces.Never);
		public override ShowWhitespaces ShowWhitespaces {
			get {
				return showWhitespaces;
			}
			set {
				if (showWhitespaces.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		PropertyWrapper<IncludeWhitespaces> includeWhitespaces = new PropertyWrapper<IncludeWhitespaces> ("IncludeWhitespaces", IncludeWhitespaces.All);
		public override IncludeWhitespaces IncludeWhitespaces {
			get {
				return includeWhitespaces;
			}
			set {
				if (includeWhitespaces.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		public override bool WrapLines {
			get {
				return MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.WrapLines;
			}
			set {
				MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.WrapLines = value;
			}
		}

		public override bool EnableQuickDiff {
			get {
				return MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.EnableQuickDiff;
			}
			set {
				MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.EnableQuickDiff = value;
			}
		}

		public override string FontName {
			get {
				return MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.FontName;
			}
			set {
				throw new InvalidOperationException ("Set font through font service");
			}
		}

		public override string GutterFontName {
			get {
				return MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.GutterFontName;
			}
			set {
				throw new InvalidOperationException ("Set font through font service");
			}
		}

		public override string ColorScheme {
			get {
				return MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.ColorScheme;
			}
			set {
				MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.ColorScheme = value;
			}
		}

		public override bool GenerateFormattingUndoStep {
			get {
				return MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.GenerateFormattingUndoStep;
			}
			set {
				MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.GenerateFormattingUndoStep = value;
			}
		}

		#endregion
	}
}

