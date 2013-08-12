// 
// ToStringGenerator.cs
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
using MonoDevelop.Ide.Gui;
using System.Collections.Generic;
using ICSharpCode.NRefactory.CSharp;
using System.Text;
using MonoDevelop.Core;
using MonoDevelop.Refactoring;
using ICSharpCode.NRefactory.TypeSystem;
using System.Linq;

namespace MonoDevelop.CodeGeneration
{
	class ToStringGenerator : ICodeGenerator
	{
		public string Icon {
			get {
				return "md-newmethod";
			}
		}
		
		public string Text {
			get {
				return GettextCatalog.GetString ("ToString() implementation");
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
		
		class CreateToString : AbstractGenerateAction
		{
			public CreateToString (CodeGenerationOptions options) : base (options)
			{
			}
			
			protected override IEnumerable<object> GetValidMembers ()
			{
				if (Options.EnclosingType == null || Options.EnclosingMember != null)
					yield break;
				foreach (IField field in Options.EnclosingType.Fields) {
					if (field.IsSynthetic)
						continue;
					yield return field;
				}

				foreach (IProperty property in Options.EnclosingType.Properties) {
					if (property.IsSynthetic)
						continue;
					if (property.CanGet)
						yield return property;
				}
			}
			
			string GetFormatString (IEnumerable<object> includedMembers)
			{
				var format = new StringBuilder ();
				format.Append ("[");
				format.Append (Options.EnclosingType.Name);
				format.Append (": ");
				int i = 0;
				foreach (IEntity member in includedMembers) {
					if (i > 0)
						format.Append (", ");
					format.Append (member.Name);
					format.Append ("={");
					format.Append (i++);
					format.Append ("}");
				}
				format.Append ("]");
				return format.ToString ();
			}
			
			protected override IEnumerable<string> GenerateCode (List<object> includedMembers)
			{
				yield return new MethodDeclaration () {
					Name = "ToString",
					ReturnType = new PrimitiveType ("string"),
					Modifiers = Modifiers.Public | Modifiers.Override,
					Body = new BlockStatement () {
						new ReturnStatement (
							new InvocationExpression (
								new MemberReferenceExpression (new TypeReferenceExpression (new PrimitiveType ("string")), "Format"), 
								new Expression [] { new PrimitiveExpression (GetFormatString (includedMembers)) }.Concat (includedMembers.Select (member => new IdentifierExpression (((IEntity)member).Name)))
							)
						)
					} 
				}.ToString (Options.FormattingOptions);
			}
		}
	}
}
