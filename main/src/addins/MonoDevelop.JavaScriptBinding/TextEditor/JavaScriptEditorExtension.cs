using MonoDevelop.DesignerSupport;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoDevelop.Core;

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

			outlineTreeStore = new Gtk.TreeStore (typeof(Gdk.Pixbuf), typeof(string), typeof(Jurassic.Compiler.JSAstNode));
			outlineTreeView = new MonoDevelop.Ide.Gui.Components.PadTreeView (outlineTreeStore);
			outlineTreeView.Realized += delegate {
				refillOutlineStore ();
			};

			outlineTreeView.TextRenderer.Xpad = 0;
			outlineTreeView.TextRenderer.Ypad = 0;
			outlineTreeView.AppendColumn ("Icon", new Gtk.CellRendererPixbuf (), "pixbuf", 0);
			outlineTreeView.AppendColumn ("Node", outlineTreeView.TextRenderer, "text", 1);

			outlineTreeView.HeadersVisible = false;

			outlineTreeView.Selection.Changed += delegate {
				Gtk.TreeIter iter;
				if (!outlineTreeView.Selection.GetSelected (out iter))
					return;
				selectSegment (outlineTreeStore.GetValue (iter, 2));
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
			if (refreshingOutline || outlineTreeView == null)
				return;
			refreshingOutline = true;
			GLib.Timeout.Add (3000, refillOutlineStoreIdleHandler);
		}

		#endregion

		#region Private Methods

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

			var fileIcon = ImageService.GetPixbuf (Stock.TextFileIcon, Gtk.IconSize.Menu);
			var parentIter = store.AppendValues (fileIcon, doc.FileName, doc);

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
					var icon = ImageService.GetPixbuf (Stock.Field, Gtk.IconSize.Menu);

					foreach (Jurassic.Compiler.VariableDeclaration variableDeclaration in variableStatement.Declarations) {
						childIter = store.AppendValues (parent, icon, string.Concat (variableDeclaration.VariableName), variableDeclaration);
					}

					buildTreeChildren (store, childIter, node.ChildNodes);

					continue;
				}

				var functionStatement = node as Jurassic.Compiler.FunctionStatement;
				if (functionStatement != null) {
					var icon = ImageService.GetPixbuf (Stock.Method, Gtk.IconSize.Menu);

					childIter = store.AppendValues (parent, icon, functionStatement.BuildFunctionSignature (), functionStatement);

					buildTreeChildren (store, childIter, functionStatement.BodyRoot.ChildNodes);

					continue;
				}

				var functionExpression = node as Jurassic.Compiler.FunctionExpression;
				if (functionExpression != null) {
					var icon = ImageService.GetPixbuf (Stock.Method, Gtk.IconSize.Menu);

					childIter = store.AppendValues (parent, icon, functionExpression.BuildFunctionSignature (), functionExpression);

					buildTreeChildren (store, childIter, functionExpression.BodyRoot.ChildNodes);

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

		#endregion

	}
}
