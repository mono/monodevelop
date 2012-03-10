// 
// HighlightUsagesExtension.cs
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
using MonoDevelop.Ide.Gui.Content;
using Mono.TextEditor;
using System.Collections.Generic;
using Gdk;
using MonoDevelop.CSharp.Resolver;
using MonoDevelop.Projects.Text;
using System.Linq;
using MonoDevelop.Core;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Resolver;
using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.SourceEditor;
using ICSharpCode.NRefactory.Semantics;


namespace MonoDevelop.CSharp.Highlighting
{
	public class HighlightUsagesExtension : TextEditorExtension, IQuickTaskProvider
	{
		TextEditorData textEditorData;
		ITextEditorResolver textEditorResolver;
		
		public override void Initialize ()
		{
			base.Initialize ();
			
			textEditorResolver = base.Document.GetContent<ITextEditorResolver> ();
			textEditorData = base.Document.Editor;
			textEditorData.Caret.PositionChanged += HandleTextEditorDataCaretPositionChanged;
			textEditorData.Document.TextReplaced += HandleTextEditorDataDocumentTextReplaced;
			textEditorData.SelectionChanged += HandleTextEditorDataSelectionChanged;
		}

		void HandleTextEditorDataSelectionChanged (object sender, EventArgs e)
		{
			RemoveMarkers (false);
		}

		void HandleTextEditorDataDocumentTextReplaced (object sender, DocumentChangeEventArgs e)
		{
			RemoveMarkers (false);
		}
		
		public override void Dispose ()
		{
			textEditorData.SelectionChanged -= HandleTextEditorDataSelectionChanged;
			textEditorData.Caret.PositionChanged -= HandleTextEditorDataCaretPositionChanged;
			textEditorData.Document.TextReplaced -= HandleTextEditorDataDocumentTextReplaced;
			base.Dispose ();
			RemoveTimer ();
		}
		
		uint popupTimer = 0;
		
		public bool IsTimerOnQueue {
			get {
				return popupTimer != 0;
			}
		}
		
		public void ForceUpdate ()
		{
			RemoveTimer ();
			DelayedTooltipShow ();
		}
		
		void RemoveTimer ()
		{
			if (popupTimer != 0) {
				GLib.Source.Remove (popupTimer);
				popupTimer = 0;
			}
		}

		void HandleTextEditorDataCaretPositionChanged (object sender, DocumentLocationEventArgs e)
		{
			if (!SourceEditor.DefaultSourceEditorOptions.Instance.EnableHighlightUsages)
				return;
			if (!textEditorData.IsSomethingSelected && markers.Values.Any (m => m.Contains (textEditorData.Caret.Offset)))
				return;
			RemoveMarkers (textEditorData.IsSomethingSelected);
			RemoveTimer ();
			if (!textEditorData.IsSomethingSelected)
				popupTimer = GLib.Timeout.Add (1000, DelayedTooltipShow);
		}
		
		bool DelayedTooltipShow ()
		{
			try {
				ResolveResult resolveResult = textEditorResolver.GetLanguageItem (textEditorData.Caret.Offset);
				if (resolveResult == null || resolveResult.IsError) {
					if (quickTasks.Count > 0) {
						quickTasks.Clear ();
						OnTasksUpdated (EventArgs.Empty);
					}
					return false;
				}
				
				ShowReferences (GetReferences (resolveResult));
			} catch (Exception e) {
				LoggingService.LogError ("Unhandled Exception in HighlightingUsagesExtension", e);
			} finally {
				popupTimer = 0;
			}
			return false;
		}
		
		void ShowReferences (IEnumerable<MemberReference> references)
		{
			RemoveMarkers (false);
			var lineNumbers = new HashSet<int> ();
			quickTasks.Clear ();
			if (references != null) {
				bool alphaBlend = false;
				foreach (var r in references) {
					var marker = GetMarker (r.Region.BeginLine);
					
					quickTasks.Add (new QuickTask (null, r.Region.Begin, QuickTaskSeverity.Usage));
					
					int offset = r.Offset;
					int endOffset = offset + r.Length;
					if (!alphaBlend && textEditorData.Parent.TextViewMargin.SearchResults.Any (sr => sr.Contains (offset) || sr.Contains (endOffset) ||
					                                                        offset < sr.Offset && sr.EndOffset < endOffset)) {
						textEditorData.Parent.TextViewMargin.AlphaBlendSearchResults = alphaBlend = true;
					}
					marker.Usages.Add (new Mono.TextEditor.Segment (offset, endOffset - offset));
					lineNumbers.Add (r.Region.BeginLine);
				}
			}
			foreach (int line in lineNumbers)
				textEditorData.Document.CommitLineUpdate (line);
			OnTasksUpdated (EventArgs.Empty);
		}
		
		List<QuickTask> quickTasks = new List<QuickTask> ();
		public IEnumerable<QuickTask> QuickTasks {
			get {
				return quickTasks;
			}
		}
		
		public event EventHandler TasksUpdated;
		
		protected virtual void OnTasksUpdated (EventArgs e)
		{
			EventHandler handler = this.TasksUpdated;
			if (handler != null)
				handler (this, e);
		}

		List<MemberReference> GetReferences (ResolveResult resolveResult)
		{
			var finder = new MonoDevelop.CSharp.Refactoring.CSharpReferenceFinder ();
			if (resolveResult is MemberResolveResult) {
				finder.SetSearchedMembers (new [] { ((MemberResolveResult)resolveResult).Member });
			} else if (resolveResult is TypeResolveResult) {
				finder.SetSearchedMembers (new [] { resolveResult.Type });
			} else if (resolveResult is MethodGroupResolveResult) { 
				finder.SetSearchedMembers (((MethodGroupResolveResult)resolveResult).Methods);
			} else if (resolveResult is NamespaceResolveResult) { 
				finder.SetSearchedMembers (new [] { ((NamespaceResolveResult)resolveResult).NamespaceName });
			} else if (resolveResult is LocalResolveResult) { 
				finder.SetSearchedMembers (new [] { ((LocalResolveResult)resolveResult).Variable });
			} else {
				return null;
			}
			
			try {
				return new List<MemberReference> (finder.FindInDocument (Document));
			} catch (Exception e) {
				LoggingService.LogError ("Error in highlight usages extension.", e);
			}
			return null;
		}

		Dictionary<int, UsageMarker> markers = new Dictionary<int, UsageMarker> ();
		
		public Dictionary<int, UsageMarker> Markers {
			get { return this.markers; }
		}
		
		void RemoveMarkers (bool updateLine)
		{
			if (markers.Count == 0)
				return;
			textEditorData.Parent.TextViewMargin.AlphaBlendSearchResults = false;
			foreach (var pair in markers) {
				textEditorData.Document.RemoveMarker (pair.Value, true);
			}
			markers.Clear ();
		}
		
		UsageMarker GetMarker (int line)
		{
			UsageMarker result;
			if (!markers.TryGetValue (line, out result)) {
				result = new UsageMarker ();
				textEditorData.Document.AddMarker (line, result);
				markers.Add (line, result);
			}
			return result;
		}

		
		public class UsageMarker : TextMarker, IBackgroundMarker
		{
			List<ISegment> usages = new List<ISegment> ();
			
			public List<ISegment> Usages {
				get { return this.usages; }
			}
			
			public bool Contains (int offset)
			{
				return usages.Any (u => u.Offset <= offset && offset <= u.EndOffset);
			}
			
			public bool DrawBackground (TextEditor editor, Cairo.Context cr, TextViewMargin.LayoutWrapper layout, int selectionStart, int selectionEnd, int startOffset, int endOffset, double y, double startXPos, double endXPos, ref bool drawBg)
			{
				drawBg = false;
				if (selectionStart >= 0 || editor.CurrentMode is TextLinkEditMode)
					return true;
				foreach (ISegment usage in Usages) {
					int markerStart = usage.Offset;
					int markerEnd = usage.EndOffset;
					
					if (markerEnd < startOffset || markerStart > endOffset) 
						return true; 
					
					double @from;
					double to;
					
					if (markerStart < startOffset && endOffset < markerEnd) {
						@from = startXPos;
						to = endXPos;
					} else {
						int start = startOffset < markerStart ? markerStart : startOffset;
						int end = endOffset < markerEnd ? endOffset : markerEnd;
						
						uint curIndex = 0, byteIndex = 0;
						TextViewMargin.TranslateToUTF8Index (layout.LineChars, (uint)(start - startOffset), ref curIndex, ref byteIndex);
						
						int x_pos = layout.Layout.IndexToPos ((int)byteIndex).X;
						
						@from = startXPos + (int)(x_pos / Pango.Scale.PangoScale);
						
						TextViewMargin.TranslateToUTF8Index (layout.LineChars, (uint)(end - startOffset), ref curIndex, ref byteIndex);
						x_pos = layout.Layout.IndexToPos ((int)byteIndex).X;
			
						to = startXPos + (int)(x_pos / Pango.Scale.PangoScale);
					}
		
					@from = System.Math.Max (@from, editor.TextViewMargin.XOffset);
					to = System.Math.Max (to, editor.TextViewMargin.XOffset);
					if (@from < to) {
						cr.Color = (HslColor)editor.ColorStyle.UsagesHighlightRectangle.BackgroundColor;
						cr.Rectangle (@from + 1, y + 1, to - @from - 1, editor.LineHeight - 2);
						cr.Fill ();
						
						cr.Color = (HslColor)editor.ColorStyle.UsagesHighlightRectangle.Color;
						cr.Rectangle (@from + 0.5, y + 0.5, to - @from, editor.LineHeight - 1);
						cr.Stroke ();
					}
				}
				return true;
			}
		}
	}
}

