// 
// AbstractGenerateAction.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Text;
using Gtk;
using System.Collections.Generic;
using MonoDevelop.Refactoring;
using MonoDevelop.Ide;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Components;
using Microsoft.CodeAnalysis;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Core.Text;
using MonoDevelop.CSharp.Completion;
using MonoDevelop.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;
using MonoDevelop.Core;

namespace MonoDevelop.CodeGeneration
{
	abstract class AbstractGenerateAction : IGenerateAction
	{
		readonly TreeStore store = new TreeStore (typeof(bool), typeof(Xwt.Drawing.Image), typeof(string), typeof(object));
		readonly CodeGenerationOptions options;
		
		internal CodeGenerationOptions Options {
			get {
				return options; 
			}
		}
		
		public TreeStore Store {
			get { return store; }
		}
		
		protected AbstractGenerateAction (CodeGenerationOptions options)
		{
			this.options = options;
		}
		
		public void Initialize (TreeView treeView)
		{
			var column = new TreeViewColumn ();

			var toggleRenderer = new CellRendererToggle ();
			toggleRenderer.Toggled += ToggleRendererToggled;
			column.PackStart (toggleRenderer, false);
			column.AddAttribute (toggleRenderer, "active", 0);

			var pixbufRenderer = new CellRendererImage ();
			column.PackStart (pixbufRenderer, false);
			column.AddAttribute (pixbufRenderer, "image", 1);

			var textRenderer = new CellRendererText ();
			column.PackStart (textRenderer, true);
			column.AddAttribute (textRenderer, "text", 2);
			column.Expand = true;

			treeView.AppendColumn (column);
			foreach (object obj in GetValidMembers ()) {
				var member = obj as ISymbol;
				if (member != null) {
					Store.AppendValues (false, ImageService.GetIcon (member.GetStockIcon (), IconSize.Menu), member.ToDisplayString (Ambience.LabelFormat), member);
					continue;
				}

				var tuple = obj as Tuple<ISymbol, bool>;
				if (tuple != null) {
					Store.AppendValues (false, ImageService.GetIcon (tuple.Item1.GetStockIcon (), IconSize.Menu), tuple.Item1.ToDisplayString (Ambience.LabelFormat), tuple);
					continue;
				}
			}
			
			treeView.Model = store;
			treeView.SearchColumn = -1; // disable the interactive search
		}
		
		void ToggleRendererToggled (object o, ToggledArgs args)
		{
			TreeIter iter;
			if (store.GetIterFromString (out iter, args.Path)) {
				bool active = (bool)store.GetValue (iter, 0);
				store.SetValue (iter, 0, !active);
			}
		}
		
		protected abstract IEnumerable<object> GetValidMembers ();
		
		public bool IsValid ()
		{
			return GetValidMembers ().Any ();
		}
		
		protected abstract IEnumerable<string> GenerateCode (List<object> includedMembers);
		
		static string AddIndent (string text, string indent)
		{
			var doc = TextEditorFactory.CreateNewReadonlyDocument (new StringTextSource (text), "");
			var result = StringBuilderCache.Allocate ();
			foreach (var line in doc.GetLines ()) {
				result.Append (indent);
				result.Append (doc.GetTextAt (line.SegmentIncludingDelimiter));
			}
			return StringBuilderCache.ReturnAndFree (result);
		}

		public void GenerateCode (Gtk.TreeView treeView)
		{
			TreeIter iter;
			if (!store.GetIterFirst (out iter))
				return;
			var includedMembers = new List<object> ();
			do {
				bool include = (bool)store.GetValue (iter, 0);
				if (include)
					includedMembers.Add (store.GetValue (iter, 3));
			} while (store.IterNext (ref iter));
			if (includedMembers.Count == 0) {
				if (treeView.Selection.GetSelected (out iter)) {
					includedMembers.Add (store.GetValue (iter, 3));
				}
			}
			var output = StringBuilderCache.Allocate ();
			string indent = options.Editor.GetVirtualIndentationString (options.Editor.CaretLine);
			foreach (string nodeText in GenerateCode (includedMembers)) {
				if (output.Length > 0) {
					output.AppendLine ();
					output.AppendLine ();
				}
				output.Append (AddIndent (nodeText, indent));
			}

			if (output.Length > 0) {
				var data = options.Editor;
				data.EnsureCaretIsNotVirtual ();
				int offset = data.CaretOffset;
				var text = StringBuilderCache.ReturnAndFree (output).TrimStart ();
				using (var undo = data.OpenUndoGroup ()) {
					data.InsertAtCaret (text);
					OnTheFlyFormatter.Format (data, options.DocumentContext, offset, offset + text.Length);
				}
			}
		}
	}
}
