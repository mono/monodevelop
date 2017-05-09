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
using System.Collections.Generic;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.DesignerSupport;
using MonoDevelop.TextTemplating.Parser;
using MonoDevelop.Ide;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Editor;
using System.Threading.Tasks;
using System.Threading;
using MonoDevelop.Core;

namespace MonoDevelop.TextTemplating.Gui
{
	public class T4EditorExtension : CompletionTextEditorExtension, IOutlinedDocument
	{
		bool disposed;
		T4ParsedDocument parsedDoc;
		
		public T4EditorExtension ()
		{
		}
		
		protected override void Initialize ()
		{
			base.Initialize ();
			DocumentContext.DocumentParsed += HandleDocumentDocumentParsed;
			HandleDocumentDocumentParsed (this, EventArgs.Empty);
		}

		void HandleDocumentDocumentParsed (object sender, EventArgs e)
		{
			parsedDoc = (T4ParsedDocument)DocumentContext.ParsedDocument;
			if (parsedDoc != null)
				RefreshOutline ();
		}
		
		public override void Dispose ()
		{
			if (disposed)
				return;
			disposed = true;
			base.Dispose ();
		}
		
		#region Convenience accessors, from BaseXmlEditorExtension
		
		protected T4ParsedDocument ParsedDoc {
			get { return parsedDoc; }
		}

		protected TextEditor EditableBuffer {
			get {
				if (DocumentContext == null)
					throw new InvalidOperationException ("Editor extension not yet initialized");
				return DocumentContext.GetContent<TextEditor> ();
			}
		}
		
		protected string GetBufferText (DomRegion region)
		{
			int start = Editor.LocationToOffset (region.BeginLine, region.BeginColumn);
			int end = Editor.LocationToOffset (region.EndLine, region.EndColumn);
			if (end > start && start >= 0)
				return Editor.GetTextBetween (start, end);
			else
				return null;
		}
		
		#endregion
		
		#region Code completion

		public override Task<ICompletionDataList> HandleCodeCompletionAsync (CodeCompletionContext completionContext, CompletionTriggerInfo triggerInfo, CancellationToken token = default(CancellationToken))
		{
			if (triggerInfo.CompletionTriggerReason == CompletionTriggerReason.CharTyped) {
				char completionChar = triggerInfo.TriggerCharacter.Value;
				int pos = completionContext.TriggerOffset;
				if (pos > 0 && Editor.GetCharAt (pos - 1) == completionChar) {
					return HandleCodeCompletion (completionContext, false);
				}
			} else if (triggerInfo.CompletionTriggerReason == CompletionTriggerReason.CompletionCommand) {
				int pos = completionContext.TriggerOffset;
				if (pos <= 0)
					return null;
				return HandleCodeCompletion ((CodeCompletionContext)completionContext, true);

			}
			return null;
		}

		protected virtual Task<ICompletionDataList> HandleCodeCompletion (
		    CodeCompletionContext completionContext, bool forced)
		{
			//IEditableTextBuffer buf = this.EditableBuffer;
			return Task.FromResult<ICompletionDataList> (null);
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
			var sw = new MonoDevelop.Components.CompactScrolledWindow ();;
			sw.Add (outlineTreeView);
			sw.ShowAll ();
			return sw;
		}
		
		IEnumerable<Gtk.Widget> IOutlinedDocument.GetToolbarWidgets ()
		{
			return null;
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
			Runtime.AssertMainThread ();
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
			outlineTreeView = null;
		}
		
		void SelectSegment (Mono.TextTemplating.ISegment seg)
		{
			int s = Editor.LocationToOffset (seg.TagStartLocation.Line, seg.TagStartLocation.Column);
			if (s > -1) {
				Editor.CaretOffset = s;
				Editor.CenterTo (s);
			}
		}
		
		#endregion
	}
}
