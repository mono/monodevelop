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
using MonoDevelop.Core;
using System.Linq;
using MonoDevelop.CodeGeneration;
using MonoDevelop.CSharp.Completion;
using Microsoft.CodeAnalysis;
using MonoDevelop.CSharp.Refactoring;
using ICSharpCode.NRefactory6.CSharp;

namespace MonoDevelop.CodeGeneration
{
	abstract class BaseExportCodeGenerator : ICodeGenerator
	{
		public abstract bool IsValidMember (ISymbol member);

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


//		public static Attribute GenerateExportAttribute (RefactoringContext ctx, IMember member)
//		{
//			if (member == null)
//				return null;
//
//			bool useMonoTouchNamespace = false;
//			var exportAttribute = member.GetAttribute (new FullTypeName (new TopLevelTypeName ("Foundation", "ExportAttribute")));
//			if (exportAttribute == null) {
//				useMonoTouchNamespace = true;
//				exportAttribute = member.GetAttribute (new FullTypeName (new TopLevelTypeName ("MonoTouch.Foundation", "ExportAttribute")));
//			}
//
//			if (exportAttribute == null || exportAttribute.PositionalArguments.Count == 0)
//				return null;
//
//			var astType = useMonoTouchNamespace
//				? CreateMonoTouchExportAttributeAst (ctx)
//				: CreateUnifiedExportAttributeAst (ctx);
//
//			var attr = new Attribute {
//				Type = astType,
//			};
//
//			attr.Arguments.Add (new PrimitiveExpression (exportAttribute.PositionalArguments [0].ConstantValue)); 
//			return attr;
//		}
//
//		static AstType CreateUnifiedExportAttributeAst (RefactoringContext ctx)
//		{
//			var astType = ctx.CreateShortType ("Foundation", "ExportAttribute");
//			if (astType is SimpleType) {
//				astType = new SimpleType ("Export");
//			} else {
//				astType = new MemberType (new SimpleType ("Foundation"), "Export");
//			}
//			return astType;
//		}
//
//		static AstType CreateMonoTouchExportAttributeAst (RefactoringContext ctx)
//		{
//			var astType = ctx.CreateShortType ("MonoTouch.Foundation", "ExportAttribute");
//			if (astType is SimpleType) {
//				astType = new SimpleType ("Export");
//			} else {
//				astType = new MemberType (new MemberType (new SimpleType ("MonoTouch"), "Foundation"), "Export");
//			}
//			return astType;
//		}
//
//		static IMember GetProtocolMember (RefactoringContext ctx, IType protocolType, IMember member)
//		{
//			foreach (var m in protocolType.GetMembers (m => m.SymbolKind == member.SymbolKind && m.Name == member.Name)) {
//				if (!SignatureComparer.Ordinal.Equals (m, member))
//					return null;
//				var prop = m as IProperty;
//				if (prop != null) {
//					if (prop.CanGet && GenerateExportAttribute (ctx, prop.Getter) != null ||
//						prop.CanSet && GenerateExportAttribute (ctx, prop.Setter) != null)
//						return m;
//				} else {
//					if (GenerateExportAttribute (ctx, m) != null)
//						return m;
//				}
//			}
//			return null;
//		}
//
		static string GetProtocol (ISymbol member)
		{
			var attr = member.GetAttributes ().FirstOrDefault (a => a.AttributeClass.Name == "ExportAttribute" && ProtocolMemberContextHandler.IsFoundationNamespace (a.AttributeClass.ContainingNamespace));
			if (attr == null || attr.ConstructorArguments.Length == 0)
				return null;
			return attr.ConstructorArguments.First ().Value.ToString ();
		}

		public static bool IsImplemented (ITypeSymbol type, ISymbol protocolMember)
		{
			foreach (var m in type.GetMembers().Where (m => m.Kind == protocolMember.Kind && m.Name == protocolMember.Name)) {
				var p = m as IPropertySymbol;
				if (p != null) {
					if (p.GetMethod != null && ((IPropertySymbol)protocolMember).GetMethod != null && GetProtocol (p.GetMethod) == GetProtocol (((IPropertySymbol)protocolMember).GetMethod))
						return true;
					if (p.SetMethod != null && ((IPropertySymbol)protocolMember).SetMethod != null && GetProtocol (p.SetMethod) == GetProtocol (((IPropertySymbol)protocolMember).SetMethod))
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
				foreach (var t in type.GetBaseTypes ()) {
					string name;
					if (!ProtocolMemberContextHandler.HasProtocolAttribute (t, out name))
						continue;
					var protocolType = Options.CurrentState.Compilation.GetTypeByMetadataName (t.ContainingNamespace.GetFullName () + "." + name);
					if (protocolType == null)
						break;
					foreach (var member in protocolType.GetMembers().OfType<IMethodSymbol>()) {
						if (member.ExplicitInterfaceImplementations.Length > 0)
							continue;
						if (!cg.IsValidMember (member))
							continue;
						if (IsImplemented (type, member))
							continue;
						if (member.GetAttributes ().Any (a => a.AttributeClass.Name == "ExportAttribute" && ProtocolMemberContextHandler.IsFoundationNamespace (a.AttributeClass.ContainingNamespace)))
							yield return member;
					}
					foreach (var member in protocolType.GetMembers().OfType<IPropertySymbol>()) {
						if (member.ExplicitInterfaceImplementations.Length > 0)
							continue;
						if (!cg.IsValidMember (member))
							continue;
						if (IsImplemented (type, member))
							continue;
						if (member.GetMethod != null && member.GetMethod.GetAttributes ().Any (a => a.AttributeClass.Name == "ExportAttribute" && ProtocolMemberContextHandler.IsFoundationNamespace (a.AttributeClass.ContainingNamespace)) ||
							member.SetMethod != null && member.SetMethod.GetAttributes ().Any (a => a.AttributeClass.Name == "ExportAttribute" && ProtocolMemberContextHandler.IsFoundationNamespace (a.AttributeClass.ContainingNamespace)))
							yield return member;
					}
				}
			}

			protected override IEnumerable<string> GenerateCode (List<object> includedMembers)
			{
				foreach (ISymbol member in includedMembers) {
					yield return CSharpCodeGenerator.CreateProtocolMemberImplementation (Options.DocumentContext, Options.Editor, Options.EnclosingType, Options.EnclosingPart.GetLocation (), member, false, null).Code;
				}
			}
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

		public override bool IsValidMember (ISymbol member)
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

		public override bool IsValidMember (ISymbol member)
		{
			return member.IsAbstract;
		}
	}
}