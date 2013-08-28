// 
// MDRefactoringScript.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using System.Linq;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using MonoDevelop.Ide.Gui;
using ICSharpCode.NRefactory.CSharp;
using Mono.TextEditor;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Core;
using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Refactoring.Rename;
using ICSharpCode.NRefactory.CSharp.Resolver;
using System.IO;
using MonoDevelop.CSharp.Formatting;
using MonoDevelop.Ide;
using System.Threading.Tasks;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.Projects;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.CSharp.Refactoring.CodeActions
{
	class MDRefactoringScript : DocumentScript
	{
		readonly MDRefactoringContext context;
		readonly IDisposable undoGroup;
		readonly ICSharpCode.NRefactory.Editor.ITextSourceVersion startVersion;
		int operationsRunning = 0;

		public MDRefactoringScript (MDRefactoringContext context, CSharpFormattingOptions formattingOptions) : base(context.TextEditor.Document, formattingOptions, context.TextEditor.CreateNRefactoryTextEditorOptions ())
		{
			this.context = context;
			undoGroup  = this.context.TextEditor.OpenUndoGroup ();
			this.startVersion = this.context.TextEditor.Version;

		}

		void Rollback ()
		{
			DisposeOnClose (true);
			foreach (var ver in this.context.TextEditor.Version.GetChangesTo (this.startVersion)) {
				context.TextEditor.Document.Replace (ver.Offset, ver.RemovalLength, ver.InsertedText.Text);
			}
		}

		public override void Select (AstNode node)
		{
			var seg = GetSegment (node);
			var startOffset = seg.Offset;
			var endOffset   = seg.EndOffset;
			while (startOffset < endOffset) {
				char ch = context.TextEditor.GetCharAt (startOffset);
				if (!char.IsWhiteSpace (ch))
					break;
				startOffset++;
			}
			while (startOffset < endOffset && endOffset > 0) {
				char ch = context.TextEditor.GetCharAt (endOffset - 1);
				if (!char.IsWhiteSpace (ch))
					break;
				endOffset--;
			}

			context.TextEditor.Caret.Offset = endOffset;
			context.TextEditor.SelectionRange = new TextSegment (startOffset, endOffset - startOffset);
		}

		public override Task<Script> InsertWithCursor (string operation, InsertPosition defaultPosition, IEnumerable<AstNode> nodes)
		{
			var tcs = new TaskCompletionSource<Script> ();
			var editor = context.TextEditor;
			DocumentLocation loc = context.TextEditor.Caret.Location;
			var declaringType = context.ParsedDocument.GetInnermostTypeDefinition (loc);
			var mode = new InsertionCursorEditMode (
				editor.Parent,
				CodeGenerationService.GetInsertionPoints (context.TextEditor, context.ParsedDocument, declaringType));
			if (mode.InsertionPoints.Count == 0) {
				MessageService.ShowError (
					GettextCatalog.GetString ("No valid insertion point can be found in type '{0}'.", declaringType.Name)
				);
				return tcs.Task;
			}
			var helpWindow = new Mono.TextEditor.PopupWindow.InsertionCursorLayoutModeHelpWindow ();
			helpWindow.TitleText = operation;
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
					if (mode.InsertionPoints [i].Location < loc)
						mode.CurIndex = i;
				}
				break;
			case InsertPosition.After:
				for (int i = 0; i < mode.InsertionPoints.Count; i++) {
					if (mode.InsertionPoints [i].Location > loc) {
						mode.CurIndex = i;
						break;
					}
				}
				break;
			}
			operationsRunning++;
			mode.StartMode ();
			mode.Exited += delegate(object s, InsertionCursorEventArgs iCArgs) {
				if (iCArgs.Success) {
					if (iCArgs.InsertionPoint.LineAfter == NewLineInsertion.None && 
					    iCArgs.InsertionPoint.LineBefore == NewLineInsertion.None && nodes.Count () > 1) {
						iCArgs.InsertionPoint.LineAfter = NewLineInsertion.BlankLine;
					}
					foreach (var node in nodes.Reverse ()) {
						var output = OutputNode (CodeGenerationService.CalculateBodyIndentLevel (declaringType), node);
						var offset = context.TextEditor.LocationToOffset (iCArgs.InsertionPoint.Location);
						var delta = iCArgs.InsertionPoint.Insert (editor, output.Text);
						output.RegisterTrackedSegments (this, delta + offset);
					}
					tcs.SetResult (this);
				} else {
					Rollback ();
				}
				DisposeOnClose (); 
			};
			return tcs.Task;
		}

		List<Script> startedScripts = new List<Script> ();

		public override Task<Script> InsertWithCursor (string operation, ITypeDefinition parentType, Func<Script, RefactoringContext, IEnumerable<AstNode>> nodeCallback)
		{
			var tcs = new TaskCompletionSource<Script>();
			if (parentType == null)
				return tcs.Task;
			var part = parentType.Parts.FirstOrDefault ();
			if (part == null)
				return tcs.Task;

			var loadedDocument = Ide.IdeApp.Workbench.OpenDocument (part.Region.FileName);
			loadedDocument.RunWhenLoaded (delegate {
				var editor = loadedDocument.Editor;
				var loc = part.Region.Begin;
				var parsedDocument = loadedDocument.UpdateParseDocument ();
				var declaringType = parsedDocument.GetInnermostTypeDefinition (loc);
				MDRefactoringScript script;

				if (loadedDocument.Editor != this.context.TextEditor) {
					script = new MDRefactoringScript (new MDRefactoringContext (loadedDocument, loc, context.CancellationToken), FormattingOptions);
					startedScripts.Add (script);
				} else {
					script = this;
				}
				var nodes = nodeCallback (script, script.context);
				var mode = new InsertionCursorEditMode (
					editor.Parent,
					CodeGenerationService.GetInsertionPoints (loadedDocument, declaringType));
				if (mode.InsertionPoints.Count == 0) {
					MessageService.ShowError (
						GettextCatalog.GetString ("No valid insertion point can be found in type '{0}'.", declaringType.Name)
					);
					return;
				}
				if (declaringType.Kind == TypeKind.Enum) {
					foreach (var node in nodes.Reverse ()) {
						var output = OutputNode (CodeGenerationService.CalculateBodyIndentLevel (declaringType), node);
						var point = mode.InsertionPoints.First ();
						var offset = loadedDocument.Editor.LocationToOffset (point.Location);
						var text = output.Text + ",";
						var delta = point.Insert (editor, text);
						output.RegisterTrackedSegments (script, delta + offset);
					}
					tcs.SetResult (script);
					return;
				}

				var helpWindow = new Mono.TextEditor.PopupWindow.InsertionCursorLayoutModeHelpWindow ();
				helpWindow.TitleText = operation;
				mode.HelpWindow = helpWindow;
				
				mode.CurIndex = 0;
				operationsRunning++;
				mode.StartMode ();
				mode.Exited += delegate(object s, InsertionCursorEventArgs iCArgs) {
					if (iCArgs.Success) {
						if (iCArgs.InsertionPoint.LineAfter == NewLineInsertion.None && 
						    iCArgs.InsertionPoint.LineBefore == NewLineInsertion.None && nodes.Count () > 1) {
							iCArgs.InsertionPoint.LineAfter = NewLineInsertion.BlankLine;
						}
						foreach (var node in nodes.Reverse ()) {
							var output = OutputNode (CodeGenerationService.CalculateBodyIndentLevel (declaringType), node);
							var offset = loadedDocument.Editor.LocationToOffset (iCArgs.InsertionPoint.Location);
							var text = output.Text;
							var delta = iCArgs.InsertionPoint.Insert (editor, text);
							output.RegisterTrackedSegments (script, delta + offset);
						}
						tcs.SetResult (script);
					} else {
						Rollback ();
					}
					DisposeOnClose (); 
				};
			});
		
			return tcs.Task;
		}

		public override Task Link (params AstNode[] nodes)
		{
			var tcs = new TaskCompletionSource<object> ();
			var segments = new List<TextSegment> (nodes.Select (node => new TextSegment (GetSegment (node))).OrderBy (s => s.Offset));
			
			var link = new TextLink ("name");
			segments.ForEach (s => link.AddLink (s));
			var links = new List<TextLink> ();
			links.Add (link);
			var tle = new TextLinkEditMode (context.TextEditor.Parent, 0, links);
			tle.SetCaretPosition = false;
			if (tle.ShouldStartTextLinkMode) {
				operationsRunning++;
				context.TextEditor.Caret.Offset = segments [0].EndOffset;
				tle.OldMode = context.TextEditor.CurrentMode;
				tle.Cancel += (sender, e) => Rollback ();
				tle.Exited += (sender, e) => DisposeOnClose (); 
				tle.StartMode ();
				context.TextEditor.CurrentMode = tle;
				if (IdeApp.Workbench.ActiveDocument != null)
					IdeApp.Workbench.ActiveDocument.ReparseDocument ();
			}
			return tcs.Task;
		}

		bool isDisposed = false;
		void DisposeOnClose (bool force = false)
		{
			if (isDisposed)
				return;
			if (force)
				operationsRunning = 0;
			if (operationsRunning-- == 0) {
				isDisposed = true;
				undoGroup.Dispose ();
				base.Dispose ();
			}
			foreach (var script in startedScripts)
				script.Dispose ();
		}
		
		public override void Dispose ()
		{
			DisposeOnClose ();
		}

		public override void Rename (ISymbol symbol, string name)
		{
			if (symbol is IEntity)
				RenameRefactoring.Rename ((IEntity)symbol, name);
			if (symbol is IVariable)
				RenameRefactoring.RenameVariable ((IVariable)symbol, name);
			if (symbol is INamespace)
				RenameRefactoring.RenameNamespace ((INamespace)symbol, name);
			if (symbol is ITypeParameter)
				RenameRefactoring.RenameTypeParameter ((ITypeParameter)symbol, name);
		}

		public override void DoGlobalOperationOn (IEnumerable<IEntity> entities, Action<RefactoringContext, Script, IEnumerable<AstNode>> callback, string operationName = null)
		{
			using (var monitor = IdeApp.Workbench.ProgressMonitors.GetBackgroundProgressMonitor (operationName ?? GettextCatalog.GetString ("Performing refactoring task..."), null)) {
				var col = entities.SelectMany (entity => ReferenceFinder.FindReferences (entity, true, monitor)).OfType<CSharpReferenceFinder.CSharpMemberReference> ().GroupBy(reference => reference.FileName);

				foreach (var r in col) {
					string filename = r.Key;

					bool isOpen;
					System.Text.Encoding encoding;
					bool hadBom;

					var data = TextFileProvider.Instance.GetTextEditorData (filename, out hadBom, out encoding, out isOpen);

					var firstReference = r.First ();

					var project = firstReference.Project;

					ParsedDocument parsedDocument;
					using (var reader = new StreamReader (data.OpenStream ()))
						parsedDocument = new MonoDevelop.CSharp.Parser.TypeSystemParser ().Parse (true, filename, reader, project);

					var resolver = new CSharpAstResolver (TypeSystemService.GetCompilation (project), firstReference.SyntaxTree, parsedDocument.ParsedFile as CSharpUnresolvedFile);

					var ctx = new MDRefactoringContext (project as DotNetProject, data, parsedDocument, resolver, firstReference.AstNode.StartLocation, this.context.CancellationToken);
					var script = new MDRefactoringScript (ctx, FormattingOptions);

					callback (ctx, script, r.Select (reference => reference.AstNode));

					if (!isOpen) {
						script.Dispose ();
						Mono.TextEditor.Utils.TextFileUtility.WriteText (filename, data.Text, encoding, hadBom);
					}
				}
			}
		}

		public override void CreateNewType (AstNode newType, NewTypeContext ntctx)
		{
			if (newType == null)
				throw new System.ArgumentNullException ("newType");
			var correctFileName = MoveTypeToFile.GetCorrectFileName (context, (EntityDeclaration)newType);
			
			var content = context.TextEditor.Text;
			
			var types = new List<EntityDeclaration> (context.Unit.GetTypes ());
			types.Sort ((x, y) => y.StartLocation.CompareTo (x.StartLocation));

			foreach (var removeType in types) {
				var start = context.GetOffset (removeType.StartLocation);
				var end = context.GetOffset (removeType.EndLocation);
				content = content.Remove (start, end - start);
			}
			
			var insertLocation = types.Count > 0 ? context.GetOffset (types.Last ().StartLocation) : -1;
			if (insertLocation < 0 || insertLocation > content.Length)
				insertLocation = content.Length;
			content = content.Substring (0, insertLocation) + newType.ToString (FormattingOptions) + content.Substring (insertLocation);

			var formatter = new MonoDevelop.CSharp.Formatting.CSharpFormatter ();
			var policy = context.Project.Policies.Get<CSharpFormattingPolicy> ();
			var textPolicy = context.Project.Policies.Get<TextStylePolicy> ();

			content = formatter.FormatText (policy, textPolicy, MonoDevelop.CSharp.Formatting.CSharpFormatter.MimeType, content, 0, content.Length);

			File.WriteAllText (correctFileName, content);
			context.Project.AddFile (correctFileName);
			IdeApp.ProjectOperations.Save (context.Project);
			IdeApp.Workbench.OpenDocument (correctFileName);
		}

	}
}

