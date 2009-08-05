// 
// WriteLineGenerator.cs
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
using MonoDevelop.Components;
using Gtk;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects.Dom;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Ast;
using System.Text;
using MonoDevelop.Core;
using MonoDevelop.Refactoring;

namespace MonoDevelop.CodeGeneration
{
	public class WriteLineGenerator: ICodeGenerator
	{
		public string Icon {
			get {
				return "md-newmethod";
			}
		}
		
		public string Text {
			get {
				return GettextCatalog.GetString ("WriteLine call");
			}
		}
		
		public string GenerateDescription {
			get {
				return GettextCatalog.GetString ("Select members to be outputted.");
			}
		}
		
		public bool IsValid (CodeGenerationOptions options)
		{
			return new CreateToString (options).IsValid ();
		}
		
		public IGenerateAction InitalizeSelection (CodeGenerationOptions options, Gtk.TreeView treeView)
		{
			CreateToString createToString = new CreateToString (options);
			createToString.Initialize (treeView);
			return createToString;
		}
		
		class CreateToString : IGenerateAction
		{
			CodeGenerationOptions options;
			TreeStore store = new TreeStore (typeof(bool), typeof(Gdk.Pixbuf), typeof(string), typeof(IMember));
			
			public CreateToString (CodeGenerationOptions options)
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

				CellRendererPixbuf pixbufRenderer = new CellRendererPixbuf ();
				column.PackStart (pixbufRenderer, false);
				column.AddAttribute (pixbufRenderer, "pixbuf", 1);

				CellRendererText textRenderer = new CellRendererText ();
				column.PackStart (textRenderer, true);
				column.AddAttribute (textRenderer, "text", 2);
				column.Expand = true;
				
				treeView.AppendColumn (column);

				foreach (IField field in options.EnclosingType.Fields) {
					store.AppendValues (false, ImageService.GetPixbuf (field.StockIcon, IconSize.Menu), field.Name, field);
				}

				foreach (IProperty property in options.EnclosingType.Properties) {
					if (!property.HasGet)
						continue;
					store.AppendValues (false, ImageService.GetPixbuf (property.StockIcon, IconSize.Menu), property.Name, property);
				}
				treeView.Model = store;
			}
			
			public bool IsValid ()
			{
				if (options.EnclosingType == null || options.EnclosingMember == null)
					return false;
				return options.EnclosingType.FieldCount > 0 || options.EnclosingType.PropertyCount > 0;
			}
			
			public void GenerateCode ()
			{
				TreeIter iter;
				if (!store.GetIterFirst (out iter))
					return;
				List<IMember> includedMembers = new List<IMember> ();
				do {
					bool include = (bool)store.GetValue (iter, 0);
					if (include)
						includedMembers.Add ((IMember)store.GetValue (iter, 3));
				} while (store.IterNext (ref iter));

				INRefactoryASTProvider astProvider = options.GetASTProvider ();
				if (astProvider == null)
					return;

				StringBuilder format = new StringBuilder ();
				int i = 0;
				foreach (IMember member in includedMembers) {
					if (i > 0)
						format.Append (", ");
					format.Append (member.Name);
					format.Append ("={");
					format.Append (i++);
					format.Append ("}");
				}

				InvocationExpression invocationExpression = new InvocationExpression (new MemberReferenceExpression (new IdentifierExpression ("Console"), "WriteLine"));
				invocationExpression.Arguments.Add (new PrimitiveExpression (format.ToString ()));
				foreach (IMember member in includedMembers) {
					invocationExpression.Arguments.Add (new IdentifierExpression (member.Name));
				}
				
				string output = astProvider.OutputNode (options.Dom, new ExpressionStatement (invocationExpression));
				options.Document.TextEditor.InsertText (options.Document.TextEditor.CursorPosition, output);
			}
			
			void ToggleRendererToggled (object o, ToggledArgs args)
			{
				Gtk.TreeIter iter;
				if (store.GetIterFromString (out iter, args.Path)) {
					bool active = (bool)store.GetValue (iter, 0);
					store.SetValue (iter, 0, !active);
				}
			}
		}
	}
}
