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
using MonoDevelop.Refactoring.ExtractMethod;
using ICSharpCode.NRefactory.Visitors;
using ICSharpCode.NRefactory;

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
			return new CreateWriteLine (options).IsValid ();
		}
		
		public IGenerateAction InitalizeSelection (CodeGenerationOptions options, Gtk.TreeView treeView)
		{
			CreateWriteLine createToString = new CreateWriteLine (options);
			createToString.Initialize (treeView);
			return createToString;
		}
		
		class CreateWriteLine : AbstractGenerateAction
		{
			public CreateWriteLine (CodeGenerationOptions options) : base (options)
			{
			}
			
			protected override IEnumerable<IBaseMember> GetValidMembers ()
			{
				if (Options.EnclosingType == null || Options.EnclosingMember == null)
					yield break;
				
				// add local variables
				LookupTableVisitor visitor = new LookupTableVisitor (ICSharpCode.NRefactory.SupportedLanguage.CSharp);
				Location location = new Location (Options.Document.TextEditor.CursorColumn, Options.Document.TextEditor.CursorLine);
				INRefactoryASTProvider provider = Options.GetASTProvider ();
				var result = provider.ParseFile (Options.Document.TextEditor.Text);
				result.AcceptVisitor (visitor, null);
				foreach (var list in visitor.Variables.Values) {
					foreach (LocalLookupVariable varDescr in list) {
						if (varDescr.StartPos <= location && location <= varDescr.EndPos)
							yield return new LocalVariable (Options.EnclosingMember, varDescr.Name, varDescr.TypeRef.ConvertToReturnType (), DomRegion.Empty);
					}
				}
				
				// add parameters
				IMethod method = Options.EnclosingMember as IMethod;
				if (method != null) {
					foreach (IParameter param in method.Parameters)
						yield return param;
				}
				
				IProperty p = Options.EnclosingMember as IProperty;
				if (p != null) {
					foreach (IParameter param in p.Parameters)
						yield return param;
				}
				
				// add type members
				foreach (IField field in Options.EnclosingType.Fields) {
					if (field.IsSpecialName)
						continue;
					yield return field;
				}

				foreach (IProperty property in Options.EnclosingType.Properties) {
					if (property.IsSpecialName)
						continue;
					if (property.HasGet)
						yield return property;
				}
			}
			
			protected override IEnumerable<ICSharpCode.NRefactory.Ast.INode> GenerateCode (List<IBaseMember> includedMembers)
			{
				StringBuilder format = new StringBuilder ();
				int i = 0;
				foreach (IBaseMember member in includedMembers) {
					if (i > 0)
						format.Append (", ");
					format.Append (member.Name);
					format.Append ("={");
					format.Append (i++);
					format.Append ("}");
				}

				InvocationExpression invocationExpression = new InvocationExpression (new MemberReferenceExpression (new IdentifierExpression ("Console"), "WriteLine"));
				invocationExpression.Arguments.Add (new PrimitiveExpression (format.ToString ()));
				foreach (IBaseMember member in includedMembers) {
					invocationExpression.Arguments.Add (new IdentifierExpression (member.Name));
				}
				
				yield return new ExpressionStatement (invocationExpression);
			}
		}
	}
}
