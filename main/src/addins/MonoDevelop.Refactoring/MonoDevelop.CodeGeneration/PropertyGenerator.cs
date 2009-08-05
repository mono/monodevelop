// 
// PropertyGenerator.cs
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

using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.PrettyPrinter;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects.Dom;
using Gtk;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Core.Gui;
using System.Collections.Generic;
using MonoDevelop.Refactoring;
using System.Text;

namespace MonoDevelop.CodeGeneration
{
	public class PropertyGenerator : ICodeGenerator
	{
		public string Icon {
			get {
				return "md-property";
			}
		}
		
		public string Text {
			get {
				return GettextCatalog.GetString ("Properties");
			}
		}
		
		public string GenerateDescription {
			get {
				return GettextCatalog.GetString ("Select members which should be exposed.");
			}
		}
		
		public bool IsValid (CodeGenerationOptions options)
		{
			return new CreateProperty (options).IsValid ();
		}
		
		
		public IGenerateAction InitalizeSelection (CodeGenerationOptions options, Gtk.TreeView treeView)
		{
			CreateProperty createProperty = new CreateProperty (options);
			createProperty.Initialize (treeView);
			return createProperty;
		}
		
		internal class CreateProperty : IGenerateAction
		{
			TreeStore store = new TreeStore (typeof(bool), typeof(Gdk.Pixbuf), typeof(string), typeof(IMember));
			CodeGenerationOptions options;
			
			public bool ReadOnly {
				get;
				set;
			}
			
			public CreateProperty (CodeGenerationOptions options)
			{
				this.options = options;
			}
			
			public bool IsValid ()
			{
				if (options.EnclosingType == null || options.EnclosingMember != null)
					return false;
				foreach (IField field in options.EnclosingType.Fields) {
					List<IMember> list = options.EnclosingType.SearchMember (CreatePropertyName (field), true);
					if (list == null || list.Count == 0)
						return true;
				}
				return false;
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
					List<IMember> list = options.EnclosingType.SearchMember (CreatePropertyName (field), true);
					if (list == null || list.Count == 0)
						store.AppendValues (false, ImageService.GetPixbuf (field.StockIcon, IconSize.Menu), field.Name, field);
				}

				treeView.Model = store;
			}
			
			static string CreatePropertyName (IMember member)
			{
				return char.ToUpper (member.Name[0]) + member.Name.Substring (1);
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

				StringBuilder output = new StringBuilder ();

				foreach (IField field in includedMembers) {
					PropertyDeclaration propertyDeclaration = new PropertyDeclaration (ICSharpCode.NRefactory.Ast.Modifiers.Public, null, CreatePropertyName (field), null);
					propertyDeclaration.TypeReference = field.ReturnType.ConvertToTypeReference ();
					propertyDeclaration.GetRegion = new PropertyGetRegion (new BlockStatement (), null);
					MemberReferenceExpression memberReference = new MemberReferenceExpression (new ThisReferenceExpression (), field.Name);
					propertyDeclaration.GetRegion.Block.Children.Add (new ReturnStatement (memberReference));

					if (!ReadOnly) {
						propertyDeclaration.SetRegion = new PropertySetRegion (new BlockStatement (), null);
						AssignmentExpression assign = new AssignmentExpression (memberReference, AssignmentOperatorType.Assign, new IdentifierExpression ("value"));
						propertyDeclaration.SetRegion.Block.Children.Add (new ExpressionStatement (assign));
					}

					if (output.Length > 0)
						output.AppendLine ();
					output.Append (astProvider.OutputNode (options.Dom, propertyDeclaration, RefactoringOptions.GetIndent (options.Document, options.EnclosingType) + "\t"));
				}

				options.Document.TextEditor.InsertText (options.Document.TextEditor.GetPositionFromLineColumn (options.Document.TextEditor.CursorLine, 1), output.ToString ());
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
