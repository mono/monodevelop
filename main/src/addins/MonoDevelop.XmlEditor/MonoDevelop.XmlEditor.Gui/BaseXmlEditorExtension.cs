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

namespace MonoDevelop.XmlEditor.Gui
{
	
	
	public abstract class BaseXmlEditorExtension : CompletionTextEditorExtension, IPathedDocument, IOutlinedDocument
	{
		DocumentStateTracker<Parser> tracker;
		ParsedDocument lastCU;
		
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
			string title = doc.Title;
			foreach (string ext in SupportedExtensions )
				if (doc.Title.EndsWith (ext))
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
		
		#endregion

		#region Code completion

		public override ICompletionDataList CodeCompletionCommand (ICodeCompletionContext completionContext)
		{
			int pos = completionContext.TriggerOffset;
			string txt = Editor.GetText (pos - 1, pos);
			int triggerWordLength = 0;
			ICompletionDataList list = null;
			if (txt.Length > 0)
				list = HandleCodeCompletion ((CodeCompletionContext) completionContext, true, ref triggerWordLength);
			
			return list;
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
			tracker.UpdateEngine ();

			//FIXME: lines in completionContext are zero-indexed, but ILocation and buffer are 1-indexed.
			//This could easily cause bugs.
			int line = completionContext.TriggerLine + 1, col = completionContext.TriggerLineOffset;
			
			ITextBuffer buf = this.Buffer;

			// completionChar may be a space even if the current char isn't, when ctrl-space is fired t
			int currentPosition = buf.CursorPosition - 1;
			char currentChar = buf.GetCharAt (currentPosition);
			char previousChar = buf.GetCharAt (currentPosition - 1);

			LoggingService.LogDebug ("Attempting completion for state '{0}'x{1}, previousChar='{2}'," 
				+ " currentChar='{3}', forced='{4}'", tracker.Engine.CurrentState,
				tracker.Engine.CurrentStateLength, previousChar, currentChar, forced);
			
			//closing tag completion
			if (tracker.Engine.CurrentState is XmlFreeState && currentPosition - 1 > 0 && currentChar == '>') {
				//get name of current node in document that's being ended
				XElement el = tracker.Engine.Nodes.Peek () as XElement;
				if (el != null && el.Position.End >= currentPosition && !el.IsClosed && el.IsNamed) {
					CompletionDataList cp = new CompletionDataList ();
					cp.Add (new MonoDevelop.XmlEditor.Completion.XmlTagCompletionData (
					        String.Concat ("</", el.Name.FullName, ">"), 0, true));
					return cp;
				}
				return null;
			}
			
			//element completion
			if (currentChar == '<' && tracker.Engine.CurrentState is XmlFreeState) {
				CompletionDataList list = new CompletionDataList ();
				GetElementCompletions (list);
				return list.Count > 0? list : null;
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
			if ((forced && Tracker.Engine.Nodes.Peek () is IAttributedXObject)
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
					if (currentPosition + 1 < buf.Length)
						next = buf.GetCharAt (currentPosition + 1);
					
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
		
		//FIXME: this should offer all unclosed tags, and accepting them should close back up to that level
		protected static void AddCloseTag (CompletionDataList completionList, NodeStack stack)
		{
			//FIXME: search forward to see if tag's closed already
			foreach (XObject ob in stack) {
				XElement el = ob as XElement;
				if (el != null && el.IsNamed && !el.IsClosed) {
					string name = el.Name.FullName;
					completionList.Add ("/" + name + ">", Gtk.Stock.GoBack,
					                    GettextCatalog.GetString ("Closing tag for '{0}'", name));
					return;
				}
			}
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
				while (ob.Position.End < ob.Position.Start &&
			       		treeParser.Position < textLen && treeParser.Nodes.Peek () != ob.Parent)
				{
					char c = Editor.GetCharAt (treeParser.Position);
					treeParser.Push (c);
				}
			}
			
			if (el == null) {
				MonoDevelop.Core.LoggingService.LogDebug ("Selecting {0}", ob.Position);
				int s = ob.Position.Start;
				int e = ob.Position.End;
				if (s > -1 && e > s)
					Editor.Select (s, e);
			}
			else if (el.IsClosed) {
				MonoDevelop.Core.LoggingService.LogDebug ("Selecting {0}-{1}",
				    el.Position, el.ClosingTag.Position);
				
				if (el.IsSelfClosing)
					contents = false;
				
				//pick out the locations, with some offsets to account for the parsing model
				int s = contents? el.Position.End : el.Position.Start;
				int e = contents? el.ClosingTag.Position.Start : el.ClosingTag.Position.End;
				
				if (s > -1 && e > s)
					Editor.Select (s, e);
			} else {
				MonoDevelop.Core.LoggingService.LogDebug ("No end tag found for selection");
			}
		}
		
		public event EventHandler<DocumentPathChangedEventArgs> PathChanged;
		
		protected void OnPathChanged (string[] oldPath, int oldSelectedIndex)
		{
			if (PathChanged != null)
				PathChanged (this, new DocumentPathChangedEventArgs (oldPath, oldSelectedIndex));
		}
		
		XName GetCompleteName ()
		{
			Debug.Assert (this.tracker.Engine.CurrentState is XmlNameState);
			
			int pos = this.tracker.Engine.Position;
			
			//hoist this as it may not be cheap to evaluate (P/Invoke), but won't be changing during the loop
			int textLen = Editor.TextLength;
			
			//try to find the end of the name, but don't go too far
			for (int len = 0; pos < textLen && len < 30; pos++, len++) {
				char c = Editor.GetCharAt (pos);
				if (!char.IsLetterOrDigit (c) && c != ':' && c != '_')
					break;
			}
			
			return new XName (Editor.GetText (this.tracker.Engine.Position - this.tracker.Engine.CurrentStateLength, pos));
		}
		
		List<XObject> GetCurrentPath ()
		{
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
		}
		
		protected virtual void OutlineSelectionChanged (object selection)
		{
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
		
		#endregion
	}
}
