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
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Refactoring;
using MonoDevelop.Ide;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.CodeGeneration
{
	public abstract class AbstractGenerateAction : IGenerateAction
	{
		TreeStore store = new TreeStore (typeof(bool), typeof(Gdk.Pixbuf), typeof(string), typeof(object));
		CodeGenerationOptions options;
		
		public CodeGenerationOptions Options {
			get {
				return this.options; 
			}
		}
		
		public TreeStore Store {
			get { return this.store; }
		}
		
		public AbstractGenerateAction (CodeGenerationOptions options)
		{
			this.options = options;
		}
		
		public void Initialize (Gtk.TreeView treeView)
		{
			TreeViewColumn column = new TreeViewColumn ();

			CellRendererToggle toggleRenderer = new CellRendererToggle ();
			toggleRenderer.Toggled += ToggleRendererToggled;
			column.PackStart (toggleRenderer, false);
			column.AddAttribute (toggleRenderer, "active", 0);

			var pixbufRenderer = new CellRendererPixbuf ();
			column.PackStart (pixbufRenderer, false);
			column.AddAttribute (pixbufRenderer, "pixbuf", 1);

			CellRendererText textRenderer = new CellRendererText ();
			column.PackStart (textRenderer, true);
			column.AddAttribute (textRenderer, "text", 2);
			column.Expand = true;

			treeView.AppendColumn (column);
			Ambience ambience = AmbienceService.GetAmbienceForFile (options.Document.FileName);
			foreach (object obj in GetValidMembers ()) {
				var member = obj as IEntity;
				if (member != null) {
					Store.AppendValues (false, ImageService.GetPixbuf (member.GetStockIcon (), IconSize.Menu), ambience.GetString (member, OutputFlags.ClassBrowserEntries), member);
					continue;
				}

				var tuple = obj as Tuple<IMember, bool>;
				if (tuple != null) {
					Store.AppendValues (false, ImageService.GetPixbuf (tuple.Item1.GetStockIcon (), IconSize.Menu), ambience.GetString (tuple.Item1, OutputFlags.ClassBrowserEntries), tuple);
					continue;
				}

				var variable = obj as IVariable;
				if (variable != null)
					Store.AppendValues (false, ImageService.GetPixbuf (variable.GetStockIcon (), IconSize.Menu), variable.Name, variable);
			}
			
			treeView.Model = store;
		}
		
		void ToggleRendererToggled (object o, ToggledArgs args)
		{
			Gtk.TreeIter iter;
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
			Mono.TextEditor.TextDocument doc = new Mono.TextEditor.TextDocument ();
			doc.Text = text;
			StringBuilder result = new StringBuilder ();
			foreach (var line in doc.Lines) {
				result.Append (indent);
				result.Append (doc.GetTextAt (line.SegmentIncludingDelimiter));
			}
			return result.ToString ();
		}

		public void GenerateCode ()
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

			var output = new StringBuilder ();
			string indent = RefactoringOptions.GetIndent (options.Document, options.EnclosingMember != null ? (IEntity)options.EnclosingMember : options.EnclosingType) + "\t";
			foreach (string nodeText in GenerateCode (includedMembers)) {
				if (output.Length > 0) {
					output.AppendLine ();
					output.AppendLine ();
				}
				output.Append (AddIndent (nodeText, indent));
			}

			if (output.Length > 0) {
				var data = options.Document.Editor;
				data.InsertAtCaret (output.ToString ().TrimStart ());
			}
		}
	}
}
