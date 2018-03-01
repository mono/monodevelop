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
using System.Collections.Generic;
using System.Text;
using MonoDevelop.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;

namespace MonoDevelop.CodeGeneration
{
	class WriteLineGenerator: ICodeGenerator
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
			return new CreateWriteLine (options).IsValid ();
		}
		
		public IGenerateAction InitalizeSelection (CodeGenerationOptions options, Gtk.TreeView treeView)
		{
			var createToString = new CreateWriteLine (options);
			createToString.Initialize (treeView);
			return createToString;
		}
		
		class CreateWriteLine : AbstractGenerateAction
		{
			public CreateWriteLine (CodeGenerationOptions options) : base (options)
			{
			}
			
			protected override IEnumerable<object> GetValidMembers ()
			{
				if (Options == null || Options.EnclosingType == null)
					yield break;
				if (Options.EnclosingMember == null)
					yield break;
				if (Options.DocumentContext == null)
					yield break;
				var editor = Options.Editor;
				if (editor == null)
					yield break;
				
				// add local variables
				var state = Options.CurrentState;
				if (state != null) {
					foreach (var v in state.LookupSymbols (editor.CaretOffset).OfType<ILocalSymbol> ())
						yield return v;
				}

				// add parameters
				if (Options.EnclosingMember is IMethodSymbol) {
					foreach (var param in ((IMethodSymbol)Options.EnclosingMember).Parameters)
						yield return param;
				}
				if (Options.EnclosingMember is IPropertySymbol) {
					foreach (var param in ((IPropertySymbol)Options.EnclosingMember).Parameters)
						yield return param;
				}

				// add type members
				foreach (IFieldSymbol field in Options.EnclosingType.GetMembers ().OfType<IFieldSymbol> ()) {
					if (field.IsImplicitlyDeclared)
						continue;
					yield return field;
				}

				foreach (IPropertySymbol property in Options.EnclosingType.GetMembers ().OfType<IPropertySymbol> ()) {
					if (property.IsImplicitlyDeclared)
						continue;
					if (property.GetMethod != null)
						yield return property;
				}
			}
			
			static string GetName (object m)
			{
				return ((ISymbol)m).Name;
			}
			
			protected override IEnumerable<string> GenerateCode (List<object> includedMembers)
			{
				var format = StringBuilderCache.Allocate ();
				int i = 0;
				format.Append ("$\"");
				foreach (var member in includedMembers) {
					if (i > 0)
						format.Append (", ");
					format.Append (GetName (member));
					format.Append ("={");
					format.Append (member.ToString ());
					format.Append ("}");
					i++;
				}
				format.Append ("\"");
				var arguments = new List<ArgumentSyntax> ();
				arguments.Add (SyntaxFactory.Argument (SyntaxFactory.ParseExpression (StringBuilderCache.ReturnAndFree (format))));
				var node = 
					SyntaxFactory.ExpressionStatement (
						SyntaxFactory.InvocationExpression (
							SyntaxFactory.MemberAccessExpression (SyntaxKind.SimpleMemberAccessExpression,
								SyntaxFactory.MemberAccessExpression (SyntaxKind.SimpleMemberAccessExpression, 
									SyntaxFactory.IdentifierName ("System"), 
									SyntaxFactory.IdentifierName ("Console")
								),
								SyntaxFactory.IdentifierName ("WriteLine")
							),
							SyntaxFactory.ArgumentList (
								SyntaxFactory.SeparatedList<ArgumentSyntax> (arguments)
							)
						)
					);

				yield return Options.OutputNode (node).Result;
			}
		}
	}
}
