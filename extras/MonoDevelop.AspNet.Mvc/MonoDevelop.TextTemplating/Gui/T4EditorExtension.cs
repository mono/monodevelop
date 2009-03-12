// 
// T4EditorExtension.cs
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
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.DesignerSupport;
using MonoDevelop.TextTemplating.Parser;

namespace MonoDevelop.TextTemplating.Gui
{
	
	
	public class T4EditorExtension : CompletionTextEditorExtension, IOutlinedDocument
	{
		bool disposed;
		T4ParsedDocument parsedDoc;
		
		public T4EditorExtension ()
		{
		}
		
		public override bool ExtendsEditor (MonoDevelop.Ide.Gui.Document doc, MonoDevelop.Ide.Gui.Content.IEditableTextBuffer editor)
		{
			return doc.Name.EndsWith (".tt");
		}
		
		public override void Initialize ()
		{
			base.Initialize ();
			MonoDevelop.Projects.Dom.Parser.ProjectDomService.ParsedDocumentUpdated += OnParseInformationChanged;
			parsedDoc = (T4ParsedDocument)Document.ParsedDocument;
			if (parsedDoc != null) {
				RefreshOutline ();
			}
		}
		
		public override void Dispose ()
		{
			if (disposed)
				return;
			disposed = true;
			MonoDevelop.Projects.Dom.Parser.ProjectDomService.ParsedDocumentUpdated
				-= OnParseInformationChanged;
			base.Dispose ();
		}
		
		void OnParseInformationChanged (object sender, MonoDevelop.Projects.Dom.ParsedDocumentEventArgs args)
		{
			if (FileName == args.FileName && args.ParsedDocument != null) {
				parsedDoc = (T4ParsedDocument)args.ParsedDocument;
				RefreshOutline ();
			}
		}
		
		#region Convenience accessors, from BaseXmlEditorExtension
		
		protected T4ParsedDocument ParsedDoc {
			get { return parsedDoc; }
		}
		
		protected ITextBuffer Buffer {
			get {
				if (Document == null)
					throw new InvalidOperationException ("Editor extension not yet initialized");
				return Document.GetContent<ITextBuffer> ();
			}
		}
		
		protected IEditableTextBuffer EditableBuffer {
			get {
				if (Document == null)
					throw new InvalidOperationException ("Editor extension not yet initialized");
				return Document.GetContent<IEditableTextBuffer> ();
			}
		}
		
		protected string GetBufferText (DomRegion region)
		{
			MonoDevelop.Ide.Gui.Content.ITextBuffer buf = Buffer;
			int start = buf.GetPositionFromLineColumn (region.Start.Line, region.Start.Column);
			int end = buf.GetPositionFromLineColumn (region.End.Line, region.End.Column);
			if (end > start && start >= 0)
				return buf.GetText (start, end);
			else
				return null;
		}
		
		#endregion
		
		#region Code completion

		public override ICompletionDataList CodeCompletionCommand (ICodeCompletionContext completionContext)
		{
			int pos = completionContext.TriggerOffset;
			string txt = Editor.GetText (pos - 1, pos);
			int triggerWordLength = 0;
			if (txt.Length > 0) {
				return HandleCodeCompletion ((CodeCompletionContext) completionContext, true, ref triggerWordLength);
			}
			return null;
		}

		public override ICompletionDataList HandleCodeCompletion (
		    ICodeCompletionContext completionContext, char completionChar, ref int triggerWordLength)
		{
			int pos = completionContext.TriggerOffset;
			if (pos > 0 && Editor.GetCharAt (pos - 1) == completionChar) {
				return HandleCodeCompletion ((CodeCompletionContext) completionContext, 
				                             false, ref triggerWordLength);
			}
			return null;
		}

		protected virtual ICompletionDataList HandleCodeCompletion (
		    CodeCompletionContext completionContext, bool forced, ref int triggerWordLength)
		{
			//IEditableTextBuffer buf = this.EditableBuffer;
			return null;
		}
		
		#endregion
		
		#region Outline
		
		bool refreshingOutline = false;
		MonoDevelop.Ide.Gui.Components.PadTreeView outlineTreeView;
		Gtk.TreeStore outlineTreeStore;
		
		Gtk.Widget IOutlinedDocument.GetOutlineWidget ()
		{
			if (outlineTreeView != null)
				return outlineTreeView;
			
			outlineTreeStore = new Gtk.TreeStore (typeof(string), typeof (Gdk.Color), typeof (Mono.TextTemplating.ISegment));
			outlineTreeView = new MonoDevelop.Ide.Gui.Components.PadTreeView (outlineTreeStore);
			outlineTreeView.Realized += delegate { RefillOutlineStore (); };
			
			outlineTreeView.TextRenderer.Xpad = 0;
			outlineTreeView.TextRenderer.Ypad = 0;
			outlineTreeView.AppendColumn ("Node", outlineTreeView.TextRenderer, "text", 0, "foreground-gdk", 1);
			
			outlineTreeView.HeadersVisible = false;
			
			outlineTreeView.Selection.Changed += delegate {
				Gtk.TreeIter iter;
				if (!outlineTreeView.Selection.GetSelected (out iter))
					return;
				SelectSegment ((Mono.TextTemplating.ISegment )outlineTreeStore.GetValue (iter, 2));
			};
			
			RefillOutlineStore ();
			
			Gtk.ScrolledWindow sw = new Gtk.ScrolledWindow ();
			sw.Add (outlineTreeView);
			sw.ShowAll ();
			return sw;
		}
		
		void RefreshOutline ()
		{
			if (refreshingOutline || outlineTreeView == null )
				return;
			refreshingOutline = true;
			GLib.Timeout.Add (3000, refillOutlineStoreIdleHandler);
		}
		
		bool refillOutlineStoreIdleHandler ()
		{
			refreshingOutline = false;
			RefillOutlineStore ();
			return false;
		}
		
		void RefillOutlineStore (T4ParsedDocument doc, Gtk.TreeStore store)
		{
			if (doc == null)
				return;
			
			Gdk.Color normal   = new Gdk.Color (0x00, 0x00, 0x00);
			Gdk.Color blue     = new Gdk.Color (0x10, 0x40, 0xE0);
			Gdk.Color green    = new Gdk.Color (0x08, 0xC0, 0x30);
			Gdk.Color orange   = new Gdk.Color (0xFF, 0xA0, 0x00);
			Gdk.Color red      = new Gdk.Color (0xC0, 0x00, 0x20);
			
			Gtk.TreeIter parent = Gtk.TreeIter.Zero;
			foreach (Mono.TextTemplating.ISegment segment in doc.TemplateSegments) {
				Mono.TextTemplating.Directive dir = segment as Mono.TextTemplating.Directive;
				if (dir != null) {
					parent = Gtk.TreeIter.Zero;
					store.AppendValues ("<#@ " + dir.Name + " #>", red, segment);
					continue;
				}
				Mono.TextTemplating.TemplateSegment ts = segment as Mono.TextTemplating.TemplateSegment;
				if (ts != null) {
					string name;
					if (ts.Text.Length > 40) {
						name = ts.Text.Substring (0, 40) + "...";
					} else {
						name = ts.Text;
					}
					name = name.Replace ('\n', ' ').Trim ();
					if (name.Length == 0)
						continue;
					
					if (ts.Type == Mono.TextTemplating.SegmentType.Expression) {
						store.AppendValues (parent, "<#= " + name + " #>", orange, segment);
					} else {
						if (ts.Type == Mono.TextTemplating.SegmentType.Block) {
							name = "<#" + name + " #>";
							store.AppendValues (name, blue, segment);
							parent = Gtk.TreeIter.Zero;
						} else if (ts.Type == Mono.TextTemplating.SegmentType.Helper) {
							name = "<#+" + name + " #>";
							store.AppendValues (name, green, segment);
							parent = Gtk.TreeIter.Zero;
						} else if (ts.Type == Mono.TextTemplating.SegmentType.Content) {
							parent = store.AppendValues (name, normal, segment);
						}
					}
				}
			}
		}
		
		void RefillOutlineStore ()
		{
			MonoDevelop.Core.Gui.DispatchService.AssertGuiThread ();
			Gdk.Threads.Enter ();
			refreshingOutline = false;
			if (outlineTreeStore == null || !outlineTreeView.IsRealized)
				return;
			outlineTreeStore.Clear ();
			
			if (ParsedDoc != null) {
				DateTime start = DateTime.Now;
				RefillOutlineStore (ParsedDoc, outlineTreeStore);
				outlineTreeView.ExpandAll ();
				outlineTreeView.ExpandAll ();
				MonoDevelop.Core.LoggingService.LogDebug ("Built outline in {0}ms", (DateTime.Now - start).Milliseconds);
			}
			
			Gdk.Threads.Leave ();
		}

		void IOutlinedDocument.ReleaseOutlineWidget ()
		{
			if (outlineTreeView == null)
				return;
			
			Gtk.ScrolledWindow w = (Gtk.ScrolledWindow) outlineTreeView.Parent;
			w.Destroy ();
			outlineTreeView.Destroy ();
			outlineTreeStore.Dispose ();
			outlineTreeStore = null;
			outlineTreeView = null;
		}
		
		void SelectSegment (Mono.TextTemplating.ISegment seg)
		{
			int s = Editor.GetPositionFromLineColumn (seg.TagStartLocation.Line, seg.TagStartLocation.Column);
			if (s > -1) {
				Editor.CursorPosition = s;
				Editor.ShowPosition (s);
			}
		}
		
		#endregion
	}
}
