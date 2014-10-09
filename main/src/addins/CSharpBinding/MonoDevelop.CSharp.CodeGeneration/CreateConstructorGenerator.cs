// 
// CreateConstructorGenerator.cs
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

using Gtk;
using System.Collections.Generic;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Core;
using System.Linq;
using Microsoft.CodeAnalysis;
using ICSharpCode.NRefactory6.CSharp;

namespace MonoDevelop.CodeGeneration
{
	class CreateConstructorGenerator : ICodeGenerator
	{
		public string Icon {
			get {
				return "md-newmethod";
			}
		}
		
		public string Text {
			get {
				return GettextCatalog.GetString ("Constructor");
			}
		}
		
		public string GenerateDescription {
			get {
				return GettextCatalog.GetString ("Select members to be initialized by the constructor.");
			}
		}
		
		public bool IsValid (CodeGenerationOptions options)
		{
			var createConstructor = new CreateConstructor (options);
			return createConstructor.IsValid ();
		}
		
		public IGenerateAction InitalizeSelection (CodeGenerationOptions options, TreeView treeView)
		{
			var createConstructor = new CreateConstructor (options);
			createConstructor.Initialize (treeView);
			return createConstructor;
		}
		
		class CreateConstructor : AbstractGenerateAction
		{
			public CreateConstructor (CodeGenerationOptions options) : base (options)
			{
			}
			
			protected override IEnumerable<object> GetValidMembers ()
			{
				if (Options.EnclosingType == null || Options.EnclosingMember != null)
					yield break;

				var bt = Options.EnclosingType.BaseType;

				if (bt != null) {
					var ctors = bt.GetMembers ().OfType<IMethodSymbol> ().Where(m => m.MethodKind == MethodKind.Constructor && !m.IsImplicitlyDeclared).ToList ();
					foreach (IMethodSymbol ctor in ctors) {
						if (ctor.Parameters.Length > 0 || ctors.Count > 1) {
							yield return ctor;
						}
					} 
				}

				foreach (IFieldSymbol field in Options.EnclosingType.GetMembers ().OfType<IFieldSymbol> ()) {
					if (field.IsImplicitlyDeclared)
						continue;
					yield return field;
				}

				foreach (IPropertySymbol property in Options.EnclosingType.GetMembers ().OfType<IPropertySymbol> ()) {
					if (property.IsImplicitlyDeclared)
						continue;
					if (property.SetMethod == null)
						continue;
					yield return property;
				}
			}
			
			static string CreateParameterName (ISymbol member)
			{
				if (char.IsUpper (member.Name[0]))
					return char.ToLower (member.Name[0]) + member.Name.Substring (1);
				return member.Name;
			}
			
			protected override IEnumerable<string> GenerateCode (List<object> includedMembers)
			{
				bool gotConstructorOverrides = false;
				foreach (IMethodSymbol m in includedMembers.OfType<IMethodSymbol> ().Where (m => m.MethodKind == MethodKind.Constructor)) {
					gotConstructorOverrides = true;
					var init = new ConstructorInitializer {
						ConstructorInitializerType = ConstructorInitializerType.Base
					};

					var overridenConstructor = new ConstructorDeclaration {
						Name = Options.EnclosingType.Name,
						Modifiers = Modifiers.Public,
						Body = new BlockStatement (),
					};

					if (m.Parameters.Length > 0)
						overridenConstructor.Initializer = init;

					foreach (var par in m.Parameters) {
						overridenConstructor.Parameters.Add (new ParameterDeclaration (new SimpleType (Options.CreateShortType (par.Type)), par.Name));
						init.Arguments.Add (new IdentifierExpression(par.Name)); 
					}

					foreach (ISymbol member in includedMembers) {
						if (member.Kind == SymbolKind.Method)
							continue;
						overridenConstructor.Parameters.Add (new ParameterDeclaration (new SimpleType (Options.CreateShortType (member.GetReturnType ())), CreateParameterName (member)));

						var memberReference = new MemberReferenceExpression (new ThisReferenceExpression (), member.Name);
						var assign = new AssignmentExpression (memberReference, AssignmentOperatorType.Assign, new IdentifierExpression (CreateParameterName (member)));
						overridenConstructor.Body.Statements.Add (new ExpressionStatement (assign));
					}

					yield return overridenConstructor.ToString ();
				}
				if (gotConstructorOverrides)
					yield break;
				var constructorDeclaration = new ConstructorDeclaration {
					Name = Options.EnclosingType.Name,
					Modifiers = Modifiers.Public,
					Body = new BlockStatement ()
				};

				foreach (ISymbol member in includedMembers) {
					constructorDeclaration.Parameters.Add (new ParameterDeclaration (new SimpleType (Options.CreateShortType (member.GetReturnType ())), CreateParameterName (member)));

					var memberReference = new MemberReferenceExpression (new ThisReferenceExpression (), member.Name);
					var assign = new AssignmentExpression (memberReference, AssignmentOperatorType.Assign, new IdentifierExpression (CreateParameterName (member)));
					constructorDeclaration.Body.Statements.Add (new ExpressionStatement (assign));
				}
				
				yield return constructorDeclaration.ToString ();
			}
		}
	}
}
