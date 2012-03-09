// 
// MDRefactoringContext.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp;
using System.Collections.Generic;
using Mono.TextEditor;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Refactoring;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.CSharp.Resolver;
using System.Linq;
using MonoDevelop.TypeSystem;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using System.Threading;

namespace MonoDevelop.CSharp.ContextAction
{
	public class MDRefactoringContext : RefactoringContext
	{
		public MonoDevelop.Ide.Gui.Document Document {
			get;
			private set;
		}
		
		public override bool HasCSharp3Support {
			get {
				var project = Document.Project as DotNetProject;
				if (project == null)
					return true;
				switch (project.TargetFramework.ClrVersion) {
				case ClrVersion.Net_1_1:
				case ClrVersion.Net_2_0:
				case ClrVersion.Clr_2_1:
					return false;
				default:
					return true;
				}
			}
		}
		
		public override CSharpFormattingOptions FormattingOptions {
			get {
				return Document.GetFormattingOptions ();
			}
		}
		
		public override string EolMarker {
			get {
				return Document.Editor.EolMarker;
			}
		}
		
		public override bool IsSomethingSelected { 
			get {
				return Document.Editor.IsSomethingSelected;
			}
		}
		
		public override string SelectedText {
			get {
				return Document.Editor.SelectedText;
			}
		}
		
		public override int SelectionStart {
			get {
				return Document.Editor.SelectionRange.Offset;
			}
		}
		
		public override int SelectionEnd { 
			get {
				return Document.Editor.SelectionRange.EndOffset;
			}
		}
		
		public override int SelectionLength {
			get {
				return Document.Editor.SelectionRange.Length;
			}
		}

		public override int GetOffset (TextLocation location)
		{
			return Document.Editor.LocationToOffset (location.Line, location.Column);
		}
		
		public override TextLocation GetLocation (int offset)
		{
			var loc = Document.Editor.OffsetToLocation (offset);
			return new TextLocation (loc.Line, loc.Column);
		}

		public override string GetText (int offset, int length)
		{
			return Document.Editor.GetTextAt (offset, length);
		}
		
		public override string GetText (ICSharpCode.NRefactory.Editor.ISegment segment)
		{
			return Document.Editor.GetTextAt (segment.Offset, segment.Length);
		}
		
		public override ICSharpCode.NRefactory.Editor.IDocumentLine GetLineByOffset (int offset)
		{
			return Document.Editor.GetLineByOffset (offset);
		}
		
		#region IChangeFactory implementation
		
		class MdTextReplaceAction : TextReplaceAction
		{
			MonoDevelop.Ide.Gui.Document doc;
			
			public MdTextReplaceAction (MonoDevelop.Ide.Gui.Document doc, int offset, int removedChars, string insertedText) : base (offset, removedChars, insertedText)
			{
				if (doc == null)
					throw new ArgumentNullException ("doc");
				this.doc = doc;
			}
			
			public override void Perform (Script script)
			{
				doc.Editor.Replace (Offset, RemovedChars, InsertedText);
			}
		}
		
		public override TextReplaceAction CreateTextReplaceAction (int offset, int removedChars, string insertedText)
		{
			return new MdTextReplaceAction (Document, offset, removedChars, insertedText);
		}
		
		class MdNodeOutputAction : NodeOutputAction
		{
			MonoDevelop.Ide.Gui.Document doc;
			
			public MdNodeOutputAction (MonoDevelop.Ide.Gui.Document doc, int offset, int removedChars, NodeOutput output) : base (offset, removedChars, output)
			{
				if (doc == null)
					throw new ArgumentNullException ("doc");
				if (output == null)
					throw new ArgumentNullException ("output");
				this.doc = doc;
			}
			
			public override void Perform (Script script)
			{
				doc.Editor.Replace (Offset, RemovedChars, NodeOutput.Text);
			}
		}
		
		public override NodeOutputAction CreateNodeOutputAction (int offset, int removedChars, NodeOutput output)
		{
			return new MdNodeOutputAction (Document, offset, removedChars, output);
		}
		
		class MdNodeSelectionAction : NodeSelectionAction
		{
			MonoDevelop.Ide.Gui.Document doc;
			
			public MdNodeSelectionAction (MonoDevelop.Ide.Gui.Document doc, AstNode node) : base (node)
			{
				if (doc == null)
					throw new ArgumentNullException ("doc");
				this.doc = doc;
			}
			
			public override void Perform (Script script)
			{
				foreach (var action in script.Actions) {
					if (action == this)
						break;
					var noa = action as NodeOutputAction;
					if (noa == null)
						continue;
					NodeOutput.Segment segment;
					if (noa.NodeOutput.NodeSegments.TryGetValue (AstNode, out segment)) {
						var lead = noa.Offset + segment.EndOffset;
						doc.Editor.Caret.Offset = lead;
						doc.Editor.SetSelection (noa.Offset + segment.Offset, lead);
					}
				}
			}
		}
		
		public override NodeSelectionAction CreateNodeSelectionAction (AstNode node)
		{
			return new MdNodeSelectionAction (this.Document, node);
		}
		
		class MdFormatTextAction : FormatTextAction
		{
			MDRefactoringContext ctx;

			public MdFormatTextAction (MDRefactoringContext ctx, Func<RefactoringContext, AstNode> callback) : base (callback)
			{
				this.ctx = ctx;
			}

			public override void Perform (Script script)
			{
				ctx.Document.UpdateParseDocument ();
				ctx.Unit = ctx.Document.ParsedDocument.GetAst<CompilationUnit> ();
			
				var node = Callback (ctx);
				if (node != null)
					node.FormatText (ctx.Document);
			}
		}
		
		public override FormatTextAction CreateFormatTextAction (Func<RefactoringContext, AstNode> callback)
		{
			return new MdFormatTextAction (this, callback);
		}
		
		class MdLinkAction : CreateLinkAction
		{
			MDRefactoringContext ctx;
			
			public MdLinkAction (MDRefactoringContext ctx, IEnumerable<AstNode> linkedNodes) : base (linkedNodes)
			{
				this.ctx = ctx;
			}
			
			public override void Perform (Script script)
			{
				List<Segment> segments = new List<Segment> ();
				foreach (var action in script.Actions) {
					if (action == this)
						break;
					var noa = action as NodeOutputAction;
					if (noa == null)
						continue;
					foreach (var astNode in Linked) {
						NodeOutput.Segment segment;
						if (noa.NodeOutput.NodeSegments.TryGetValue (astNode, out segment))
							segments.Add (new Segment (noa.Offset + segment.Offset, segment.Length));
					}
				}
				segments.Sort ((x, y) => x.Offset.CompareTo (y.Offset));
				var link = new TextLink ("name");
				segments.ForEach (s => link.AddLink (s));
				var links = new List<TextLink> ();
				links.Add (link);
				var tle = new TextLinkEditMode (ctx.Document.Editor.Parent, 0, links);
				tle.SetCaretPosition = false;
				if (tle.ShouldStartTextLinkMode) {
					tle.OldMode = ctx.Document.Editor.CurrentMode;
					tle.StartMode ();
					ctx.Document.Editor.CurrentMode = tle;
				}
			}
		}
		
		public override CreateLinkAction CreateLinkAction (IEnumerable<AstNode> linkedNodes)
		{
			return new MdLinkAction (this, linkedNodes);
		}
		
		#endregion
		
		public class MdScript : Script
		{
			MDRefactoringContext ctx;
			
			public MdScript (MDRefactoringContext ctx) : base(ctx)
			{
				this.ctx = ctx;
			}

			public static void RunActions (IList<ICSharpCode.NRefactory.CSharp.Refactoring.Action> actions, Script script)
			{
				for (int i = 0; i < actions.Count; i++) {
					actions [i].Perform (script);
					var replaceChange = actions [i] as TextReplaceAction;
					if (replaceChange == null)
						continue;
					for (int j = 0; j < actions.Count; j++) {
						if (i == j)
							continue;
						var change = actions [j] as TextReplaceAction;
						if (change == null)
							continue;
						if (replaceChange.Offset >= 0 && change.Offset >= 0) {
							if (replaceChange.Offset < change.Offset) {
								change.Offset -= replaceChange.RemovedChars;
								if (!string.IsNullOrEmpty (replaceChange.InsertedText))
									change.Offset += replaceChange.InsertedText.Length;
							} else if (replaceChange.Offset < change.Offset + change.RemovedChars) {
								change.RemovedChars = Math.Max (0, change.RemovedChars - replaceChange.RemovedChars);
								change.Offset = replaceChange.Offset + (!string.IsNullOrEmpty (replaceChange.InsertedText) ? replaceChange.InsertedText.Length : 0);
							}
						}
					}
				}
			}
			
			public override void Dispose ()
			{
				using (ctx.Document.Editor.OpenUndoGroup ()) {
					RunActions (changes, this);
				}
			}
			
			public override void InsertWithCursor (string operation, AstNode node, InsertPosition defaultPosition)
			{
				var editor = ctx.Document.Editor;
				var mode = new InsertionCursorEditMode (editor.Parent, CodeGenerationService.GetInsertionPoints (ctx.Document, ctx.Document.ParsedDocument.GetInnermostTypeDefinition (ctx.Location)));
				var helpWindow = new Mono.TextEditor.PopupWindow.ModeHelpWindow ();
				helpWindow.TransientFor = MonoDevelop.Ide.IdeApp.Workbench.RootWindow;
				helpWindow.TitleText = string.Format (GettextCatalog.GetString ("<b>{0} -- Targeting</b>"), operation);
				helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Key</b>"), GettextCatalog.GetString ("<b>Behavior</b>")));
				helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Up</b>"), GettextCatalog.GetString ("Move to <b>previous</b> target point.")));
				helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Down</b>"), GettextCatalog.GetString ("Move to <b>next</b> target point.")));
				helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Enter</b>"), GettextCatalog.GetString ("<b>Accept</b> target point.")));
				helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Esc</b>"), GettextCatalog.GetString ("<b>Cancel</b> this operation.")));
				mode.HelpWindow = helpWindow;
				
				switch (defaultPosition) {
				case InsertPosition.Start:
					mode.CurIndex = 0;
					break;
				case InsertPosition.End:
					mode.CurIndex = mode.InsertionPoints.Count - 1;
					break;
				case InsertPosition.Before:
					for (int i = 0; i < mode.InsertionPoints.Count; i++) {
						if (mode.InsertionPoints [i].Location < new DocumentLocation (ctx.Location.Line, ctx.Location.Column))
							mode.CurIndex = i;
					}
					break;
				case InsertPosition.After:
					for (int i = 0; i < mode.InsertionPoints.Count; i++) {
						if (mode.InsertionPoints [i].Location > new DocumentLocation (ctx.Location.Line, ctx.Location.Column)) {
							mode.CurIndex = i;
							break;
						}
					}
					break;
				}
				
				mode.StartMode ();
				mode.Exited += delegate(object s, InsertionCursorEventArgs iCArgs) {
					if (iCArgs.Success) {
						var output = OutputNode (GetIndentLevelAt (editor.LocationToOffset (iCArgs.InsertionPoint.Location)), node);
						iCArgs.InsertionPoint.Insert (editor, output.Text);
					}
				};
			}
			
		}
		
		public override Script StartScript ()
		{
			return new MdScript (this);
		}
		
		CSharpParsedFile CSharpParsedFile { get; set; }
		
		public MDRefactoringContext (MonoDevelop.Ide.Gui.Document document, TextLocation loc)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			this.Document = document;
			this.Compilation = document.Compilation;
			this.Location = new TextLocation (loc.Line, loc.Column);
			this.Unit = document.ParsedDocument.GetAst<CompilationUnit> ();
			this.CSharpParsedFile = document.ParsedDocument.ParsedFile as CSharpParsedFile;
		}
		
		public override AstType CreateShortType (IType fullType)
		{
			var parsedFile = Document.ParsedDocument.ParsedFile as CSharpParsedFile;
			
			var csResolver = parsedFile.GetResolver (Document.Compilation, Document.Editor.Caret.Location);
			
			var builder = new ICSharpCode.NRefactory.CSharp.Refactoring.TypeSystemAstBuilder (csResolver);
			return builder.ConvertType (fullType);			
		}
		
		/*
		Dictionary<AstNode, MonoDevelop.Projects.Dom.ResolveResult> resolveCache = new Dictionary<AstNode, MonoDevelop.Projects.Dom.ResolveResult> ();
		MonoDevelop.Projects.Dom.ResolveResult Resolve (AstNode node)
		{
			MonoDevelop.Projects.Dom.ResolveResult result;
			if (!resolveCache.TryGetValue (node, out result))
				resolveCache [node] = result = Resolver.Resolve (node.ToString (), new  MonoDevelop.Projects.Dom.TextLocation (Location.Line, Location.Column));
			return result;
		}*/
		
		ParsedDocument ParsedDocument {
			get {
				return Document.ParsedDocument;
			}
		}
		
		public override ResolveResult Resolve (AstNode node, CancellationToken cancellationToken)
		{
			var parsedFile = Document.ParsedDocument.ParsedFile as CSharpParsedFile;
			var cu = Document.ParsedDocument.GetAst<CompilationUnit> ();
			
			var resolver = new CSharpAstResolver (Document.Compilation, cu, parsedFile);
			return resolver.Resolve (node, cancellationToken);
		}

		public override void ReplaceReferences (ICSharpCode.NRefactory.TypeSystem.IMember member, EntityDeclaration replaceWidth)
		{
			// TODO
		}

		/*
		public bool IsValid {
			get {
				return Unit != null;
			}
		}
		
		public int GetIndentLevel (AstNode node)
		{
			return GetIndentLevel (node.StartLocation.Line);
		}
				
		public int GetIndentLevel (int line)
		{
			return Document.CalcIndentLevel (Document.Editor.GetLineIndent (line));
		}
		
		public void SetSelection (AstNode node)
		{
			this.selectNode = node;
		}
		
		AstNode selectNode = null;
		
		
		NodeOutput OutputNode (AstNode node, int indentLevel, Action<int, AstNode> outputStarted = null)
		{
			NodeOutput result = new NodeOutput ();
			
			return Document.OutputNode (node, indentLevel, delegate(int outOffset, AstNode outNode) {
				result.nodeSegments [outNode] = new Segment (outOffset, 0);
				if (outputStarted != null)
					outputStarted (outOffset, outNode);
			}, delegate(int outOffset, AstNode outNode) {
				result.nodeSegments [outNode].Length = outOffset - result.nodeSegments [outNode].Offset;
			});
		}
		
		public void StartTextLinkMode (int baseOffset, int replaceLength, IEnumerable<int> offsets)
		{
			CommitChanges ();
		}
		
		

		public string GetText (AstNode node)
		{
			return Document.Editor.GetTextAt (GetSegment (node));
		}
		
		public ISegment GetSegment (AstNode node)
		{
			var startOffset = Document.Editor.LocationToOffset (node.StartLocation.Line, node.StartLocation.Column);
			var endOffset   = Document.Editor.LocationToOffset (node.EndLocation.Line, node.EndLocation.Column);
			
			return new Segment (startOffset, endOffset - startOffset);
		}
		
		public void FormatText (Func<MDRefactoringContext, AstNode> update)
		{
		}
		*/


	}
}
