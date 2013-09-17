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
					foreach (var attrs in t.GetDefinition ().GetAttributes ()) {
						if (attrs.AttributeType.Name != "ProtocolAttribute" || attrs.AttributeType.Namespace != "MonoTouch.Foundation")
							continue;
						foreach (var na in attrs.NamedArguments) {
							if (na.Key.Name != "Name")
								continue;
							string name = na.Value.ConstantValue as string;
							if (name == null)
								break;
							var protocolType = Options.Document.Compilation.FindType (new FullTypeName (new TopLevelTypeName (t.Namespace, name)));
							if (protocolType == null)
								break;
							foreach (var member in protocolType.GetMembers ()) {
								if (member.Attributes.Any (a => a.AttributeType.Name == "ExportAttribute" &&  a.AttributeType.Namespace == "MonoTouch.Foundation"))
									yield return member;
							}
						}
					}
				}
			}

			protected override IEnumerable<string> GenerateCode (List<object> includedMembers)
			{
				var generator = Options.CreateCodeGenerator ();
				generator.AutoIndent = false;
				var ctx = new MDRefactoringContext (Options.Document, Options.Document.Editor.Caret.Location);
				var builder = ctx.CreateTypeSystemAstBuilder ();

				foreach (IMember member in includedMembers) {
					var method = builder.ConvertEntity (member) as MethodDeclaration;
					method.Body = new BlockStatement () {
						new ThrowStatement (new ObjectCreateExpression (ctx.CreateShortType ("System", "NotImplementedException")))
					};
					var astType = ctx.CreateShortType ("MonoTouch.Foundation", "ExportAttribute");
					if (astType is SimpleType) {
						astType = new SimpleType ("Export");
					} else {
						astType = new MemberType (new MemberType (new SimpleType ("MonoTouch"), "Foundation"), "Export");
					}

					var attr = new ICSharpCode.NRefactory.CSharp.Attribute {
						Type = astType,
					};
					method.Modifiers &= ~Modifiers.Virtual;
					var exportAttribute = member.GetAttribute (new FullTypeName (new TopLevelTypeName ("MonoTouch.Foundation", "ExportAttribute"))); 
					attr.Arguments.Add (new PrimitiveExpression (exportAttribute.PositionalArguments.First ().ConstantValue)); 
					method.Attributes.Add (new AttributeSection {
						Attributes = { attr }
					}); 
					yield return method.ToString ();
				}
			}
		}
	}
}

