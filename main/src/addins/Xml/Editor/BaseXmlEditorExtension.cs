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
using System.Linq;

using Gtk;


using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.DesignerSupport;
using MonoDevelop.Ide;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Projects;
using MonoDevelop.Xml.Completion;
using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Parser;
using System.Threading.Tasks;
using System.Threading;

namespace MonoDevelop.Xml.Editor
{
	public abstract class BaseXmlEditorExtension : CompletionTextEditorExtension, IPathedDocument, IOutlinedDocument
	{
		DocumentStateTracker<XmlParser> tracker;
		ParsedDocument lastCU;
		
		XDocType docType;
		
		MonoDevelop.Ide.Gui.Components.PadTreeView outlineTreeView;
		TreeStore outlineTreeStore;
		List<DotNetProject> ownerProjects = new List<DotNetProject> ();

		#region Setup and teardown

		public override bool IsValidInContext (DocumentContext context)
		{
			//can only attach if there is not already an attached BaseXmlEditorExtension
			return context.GetContent<BaseXmlEditorExtension> () == null;
		}
		
		protected virtual XmlRootState CreateRootState ()
		{
			return new XmlRootState ();
		}
		protected override void Initialize ()
		{
			base.Initialize ();

			// Delay the execution of UpdateOwnerProjects since it may end calling Document.AttachToProject,
			// which shouldn't be called while the extension chain is being initialized.
			// TODO: Move handling of owner projects to Document
			Application.Invoke (delegate {
				UpdateOwnerProjects ();
			});

			var parser = new XmlParser (CreateRootState (), false);
			tracker = new DocumentStateTracker<XmlParser> (parser, Editor);
			DocumentContext.DocumentParsed += UpdateParsedDocument;
			Editor.CaretPositionChanged += HandleCaretPositionChanged;

			if (DocumentContext.ParsedDocument != null) {
				lastCU = DocumentContext.ParsedDocument;
				OnParsedDocumentUpdated ();
			}

			if (IdeApp.Workspace != null) {
				IdeApp.Workspace.FileAddedToProject += HandleProjectChanged;
				IdeApp.Workspace.FileRemovedFromProject += HandleProjectChanged;
			}
		}

		void HandleProjectChanged (object sender, ProjectFileEventArgs e)
		{
			if (e.Any (f => f.ProjectFile.FilePath == DocumentContext.Name))
				UpdateOwnerProjects ();
		}

		void UpdateOwnerProjects ()
		{
			if (IdeApp.Workspace == null) {
				ownerProjects = new List<DotNetProject> ();
				return;
			}
			var projects = new HashSet<DotNetProject> (IdeApp.Workspace.GetAllItems<DotNetProject> ().Where (p => p.IsFileInProject (DocumentContext.Name)));
			if (ownerProjects == null || !projects.SetEquals (ownerProjects)) {
				ownerProjects = projects.OrderBy (p => p.Name).ToList ();
				var dnp = DocumentContext.Project as DotNetProject;
				if (ownerProjects.Count > 0 && (dnp == null || !ownerProjects.Contains (dnp))) {
					// If the project for the document is not a DotNetProject but there is a project containing this file
					// in the current solution, then use that project
					var pp = DocumentContext.Project != null ? ownerProjects.FirstOrDefault (p => p.ParentSolution == DocumentContext.Project.ParentSolution) : null;
					if (pp != null)
						DocumentContext.AttachToProject (pp);
				}
			}
			if (DocumentContext.Project == null && ownerProjects.Count > 0)
				DocumentContext.AttachToProject (ownerProjects[0]);
			UpdatePath ();
		}

		void UpdateParsedDocument (object sender, EventArgs args)
		{
			lastCU = DocumentContext.ParsedDocument;
			OnParsedDocumentUpdated ();
		}

		public override void Dispose ()
		{
			Editor.CaretPositionChanged -= HandleCaretPositionChanged;

			if (tracker != null) {
				tracker = null;
				base.Dispose ();
			}

			DocumentContext.DocumentParsed -= UpdateParsedDocument;

			if (IdeApp.Workspace != null) {
				IdeApp.Workspace.FileAddedToProject -= HandleProjectChanged;
				IdeApp.Workspace.FileRemovedFromProject -= HandleProjectChanged;
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

		protected DocumentStateTracker<XmlParser> Tracker {
			get { return tracker; }
		}
		
		protected string GetBufferText (DocumentRegion region)
		{
			int start = Editor.LocationToOffset (region.BeginLine, region.BeginColumn);
			int end = Editor.LocationToOffset (region.EndLine, region.EndColumn);
			if (end > start && start >= 0)
				return Editor.GetTextBetween (start, end);
			return null;
		}
		
		#endregion

		public override string CompletionLanguage {
			get { return "Xml"; }
		}

		protected FilePath FileName {
			get {
				return Editor.FileName;
			}
		}
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
		
		public override bool KeyPress (KeyDescriptor descriptor)
		{
			if (Editor.Options.IndentStyle == IndentStyle.Smart) {
				var newLine = Editor.CaretLine + 1;
				var ret = base.KeyPress (descriptor);
				if (descriptor.SpecialKey == SpecialKey.Return && Editor.CaretLine == newLine) {
					string indent = GetLineIndent (newLine);
					var oldIndent = Editor.GetLineIndent (newLine);
					var seg = Editor.GetLine (newLine);
					if (oldIndent != indent) {
						using (var undo = Editor.OpenUndoGroup ()) {
							Editor.ReplaceText (seg.Offset, oldIndent.Length, indent);
						}
					}
				}
				return ret;
			}
			return base.KeyPress (descriptor);
		}
		
		#region Code completion

		public override Task<ICompletionDataList> CodeCompletionCommand (CodeCompletionContext completionContext)
		{
			int pos = completionContext.TriggerOffset;
			if (pos <= 0)
				return null;
			tracker.UpdateEngine ();
			return HandleCodeCompletion (completionContext, true, default(CancellationToken));
		}

		public override Task<ICompletionDataList> HandleCodeCompletionAsync (CodeCompletionContext completionContext, char completionChar, CancellationToken token = default(CancellationToken))
		{
			int pos = completionContext.TriggerOffset;
			char ch = CompletionWidget != null ? CompletionWidget.GetChar (pos - 1) : Editor.GetCharAt (pos - 1);
			if (pos > 0 && ch == completionChar) {
				tracker.UpdateEngine ();
				return HandleCodeCompletion (completionContext, false, token);
			}
			return null;
		}

		protected virtual Task<ICompletionDataList> HandleCodeCompletion (
			CodeCompletionContext completionContext, bool forced, CancellationToken token)
		{
			var buf = this.Editor;

			// completionChar may be a space even if the current char isn't, when ctrl-space is fired t
			var currentLocation = new DocumentLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset);
			char currentChar = completionContext.TriggerOffset < 1? ' ' : buf.GetCharAt (completionContext.TriggerOffset - 1);
			char previousChar = completionContext.TriggerOffset < 2? ' ' : buf.GetCharAt (completionContext.TriggerOffset - 2);

			LoggingService.LogDebug ("Attempting completion for state '{0}'x{1}, previousChar='{2}'," 
				+ " currentChar='{3}', forced='{4}'", tracker.Engine.CurrentState,
				tracker.Engine.CurrentStateLength, previousChar, currentChar, forced);
			
			//closing tag completion
			if (tracker.Engine.CurrentState is XmlRootState && currentChar == '>')
				return Task.FromResult (ClosingTagCompletion (buf, currentLocation));
			
			// Auto insert '>' when '/' is typed inside tag state (for quick tag closing)
			//FIXME: avoid doing this when the next non-whitespace char is ">" or ignore the next ">" typed
			if (XmlEditorOptions.AutoInsertFragments && tracker.Engine.CurrentState is XmlTagState && currentChar == '/') {
				buf.InsertAtCaret (">");
				return null;
			}
			
			//entity completion
			if (currentChar == '&' && (tracker.Engine.CurrentState is XmlRootState ||
			                           tracker.Engine.CurrentState is XmlAttributeValueState))
			{
				var list = new CompletionDataList ();
				
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
				return Task.FromResult ((ICompletionDataList)list);
			}
			
			//doctype completion
			if (tracker.Engine.CurrentState is XmlDocTypeState) {
				if (tracker.Engine.CurrentStateLength == 1) {
					CompletionDataList list = GetDocTypeCompletions ();
					if (list != null && list.Count > 0)
						return Task.FromResult ((ICompletionDataList)list);
				}
				return null;
			}

			//attribute value completion
			//determine whether to trigger completion within attribute values quotes
			if ((Tracker.Engine.CurrentState is XmlAttributeValueState)
			    //trigger on the opening quote
			    && ((Tracker.Engine.CurrentStateLength == 1 && (currentChar == '\'' || currentChar == '"'))
			    //or trigger on first letter of value, if unforced
			    || (forced || Tracker.Engine.CurrentStateLength == 2))) {
				var att = (XAttribute)Tracker.Engine.Nodes.Peek ();

				if (att.IsNamed) {
					var attributedOb = Tracker.Engine.Nodes.Peek (1) as IAttributedXObject;
					if (attributedOb == null)
						return null;

					//if triggered by first letter of value or forced, grab those letters

					var result = GetAttributeValueCompletions (attributedOb, att);
					if (result != null) {
						result.TriggerWordLength = Tracker.Engine.CurrentStateLength - 1;
						return Task.FromResult ((ICompletionDataList)result);
					}
					return null;
				}
			}
			
			//attribute name completion
			if ((forced && Tracker.Engine.Nodes.Peek () is IAttributedXObject && !tracker.Engine.Nodes.Peek ().IsEnded)
			     || ((Tracker.Engine.CurrentState is XmlNameState
			    && Tracker.Engine.CurrentState.Parent is XmlAttributeState) ||
			    Tracker.Engine.CurrentState is XmlTagState)
			    && (Tracker.Engine.CurrentStateLength == 1 || forced)) {
				IAttributedXObject attributedOb = (Tracker.Engine.Nodes.Peek () as IAttributedXObject) ?? 
					Tracker.Engine.Nodes.Peek (1) as IAttributedXObject;
				if (attributedOb == null)
					return null;
				
				//attributes
				if (attributedOb.Name.IsValid && (forced ||
					(char.IsWhiteSpace (previousChar) && char.IsLetter (currentChar))))
				{
					var existingAtts = new Dictionary<string,string> (StringComparer.OrdinalIgnoreCase);
					
					foreach (XAttribute att in attributedOb.Attributes) {
						existingAtts [att.Name.FullName] = att.Value ?? string.Empty;
					}
					var result = GetAttributeCompletions (attributedOb, existingAtts);
					if (result != null) {
						if (!forced)
							result.TriggerWordLength = 1;
						return Task.FromResult ((ICompletionDataList)result);
					}
					return null;
				}
			}
			
//			if (Tracker.Engine.CurrentState is XmlRootState) {
//				if (line < 3) {
//				cp.Add ("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>");
//			}

			//element completion
			if (currentChar == '<' && tracker.Engine.CurrentState is XmlRootState ||
				(tracker.Engine.CurrentState is XmlNameState && forced)) {
				var list = new CompletionDataList ();
				GetElementCompletions (list);
				AddCloseTag (list, Tracker.Engine.Nodes);
				return Task.FromResult ((ICompletionDataList)(list.Count > 0? list : null));
			}

			if (forced && Tracker.Engine.CurrentState is XmlRootState) {
				var list = new CompletionDataList ();
				MonoDevelop.Ide.CodeTemplates.CodeTemplateService.AddCompletionDataForFileName (DocumentContext.Name, list);
				return Task.FromResult ((ICompletionDataList)(list.Count > 0? list : null));
			}
			
			return null;
		}



		protected virtual ICompletionDataList ClosingTagCompletion (TextEditor buf, DocumentLocation currentLocation)

		{

			//get name of current node in document that's being ended

			var el = tracker.Engine.Nodes.Peek () as XElement;

			if (el != null && el.Region.End >= currentLocation && !el.IsClosed && el.IsNamed) {

				string tag = String.Concat ("</", el.Name.FullName, ">");

				if (XmlEditorOptions.AutoCompleteElements) {



					//						//make sure we have a clean atomic undo so the user can undo the tag insertion

					//						//independently of the >

					//						bool wasInAtomicUndo = this.Editor.Document.IsInAtomicUndo;

					//						if (wasInAtomicUndo)

					//							this.Editor.Document.EndAtomicUndo ();



					using (var undo = buf.OpenUndoGroup ()) {

						buf.InsertText (buf.CaretOffset, tag);

						buf.CaretOffset -= tag.Length;

					}



					//						if (wasInAtomicUndo)

					//							this.Editor.Document.BeginAtomicUndo ();



					return null;

				} else {

					var cp = new CompletionDataList ();

					cp.Add (new XmlTagCompletionData (tag, 0, true));

					return cp;

				}

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
			var seg = Editor.GetLine (line);
			
			//reset the tracker to the beginning of the line
			Tracker.UpdateEngine (seg.Offset);
			
			//calculate the indentation
			var startElementDepth = GetElementIndentDepth (Tracker.Engine.Nodes);
			var attributeDepth = GetAttributeIndentDepth (Tracker.Engine.Nodes);
			
			//update the tracker to the end of the line 
			Tracker.UpdateEngine (seg.Offset + seg.Length);
			
			//if end depth is less than start depth, then reduce start depth
			//because that means there are closing tags on the line, and they de-indent the line they're on
			var endElementDepth = GetElementIndentDepth (Tracker.Engine.Nodes);
			var elementDepth = Math.Min (endElementDepth, startElementDepth);
			
			//FIXME: use policies
			return new string ('\t', elementDepth + attributeDepth);
		}
		
		static int GetElementIndentDepth (NodeStack nodes)
		{
			return nodes.OfType<XElement> ().Count (el => !el.IsClosed);
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
				var el = obj as XElement;
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
				var el = obj as XElement;
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
			var elements = new List<XElement> ();
			foreach (XObject ob in stack) {
				var el = ob as XElement;
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
		bool pathUpdateQueued;
		
		void HandleCaretPositionChanged (object sender, EventArgs e)
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
			XmlParser treeParser = tracker.Engine.GetTreeParser ();
			
			//locate the node
			var path = new List<XObject> (treeParser.Nodes);
			//note: list is backwards, and we want ignore the root XDocument
			XObject ob = path [path.Count - (depth + 2)];
			var node = ob as XNode;
			var el = node as XElement;
			
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
				while (ob.Region.End < ob.Region.Begin &&
			       		treeParser.Position < textLen && treeParser.Nodes.Peek () != ob.Parent)
				{
					char c = Editor.GetCharAt (treeParser.Position);
					treeParser.Push (c);
				}
			}
			
			if (el == null) {
				LoggingService.LogDebug ("Selecting {0}", ob.Region);
				EditorSelect (ob.Region);
			}
			else if (el.IsClosed) {
				LoggingService.LogDebug ("Selecting {0}-{1}",
				    el.Region.Begin, el.ClosingTag.Region.End);
				
				contents &= !el.IsSelfClosing;
				
				//pick out the locations, with some offsets to account for the parsing model
				var s = contents? el.Region.End : el.Region.Begin;
				var e = contents? el.ClosingTag.Region.Begin : el.ClosingTag.Region.End;
				EditorSelect (new DocumentRegion (s, e));
			} else {
				LoggingService.LogDebug ("No end tag found for selection");
			}
		}
		
		protected void EditorSelect (DocumentRegion region)
		{
			int s = Editor.LocationToOffset (region.BeginLine, region.BeginColumn);
			int e = Editor.LocationToOffset (region.EndLine, region.EndColumn);
			if (s > -1 && e > s) {
				Editor.SetSelection (s, e);
				Editor.ScrollTo (s);
			}
		}
		
		public event EventHandler<DocumentPathChangedEventArgs> PathChanged;
		
		public Widget CreatePathWidget (int index)
		{
			if (ownerProjects.Count > 1 && index == 0) {
				var window = new DropDownBoxListWindow (new DataProvider (this));
				window.FixedRowHeight = 22;
				window.MaxVisibleRows = 14;
				window.SelectItem (currentPath [index].Tag);
				return window;
			} else {
				if (ownerProjects.Count > 1)
					index--;
				var menu = new Menu ();
				var mi = new MenuItem (GettextCatalog.GetString ("Select"));
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
		}


		class DataProvider : DropDownBoxListWindow.IListDataProvider
		{

			readonly BaseXmlEditorExtension ext;

			public DataProvider (BaseXmlEditorExtension ext)
			{
				if (ext == null)
					throw new ArgumentNullException ("ext");
				this.ext = ext;
			}

			#region IListDataProvider implementation

			public void Reset ()
			{
			}

			public string GetMarkup (int n)
			{
				return GLib.Markup.EscapeText (ext.ownerProjects [n].Name);
			}

			public Xwt.Drawing.Image GetIcon (int n)
			{
				return ImageService.GetIcon (ext.ownerProjects [n].StockIcon, IconSize.Menu);
			}

			public object GetTag (int n)
			{
				return ext.ownerProjects [n];
			}

			public void ActivateItem (int n)
			{
				ext.DocumentContext.AttachToProject (ext.ownerProjects [n]);
			}

			public int IconCount {
				get {
					return ext.ownerProjects.Count;
				}
			}

			#endregion
		}
		
		protected void OnPathChanged (PathEntry[] oldPath)
		{
			if (PathChanged != null)
				PathChanged (this, new DocumentPathChangedEventArgs (oldPath));
		}
		
		protected XName GetCompleteName ()
		{
			Debug.Assert (tracker.Engine.CurrentState is XmlNameState);
			
			int end = tracker.Engine.Position;
			int start = end - tracker.Engine.CurrentStateLength;
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
			}
			return new XName (Editor.GetTextBetween (start, end));
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
		
		protected List<XObject> GetCurrentPath ()
		{
			if (tracker == null)
				return null;

			tracker.UpdateEngine ();
			var path = new List<XObject> (tracker.Engine.Nodes);
			
			//remove the root XDocument
			path.RemoveAt (path.Count - 1);
			
			//complete incomplete XName if present
			if (tracker.Engine.CurrentState is XmlNameState && path[0] is INamedXObject) {
				path [0] = path [0].ShallowCopy ();
				XName completeName = GetCompleteName ();
				((INamedXObject)path[0]).Name = completeName;
			}
			path.Reverse ();
			return path;
		}
		
		void UpdatePath ()
		{
			//update timeout could get called after disposed
			if (tracker == null)
				return;

			List<XObject> l = GetCurrentPath ();
			
			//build the list
			var path = new List<PathEntry> ();
			if (ownerProjects.Count > 1) {
				// Current project if there is more than one
				path.Add (new PathEntry (ImageService.GetIcon (DocumentContext.Project.StockIcon), GLib.Markup.EscapeText (DocumentContext.Project.Name)) { Tag = DocumentContext.Project });
			}
			if (l != null) {
				for (int i = 0; i < l.Count; i++) {
					path.Add (new PathEntry (GLib.Markup.EscapeText (l [i].FriendlyPathRepresentation ?? "<>")));
				}
			}
			
			PathEntry[] oldPath = currentPath;
			currentPath = path.ToArray ();
			
			OnPathChanged (oldPath);
		}
		
		public PathEntry[] CurrentPath {
			get { return currentPath; }
		}
		#endregion
		
		#region IOutlinedDocument
		
		bool refreshingOutline;
		
		Widget IOutlinedDocument.GetOutlineWidget ()
		{
			if (outlineTreeView != null)
				return outlineTreeView;
			
			outlineTreeStore = new TreeStore (typeof (object));
			outlineTreeView = new MonoDevelop.Ide.Gui.Components.PadTreeView (outlineTreeStore);
			
			System.Reflection.PropertyInfo prop = typeof (TreeView).GetProperty ("EnableTreeLines");
			if (prop != null)
				prop.SetValue (outlineTreeView, true, null);
			
			outlineTreeView.Realized += delegate { refillOutlineStore (); };
			
			InitializeOutlineColumns (outlineTreeView);
			
			outlineTreeView.HeadersVisible = false;
			
			outlineTreeView.Selection.Changed += delegate {
				TreeIter iter;
				if (!outlineTreeView.Selection.GetSelected (out iter))
					return;
				OutlineSelectionChanged (outlineTreeStore.GetValue (iter, 0));
			};
			
			refillOutlineStore ();
			
			var sw = new CompactScrolledWindow ();
			sw.Add (outlineTreeView);
			sw.ShowAll ();
			return sw;
		}
		
		IEnumerable<Widget> IOutlinedDocument.GetToolbarWidgets ()
		{
			return null;
		}
		
		protected virtual void InitializeOutlineColumns (MonoDevelop.Ide.Gui.Components.PadTreeView outlineTree)
		{
			outlineTree.TextRenderer.Xpad = 0;
			outlineTree.TextRenderer.Ypad = 0;
			outlineTree.AppendColumn ("Node", outlineTree.TextRenderer, new TreeCellDataFunc (outlineTreeDataFunc));
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
		
		protected virtual void RefillOutlineStore (ParsedDocument doc, TreeStore store)
		{
			XDocument xdoc = ((XmlParsedDocument)doc).XDocument;
			if (xdoc == null)
				return;
			BuildTreeChildren (store, TreeIter.Zero, xdoc);
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
			
			var w = (ScrolledWindow) outlineTreeView.Parent;
			w.Destroy ();
			outlineTreeView.Destroy ();
			outlineTreeStore.Dispose ();
			outlineTreeStore = null;
			outlineTreeView = null;
		}
		
		static void BuildTreeChildren (TreeStore store, TreeIter parent, XContainer p)
		{
			foreach (XNode n in p.Nodes) {
				TreeIter childIter;
				if (!parent.Equals (TreeIter.Zero))
					childIter = store.AppendValues (parent, n);
				else
					childIter = store.AppendValues (n);
				
				var c = n as XContainer;
				if (c != null && c.FirstChild != null)
					BuildTreeChildren (store, childIter, c);
			}
		}
		
		void outlineTreeDataFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			var txtRenderer = (CellRendererText) cell;
			var n = (XNode) model.GetValue (iter, 0);
			txtRenderer.Text = n.FriendlyPathRepresentation;
		}
		
		public void SelectNode (XNode n)
		{
			var region = n.Region;
			
			var el = n as XElement;
			if (el != null && el.IsClosed && el.ClosingTag.Region.End > region.End) {
				region = new DocumentRegion (region.Begin, el.ClosingTag.Region.End);
			}
			EditorSelect (region);
		}		
		#endregion
	}
}
