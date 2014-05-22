//
// JavaScriptEditorExtension.cs
//
// Author:
//       Harsimran Bath <harsimranbath@gmail.com>
//
// Copyright (c) 2014 Harsimran
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
using MonoDevelop.DesignerSupport;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeCompletion;
using Gtk;
using MonoDevelop.JavaScript.Parser;
using MonoDevelop.Projects;

namespace MonoDevelop.JavaScript.TextEditor
{
	class JavaScriptEditorExtension : CompletionTextEditorExtension, IOutlinedDocument
	{

		#region Variables

		bool disposed;
		bool refreshingOutline = false;
		MonoDevelop.Ide.Gui.Components.PadTreeView outlineTreeView;
		Gtk.TreeStore outlineTreeStore;
		readonly Gdk.Color normal = new Gdk.Color (0x00, 0x00, 0x00);

		#endregion

		#region Properties

		public JavaScript.Parser.JavaScriptParsedDocument ParsedDoc { get; private set; }

		#endregion

		#region Initialization and Events

		public JavaScriptEditorExtension ()
		{
		}

		public override void Initialize ()
		{
			base.Initialize ();
			Document.DocumentParsed += HandleDocumentParsed;
			HandleDocumentParsed (this, EventArgs.Empty);
		}

		void HandleDocumentParsed (object sender, EventArgs e)
		{
			ParsedDoc = (JavaScript.Parser.JavaScriptParsedDocument)Document.ParsedDocument;
			if (ParsedDoc != null)
				refreshOutline ();
		}

		public override void Dispose ()
		{
			if (disposed)
				return;
			disposed = true;
			base.Dispose ();
		}

		#endregion

		#region Document Outline Implementation

		public Gtk.Widget GetOutlineWidget ()
		{
			if (outlineTreeView != null)
				return outlineTreeView;

			outlineTreeStore = new Gtk.TreeStore (typeof(object));
			outlineTreeView = new MonoDevelop.Ide.Gui.Components.PadTreeView (outlineTreeStore);
			outlineTreeView.Realized += delegate {
				refillOutlineStore ();
			};

			outlineTreeView.TextRenderer.Xpad = 0;
			outlineTreeView.TextRenderer.Ypad = 0;

			var pixRenderer = new CellRendererPixbuf ();
			pixRenderer.Xpad = 0;
			pixRenderer.Ypad = 0;

			var treeCol = new TreeViewColumn ();
			treeCol.PackStart (pixRenderer, false);

			treeCol.SetCellDataFunc (pixRenderer, new TreeCellDataFunc (outlineTreeIconFunc));
			treeCol.PackStart (outlineTreeView.TextRenderer, true);

			treeCol.SetCellDataFunc (outlineTreeView.TextRenderer, new TreeCellDataFunc (outlineTreeTextFunc));
			outlineTreeView.AppendColumn (treeCol);

			outlineTreeView.HeadersVisible = false;

			outlineTreeView.Selection.Changed += delegate {
				Gtk.TreeIter iter;
				if (!outlineTreeView.Selection.GetSelected (out iter))
					return;
				selectSegment (outlineTreeStore.GetValue (iter, 0));
			};

			refillOutlineStore ();
			var sw = new MonoDevelop.Components.CompactScrolledWindow ();

			sw.Add (outlineTreeView);
			sw.ShowAll ();
			return sw;
		}

		public IEnumerable<Gtk.Widget> GetToolbarWidgets ()
		{
			//var widgets = new List<Gtk.Widget>();

			//var searchTextBox = new Gtk.TextView();
			//searchTextBox.Buffer.Text = "Search...";
			//searchTextBox.WidthRequest = 300;
			//widgets.Add(searchTextBox);

			//return widgets;

			return null;
		}

		public void ReleaseOutlineWidget ()
		{
			if (outlineTreeView == null)
				return;

			Gtk.ScrolledWindow w = (Gtk.ScrolledWindow)outlineTreeView.Parent;
			w.Destroy ();
			outlineTreeView.Destroy ();
			outlineTreeStore.Dispose ();
			outlineTreeStore = null;
			outlineTreeView = null;
		}

		#endregion

		#region Completion TextEditor Implementation

		public override string CompletionLanguage { get { return "JavaScript"; } }

		public override bool CanRunCompletionCommand ()
		{
			return true;
		}

		public override bool CanRunParameterCompletionCommand ()
		{
			return false;
		}

		public override ICompletionDataList HandleCodeCompletion (CodeCompletionContext completionContext, char completionChar, ref int triggerWordLength)
		{
			if (char.IsLetterOrDigit (completionChar) || completionChar == '_') {
				if (completionContext.TriggerOffset > 1 &&
				    char.IsLetterOrDigit (document.Editor.GetCharAt (completionContext.TriggerOffset - 2)))
					return null;
				triggerWordLength = 1;
			} else
				return null;

			if (Ide.IdeApp.ProjectOperations.CurrentSelectedProject == null)
				return null;

			var documents = new List<JavaScriptParsedDocument> ();
			foreach (var file in Ide.IdeApp.ProjectOperations.CurrentSelectedProject.Files) {
				if (!file.Name.ToUpper ().EndsWith (".JS"))
					continue;

				var parsedDoc = file.ExtendedProperties [JavaScriptParser.ParsedDocumentProperty] as JavaScriptParsedDocument;
				if (parsedDoc == null ||
				    !parsedDoc.IsInvalid) {
					continue;
				}

				documents.Add (parsedDoc);
			}

			// TODO: Find the current scope.
			var dataList = new CompletionDataList ();
			foreach (JavaScriptParsedDocument parsedDocument in documents) {
				dataList.AddRange (buildCodeCompletionList (parsedDocument.AstNodes));
			}
			return dataList;
		}

		public override ParameterDataProvider HandleParameterCompletion (CodeCompletionContext completionContext, char completionChar)
		{
			return null;
		}

		#endregion

		#region Private Methods

		void outlineTreeIconFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			var pixRenderer = (CellRendererPixbuf)cell;
			object o = model.GetValue (iter, 0);

			if (o is Jurassic.Compiler.FunctionStatement || o is Jurassic.Compiler.FunctionExpression) {
				pixRenderer.Pixbuf = ImageService.GetPixbuf (MonoDevelop.Ide.Gui.Stock.Method, IconSize.Menu);
			} else if (o is Jurassic.Compiler.VariableDeclaration) {
				pixRenderer.Pixbuf = ImageService.GetPixbuf (MonoDevelop.Ide.Gui.Stock.Field, IconSize.Menu);
			} else if (o is JavaScript.Parser.JavaScriptParsedDocument) {
				pixRenderer.Pixbuf = ImageService.GetPixbuf (MonoDevelop.Ide.Gui.Stock.FileXmlIcon, IconSize.Menu);
			} else {
				throw new ArgumentException (string.Format ("Type {0} is not supported in JavaScript Outline.", o.GetType ().Name));
			}
		}

		void outlineTreeTextFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			var txtRenderer = (CellRendererText)cell;
			object o = model.GetValue (iter, 0);

			var functionExpression = o as Jurassic.Compiler.FunctionExpression;
			if (functionExpression != null) {
				txtRenderer.Text = functionExpression.BuildFunctionSignature ();
				return;
			} 

			var functionStatement = o as Jurassic.Compiler.FunctionStatement;
			if (functionStatement != null) {
				txtRenderer.Text = functionStatement.BuildFunctionSignature ();
				return;
			} 

			var varDeclaration = o as Jurassic.Compiler.VariableDeclaration;
			if (varDeclaration != null) {
				txtRenderer.Text = varDeclaration.VariableName;
				return;
			}

			var document = o as JavaScript.Parser.JavaScriptParsedDocument;
			if (document != null) {
				txtRenderer.Text = System.IO.Path.GetFileName (document.FileName);
				return;
			}

			throw new ArgumentException (string.Format ("Type {0} is not supported in JavaScript Outline.", o.GetType ().Name));
		}

		bool refillOutlineStoreIdleHandler ()
		{
			refreshingOutline = false;
			refillOutlineStore ();
			return false;
		}

		void refreshOutline ()
		{
			if (refreshingOutline || outlineTreeView == null)
				return;
			refreshingOutline = true;
			GLib.Timeout.Add (3000, refillOutlineStoreIdleHandler);
		}

		void refillOutlineStore ()
		{
			DispatchService.AssertGuiThread ();
			Gdk.Threads.Enter ();
			refreshingOutline = false;
			if (outlineTreeStore == null || !outlineTreeView.IsRealized)
				return;
			outlineTreeStore.Clear ();

			if (ParsedDoc != null) {
				DateTime start = DateTime.Now;
				refillOutlineStore (ParsedDoc, outlineTreeStore);
				outlineTreeView.ExpandAll ();
				outlineTreeView.ExpandAll ();
				LoggingService.LogDebug ("Built outline in {0}ms", (DateTime.Now - start).Milliseconds);
			}

			Gdk.Threads.Leave ();
		}

		void refillOutlineStore (JavaScript.Parser.JavaScriptParsedDocument doc, Gtk.TreeStore store)
		{
			if (doc == null)
				return;

			var parentIter = store.AppendValues (doc);
			buildTreeChildren (store, parentIter, doc.AstNodes);
		}

		void buildTreeChildren (Gtk.TreeStore store, Gtk.TreeIter parent, IEnumerable<Jurassic.Compiler.JSAstNode> nodes)
		{
			if (nodes == null)
				return;

			foreach (Jurassic.Compiler.JSAstNode node in nodes) {
				Gtk.TreeIter childIter = default (Gtk.TreeIter);
				var variableStatement = node as Jurassic.Compiler.VarStatement;
				if (variableStatement != null) {
					foreach (Jurassic.Compiler.VariableDeclaration variableDeclaration in variableStatement.Declarations) {
						childIter = store.AppendValues (parent, variableDeclaration);
					}

					continue;
				}

				var functionStatement = node as Jurassic.Compiler.FunctionStatement;
				if (functionStatement != null) {
					childIter = store.AppendValues (parent, functionStatement);
					buildTreeChildren (store, childIter, functionStatement.BodyRoot.ChildNodes);
					continue;
				}

				var functionExpression = node as Jurassic.Compiler.FunctionExpression;
				if (functionExpression != null) {
					childIter = store.AppendValues (parent, functionExpression);
					buildTreeChildren (store, childIter, functionExpression.BodyRoot.ChildNodes);
					continue;
				}

				var literal = node as Jurassic.Compiler.LiteralExpression;
				if (literal != null) {
					if (literal.Value != null) {
						var properties = literal.Value as Dictionary<string, object>;
						if (properties != null) {
							for (int i = 0; i < properties.Count; i++) {
								string key = properties.Keys.ElementAt (i); // Key holds the value for then 
								object value = properties.Values.ElementAt (i);

								var objFuncExpression = value as Jurassic.Compiler.FunctionExpression;
								if (objFuncExpression != null) {
									childIter = store.AppendValues (parent, objFuncExpression);
									buildTreeChildren (store, childIter, objFuncExpression.BodyRoot.ChildNodes);
									continue;
								}

								var objGetSetFunc = value as Jurassic.Compiler.Parser.ObjectLiteralAccessor;
								if (objGetSetFunc != null) {
									if (objGetSetFunc.Getter != null) {
										childIter = store.AppendValues (parent, objGetSetFunc.Getter);
										buildTreeChildren (store, childIter, objGetSetFunc.Getter.BodyRoot.ChildNodes);
									}
									if (objGetSetFunc.Setter != null) {
										childIter = store.AppendValues (parent, objGetSetFunc.Setter);
										buildTreeChildren (store, childIter, objGetSetFunc.Setter.BodyRoot.ChildNodes);
									}
								}
							}
						}
					}

					continue;
				}


				buildTreeChildren (store, parent, node.ChildNodes);
			}
		}

		void selectSegment (object node)
		{
			int line = 0, column = 0;

			if (node is Jurassic.Compiler.VariableDeclaration) {
				line = (node as Jurassic.Compiler.VariableDeclaration).SourceSpan.StartLine;
				column = (node as Jurassic.Compiler.VariableDeclaration).SourceSpan.StartColumn;
			} else if (node is Jurassic.Compiler.FunctionStatement) {
				line = (node as Jurassic.Compiler.FunctionStatement).SourceSpan.StartLine;
				column = (node as Jurassic.Compiler.FunctionStatement).SourceSpan.StartColumn;
			} else if (node is Jurassic.Compiler.FunctionExpression) {
				line = (node as Jurassic.Compiler.FunctionExpression).SourceSpan.StartLine;
				column = (node as Jurassic.Compiler.FunctionExpression).SourceSpan.StartColumn;
			} else if (node is JavaScript.Parser.JavaScriptParsedDocument) {
				line = 0;
				column = 0;
			} else {
				return;
			}

			int s = Editor.Document.LocationToOffset (line, column);
			if (s > -1) {
				Editor.Caret.Offset = s;
				Editor.CenterTo (s);
			}
		}

		CompletionDataList buildCodeCompletionList (IEnumerable<Jurassic.Compiler.JSAstNode> nodes)
		{
			// TODO: Store all functions, variables in project, similar C Binding
			if (nodes == null)
				return new CompletionDataList ();

			var completionList = new CompletionDataList ();

			foreach (Jurassic.Compiler.JSAstNode node in nodes) {
				var variableStatement = node as Jurassic.Compiler.VarStatement;
				if (variableStatement != null) {
					foreach (Jurassic.Compiler.VariableDeclaration variableDeclaration in variableStatement.Declarations) {
						completionList.Add (new CompletionData (variableDeclaration));
					}
					completionList.AddRange (buildCodeCompletionList (variableStatement.ChildNodes));
					continue;
				}

				var functionStatement = node as Jurassic.Compiler.FunctionStatement;
				if (functionStatement != null) {
					completionList.Add (new CompletionData (functionStatement));
					completionList.AddRange (buildCodeCompletionList (functionStatement.BodyRoot.ChildNodes));
					continue;
				}

				var functionExpression = node as Jurassic.Compiler.FunctionExpression;
				if (functionExpression != null) {
					completionList.Add (new CompletionData (functionExpression));
					completionList.AddRange (buildCodeCompletionList (functionExpression.BodyRoot.ChildNodes));
					continue;
				}

				completionList.AddRange (buildCodeCompletionList (node.ChildNodes));
			}

			return completionList;
		}

		#endregion

	}
}
