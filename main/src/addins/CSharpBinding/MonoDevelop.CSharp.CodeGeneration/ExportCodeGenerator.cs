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
using System.Linq;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using MonoDevelop.CSharp.Refactoring.CodeActions;
using MonoDevelop.CodeGeneration;

namespace MonoDevelop.CodeGeneration
{
	abstract class BaseExportCodeGenerator : ICodeGenerator
	{
		public abstract bool IsValidMember (IMember member);

		#region ICodeGenerator implementation

		bool ICodeGenerator.IsValid (CodeGenerationOptions options)
		{
			return new ExportMethods (this, options).IsValid ();
		}

		IGenerateAction ICodeGenerator.InitalizeSelection (CodeGenerationOptions options, TreeView treeView)
		{
			var exportMethods = new ExportMethods (this, options);
			exportMethods.Initialize (treeView);
			return exportMethods;
		}

		string ICodeGenerator.Icon {
			get {
				return "md-method";
			}
		}

		public abstract string Text {
			get;
		}

		public abstract string GenerateDescription {
			get;
		}

		#endregion

		public static bool HasProtocolAttribute (IType type, out string name)
		{
			foreach (var attrs in type.GetDefinition ().GetAttributes ()) {
				if (attrs.AttributeType.Name == "ProtocolAttribute" && attrs.AttributeType.Namespace == "MonoTouch.Foundation") {
					foreach (var na in attrs.NamedArguments) {
						if (na.Key.Name != "Name")
							continue;
						name = na.Value.ConstantValue as string;
						if (name != null)
							return true;
					}
				}
			}
			name = null;
			return false;
		}

		public static Attribute GenerateExportAttribute (RefactoringContext ctx, IMember member)
		{
			if (member == null)
				return null;
			var astType = ctx.CreateShortType ("MonoTouch.Foundation", "ExportAttribute");
			if (astType is SimpleType) {
				astType = new SimpleType ("Export");
			} else {
				astType = new MemberType (new MemberType (new SimpleType ("MonoTouch"), "Foundation"), "Export");
			}

			var attr = new Attribute {
				Type = astType,
			};
			var exportAttribute = member.GetAttribute (new FullTypeName (new TopLevelTypeName ("MonoTouch.Foundation", "ExportAttribute"))); 
			if (exportAttribute == null || exportAttribute.PositionalArguments.Count == 0)
				return null;
			attr.Arguments.Add (new PrimitiveExpression (exportAttribute.PositionalArguments [0].ConstantValue)); 
			return attr;

		}

		static IMember GetProtocolMember (RefactoringContext ctx, IType protocolType, IMember member)
		{
			foreach (var m in protocolType.GetMembers (m => m.SymbolKind == member.SymbolKind && m.Name == member.Name)) {
				if (!SignatureComparer.Ordinal.Equals (m, member))
					return null;
				var prop = m as IProperty;
				if (prop != null) {
					if (prop.CanGet && GenerateExportAttribute (ctx, prop.Getter) != null ||
						prop.CanSet && GenerateExportAttribute (ctx, prop.Setter) != null)
						return m;
				} else {
					if (GenerateExportAttribute (ctx, m) != null)
						return m;
				}
			}
			return null;
		}

		static string GetProtocol (IMember member)
		{
			var attr = member.Attributes.FirstOrDefault (a => a.AttributeType.Name == "ExportAttribute" && a.AttributeType.Namespace == "MonoTouch.Foundation");
			if (attr == null || attr.PositionalArguments.Count == 0)
				return null;
			return attr.PositionalArguments.First ().ConstantValue.ToString ();
		}

		public static bool IsImplemented (IType type, IMember protocolMember)
		{
			foreach (var m in type.GetMembers (m => m.SymbolKind == protocolMember.SymbolKind && m.Name == protocolMember.Name)) {
				var p = m as IProperty;
				if (p != null) {
					if (p.CanGet && ((IProperty)protocolMember).CanGet && GetProtocol (p.Getter) == GetProtocol (((IProperty)protocolMember).Getter))
						return true;
					if (p.CanSet && ((IProperty)protocolMember).CanSet && GetProtocol (p.Setter) == GetProtocol (((IProperty)protocolMember).Setter))
						return true;
					continue;
				}
				if (GetProtocol (m) == GetProtocol (protocolMember))
					return true;
			}
			return false;
		}

		class ExportMethods : AbstractGenerateAction
		{
			readonly BaseExportCodeGenerator cg;

			public ExportMethods (BaseExportCodeGenerator cg, CodeGenerationOptions options) : base (options)
			{
				this.cg = cg;
			}


			protected override IEnumerable<object> GetValidMembers ()
			{
				var type = Options.EnclosingType;
				if (type == null || Options.EnclosingMember != null)
					yield break;
				foreach (var t in type.DirectBaseTypes) {
					string name;
					if (!HasProtocolAttribute (t, out name))
						continue;
					var protocolType = Options.Document.Compilation.FindType (new FullTypeName (new TopLevelTypeName (t.Namespace, name)));
					if (protocolType == null)
						break;
					foreach (var member in protocolType.GetMethods (null, GetMemberOptions.IgnoreInheritedMembers)) {
						if (member.ImplementedInterfaceMembers.Any ())
							continue;
						if (!cg.IsValidMember (member))
							continue;
						if (IsImplemented (type, member))
							continue;
						if (member.Attributes.Any (a => a.AttributeType.Name == "ExportAttribute" &&  a.AttributeType.Namespace == "MonoTouch.Foundation"))
							yield return member;
					}
					foreach (var member in protocolType.GetProperties (null, GetMemberOptions.IgnoreInheritedMembers)) {
						if (member.ImplementedInterfaceMembers.Any ())
							continue;
						if (!cg.IsValidMember (member))
							continue;
						if (IsImplemented (type, member))
							continue;
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
					yield return GenerateMemberCode (ctx, builder, member);
				}
			}
		}
	
		internal static string GenerateMemberCode (MDRefactoringContext ctx, TypeSystemAstBuilder builder, IMember member)
		{
			var method = builder.ConvertEntity (member) as MethodDeclaration;
			if (method != null) {
				method.Body = new BlockStatement {
					new ThrowStatement (new ObjectCreateExpression (ctx.CreateShortType ("System", "NotImplementedException")))
				};
				method.Modifiers &= ~Modifiers.Virtual;
				method.Modifiers &= ~Modifiers.Abstract;
				method.Attributes.Add (new AttributeSection {
					Attributes =  {
						GenerateExportAttribute (ctx, member)
					}
				});
				return method.ToString (ctx.FormattingOptions);
			}
			var property = builder.ConvertEntity (member) as PropertyDeclaration;
			if (property == null)
				return null;
			var p = (IProperty)member;
			property.Modifiers &= ~Modifiers.Virtual;
			property.Modifiers &= ~Modifiers.Abstract;
			if (p.CanGet) {
				property.Getter.Body = new BlockStatement {
					new ThrowStatement (new ObjectCreateExpression (ctx.CreateShortType ("System", "NotImplementedException")))
				};
				property.Getter.Attributes.Add (new AttributeSection {
					Attributes =  {
						GenerateExportAttribute (ctx, p.Getter)
					}
				});
			}
			if (p.CanSet) {
				property.Setter.Body = new BlockStatement {
					new ThrowStatement (new ObjectCreateExpression (ctx.CreateShortType ("System", "NotImplementedException")))
				};
				property.Setter.Attributes.Add (new AttributeSection {
					Attributes =  {
						GenerateExportAttribute (ctx, p.Setter)
					}
				});
			}
			return property.ToString (ctx.FormattingOptions);
		}
	}

	class OptionalProtocolMemberGenerator : BaseExportCodeGenerator
	{
		public override string Text {
			get {
				return GettextCatalog.GetString ("Implement protocol members");
			}
		}

		public override string GenerateDescription {
			get {
				return GettextCatalog.GetString ("Select protocol members to implement");
			}
		}

		public override bool IsValidMember (IMember member)
		{
			return !member.IsAbstract;
		}
	}

	class RequiredProtocolMemberGenerator : BaseExportCodeGenerator
	{
		public override string Text {
			get {
				return GettextCatalog.GetString ("Implement required protocol members");
			}
		}

		public override string GenerateDescription {
			get {
				return GettextCatalog.GetString ("Select protocol members to implement");
			}
		}

		public override bool IsValidMember (IMember member)
		{
			return member.IsAbstract;
		}
	}

}

