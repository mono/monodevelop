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
using ICSharpCode.NRefactory.Ast;
using MonoDevelop.Core;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Refactoring;

namespace MonoDevelop.CodeGeneration
{
	public class CreateConstructorGenerator : ICodeGenerator
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
			CreateConstructor createConstructor = new CreateConstructor (options);
			return createConstructor.IsValid ();
		}
		
		public IGenerateAction InitalizeSelection (CodeGenerationOptions options, Gtk.TreeView treeView)
		{
			CreateConstructor createConstructor = new CreateConstructor (options);
			createConstructor.Initialize (treeView);
			return createConstructor;
		}
		
		class CreateConstructor : AbstractGenerateAction
		{
			public CreateConstructor (CodeGenerationOptions options) : base (options)
			{
			}
			
			protected override IEnumerable<IBaseMember> GetValidMembers ()
			{
				if (Options.EnclosingType == null || Options.EnclosingMember != null)
					yield break;
				foreach (IField field in Options.EnclosingType.Fields) {
					if (field.IsSpecialName)
						continue;
					yield return field;
				}

				foreach (IProperty property in Options.EnclosingType.Properties) {
					if (property.IsSpecialName)
						continue;
					if (property.GetRegion.Start.Line != property.GetRegion.End.Line || property.GetRegion.End.Column - property.GetRegion.Start.Column != 4)
						continue;
					yield return property;
				}
			}
			
			static string CreateParameterName (IMember member)
			{
				if (char.IsUpper (member.Name[0]))
					return char.ToLower (member.Name[0]) + member.Name.Substring (1);
				return member.Name;
			}
			
			protected override IEnumerable<string> GenerateCode (INRefactoryASTProvider astProvider, string indent, List<IBaseMember> includedMembers)
			{
				List<ParameterDeclarationExpression> parameters = new List<ParameterDeclarationExpression> ();
				foreach (IMember member in includedMembers) {
					parameters.Add (new ParameterDeclarationExpression (member.ReturnType.ConvertToTypeReference (), CreateParameterName (member)));
				}

				ConstructorDeclaration constructorDeclaration = new ConstructorDeclaration (Options.EnclosingType.Name, ICSharpCode.NRefactory.Ast.Modifiers.Public, parameters, null, null);
				constructorDeclaration.Body = new BlockStatement ();
				foreach (IMember member in includedMembers) {
					MemberReferenceExpression memberReference = new MemberReferenceExpression (new ThisReferenceExpression (), member.Name);
					AssignmentExpression assign = new AssignmentExpression (memberReference, AssignmentOperatorType.Assign, new IdentifierExpression (CreateParameterName (member)));
					constructorDeclaration.Body.AddChild (new ExpressionStatement (assign));
				}
				yield return astProvider.OutputNode (this.Options.Dom, constructorDeclaration, indent);
			}
		}
	}
}
