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

namespace MonoDevelop.CSharp.Refactoring.CodeActions
{
	public class MDRefactoringScript : DocumentScript
	{
		readonly MDRefactoringContext context;
		readonly Document document;
		readonly IDisposable undoGroup;
		readonly ICSharpCode.NRefactory.Editor.ITextSourceVersion startVersion;
		int operationsRunning = 0;

		public MDRefactoringScript (MDRefactoringContext context, Document document, CSharpFormattingOptions formattingOptions) : base(document.Editor.Document, formattingOptions, document.Editor.CreateNRefactoryTextEditorOptions ())
		{
			this.context = context;
			this.document = document;
			undoGroup  = this.document.Editor.OpenUndoGroup ();
			this.startVersion = this.document.Editor.Version;

		}

		void Rollback ()
		{
			DisposeOnClose (true);
			foreach (var ver in this.document.Editor.Version.GetChangesTo (this.startVersion)) {
				document.Editor.Document.Replace (ver.Offset, ver.RemovalLength, ver.InsertedText.Text);
			}
		}

		public override void Select (AstNode node)
		{
			document.Editor.SelectionRange = new TextSegment (GetSegment (node));
		}

		public override Task InsertWithCursor (string operation, InsertPosition defaultPosition, IEnumerable<AstNode> nodes)
		{
			var tcs = new TaskCompletionSource<object> ();
			var editor = document.Editor;
			DocumentLocation loc = document.Editor.Caret.Location;
			var declaringType = document.ParsedDocument.GetInnermostTypeDefinition (loc);
			var mode = new InsertionCursorEditMode (
				editor.Parent,
				CodeGenerationService.GetInsertionPoints (document, declaringType));
			if (mode.InsertionPoints.Count == 0) {
				MessageService.ShowError (
					GettextCatalog.GetString ("No valid insertion point can be found in type '{0}'.", declaringType.Name)
				);
				return tcs.Task;
			}
			var helpWindow = new Mono.TextEditor.PopupWindow.InsertionCursorLayoutModeHelpWindow ();
			helpWindow.TransientFor = MonoDevelop.Ide.IdeApp.Workbench.RootWindow;
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
			DesktopService.RemoveWindowShadow (helpWindow);
			mode.Exited += delegate(object s, InsertionCursorEventArgs iCArgs) {
				if (iCArgs.Success) {
					if (iCArgs.InsertionPoint.LineAfter == NewLineInsertion.None && 
					    iCArgs.InsertionPoint.LineBefore == NewLineInsertion.None && nodes.Count () > 1) {
						iCArgs.InsertionPoint.LineAfter = NewLineInsertion.BlankLine;
					}
					foreach (var node in nodes.Reverse ()) {
						var output = OutputNode (CodeGenerationService.CalculateBodyIndentLevel (declaringType), node);
						var offset = document.Editor.LocationToOffset (iCArgs.InsertionPoint.Location);
						var delta = iCArgs.InsertionPoint.Insert (editor, output.Text);
						output.RegisterTrackedSegments (this, delta + offset);
					}
					tcs.SetResult (null);
				} else {
					Rollback ();
				}
				DisposeOnClose (); 
			};
			return tcs.Task;
		}

		public override Task InsertWithCursor (string operation, ITypeDefinition parentType, IEnumerable<AstNode> nodes)
		{
			var tcs = new TaskCompletionSource<object>();
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
						output.RegisterTrackedSegments (this, delta + offset);
					}
					tcs.SetResult (null);
					return;
				}


				var helpWindow = new Mono.TextEditor.PopupWindow.InsertionCursorLayoutModeHelpWindow ();
				helpWindow.TransientFor = MonoDevelop.Ide.IdeApp.Workbench.RootWindow;
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
							output.RegisterTrackedSegments (this, delta + offset);
						}
						tcs.SetResult (null);
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
			var tle = new TextLinkEditMode (document.Editor.Parent, 0, links);
			tle.SetCaretPosition = false;
			if (tle.ShouldStartTextLinkMode) {
				operationsRunning++;
				document.Editor.Caret.Offset = segments [0].EndOffset;
				tle.OldMode = document.Editor.CurrentMode;
				tle.Cancel += (sender, e) => Rollback ();
				tle.Exited += (sender, e) => DisposeOnClose (); 
				tle.StartMode ();
				document.Editor.CurrentMode = tle;
				document.ReparseDocument ();
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
		}
		
		public override void Dispose ()
		{
			DisposeOnClose ();
		}

		public override void Rename (IEntity entity, string name)
		{
			RenameRefactoring.Rename (entity, name);
		}

		public override void Rename (IVariable variable, string name)
		{
			RenameRefactoring.RenameVariable (variable, name);
		}

		public override void RenameTypeParameter (IType typeParameter, string name = null)
		{
			RenameRefactoring.RenameTypeParameter ((ITypeParameter)typeParameter, name);
		}

		public override void CreateNewType (AstNode newType, NewTypeContext ntctx)
		{
			if (newType == null)
				throw new System.ArgumentNullException ("newType");
			var correctFileName = MoveTypeToFile.GetCorrectFileName (context, (EntityDeclaration)newType);
			
			var content = context.Document.Editor.Text;
			
			var types = new List<EntityDeclaration> (context.Unit.GetTypes ());
			types.Sort ((x, y) => y.StartLocation.CompareTo (x.StartLocation));

			foreach (var removeType in types) {
				var start = context.GetOffset (removeType.StartLocation);
				var end = context.GetOffset (removeType.EndLocation);
				content = content.Remove (start, end - start);
			}
			
			var insertLocation = types.Count > 0 ? context.GetOffset (types.Last ().StartLocation) : -1;
			var formattingPolicy = this.document.GetFormattingPolicy ();
			if (insertLocation < 0 || insertLocation > content.Length)
				insertLocation = content.Length;
			content = content.Substring (0, insertLocation) + newType.GetText (formattingPolicy.CreateOptions ()) + content.Substring (insertLocation);

			var formatter = new CSharpFormatter ();
			content = formatter.FormatText (formattingPolicy, null, CSharpFormatter.MimeType, content, 0, content.Length);

			File.WriteAllText (correctFileName, content);
			document.Project.AddFile (correctFileName);
			MonoDevelop.Ide.IdeApp.ProjectOperations.Save (document.Project);
			MonoDevelop.Ide.IdeApp.Workbench.OpenDocument (correctFileName);
		}

	}
}

