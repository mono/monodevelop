//
// CSharpCodeGenerationService.cs
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
using System;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using System.Linq;
using Atk;
using Gdk;

namespace MonoDevelop.CSharp.Refactoring
{
	public class CSharpCodeGenerationService : DefaultCodeGenerationService
	{
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

		public static ICSharpCode.NRefactory.CSharp.Attribute GenerateExportAttribute (RefactoringContext ctx, IMember member)
		{
			if (member == null)
				return null;
			var astType = ctx.CreateShortType ("MonoTouch.Foundation", "ExportAttribute");
			if (astType is SimpleType) {
				astType = new SimpleType ("Export");
			} else {
				astType = new MemberType (new MemberType (new SimpleType ("MonoTouch"), "Foundation"), "Export");
			}

			var attr = new ICSharpCode.NRefactory.CSharp.Attribute {
				Type = astType,
			};
			var exportAttribute = member.GetAttribute (new FullTypeName (new TopLevelTypeName ("MonoTouch.Foundation", "ExportAttribute"))); 
			if (exportAttribute == null || exportAttribute.PositionalArguments.Count == 0)
				return null;
			attr.Arguments.Add (new PrimitiveExpression (exportAttribute.PositionalArguments.First ().ConstantValue)); 
			return attr;

		}

		IMember GetProtocolMember (RefactoringContext ctx, IType protocolType, IMember member)
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

		public override EntityDeclaration GenerateMemberImplementation (RefactoringContext context, IMember member, bool explicitImplementation)
		{
			var result = base.GenerateMemberImplementation (context, member, explicitImplementation);
			string name;
			if (HasProtocolAttribute (member.DeclaringType, out name)) {
				var protocolType = context.Compilation.FindType (new FullTypeName (new TopLevelTypeName (member.DeclaringType.Namespace, name)));
				if (protocolType != null) {
					var property = result as PropertyDeclaration;
					var protocolMember = GetProtocolMember (context, protocolType, member);
					if (protocolMember != null) {
						if (property != null) {
							var ppm = (IProperty)protocolMember;
							if (ppm.CanGet) {
								var attr = CSharpCodeGenerationService.GenerateExportAttribute (context, ppm.Getter);
								if (attr != null) {
									property.Getter.Attributes.Add (new AttributeSection {
										Attributes = { attr }
									}); 
								}
							}

							if (ppm.CanSet) {
								var attr = CSharpCodeGenerationService.GenerateExportAttribute (context, ppm.Setter);
								if (attr != null) {
									property.Getter.Attributes.Add (new AttributeSection {
										Attributes = { attr }
									});
								}
							}
						} else {
							var attribute = CSharpCodeGenerationService.GenerateExportAttribute (context, protocolMember);
							if (attribute != null) {
								result.Attributes.Add (new AttributeSection {
									Attributes = { attribute  }
								});
							}
						}
					}
				}
			}

			if (CSharpCodeGenerator.IsMonoTouchModelMember (member)) {
				var m = result as MethodDeclaration;
				if (m != null) {
					for (int i = CSharpCodeGenerator.MonoTouchComments.Length - 1; i >= 0; i--) {
						m.Body.InsertChildBefore (m.Body.Statements.First (), new Comment (CSharpCodeGenerator.MonoTouchComments [i]), Roles.Comment);
					}
				}

				var p = result as PropertyDeclaration;
				if (p != null) {
					for (int i = CSharpCodeGenerator.MonoTouchComments.Length - 1; i >= 0; i--) {
						if (!p.Getter.IsNull)
							p.Getter.Body.InsertChildBefore (p.Getter.Body.Statements.First (), new Comment (CSharpCodeGenerator.MonoTouchComments [i]), Roles.Comment);
						if (!p.Setter.IsNull)
							p.Setter.Body.InsertChildBefore (p.Setter.Body.Statements.First (), new Comment (CSharpCodeGenerator.MonoTouchComments [i]), Roles.Comment);
					}
				}
			}
			return result;
		}
	}
}

