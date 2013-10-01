//
// ExportCodeGenerator.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.NRefactory.TypeSystem;
using System;
using System.Linq;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using MonoDevelop.CSharp.Refactoring.CodeActions;
using MonoDevelop.CSharp.Refactoring;

namespace MonoDevelop.CodeGeneration
{
	class ExportCodeGenerator : ICodeGenerator
	{

		#region ICodeGenerator implementation

		bool ICodeGenerator.IsValid (CodeGenerationOptions options)
		{
			return new ExportMethods (options).IsValid ();
		}

		IGenerateAction ICodeGenerator.InitalizeSelection (CodeGenerationOptions options, TreeView treeView)
		{
			var exportMethods = new ExportMethods (options);
			exportMethods.Initialize (treeView);
			return exportMethods;
		}

		string ICodeGenerator.Icon {
			get {
				return "md-method";
			}
		}

		string ICodeGenerator.Text {
			get {
				return GettextCatalog.GetString ("Implement protocol methods");
			}
		}

		string ICodeGenerator.GenerateDescription {
			get {
				return GettextCatalog.GetString ("Select methods to implement");
			}
		}

		#endregion

		class ExportMethods : AbstractGenerateAction
		{
			public ExportMethods (CodeGenerationOptions options) : base (options)
			{
			}


			protected override IEnumerable<object> GetValidMembers ()
			{
				var type = Options.EnclosingType;
				if (type == null || Options.EnclosingMember != null)
					yield break;
				foreach (var t in type.DirectBaseTypes) {
					string name;
					if (!CSharpCodeGenerationService.HasProtocolAttribute (t, out name))
						continue;
					var protocolType = Options.Document.Compilation.FindType (new FullTypeName (new TopLevelTypeName (t.Namespace, name)));
					if (protocolType == null)
						break;
					foreach (var member in protocolType.GetMethods (null, GetMemberOptions.IgnoreInheritedMembers)) {
						if (member.Attributes.Any (a => a.AttributeType.Name == "ExportAttribute" &&  a.AttributeType.Namespace == "MonoTouch.Foundation"))
							yield return member;
					}
					foreach (var member in protocolType.GetProperties (null, GetMemberOptions.IgnoreInheritedMembers)) {
						if (member.CanGet && member.Getter.Attributes.Any (a => a.AttributeType.Name == "ExportAttribute" &&  a.AttributeType.Namespace == "MonoTouch.Foundation") ||
							member.CanSet && member.Setter.Attributes.Any (a => a.AttributeType.Name == "ExportAttribute" &&  a.AttributeType.Namespace == "MonoTouch.Foundation"))
							yield return member;
					}
				}
			}

			protected override IEnumerable<string> GenerateCode (List<object> includedMembers)
			{
				var generator = Options.CreateCodeGenerator ();
				generator.AutoIndent = false;
				var ctx = MDRefactoringContext.Create (Options.Document, Options.Document.Editor.Caret.Location);
				if (ctx == null)
					yield break;
				var builder = ctx.CreateTypeSystemAstBuilder ();

				foreach (IMember member in includedMembers) {
					var method = builder.ConvertEntity (member) as MethodDeclaration;
					if (method != null) {
						method.Body = new BlockStatement () {
							new ThrowStatement (new ObjectCreateExpression (ctx.CreateShortType ("System", "NotImplementedException")))
						};
						method.Modifiers &= ~Modifiers.Virtual;
						method.Modifiers &= ~Modifiers.Abstract;

						method.Attributes.Add (new AttributeSection {
							Attributes = { CSharpCodeGenerationService.GenerateExportAttribute (ctx, member) }
						}); 
						yield return method.ToString ();
						continue;
					}
					var property = builder.ConvertEntity (member) as PropertyDeclaration;
					if (property != null) {
						var p = (IProperty)member;

						var astType = ctx.CreateShortType ("MonoTouch.Foundation", "ExportAttribute");
						if (astType is SimpleType) {
							astType = new SimpleType ("Export");
						} else {
							astType = new MemberType (new MemberType (new SimpleType ("MonoTouch"), "Foundation"), "Export");
						}

						property.Modifiers &= ~Modifiers.Virtual;
						property.Modifiers &= ~Modifiers.Abstract;

						if (p.CanGet) {
							property.Getter.Body = new BlockStatement () {
								new ThrowStatement (new ObjectCreateExpression (ctx.CreateShortType ("System", "NotImplementedException")))
							};

							property.Getter.Attributes.Add (new AttributeSection {
								Attributes = { CSharpCodeGenerationService.GenerateExportAttribute (ctx, p.Getter) }
							}); 
						}
						if (p.CanSet) {
							property.Setter.Body = new BlockStatement () {
								new ThrowStatement (new ObjectCreateExpression (ctx.CreateShortType ("System", "NotImplementedException")))
							};

							property.Setter.Attributes.Add (new AttributeSection {
								Attributes = { CSharpCodeGenerationService.GenerateExportAttribute (ctx, p.Setter)  }
							}); 
						}
						yield return property.ToString ();
						continue;
					}
				}
			}
		}
	}
}

