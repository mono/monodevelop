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
using System.Collections.Generic;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide;
using Mono.TextEditor;

namespace MonoDevelop.SourceEditor
{

	internal class StyledSourceEditorOptions : ISourceEditorOptions
	{
		PolicyContainer policyContainer;
		EventHandler changed;
		IEnumerable<string> mimeTypes;
		TextStylePolicy currentPolicy;

		public StyledSourceEditorOptions (Project styleParent, string mimeType)
		{
			UpdateStyleParent (styleParent, mimeType);
		}

		TextStylePolicy CurrentPolicy {
			get { return currentPolicy; }
		}

		public void UpdateStyleParent (Project styleParent, string mimeType)
		{
			if (policyContainer != null)
				policyContainer.PolicyChanged -= HandlePolicyChanged;

			if (string.IsNullOrEmpty (mimeType))
				mimeType = "text/plain";
			this.mimeTypes = DesktopService.GetMimeTypeInheritanceChain (mimeType);

			if (styleParent != null)
				policyContainer = styleParent.Policies;
			else
				policyContainer = MonoDevelop.Projects.Policies.PolicyService.DefaultPolicies;

			currentPolicy = policyContainer.Get<TextStylePolicy> (mimeTypes);
			policyContainer.PolicyChanged += HandlePolicyChanged;
		}

		void HandlePolicyChanged (object sender, MonoDevelop.Projects.Policies.PolicyChangedEventArgs args)
		{
			currentPolicy = policyContainer.Get<TextStylePolicy> (mimeTypes);
			this.changed (this, EventArgs.Empty);
		}

		public bool OverrideDocumentEolMarker {
			get { return DefaultSourceEditorOptions.Instance.OverrideDocumentEolMarker; }
			set {
				throw new NotSupportedException ();
			}
		}
		
		public string DefaultEolMarker {
			get { return TextStylePolicy.GetEolMarker (CurrentPolicy.EolMarker); }
			set {
				throw new NotSupportedException ();
			}
		}

		public int RulerColumn {
			get { return CurrentPolicy.FileWidth; }
			set {
				throw new NotSupportedException ();
			}
		}

		public int TabSize {
			get { return CurrentPolicy.TabWidth; }
			set {
				throw new NotSupportedException ();
			}
		}

		public bool TabsToSpaces {
			get { return CurrentPolicy.TabsToSpaces; }
			set {
				throw new NotSupportedException ();
			}
		}

		public bool RemoveTrailingWhitespaces {
			get { return CurrentPolicy.RemoveTrailingWhitespace; }
			set {
				throw new NotSupportedException ();
			}
		}

		public bool AllowTabsAfterNonTabs {
			get { return !CurrentPolicy.NoTabsAfterNonTabs; }
			set {
				throw new NotSupportedException ();
			}
		}

		public int IndentationSize {
			get { return CurrentPolicy.IndentWidth; }
			set {
				throw new NotSupportedException ();
			}
		}

		public string IndentationString {
			get { return this.TabsToSpaces ? new string (' ', this.TabSize) : "\t"; }
		}

		#region ITextEditorOptions implementation

		public bool CanResetZoom {
			get { return DefaultSourceEditorOptions.Instance.CanResetZoom; }
		}

		public bool CanZoomIn {
			get { return DefaultSourceEditorOptions.Instance.CanZoomIn; }
		}

		public bool CanZoomOut {
			get { return DefaultSourceEditorOptions.Instance.CanZoomOut; }
		}

		public event EventHandler Changed {
			add {
				if (changed == null)
					DefaultSourceEditorOptions.Instance.Changed += HandleDefaultsChanged;
				changed += value;
			}
			remove {
				changed -= value;
				if (changed == null)
					DefaultSourceEditorOptions.Instance.Changed -= HandleDefaultsChanged;
			}
		}

		void HandleDefaultsChanged (object sender, EventArgs e)
		{
			if (changed != null)
				changed (this, EventArgs.Empty);
		}

		public string ColorScheme {
			get { return DefaultSourceEditorOptions.Instance.ColorScheme; }
			set { throw new NotSupportedException (); }
		}

		public bool EnableSyntaxHighlighting {
			get { return DefaultSourceEditorOptions.Instance.EnableSyntaxHighlighting; }
			set { throw new NotSupportedException (); }
		}

		public Pango.FontDescription Font {
			get { return DefaultSourceEditorOptions.Instance.Font; }
		}

		public string FontName {
			get { return DefaultSourceEditorOptions.Instance.FontName; }
			set { throw new NotSupportedException (); }
		}

		public Mono.TextEditor.Highlighting.ColorScheme GetColorStyle ()
		{
			return DefaultSourceEditorOptions.Instance.GetColorStyle ();
		}

		public bool HighlightCaretLine {
			get { return DefaultSourceEditorOptions.Instance.HighlightCaretLine; }
			set { throw new NotSupportedException (); }
		}

		public bool HighlightMatchingBracket {
			get { return DefaultSourceEditorOptions.Instance.HighlightMatchingBracket; }
			set { throw new NotSupportedException (); }
		}

		public bool ShowFoldMargin {
			get { return DefaultSourceEditorOptions.Instance.ShowFoldMargin; }
			set { throw new NotSupportedException (); }
		}

		public bool ShowIconMargin {
			get { return DefaultSourceEditorOptions.Instance.ShowIconMargin; }
			set { throw new NotSupportedException (); }
		}

		public bool ShowLineNumberMargin {
			get { return DefaultSourceEditorOptions.Instance.ShowLineNumberMargin; }
			set { throw new NotSupportedException (); }
		}

		public bool ShowRuler {
			get { return DefaultSourceEditorOptions.Instance.ShowRuler; }
			set { throw new NotSupportedException (); }
		}

		public bool EnableAnimations {
			get { return DefaultSourceEditorOptions.Instance.EnableAnimations; }
			set { throw new NotSupportedException (); }
		}
		
		public bool UseAntiAliasing {
			get { return DefaultSourceEditorOptions.Instance.UseAntiAliasing; }
			set { throw new NotSupportedException (); }
		}

		public Mono.TextEditor.IWordFindStrategy WordFindStrategy {
			get { return DefaultSourceEditorOptions.Instance.WordFindStrategy; }
			set { throw new NotSupportedException (); }
		}

		public double Zoom {
			get { return DefaultSourceEditorOptions.Instance.Zoom; }
			set { DefaultSourceEditorOptions.Instance.Zoom = value; }
		}

		public bool DrawIndentationMarkers {
			get { return DefaultSourceEditorOptions.Instance.DrawIndentationMarkers; }
			set { DefaultSourceEditorOptions.Instance.DrawIndentationMarkers = value; }
		}

		public ShowWhitespaces ShowWhitespaces  {
			get { return DefaultSourceEditorOptions.Instance.ShowWhitespaces; }
			set { DefaultSourceEditorOptions.Instance.ShowWhitespaces = value; }
		}

		public IncludeWhitespaces IncludeWhitespaces {
			get { return DefaultSourceEditorOptions.Instance.IncludeWhitespaces; }
			set { DefaultSourceEditorOptions.Instance.IncludeWhitespaces = value; }
		}

		public bool WrapLines {
			get { return DefaultSourceEditorOptions.Instance.WrapLines; }
			set { DefaultSourceEditorOptions.Instance.WrapLines = value; }
		}

		public bool EnableQuickDiff {
			get { return DefaultSourceEditorOptions.Instance.EnableQuickDiff; }
			set { DefaultSourceEditorOptions.Instance.EnableQuickDiff = value; }
		}

		public void ZoomIn ()
		{
			DefaultSourceEditorOptions.Instance.ZoomIn ();
		}

		public void ZoomOut ()
		{
			DefaultSourceEditorOptions.Instance.ZoomOut ();
		}

		public void ZoomReset ()
		{
			DefaultSourceEditorOptions.Instance.ZoomReset ();
		}

		#endregion


		#region ISourceEditorOptions implementation

		public bool AutoInsertMatchingBracket {
			get { return DefaultSourceEditorOptions.Instance.AutoInsertMatchingBracket; }
		}

		public bool DefaultCommentFolding {
			get { return DefaultSourceEditorOptions.Instance.DefaultCommentFolding; }
		}

		public bool DefaultRegionsFolding {
			get { return DefaultSourceEditorOptions.Instance.DefaultRegionsFolding; }
		}

		public EditorFontType EditorFontType {
			get { return DefaultSourceEditorOptions.Instance.EditorFontType; }
		}

		public bool EnableAutoCodeCompletion {
			get { return DefaultSourceEditorOptions.Instance.EnableAutoCodeCompletion; }
		}

		public bool EnableCodeCompletion {
			get { return DefaultSourceEditorOptions.Instance.EnableCodeCompletion; }
		}

		public bool EnableSemanticHighlighting {
			get { return DefaultSourceEditorOptions.Instance.EnableSemanticHighlighting; }
		}

		public IndentStyle IndentStyle {
			get {
				if (DefaultSourceEditorOptions.Instance.IndentStyle == Mono.TextEditor.IndentStyle.Smart && CurrentPolicy.RemoveTrailingWhitespace)
					return IndentStyle.Virtual;
				return DefaultSourceEditorOptions.Instance.IndentStyle;
			}
			set {
				throw new NotSupportedException ("Use property 'IndentStyle' instead.");
			}
		}

		public bool TabIsReindent {
			get { return DefaultSourceEditorOptions.Instance.TabIsReindent; }
		}

		public bool UnderlineErrors {
			get { return DefaultSourceEditorOptions.Instance.UnderlineErrors; }
		}

		public bool UseViModes {
			get { return DefaultSourceEditorOptions.Instance.UseViModes; }
		}

		public bool EnableSelectionWrappingKeys { 
			get { return DefaultSourceEditorOptions.Instance.AutoInsertMatchingBracket; } 
		}


		#endregion

		public void Dispose ()
		{
			mimeTypes = null;
			if (policyContainer != null)
				policyContainer.PolicyChanged -= HandlePolicyChanged;
			if (changed != null) {
				DefaultSourceEditorOptions.Instance.Changed -= HandleDefaultsChanged;
				changed = null;
			}
		}
	}
}
