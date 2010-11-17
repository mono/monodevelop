// 
// BaseXmlEditorExtension.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.Collections.Generic;
using System.Diagnostics;

using MonoDevelop.Core;
using MonoDevelop.DesignerSupport;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Xml.StateEngine;
using MonoDevelop.XmlEditor.Completion;
using MonoDevelop.Ide;
using MonoDevelop.Components;
using Gtk;
using System.Linq;
using System.Text;

namespace MonoDevelop.XmlEditor.Gui
{
	
	
	public abstract class BaseXmlEditorExtension : CompletionTextEditorExtension, IPathedDocument, IOutlinedDocument
	{
		DocumentStateTracker<Parser> tracker;
		ParsedDocument lastCU;
		
		XDocType docType;
		
		MonoDevelop.Ide.Gui.Components.PadTreeView outlineTreeView;
		Gtk.TreeStore outlineTreeStore;

		#region Setup and teardown
		
		protected virtual RootState CreateRootState ()
		{
			return new XmlFreeState ();
		}
		
		public override void Initialize ()
		{
			base.Initialize ();
			Parser parser = new Parser (CreateRootState (), false);
			tracker = new DocumentStateTracker<Parser> (parser, Editor);
			MonoDevelop.Projects.Dom.Parser.ProjectDomService.ParsedDocumentUpdated += OnParseInformationChanged;
			
			if (Document.ParsedDocument != null) {
				lastCU = Document.ParsedDocument;
				OnParsedDocumentUpdated ();
			}
		}

		public override void Dispose ()
		{
			if (tracker != null) {
				tracker = null;
				MonoDevelop.Projects.Dom.Parser.ProjectDomService.ParsedDocumentUpdated
					-= OnParseInformationChanged;
				base.Dispose ();
			}
		}

		void OnParseInformationChanged (object sender, MonoDevelop.Projects.Dom.ParsedDocumentEventArgs args)
		{
			if (this.FileName == args.FileName && args.ParsedDocument != null) {
				lastCU = args.ParsedDocument;
				OnParsedDocumentUpdated ();
			}
		}
		
		protected virtual void OnParsedDocumentUpdated ()
		{
			RefreshOutline ();
			
			//use the doctype to select a completion schema
			var doc = CU as XmlParsedDocument;
			bool found = false;
			if (doc != null && doc.XDocument != null) {
				foreach (XNode node in doc.XDocument.Nodes) {
					if (node is XDocType) {
						DocType = (XDocType)node;
						found = true;
						break;
					}
					//cannot validly have a doctype after these nodes
					if (node is XElement || node is XCData)
						break;
				}
				if (!found)
					DocType = null;
			}
		}

		#endregion

		#region Convenience accessors
		
		protected ParsedDocument CU {
			get { return lastCU; }
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
		
		protected DocumentStateTracker<Parser> Tracker {
			get { return tracker; }
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
		
		protected XDocType DocType {
			get { return docType; }
			set {
				if (docType == value)
					return;
				docType = value;
				OnDocTypeChanged ();
			}
		}
		
		protected virtual void OnDocTypeChanged ()
		{
		}
		
		public override bool KeyPress (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			if (TextEditorProperties.IndentStyle == IndentStyle.Smart) {
				var newLine = Editor.Caret.Line + 1;
				var ret = base.KeyPress (key, keyChar, modifier);
				if (key == Gdk.Key.Return && Editor.Caret.Line == newLine) {
					string indent = GetLineIndent (newLine);
					var oldIndent = Editor.GetLineIndent (newLine);
					var seg = Editor.GetLine (newLine);
					if (oldIndent != indent) {
						int newCaretOffset = Editor.Caret.Offset;
						if (newCaretOffset > seg.Offset) {
							newCaretOffset += (indent.Length - oldIndent.Length);
						}
						Editor.Document.BeginAtomicUndo ();
						Editor.Replace (seg.Offset, oldIndent.Length, indent);
						Editor.Caret.Offset = newCaretOffset;
						Editor.Document.EndAtomicUndo ();
					}
				}
				return ret;
			}
			return base.KeyPress (key, keyChar, modifier);
		}
		
		#region Code completion

		public override ICompletionDataList CodeCompletionCommand (CodeCompletionContext completionContext)
		{
			int pos = completionContext.TriggerOffset;
			if (pos <= 0)
				return null;
			int triggerWordLength = 0;
			
			tracker.UpdateEngine ();
			return HandleCodeCompletion ((CodeCompletionContext) completionContext, true, ref triggerWordLength);
		}

		public override ICompletionDataList HandleCodeCompletion (
		    CodeCompletionContext completionContext, char completionChar, ref int triggerWordLength)
		{
			int pos = completionContext.TriggerOffset;
			char ch = CompletionWidget != null ? CompletionWidget.GetChar (pos - 1) : Editor.GetCharAt (pos - 1);
			if (pos > 0 && ch == completionChar) {
				tracker.UpdateEngine ();
				return HandleCodeCompletion ((CodeCompletionContext) completionContext, 
				                             false, ref triggerWordLength);
			}
			return null;
		}

		protected virtual ICompletionDataList HandleCodeCompletion (
		    CodeCompletionContext completionContext, bool forced, ref int triggerWordLength)
		{
			IEditableTextBuffer buf = this.EditableBuffer;

			// completionChar may be a space even if the current char isn't, when ctrl-space is fired t
			DomLocation currentLocation = new DomLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset);
			char currentChar = completionContext.TriggerOffset < 1? ' ' : buf.GetCharAt (completionContext.TriggerOffset - 1);
			char previousChar = completionContext.TriggerOffset < 2? ' ' : buf.GetCharAt (completionContext.TriggerOffset - 2);

			LoggingService.LogDebug ("Attempting completion for state '{0}'x{1}, previousChar='{2}'," 
				+ " currentChar='{3}', forced='{4}'", tracker.Engine.CurrentState,
				tracker.Engine.CurrentStateLength, previousChar, currentChar, forced);
			
			//closing tag completion
			if (tracker.Engine.CurrentState is XmlFreeState && currentChar == '>') {
				//get name of current node in document that's being ended
				XElement el = tracker.Engine.Nodes.Peek () as XElement;
				if (el != null && el.Region.End >= currentLocation && !el.IsClosed && el.IsNamed) {
					string tag = String.Concat ("</", el.Name.FullName, ">");
					if (XmlEditorOptions.AutoCompleteElements) {
						
						//make sure we have a clean atomic undo so the user can undo the tag insertion
						//independently of the >
						bool wasInAtomicUndo = this.Editor.Document.IsInAtomicUndo;
						if (wasInAtomicUndo)
							this.Editor.Document.EndAtomicUndo ();
						
						buf.BeginAtomicUndo ();
						buf.InsertText (buf.CursorPosition, tag);
						buf.CursorPosition -= tag.Length;
						buf.EndAtomicUndo ();
						
						if (wasInAtomicUndo)
							this.Editor.Document.BeginAtomicUndo ();
						
						return null;
					} else {
						CompletionDataList cp = new CompletionDataList ();
						cp.Add (new XmlTagCompletionData (tag, 0, true));
						return cp;
					}
				}
				return null;
			}
			
			// Auto insert '>' when '/' is typed inside tag state (for quick tag closing)
			//FIXME: avoid doing this when the next non-whitespace char is ">" or ignore the next ">" typed
			if (XmlEditorOptions.AutoInsertFragments && tracker.Engine.CurrentState is XmlTagState && currentChar == '/') {
				buf.BeginAtomicUndo ();
				buf.InsertText (buf.CursorPosition, ">");
				buf.EndAtomicUndo ();
				return null;
			}
			
			//element completion
			if (currentChar == '<' && tracker.Engine.CurrentState is XmlFreeState) {
				CompletionDataList list = new CompletionDataList ();
				GetElementCompletions (list);
				AddCloseTag (list, Tracker.Engine.Nodes);
				return list.Count > 0? list : null;
			}
			
			//entity completion
			if (currentChar == '&' && (tracker.Engine.CurrentState is XmlFreeState ||
			                           tracker.Engine.CurrentState is XmlAttributeValueState))
			{
				CompletionDataList list = new CompletionDataList ();
				
				//TODO: need to tweak semicolon insertion
				list.Add ("apos").Description = "'";
				list.Add ("quot").Description = "\"";
				list.Add ("lt").Description = "<";
				list.Add ("gt").Description = ">";
				list.Add ("amp").Description = "&";
				
				//not sure about these "completions". they're more like
				//shortcuts than completions but they're pretty useful
				list.Add ("'").CompletionText = "apos;";
				list.Add ("\"").CompletionText = "quot;";
				list.Add ("<").CompletionText = "lt;";
				list.Add (">").CompletionText = "gt;";
				list.Add ("&").CompletionText = "amp;";
				
				GetEntityCompletions (list);
				return list;
			}
			
			//doctype completion
			if (tracker.Engine.CurrentState is XmlDocTypeState) {
				if (tracker.Engine.CurrentStateLength == 1) {
					CompletionDataList list = GetDocTypeCompletions ();
					if (list != null && list.Count > 0)
						return list;
				}
				return null;
			}
			
			//attribute name completion
			if ((forced && Tracker.Engine.Nodes.Peek () is IAttributedXObject && !tracker.Engine.Nodes.Peek ().IsEnded)
			     || (Tracker.Engine.CurrentState is XmlNameState 
			 	 && Tracker.Engine.CurrentState.Parent is XmlAttributeState
			         && Tracker.Engine.CurrentStateLength == 1)
			) {
				IAttributedXObject attributedOb = (Tracker.Engine.Nodes.Peek () as IAttributedXObject) ?? 
					Tracker.Engine.Nodes.Peek (1) as IAttributedXObject;
				if (attributedOb == null)
					return null;
				
				//attributes
				if (attributedOb.Name.IsValid && (forced ||
					(char.IsWhiteSpace (previousChar) && char.IsLetter (currentChar))))
				{
					
					if (!forced)
						triggerWordLength = 1;
					
					var existingAtts = new Dictionary<string,string> (StringComparer.OrdinalIgnoreCase);
					
					foreach (XAttribute att in attributedOb.Attributes) {
						existingAtts [att.Name.FullName] = att.Value ?? string.Empty;
					}
					
					return GetAttributeCompletions (attributedOb, existingAtts);
				}
			}
			
			//attribute value completion
			//determine whether to trigger completion within attribute values quotes
			if ((Tracker.Engine.CurrentState is XmlDoubleQuotedAttributeValueState
			    || Tracker.Engine.CurrentState is XmlSingleQuotedAttributeValueState)
			    //trigger on the opening quote
			    && (Tracker.Engine.CurrentStateLength == 0
			        //or trigger on first letter of value, if unforced
			        || (!forced && Tracker.Engine.CurrentStateLength == 1))
			    )
			{
				XAttribute att = (XAttribute) Tracker.Engine.Nodes.Peek ();
				
				if (att.IsNamed) {
					IAttributedXObject attributedOb = Tracker.Engine.Nodes.Peek (1) as IAttributedXObject;
					if (attributedOb == null)
						return null;
					
					char next = ' ';
					if (completionContext.TriggerOffset < buf.Length)
						next = buf.GetCharAt (completionContext.TriggerOffset);
					
					char compareChar = (Tracker.Engine.CurrentStateLength == 0)? currentChar : previousChar;
					
					if ((compareChar == '"' || compareChar == '\'') 
					    && (next == compareChar || char.IsWhiteSpace (next))
					) {
						//if triggered by first letter of value, grab that letter
						if (Tracker.Engine.CurrentStateLength == 1)
							triggerWordLength = 1;
						
						return GetAttributeValueCompletions (attributedOb, att);
					}
				}
				
			}
			
//			if (Tracker.Engine.CurrentState is XmlFreeState) {
//				if (line < 3) {
//				cp.Add ("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>");
//			}
			
			if (forced && Tracker.Engine.CurrentState is XmlFreeState) {
				CompletionDataList list = new CompletionDataList ();
				MonoDevelop.Ide.CodeTemplates.CodeTemplateService.AddCompletionDataForFileName (Document.Name, list);
				return list.Count > 0? list : null;
			}
			
			return null;
		}
		
		protected virtual void GetElementCompletions (CompletionDataList list)
		{
		}
		
		protected virtual CompletionDataList GetAttributeCompletions (IAttributedXObject attributedOb,
			Dictionary<string, string> existingAtts)
		{
			return null;
		}
		
		protected virtual CompletionDataList GetAttributeValueCompletions (IAttributedXObject attributedOb, XAttribute att)
		{
			return null;
		}
		
		protected virtual void GetEntityCompletions (CompletionDataList list)
		{
		}
		
		protected virtual CompletionDataList GetDocTypeCompletions ()
		{
			return null;
		}
		
		protected string GetLineIndent (int line)
		{
			var seg = Editor.Document.GetLine (line);
			
			//reset the tracker to the beginning of the line
			Tracker.UpdateEngine (seg.Offset);
			
			//calculate the indentation
			var startElementDepth = GetElementIndentDepth (Tracker.Engine.Nodes);
			var attributeDepth = GetAttributeIndentDepth (Tracker.Engine.Nodes);
			
			//update the tracker to the end of the line 
			Tracker.UpdateEngine (seg.Offset + seg.EditableLength);
			
			//if end depth is less than start depth, then reduce start depth
			//because that means there are closing tags on the line, and they de-indent the line they're on
			var endElementDepth = GetElementIndentDepth (Tracker.Engine.Nodes);
			var elementDepth = Math.Min (endElementDepth, startElementDepth);
			
			//FIXME: use policies
			return new string ('\t', elementDepth + attributeDepth);
		}
		
		static int GetElementIndentDepth (NodeStack nodes)
		{
			return nodes.OfType<XElement> ().Where (el => !el.IsClosed).Count ();
		}
		
		static int GetAttributeIndentDepth (NodeStack nodes)
		{
			var node = nodes.Peek ();
			if (node is XElement && !node.IsEnded)
				return 1;
			if (node is XAttribute)
				return node.IsEnded? 1 : 2;
			return 0;
		}
		
		protected IEnumerable<XName> GetParentElementNames (int skip)
		{
			foreach (XObject obj in tracker.Engine.Nodes) {
				if (skip > 0) {
					skip--;
					continue;
				}
				XElement el = obj as XElement;
				if (el == null || !el.IsNamed)
					yield break;
				yield return el.Name;
			}
		}
		
		protected XName GetParentElementName (int skip)
		{
			foreach (XObject obj in tracker.Engine.Nodes) {
				if (skip > 0) {
					skip--;
					continue;
				}
				XElement el = obj as XElement;
				return el != null? el.Name : new XName ();
			}
			return new XName ();
		}
		
		protected XElement GetParentElement (int skip)
		{
			foreach (XObject obj in tracker.Engine.Nodes) {
				if (skip > 0) {
					skip--;
					continue;
				}
				return obj as XElement;
			}
			return null;
		}
		
		protected static void AddCloseTag (CompletionDataList completionList, NodeStack stack)
		{
			//FIXME: search forward to see if tag's closed already
			List<XElement> elements = new List<XElement> ();
			foreach (XObject ob in stack) {
				XElement el = ob as XElement;
				if (el == null)
					continue;
				if (!el.IsNamed || el.IsClosed)
					return;
				
				if (elements.Count == 0) {
					string name = el.Name.FullName;
					completionList.Add ("/" + name + ">", Gtk.Stock.GoBack,
					                    GettextCatalog.GetString ("Closing tag for '{0}'", name));
				} else {
					foreach (XElement listEl in elements) {
						if (listEl.Name == el.Name)
							return;
					}
					completionList.Add (new XmlMultipleClosingTagCompletionData (el, elements.ToArray ()));
				}
				elements.Add (el);
			}
		}
		
		/// <summary>
		/// Adds CDATA and comment begin tags.
		/// </summary>
		protected static void AddMiscBeginTags (CompletionDataList list)
		{
			list.Add ("!--",  "md-literal", GettextCatalog.GetString ("Comment"));
			list.Add ("![CDATA[", "md-literal", GettextCatalog.GetString ("Character data"));
		}

		#endregion
		
		#region IPathedDocument
		
		PathEntry[] currentPath;
		bool pathUpdateQueued = false;
		
		public override void CursorPositionChanged ()
		{
			if (pathUpdateQueued)
				return;
			pathUpdateQueued = true;
			GLib.Timeout.Add (500, delegate {
				pathUpdateQueued = false;
				UpdatePath ();
				return false;
			});
				
		}

		public void SelectPath (int depth)
		{
			SelectPath (depth, false);
		}

		public void SelectPathContents (int depth)
		{
			SelectPath (depth, true);
		}
		
		void SelectPath (int depth, bool contents)
		{
			//clone the parser and put it in tree mode
			Parser treeParser = this.tracker.Engine.GetTreeParser ();
			
			//locate the node
			List<XObject> path = new List<XObject> (treeParser.Nodes);
			
			//note: list is backwards, and we want ignore the root XDocument
			XObject ob = path [path.Count - (depth + 2)];
			XNode node = ob as XNode;
			XElement el = node as XElement;
			
			//hoist this as it may not be cheap to evaluate (P/Invoke), but won't be changing during the loop
			int textLen = Editor.Length;
			
			//run the parser until the tag's closed, or we move to its sibling or parent
			if (node != null) {
				while (node.NextSibling == null &&
					treeParser.Position < textLen && treeParser.Nodes.Peek () != ob.Parent)
				{
					char c = Editor.GetCharAt (treeParser.Position);
					treeParser.Push (c);
					if (el != null && el.IsClosed && el.ClosingTag.IsComplete)
						break;
				}
			} else {
				while (ob.Region.End < ob.Region.Start &&
			       		treeParser.Position < textLen && treeParser.Nodes.Peek () != ob.Parent)
				{
					char c = Editor.GetCharAt (treeParser.Position);
					treeParser.Push (c);
				}
			}
			
			if (el == null) {
				MonoDevelop.Core.LoggingService.LogDebug ("Selecting {0}", ob.Region);
				EditorSelect (ob.Region);
			}
			else if (el.IsClosed) {
				MonoDevelop.Core.LoggingService.LogDebug ("Selecting {0}-{1}",
				    el.Region.Start, el.ClosingTag.Region.End);
				
				if (el.IsSelfClosing)
					contents = false;
				
				//pick out the locations, with some offsets to account for the parsing model
				DomLocation s = contents? el.Region.End : el.Region.Start;
				DomLocation e = contents? el.ClosingTag.Region.Start : el.ClosingTag.Region.End;
				EditorSelect (new DomRegion (s, e));
			} else {
				MonoDevelop.Core.LoggingService.LogDebug ("No end tag found for selection");
			}
		}
		
		protected void EditorSelect (DomRegion region)
		{
			int s = Editor.Document.LocationToOffset (region.Start.Line, region.Start.Column);
			int e = Editor.Document.LocationToOffset (region.End.Line, region.End.Column);
			if (s > -1 && e > s) {
				Editor.SetSelection (s, e);
				Editor.ScrollTo (s);
			}
		}
		
		public event EventHandler<DocumentPathChangedEventArgs> PathChanged;
		
		public Gtk.Widget CreatePathWidget (int index)
		{
			Menu menu = new Menu ();
			MenuItem mi = new MenuItem (GettextCatalog.GetString ("Select"));
			mi.Activated += delegate {
				SelectPath (index);
			};
			menu.Add (mi);
			mi = new MenuItem (GettextCatalog.GetString ("Select contents"));
			mi.Activated += delegate {
				SelectPathContents (index);
			};
			menu.Add (mi);
			menu.ShowAll ();
			return menu;
		}
		
		protected void OnPathChanged (PathEntry[] oldPath)
		{
			if (PathChanged != null)
				PathChanged (this, new DocumentPathChangedEventArgs (oldPath));
		}
		
		protected XName GetCompleteName ()
		{
			Debug.Assert (this.tracker.Engine.CurrentState is XmlNameState);
			
			int end = this.tracker.Engine.Position;
			int start = end - this.tracker.Engine.CurrentStateLength;
			int mid = -1;
			
			int limit = Math.Min (Editor.Length, end + 35);
			
			//try to find the end of the name, but don't go too far
			for (; end < limit; end++) {
				char c = Editor.GetCharAt (end);
				
				if (c == ':') {
					if (mid == -1)
						mid = end;
					else
						break;
				} else if (!XmlChar.IsNameChar (c))
					break;
			}
			
			if (mid > 0 && end > mid + 1) {
				return new XName (Editor.GetTextBetween (start, mid), Editor.GetTextBetween (mid + 1, end));
			} else {
				return new XName (Editor.GetTextBetween (start, end));
			}
		}
		/*
		protected XNode GetFullNode ()
		{
			Parser p = (Parser) ((ICloneable)this.tracker.Engine).Clone ();
			
			//run the parser until the tag's closed, or we move to its sibling or parent
			if (node != null) {
				while (node.NextSibling == null &&
					treeParser.Position < textLen && treeParser.Nodes.Peek () != ob.Parent)
				{
					char c = Editor.GetCharAt (treeParser.Position);
					treeParser.Push (c);
					if (el != null && el.IsClosed && el.ClosingTag.IsComplete)
						break;
				}
			}
		}
		*/
		List<XObject> GetCurrentPath ()
		{
			if (this.tracker == null)
				return null;

			this.tracker.UpdateEngine ();
			List<XObject> path = new List<XObject> (this.tracker.Engine.Nodes);
			
			//remove the root XDocument
			path.RemoveAt (path.Count - 1);
			
			//complete incomplete XName if present
			if (this.tracker.Engine.CurrentState is XmlNameState && path[0] is INamedXObject) {
				path[0] = (XObject) path[0].ShallowCopy ();
				XName completeName = GetCompleteName ();
				((INamedXObject)path[0]).Name = completeName;
			}
			path.Reverse ();
			return path;
		}
		
		void UpdatePath ()
		{
			List<XObject> l = GetCurrentPath ();

			if (l == null)
				return;
			
			//build the list
			PathEntry[] path = new PathEntry[l.Count];
			for (int i = 0; i < l.Count; i++) {
				if (l[i].FriendlyPathRepresentation == null) System.Console.WriteLine(l[i].GetType ());
				path[i] = new PathEntry (l[i].FriendlyPathRepresentation ?? "<>");
			}
			
			PathEntry[] oldPath = currentPath;
			currentPath = path;
			
			OnPathChanged (oldPath);
		}
		
		public PathEntry[] CurrentPath {
			get { return currentPath; }
		}
		#endregion
		
		#region IOutlinedDocument
		
		bool refreshingOutline = false;
		
		Gtk.Widget IOutlinedDocument.GetOutlineWidget ()
		{
			if (outlineTreeView != null)
				return outlineTreeView;
			
			outlineTreeStore = new Gtk.TreeStore (typeof (object));
			outlineTreeView = new MonoDevelop.Ide.Gui.Components.PadTreeView (outlineTreeStore);
			
			System.Reflection.PropertyInfo prop = typeof (Gtk.TreeView).GetProperty ("EnableTreeLines");
			if (prop != null)
				prop.SetValue (outlineTreeView, true, null);
			
			outlineTreeView.Realized += delegate { refillOutlineStore (); };
			
			InitializeOutlineColumns (outlineTreeView);
			
			outlineTreeView.HeadersVisible = false;
			
			outlineTreeView.Selection.Changed += delegate {
				Gtk.TreeIter iter;
				if (!outlineTreeView.Selection.GetSelected (out iter))
					return;
				OutlineSelectionChanged (outlineTreeStore.GetValue (iter, 0));
			};
			
			refillOutlineStore ();
			
			var sw = new MonoDevelop.Components.CompactScrolledWindow ();
			sw.Add (outlineTreeView);
			sw.ShowAll ();
			return sw;
		}
		
		protected virtual void InitializeOutlineColumns (MonoDevelop.Ide.Gui.Components.PadTreeView outlineTree)
		{
			outlineTree.TextRenderer.Xpad = 0;
			outlineTree.TextRenderer.Ypad = 0;
			outlineTree.AppendColumn ("Node", outlineTree.TextRenderer, new Gtk.TreeCellDataFunc (outlineTreeDataFunc));
		}
		
		protected virtual void OutlineSelectionChanged (object selection)
		{
			SelectNode ((XNode)selection);
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
			refillOutlineStore ();
			return false;
		}
		
		protected virtual void RefillOutlineStore (ParsedDocument doc, Gtk.TreeStore store)
		{
			XDocument xdoc = ((XmlParsedDocument)doc).XDocument;
			if (xdoc == null)
				return;
			BuildTreeChildren (store, Gtk.TreeIter.Zero, xdoc);
		}
		
		void refillOutlineStore ()
		{
			DispatchService.AssertGuiThread ();
			Gdk.Threads.Enter ();
			refreshingOutline = false;
			if (outlineTreeStore == null || !outlineTreeView.IsRealized)
				return;
			outlineTreeStore.Clear ();
			
			if (CU != null) {
				DateTime start = DateTime.Now;
				RefillOutlineStore (CU, outlineTreeStore);
				outlineTreeView.ExpandAll ();
				outlineTreeView.ExpandAll ();
				LoggingService.LogDebug ("Built outline in {0}ms", (DateTime.Now - start).Milliseconds);
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
		
		static void BuildTreeChildren (Gtk.TreeStore store, Gtk.TreeIter parent, XContainer p)
		{
			foreach (XNode n in p.Nodes) {
				Gtk.TreeIter childIter;
				if (!parent.Equals (Gtk.TreeIter.Zero))
					childIter = store.AppendValues (parent, n);
				else
					childIter = store.AppendValues (n);
				
				XContainer c = n as XContainer;
				if (c != null && c.FirstChild != null)
					BuildTreeChildren (store, childIter, c);
			}
		}
		
		void outlineTreeDataFunc (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			Gtk.CellRendererText txtRenderer = (Gtk.CellRendererText) cell;
			XNode n = (XNode) model.GetValue (iter, 0);
			txtRenderer.Text = n.FriendlyPathRepresentation;
		}
		
		public void SelectNode (XNode n)
		{
			MonoDevelop.Projects.Dom.DomRegion region = n.Region;
			
			XElement el = n as XElement;
			if (el != null && el.IsClosed && el.ClosingTag.Region.End > region.End) {
				region.End = el.ClosingTag.Region.End;
			}
			EditorSelect (region);
		}		
		#endregion
	}
}
