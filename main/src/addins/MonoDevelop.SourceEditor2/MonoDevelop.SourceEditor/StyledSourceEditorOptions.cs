//
// StyledSourceEditorOptions.cs
// 
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
//
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using Mono.TextEditor;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.SourceEditor
{
	class StyledSourceEditorOptions : TextEditorOptions
	{
		MonoDevelop.Ide.Editor.ITextEditorOptions optionsCore;

		public MonoDevelop.Ide.Editor.ITextEditorOptions OptionsCore {
			get {
				return optionsCore;
			}
			set {
				optionsCore = value;
				OnChanged (EventArgs.Empty);
			}
		}
	
		public StyledSourceEditorOptions (MonoDevelop.Ide.Editor.ITextEditorOptions optionsCore)
		{
			if (optionsCore == null)
				throw new ArgumentNullException ("optionsCore");
			this.optionsCore = optionsCore;
			DefaultSourceEditorOptions.Instance.Changed += HandleChanged;
		}

		public override void Dispose ()
		{
			DefaultSourceEditorOptions.Instance.Changed -= HandleChanged;

			base.Dispose ();
		}

		void HandleChanged (object sender, EventArgs e)
		{
			DisposeFont ();
			OnChanged (EventArgs.Empty);
		}

		#region ITextEditorOptions implementation
		static IWordFindStrategy monoDevelopWordFindStrategy = new EmacsWordFindStrategy (false);
		static IWordFindStrategy emacsWordFindStrategy = new EmacsWordFindStrategy (true);
		static IWordFindStrategy sharpDevelopWordFindStrategy = new SharpDevelopWordFindStrategy ();

		public override IWordFindStrategy WordFindStrategy {
			get {
				switch (DefaultSourceEditorOptions.Instance.WordFindStrategy) {
				case MonoDevelop.Ide.Editor.WordFindStrategy.MonoDevelop:
					return monoDevelopWordFindStrategy;
				case MonoDevelop.Ide.Editor.WordFindStrategy.Emacs:
					return emacsWordFindStrategy;
				case MonoDevelop.Ide.Editor.WordFindStrategy.SharpDevelop:
					return sharpDevelopWordFindStrategy;
				default:
					throw new ArgumentOutOfRangeException ();
				}
			}
			set {
				throw new NotImplementedException ();
			}
		}

		public override bool AllowTabsAfterNonTabs {
			get {
				return DefaultSourceEditorOptions.Instance.AllowTabsAfterNonTabs;
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public override bool HighlightMatchingBracket {
			get {
				return DefaultSourceEditorOptions.Instance.HighlightMatchingBracket;
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public override bool TabsToSpaces {
			get {
				return optionsCore.TabsToSpaces;
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public override int IndentationSize {
			get {
				return optionsCore.IndentationSize;
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public override int TabSize {
			get {
				return optionsCore.TabSize;
			}
			set {
				throw new NotSupportedException ();
			}
		}


		public override bool ShowIconMargin {
			get {
				return optionsCore.ShowIconMargin;
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public override bool ShowLineNumberMargin {
			get {
				return optionsCore.ShowLineNumberMargin;
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public override bool ShowFoldMargin {
			get {
				return optionsCore.ShowFoldMargin;
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public override bool HighlightCaretLine {
			get {
				return optionsCore.HighlightCaretLine;
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public override int RulerColumn {
			get {
				return optionsCore.RulerColumn;
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public override bool ShowRuler {
			get {
				return optionsCore.ShowRuler;
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public override Mono.TextEditor.IndentStyle IndentStyle {
			get {
				if (optionsCore.IndentStyle == MonoDevelop.Ide.Editor.IndentStyle.Smart && optionsCore.RemoveTrailingWhitespaces)
					return Mono.TextEditor.IndentStyle.Virtual;
				return (Mono.TextEditor.IndentStyle)optionsCore.IndentStyle;
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public override bool OverrideDocumentEolMarker {
			get {
				return optionsCore.OverrideDocumentEolMarker;
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public override bool EnableSyntaxHighlighting {
			get {
				return optionsCore.EnableSyntaxHighlighting;
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public override bool EnableAnimations {
			get {
				return DefaultSourceEditorOptions.Instance.HighlightMatchingBracket;
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public override bool EnableQuickDiff {
			get {
				return DefaultSourceEditorOptions.Instance.EnableQuickDiff;
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public override bool DrawIndentationMarkers {
			get {
				return DefaultSourceEditorOptions.Instance.DrawIndentationMarkers;
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public override bool WrapLines {
			get {
				return optionsCore.WrapLines;
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public override string FontName {
			get {
				return optionsCore.FontName;
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public override string GutterFontName {
			get {
				return optionsCore.GutterFontName;
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public override string EditorThemeName {
			get {
				return optionsCore.EditorTheme;
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public override string DefaultEolMarker {
			get {
				return optionsCore.DefaultEolMarker;
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public override Mono.TextEditor.ShowWhitespaces ShowWhitespaces {
			get {
				return (Mono.TextEditor.ShowWhitespaces)optionsCore.ShowWhitespaces;
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public override Mono.TextEditor.IncludeWhitespaces IncludeWhitespaces {
			get {
				return (Mono.TextEditor.IncludeWhitespaces)optionsCore.IncludeWhitespaces;
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public override bool GenerateFormattingUndoStep {
			get {
				return optionsCore.GenerateFormattingUndoStep;
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public override bool EnableSelectionWrappingKeys {
			get {
				return optionsCore.EnableSelectionWrappingKeys;
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public override bool SmartBackspace {
			get {
				return optionsCore.SmartBackspace;
			}
			set {
				throw new NotSupportedException ();
			}
		}
		#endregion


	}
}
