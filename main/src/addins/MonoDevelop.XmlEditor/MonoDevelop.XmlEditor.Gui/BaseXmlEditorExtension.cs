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
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

using MonoDevelop.Core;
using MonoDevelop.DesignerSupport;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Xml.StateEngine;
using MonoDevelop.XmlEditor.Completion;

namespace MonoDevelop.XmlEditor.Gui
{
	
	
	public abstract class BaseXmlEditorExtension : CompletionTextEditorExtension, IPathedDocument, IOutlinedDocument
	{
		DocumentStateTracker<Parser> tracker;
		ParsedDocument lastCU;
		
		string docType;
		
		Gtk.TreeView outlineTreeView;
		Gtk.TreeStore outlineTreeStore;

		#region Setup and teardown
		
		protected virtual RootState CreateRootState ()
		{
			return new XmlFreeState ();
		}
		
		protected abstract IEnumerable<string> SupportedExtensions { get; }

		public override void Initialize ()
		{
			base.Initialize ();
			Parser parser = new Parser (CreateRootState (), false);
			tracker = new DocumentStateTracker<Parser> (parser, Editor);
			MonoDevelop.Projects.Dom.Parser.ProjectDomService.ParsedDocumentUpdated += OnParseInformationChanged;
			
			lastCU = Document.ParsedDocument;
			if (lastCU != null) {
				RefreshOutline ();
			}
		}

		public override bool ExtendsEditor (MonoDevelop.Ide.Gui.Document doc, IEditableTextBuffer editor)
		{
			string title = doc.Name;
			foreach (string ext in SupportedExtensions )
				if (title.EndsWith (ext))
					return true;
			return false;
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
				RefreshOutline ();
				
				//use the doctype to select a completion schema
				XmlParsedDocument doc = CU as XmlParsedDocument;
				bool found = false;
				if (doc != null && doc.XDocument != null) {
					foreach (XNode node in doc.XDocument.Nodes) {
						if (node is XDocType) {
							DocType = ((XDocType)node).Value;
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
		
		protected string DocType {
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
		
		protected bool AutoCompleteClosingTags { get; set; }

		#region Code completion

		public override ICompletionDataList CodeCompletionCommand (ICodeCompletionContext completionContext)
		{
			int pos = completionContext.TriggerOffset;
			string txt = Editor.GetText (pos - 1, pos);
			int triggerWordLength = 0;
			
			if (txt.Length > 0) {
				tracker.UpdateEngine ();
				return HandleCodeCompletion ((CodeCompletionContext) completionContext, true, ref triggerWordLength);
			}
			return null;
		}

		public override ICompletionDataList HandleCodeCompletion (
		    ICodeCompletionContext completionContext, char completionChar, ref int triggerWordLength)
		{
			int pos = completionContext.TriggerOffset;
			if (pos > 0 && Editor.GetCharAt (pos - 1) == completionChar) {
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
					if (AutoCompleteClosingTags) {
						buf.BeginAtomicUndo ();
						buf.InsertText (buf.CursorPosition, tag);
						buf.CursorPosition -= tag.Length;
						buf.EndAtomicUndo ();
						return null;
					} else {
						CompletionDataList cp = new CompletionDataList ();
						cp.Add (new XmlTagCompletionData (tag, 0, true));
						return cp;
					}
				}
				return null;
			}
			
			//element completion
			if (currentChar == '<' && tracker.Engine.CurrentState is XmlFreeState) {
				CompletionDataList list = new CompletionDataList ();
				GetElementCompletions (list);
				return list.Count > 0? list : null;
			}
			
			//entity completion
			if (currentChar == '&' && (tracker.Engine.CurrentState is XmlFreeState ||
			                           tracker.Engine.CurrentState is XmlAttributeValueState))
			{
				CompletionDataList list = new CompletionDataList ();
				
				//todo: need to tweak semicolon insertion
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
					
					Dictionary<string, string> existingAtts = new Dictionary<string,string>
						(StringComparer.InvariantCultureIgnoreCase);
					
					foreach (XAttribute att in attributedOb.Attributes) {
						existingAtts [att.Name.FullName] = att.Value ?? string.Empty;
					}
					
					CompletionDataList list = new CompletionDataList ();
					GetAttributeCompletions (list, attributedOb, existingAtts);
					return list.Count > 0? list : null;
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
						
						CompletionDataList list = new CompletionDataList ();
						GetAttributeValueCompletions (list, attributedOb, att);	
						return list.Count > 0? list : null;
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
		
		protected virtual void GetAttributeCompletions (CompletionDataList list, IAttributedXObject attributedOb,
		                                                Dictionary<string, string> existingAtts)
		{
		}
		
		protected virtual void GetAttributeValueCompletions (CompletionDataList list,
		                                                     IAttributedXObject attributedOb,
		                                                     XAttribute att)
		{
		}
		
		protected virtual void GetEntityCompletions (CompletionDataList list)
		{
		}
		
		protected virtual CompletionDataList GetDocTypeCompletions ()
		{
			return null;
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
		
		protected static void AddMiscBeginTags (CompletionDataList list)
		{
			list.Add ("!--",  "md-literal", GettextCatalog.GetString ("Comment"));
			list.Add ("![CDATA[", "md-literal", GettextCatalog.GetString ("Character data"));
		}

		#endregion
		
		#region IPathedDocument
		
		string[] currentPath;
		int selectedPathIndex;
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
			int textLen = Editor.TextLength;
			
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
				EditorSelect (ob.Region.Start, ob.Region.End);
			}
			else if (el.IsClosed) {
				MonoDevelop.Core.LoggingService.LogDebug ("Selecting {0}-{1}",
				    el.Region.Start, el.ClosingTag.Region.End);
				
				if (el.IsSelfClosing)
					contents = false;
				
				//pick out the locations, with some offsets to account for the parsing model
				DomLocation s = contents? el.Region.End : el.Region.Start;
				DomLocation e = contents? el.ClosingTag.Region.Start : el.ClosingTag.Region.End;
				EditorSelect (s, e);
			} else {
				MonoDevelop.Core.LoggingService.LogDebug ("No end tag found for selection");
			}
		}
		
		void EditorSelect (DomLocation start, DomLocation end)
		{
			int s = Editor.GetPositionFromLineColumn (start.Line, start.Column);
			int e = Editor.GetPositionFromLineColumn (end.Line, end.Column);
			if (s > -1 && e > s)
				Editor.Select (s, e);
		}
		
		public event EventHandler<DocumentPathChangedEventArgs> PathChanged;
		
		protected void OnPathChanged (string[] oldPath, int oldSelectedIndex)
		{
			if (PathChanged != null)
				PathChanged (this, new DocumentPathChangedEventArgs (oldPath, oldSelectedIndex));
		}
		
		protected XName GetCompleteName ()
		{
			Debug.Assert (this.tracker.Engine.CurrentState is XmlNameState);
			
			int end = this.tracker.Engine.Position;
			int start = end - this.tracker.Engine.CurrentStateLength;
			int mid = -1;
			
			int limit = Math.Min (Editor.TextLength, end + 35);
			
			//try to find the end of the name, but don't go too far
			for (; end < limit; end++) {
				char c = Editor.GetCharAt (end);
				
				if (c == ':') {
					if (mid == -1)
						mid = end - 1;
					else
						break;
				} else if (!XmlChar.IsNameChar (c))
					break;
			}
			
			if (mid > 0) {
				return new XName (Editor.GetText (start, mid), Editor.GetText (mid, end));
			} else {
				return new XName (Editor.GetText (start, end));
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
			string[] path = new string[l.Count];
			for (int i = 0; i < l.Count; i++) {
				if (l[i].FriendlyPathRepresentation == null) System.Console.WriteLine(l[i].GetType ());
				path[i] = l[i].FriendlyPathRepresentation ?? "<>";
			}
			
			string[] oldPath = currentPath;
			int oldIndex = selectedPathIndex;
			currentPath = path;
			selectedPathIndex = currentPath.Length - 1;
			
			OnPathChanged (oldPath, oldIndex);
		}
		
		public string[] CurrentPath {
			get { return currentPath; }
		}
		
		public int SelectedIndex {
			get { return selectedPathIndex; }
		}
		
		#endregion
		
		#region IOutlinedDocument
		
		bool refreshingOutline = false;
		
		Gtk.Widget IOutlinedDocument.GetOutlineWidget ()
		{
			if (outlineTreeView != null)
				return outlineTreeView;
			
			outlineTreeStore = new Gtk.TreeStore (typeof (object));
			outlineTreeView = new Gtk.TreeView (outlineTreeStore);
			
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
			
			Gtk.ScrolledWindow sw = new Gtk.ScrolledWindow ();
			sw.Add (outlineTreeView);
			sw.ShowAll ();
			return sw;
		}
		
		protected virtual void InitializeOutlineColumns (Gtk.TreeView outlineTree)
		{
			Gtk.CellRendererText crt = new Gtk.CellRendererText ();
			crt.Xpad = 0;
			crt.Ypad = 0;
			outlineTree.AppendColumn ("Node", crt, new Gtk.TreeCellDataFunc (outlineTreeDataFunc));
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
			MonoDevelop.Core.Gui.DispatchService.AssertGuiThread ();
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
			
			int s = Editor.GetPositionFromLineColumn (region.Start.Line, region.Start.Column);
			int e = Editor.GetPositionFromLineColumn (region.End.Line, region.End.Column);
			if (e > s && s > -1)
				Editor.Select (s, e);
		}		
		#endregion
	}
}
