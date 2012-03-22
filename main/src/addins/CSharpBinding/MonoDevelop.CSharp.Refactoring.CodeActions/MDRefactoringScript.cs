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
using MonoDevelop.TypeSystem;
using MonoDevelop.Core;
using System.Collections.Generic;

namespace MonoDevelop.CSharp.Refactoring.CodeActions
{
	public class MDRefactoringScript : DocumentScript
	{
		readonly Document document;
		readonly IDisposable undoGroup;

		public MDRefactoringScript (Document document, CSharpFormattingOptions formattingOptions) : base(document.Editor.Document, formattingOptions)
		{
			this.document = document;
			undoGroup  = this.document.Editor.OpenUndoGroup ();
		}

		public override void Select (AstNode node)
		{
			document.Editor.SelectionRange = new TextSegment (GetSegment (node));
		}

		public override void InsertWithCursor (string operation, AstNode node, InsertPosition defaultPosition)
		{
			Console.WriteLine (node.GetText ());
			Console.WriteLine (Environment.StackTrace);
			var editor = document.Editor;
			DocumentLocation loc = document.Editor.Caret.Location;
			var mode = new InsertionCursorEditMode (editor.Parent, CodeGenerationService.GetInsertionPoints (document, document.ParsedDocument.GetInnermostTypeDefinition (loc)));
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
			
			mode.StartMode ();
			mode.Exited += delegate(object s, InsertionCursorEventArgs iCArgs) {
				if (iCArgs.Success) {
					var output = OutputNode (CodeGenerationService.CalculateBodyIndentLevel (document.ParsedDocument.GetInnermostTypeDefinition (loc)), node);
					output.RegisterTrackedSegments (this, document.Editor.LocationToOffset (iCArgs.InsertionPoint.Location));
					iCArgs.InsertionPoint.Insert (editor, output.Text);
				}
			};
		}

		public override void Link (params AstNode[] nodes)
		{
			var segments = new List<TextSegment> (nodes.Select (node => new TextSegment (GetSegment (node))).OrderBy (s => s.Offset));

			var link = new TextLink ("name");
			segments.ForEach (s => link.AddLink (s));
			var links = new List<TextLink> ();
			links.Add (link);
			var tle = new TextLinkEditMode (document.Editor.Parent, 0, links);
			tle.SetCaretPosition = false;
			if (tle.ShouldStartTextLinkMode) {
				tle.OldMode = document.Editor.CurrentMode;
				tle.StartMode ();
				document.Editor.CurrentMode = tle;
			}
		}

		public override void Dispose ()
		{
			undoGroup.Dispose ();
			base.Dispose ();
		}

	}
}

