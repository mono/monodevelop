// 
// NullCheckGenerator.cs
//  
// Author:
//       Scott Thomas <scpeterson@novell.com>
// 
// Copyright (c) 2009 Novell
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


using System.Collections.Generic;

using Gtk;
using ICSharpCode.NRefactory.Ast;

using MonoDevelop;
using MonoDevelop.CodeGeneration;
using MonoDevelop.Core;
using MonoDevelop.Projects.Dom;

namespace MonoDevelop.CodeGeneration
{
	public class NullCheckGenerator : ICodeGenerator
	{
		public string Icon {
			get {
				return "md-newmethod";
			}
		}

		public string Text {
			get {
				return GettextCatalog.GetString ("Null check");
			}
		}

		public string GenerateDescription {
			get {
				return GettextCatalog.GetString ("Select parameters to be null-checked.");
			}
		}

		public bool IsValid (CodeGenerationOptions options)
		{
			return new NullCheckGeneratorAction (options).IsValid ();
		}

		public IGenerateAction InitalizeSelection (CodeGenerationOptions options, TreeView treeView)
		{
			var action = new NullCheckGeneratorAction (options);
			action.Initialize (treeView);
			return action;
		}
		
		class NullCheckGeneratorAction : AbstractGenerateAction
		{
			public NullCheckGeneratorAction (CodeGenerationOptions options) : base (options)
			{
			}
			
			protected override IEnumerable<IBaseMember> GetValidMembers ()
			{
				if (Options.EnclosingMember == null || !Options.EnclosingMember.CanHaveParameters) 
					yield break;
				
				foreach (var parameter in Options.EnclosingMember.Parameters) {
					IType type = Options.Dom.SearchType (new SearchTypeRequest (Options.Document.CompilationUnit, parameter.Location.Line, parameter.Location.Column, parameter.ReturnType.FullName, parameter.ReturnType.GenericArguments));
					if (type != null && (type.ClassType == MonoDevelop.Projects.Dom.ClassType.Interface || type.ClassType == MonoDevelop.Projects.Dom.ClassType.Class))
						yield return parameter;
				}
			}
			
			protected override IEnumerable<ICSharpCode.NRefactory.Ast.INode> GenerateCode (List<IBaseMember> includedMembers)
			{
				foreach (var member in includedMembers) {
					yield return new IfElseStatement (
						new BinaryOperatorExpression (
					    	new IdentifierExpression (member.Name),
					        BinaryOperatorType.Equality,
					        new PrimitiveExpression (null)
					    ), new ThrowStatement (
					    	new ObjectCreateExpression (
					        	Options.ShortenTypeName (new TypeReference ("System.ArgumentNullException")),
					            new List<Expression> { new PrimitiveExpression (member.Name) }
							)
					    )
					);
				}
			}
		}
	}
}