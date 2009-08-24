// 
// OverrideMethodsGenerator.cs
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
using MonoDevelop.Projects.Dom.Output;

namespace MonoDevelop.CodeGeneration
{
	public class OverrideMembersGenerator : ICodeGenerator
	{
		public string Icon {
			get {
				return "md-method";
			}
		}
		
		public string Text {
			get {
				return GettextCatalog.GetString ("Override members");
			}
		}
		
		public string GenerateDescription {
			get {
				return GettextCatalog.GetString ("Select members to be overridden.");
			}
		}
		
		public bool IsValid (CodeGenerationOptions options)
		{
			return new OverrideMethods (options).IsValid ();
		}
		
		public IGenerateAction InitalizeSelection (CodeGenerationOptions options, Gtk.TreeView treeView)
		{
			OverrideMethods overrideMethods = new OverrideMethods (options);
			overrideMethods.Initialize (treeView);
			return overrideMethods;
		}
		
		class OverrideMethods : AbstractGenerateAction
		{
			public OverrideMethods (CodeGenerationOptions options) : base (options)
			{
			}
			
			protected override IEnumerable<IMember> GetValidMembers ()
			{
				if (Options.EnclosingType == null || Options.EnclosingMember != null)
					yield break;
				HashSet<string> memberName = new HashSet<string> ();
				foreach (IType type in Options.Dom.GetInheritanceTree (Options.EnclosingType)) {
					if (type.Equals (Options.EnclosingType))
						continue;
					foreach (IMember member in type.Members) {
						if (member.IsSpecialName)
							continue;
						if (type.ClassType == MonoDevelop.Projects.Dom.ClassType.Interface || member.IsAbstract || member.IsVirtual || member.IsOverride) {
							string id = AmbienceService.DefaultAmbience.GetString (member, OutputFlags.ClassBrowserEntries);
							if (memberName.Contains (id))
								continue;
							memberName.Add (id);
							yield return member;
						}
					}
				}
			}
			
			static ICSharpCode.NRefactory.Ast.ParameterModifiers GetModifier (IParameter para)
			{
				if (para.IsOut)
					return ICSharpCode.NRefactory.Ast.ParameterModifiers.Out;
				if (para.IsRef)
					return ICSharpCode.NRefactory.Ast.ParameterModifiers.Ref;
				if (para.IsParams)
					return ICSharpCode.NRefactory.Ast.ParameterModifiers.Params;
				return ICSharpCode.NRefactory.Ast.ParameterModifiers.None;
			}
			
			static FieldDirection GetDirection (IParameter para)
			{
				if (para.IsOut)
					return FieldDirection.Out;
				if (para.IsRef)
					return FieldDirection.Ref;
				return FieldDirection.None;
			}
			
			static readonly INode throwNotImplemented = new ThrowStatement (new ObjectCreateExpression (new TypeReference ("System.NotImplementedException"), null));
			protected override IEnumerable<INode> GenerateCode (List<IMember> includedMembers)
			{
				foreach (IMember member in includedMembers) {
					ICSharpCode.NRefactory.Ast.Modifiers modifier = (((ICSharpCode.NRefactory.Ast.Modifiers)member.Modifiers) & ~(ICSharpCode.NRefactory.Ast.Modifiers.Abstract | ICSharpCode.NRefactory.Ast.Modifiers.Virtual));
					bool isInterfaceMember = member.DeclaringType.ClassType == MonoDevelop.Projects.Dom.ClassType.Interface;
					if (!isInterfaceMember)
						modifier |= ICSharpCode.NRefactory.Ast.Modifiers.Override;
					if (isInterfaceMember)
						modifier |= ICSharpCode.NRefactory.Ast.Modifiers.Public;
					
					MemberReferenceExpression baseReference = new MemberReferenceExpression (new BaseReferenceExpression (), member.Name);
					
					if (member is IMethod) {
						IMethod method = (IMethod)member;
						MethodDeclaration methodDeclaration = new MethodDeclaration () { 
							Name = method.Name,
							TypeReference = member.ReturnType.ConvertToTypeReference (),
							Modifier = modifier,
							Body = new BlockStatement ()
						};
						
						List<Expression> arguments = new List<Expression> ();
						foreach (IParameter parameter in method.Parameters) {
							methodDeclaration.Parameters.Add (new ParameterDeclarationExpression (MatchNamespaceImports (parameter.ReturnType.ConvertToTypeReference ()), parameter.Name, GetModifier (parameter)));
							arguments.Add (new DirectionExpression (GetDirection (parameter), new IdentifierExpression (parameter.Name)));
						}
						
						if (isInterfaceMember) {
							methodDeclaration.Body.AddChild (throwNotImplemented);
						} else {
							InvocationExpression baseInvocation = new InvocationExpression (baseReference, arguments);
							if (method.ReturnType.FullName == "System.Void") {
								methodDeclaration.Body.AddChild (new ExpressionStatement (baseInvocation));
							} else {
								methodDeclaration.Body.AddChild (new ReturnStatement (baseInvocation));
							}
						}
						yield return methodDeclaration;
					}
					
					if (member is IProperty) {
						IProperty property = (IProperty)member;
						PropertyDeclaration propertyDeclaration = new PropertyDeclaration (modifier, null, member.Name, null);
						propertyDeclaration.TypeReference = MatchNamespaceImports (member.ReturnType.ConvertToTypeReference ());
						if (property.HasGet) {
							BlockStatement block = new BlockStatement ();
							block.AddChild (isInterfaceMember ? throwNotImplemented : new ReturnStatement (baseReference));
							propertyDeclaration.GetRegion = new PropertyGetRegion (block, null);
						}
						if (property.HasSet) {
							BlockStatement block = new BlockStatement ();
							block.AddChild (isInterfaceMember ? throwNotImplemented : new ExpressionStatement (new AssignmentExpression (baseReference, AssignmentOperatorType.Assign, new IdentifierExpression ("value"))));
							propertyDeclaration.SetRegion = new PropertySetRegion (block, null);
						}
						yield return propertyDeclaration;
					}
				}
				
			}

			ICSharpCode.NRefactory.Ast.TypeReference MatchNamespaceImports (ICSharpCode.NRefactory.Ast.TypeReference typeReference)
			{
				string prefix = "";
				foreach (IUsing u in Options.Document.CompilationUnit.Usings) {
					if (!u.IsFromNamespace || u.Region.Contains (Options.Document.TextEditor.CursorLine, Options.Document.TextEditor.CursorColumn)) {
						foreach (string n in u.Namespaces) {
							if (n.Length <= prefix.Length)
								continue;
							if (typeReference.Type.StartsWith (n + "."))
								prefix = n;
						}
					}
				}
				if (!string.IsNullOrEmpty (prefix))
					typeReference.Type = typeReference.Type.Substring (prefix.Length + 1);
				return typeReference;
			}
		}
	}
}
