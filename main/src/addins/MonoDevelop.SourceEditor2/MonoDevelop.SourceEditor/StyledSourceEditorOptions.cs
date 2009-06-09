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

namespace MonoDevelop.SourceEditor
{
	
	internal class StyledSourceEditorOptions : ISourceEditorOptions
	{
		IPolicyContainer policyContainer;
		EventHandler changed;
		IEnumerable<string> mimeTypes;
		TextStylePolicy currentPolicy;
		
		public StyledSourceEditorOptions (Project styleParent, string mimeType)
		{
			UpdateStyleParent (styleParent, mimeType);
		}
		
		TextStylePolicy CurrentPolicy {
			get {
				return currentPolicy;
			}
		}
		
		public void UpdateStyleParent (Project styleParent, string mimeType)
		{
			if (policyContainer != null)
				policyContainer.PolicyChanged -= HandlePolicyChanged;
			
			if (string.IsNullOrEmpty (mimeType))
				mimeType = "text/plain";
			this.mimeTypes = MonoDevelop.Core.Gui.Services.PlatformService.GetMimeTypeInheritanceChain (mimeType);
			
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
			get {
				return DefaultSourceEditorOptions.Instance.OverrideDocumentEolMarker;
			}
		}
		
		public string DefaultEolMarker {
			get {
				return DefaultSourceEditorOptions.Instance.DefaultEolMarker;
			}
		}
		
		public int RulerColumn {
			get {
				return CurrentPolicy.FileWidth;
			}
		}

		public int TabSize {
			get {
				return CurrentPolicy.TabWidth;
			}
		}
		
		public bool TabsToSpaces {
			get {
				return CurrentPolicy.TabsToSpaces;
			}
		}
		
		public bool RemoveTrailingWhitespaces {
			get {
				return CurrentPolicy.RemoveTrailingWhitespace;
			}
		}
		
		public bool AllowTabsAfterNonTabs {
			get {
				return !CurrentPolicy.NoTabsAfterNonTabs;
			}
		}
		
		public int IndentationSize {
			get { return TabSize; }
		}
		
		public string IndentationString {
			get {
				return this.TabsToSpaces ? new string (' ', this.TabSize) : "\t";
			}
		}
		
		#region ITextEditorOptions implementation 
		
		public bool AutoIndent {
			get { return DefaultSourceEditorOptions.Instance.AutoIndent; }
		}
		
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
				changed += value;
				if (changed != null)
					DefaultSourceEditorOptions.Instance.Changed += HandleDefaultsChanged;
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
		}
		
		public bool EnableSyntaxHighlighting {
			get { return DefaultSourceEditorOptions.Instance.EnableSyntaxHighlighting; }
		}
		
		public Pango.FontDescription Font {
			get { return DefaultSourceEditorOptions.Instance.Font; }
		}
		
		public string FontName {
			get { return DefaultSourceEditorOptions.Instance.FontName; }
		}
		
		public Mono.TextEditor.Highlighting.Style GetColorStyle (Gtk.Widget widget)
		{
			return DefaultSourceEditorOptions.Instance.GetColorStyle (widget);
		}
		
		public bool HighlightCaretLine {
			get { return DefaultSourceEditorOptions.Instance.HighlightCaretLine; }
		}
		
		public bool HighlightMatchingBracket {
			get { return DefaultSourceEditorOptions.Instance.HighlightMatchingBracket; }
		}
		
		public bool ShowEolMarkers {
			get { return DefaultSourceEditorOptions.Instance.ShowEolMarkers; }
		}
		
		public bool ShowFoldMargin {
			get { return DefaultSourceEditorOptions.Instance.ShowFoldMargin; }
		}
		
		public bool ShowIconMargin {
			get { return DefaultSourceEditorOptions.Instance.ShowIconMargin; }
		}
		
		public bool ShowInvalidLines {
			get { return DefaultSourceEditorOptions.Instance.ShowInvalidLines; }
		}
		
		public bool ShowLineNumberMargin {
			get { return DefaultSourceEditorOptions.Instance.ShowLineNumberMargin; }
		}
		
		public bool ShowRuler {
			get { return DefaultSourceEditorOptions.Instance.ShowRuler; }
		}
		
		public bool ShowSpaces {
			get { return DefaultSourceEditorOptions.Instance.ShowSpaces; }
		}
		
		public bool ShowTabs {
			get { return DefaultSourceEditorOptions.Instance.ShowTabs; }
		}
		
		public Mono.TextEditor.IWordFindStrategy WordFindStrategy {
			get { return DefaultSourceEditorOptions.Instance.WordFindStrategy; }
		}
		
		public double Zoom {
			get { return DefaultSourceEditorOptions.Instance.Zoom; }
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
		
		public bool EnableQuickFinder {
			get { return DefaultSourceEditorOptions.Instance.EnableQuickFinder; }
		}
		
		public bool EnableSemanticHighlighting {
			get { return DefaultSourceEditorOptions.Instance.EnableSemanticHighlighting; }
		}
		
		public IndentStyle IndentStyle {
			get { return DefaultSourceEditorOptions.Instance.IndentStyle; }
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
		
		#endregion 
		
		public void Dispose ()
		{
			if (policyContainer != null)
				policyContainer.PolicyChanged -= HandlePolicyChanged;
			if (changed != null) {
				DefaultSourceEditorOptions.Instance.Changed -= HandleDefaultsChanged;
				changed = null;
			}
		}
	}
}
