// 
// CSharpCodeGenerator.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Projects.Dom;
using System.Text;
using MonoDevelop.CSharp.Formatting;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Projects.CodeGeneration;

namespace MonoDevelop.CSharp.Refactoring
{
	public class CSharpCodeGenerator : MonoDevelop.Projects.CodeGeneration.CodeGenerator
	{
		static CSharpAmbience ambience = new CSharpAmbience ();
		
		OutputVisitor visitor;
		MonoDevelop.CSharp.Formatting.CSharpFormattingPolicy policy;
		
		public MonoDevelop.CSharp.Formatting.CSharpFormattingPolicy Policy {
			get {
				return this.policy;
			}
			set {
				policy = value;
			}
		}
		
		public CSharpCodeGenerator ()
		{
			visitor = new OutputVisitor (this);
			
			IEnumerable<string> types = MonoDevelop.Ide.DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);
			policy = MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<CSharpFormattingPolicy> (types);
		}
		
		class CodeGenerationOptions
		{
			public bool ExplicitDeclaration { get; set; }
			public IType ImplementingType { get; set; }
		}
		
		public override string WrapInRegions (string regionName, string text)
		{
			StringBuilder result = new StringBuilder ();
			AppendIndent (result);
			result.Append ("#region ");
			result.Append (regionName);
			AppendLine (result);
			result.Append (text);
			AppendLine (result);
			AppendIndent (result);
			result.Append ("#endregion");
			return result.ToString ();
		}
		
		public override CodeGeneratorMemberResult CreateMemberImplementation (IType implementingType, IMember member,
		                                                                      bool explicitDeclaration)
		{
			SetIndentTo (implementingType);
			var options = new CodeGenerationOptions () {
				ExplicitDeclaration = explicitDeclaration,
				ImplementingType = implementingType,
			};
			return member.AcceptVisitor (visitor, options);
		}
		
		void AppendBraceStart (StringBuilder result, BraceStyle braceStyle)
		{
			switch (braceStyle) {
			case BraceStyle.EndOfLine:
				result.Append (" {");
				AppendLine (result);
				break;
			case BraceStyle.EndOfLineWithoutSpace:
				result.Append ("{");
				AppendLine (result);
				break;
			case BraceStyle.NextLine:
				AppendLine (result);
				AppendIndent (result);
				result.Append ("{");
				AppendLine (result);
				break;
			case BraceStyle.NextLineShifted:
				AppendLine (result);
				result.Append (GetIndent (IndentLevel + 1));
				result.Append ("{");
				AppendLine (result);
				break;
			case BraceStyle.NextLineShifted2:
				AppendLine (result);
				result.Append (GetIndent (IndentLevel + 1));
				result.Append ("{");
				AppendLine (result);
				IndentLevel++;
				break;
			default:
				goto case BraceStyle.NextLine;
			}
			IndentLevel++;
		}
		
		void AppendBraceEnd (StringBuilder result, BraceStyle braceStyle)
		{
			switch (braceStyle) {
			case BraceStyle.EndOfLineWithoutSpace:
			case BraceStyle.NextLine:
			case BraceStyle.EndOfLine:
				IndentLevel --;
				AppendIndent (result);
				result.Append ("}");
				break;
			case BraceStyle.NextLineShifted:
				AppendIndent (result);
				result.Append ("}");
				IndentLevel--;
				break;
			case BraceStyle.NextLineShifted2:
				IndentLevel--;
				AppendIndent (result);
				result.Append ("}");
				IndentLevel--;
				break;
			default:
				goto case BraceStyle.NextLine;
			}
		}
		
		void AppendIndent (StringBuilder result)
		{
			result.Append (GetIndent (IndentLevel));
		}
		
		static void AppendReturnType (StringBuilder result, IType implementingType, IReturnType type)
		{
			var shortType = implementingType.CompilationUnit.ShortenTypeName (type, implementingType.BodyRegion.IsEmpty ? implementingType.Location : implementingType.BodyRegion.Start);
			result.Append (ambience.GetString (shortType, OutputFlags.IncludeGenerics | OutputFlags.UseFullName));
		}
		
		/*
		void ResolveReturnTypes ()
		{
			returnType = member.ReturnType;
			foreach (IUsing u in unit.Usings) {
				foreach (KeyValuePair<string, IReturnType> alias in u.Aliases) {
					if (alias.Key == member.ReturnType.FullName) {
						returnType = alias.Value;
						return;
					}
				}
			}
		}*/
		
		class OutputVisitor : AbstractDomVisitor<CodeGenerationOptions, CodeGeneratorMemberResult>
		{
			CSharpCodeGenerator generator;
			
			public OutputVisitor (CSharpCodeGenerator generator)
			{
				this.generator = generator;
			}
			
			public override CodeGeneratorMemberResult Visit (IField field, CodeGenerationOptions options)
			{
				StringBuilder result = new StringBuilder ();
				generator.AppendIndent (result);
				result.Append (ambience.GetString (field.Modifiers));
				result.Append (" ");
				result.Append (ambience.GetString (field.ReturnType, OutputFlags.IncludeGenerics));
				result.Append (" ");
				result.Append (field.Name);
				result.Append (";");
				return new CodeGeneratorMemberResult (result.ToString (), -1, -1);
			}
			
			public override CodeGeneratorMemberResult Visit (IEvent evt, CodeGenerationOptions options)
			{
				StringBuilder result = new StringBuilder ();
				
				AppendModifiers (result, options, evt);
				
				result.Append ("event ");
				AppendReturnType (result, options.ImplementingType, evt.ReturnType);
				result.Append (" ");
				if (options.ExplicitDeclaration) {
					result.Append (ambience.GetString (new DomReturnType (evt.DeclaringType), OutputFlags.IncludeGenerics));
					result.Append (".");
				}
				result.Append (evt.Name);
				if (options.ExplicitDeclaration) {
					generator.AppendBraceStart (result, generator.policy.EventBraceStyle);
					generator.AppendIndent (result);
					result.Append ("add");
					generator.AppendBraceStart (result, generator.policy.EventAddBraceStyle);
					generator.AppendIndent (result);
					result.Append ("// TODO");
					generator.AppendLine (result);
					generator.AppendBraceEnd (result, generator.policy.EventAddBraceStyle);
					
					generator.AppendIndent (result);
					result.Append ("remove");
					generator.AppendBraceStart (result, generator.policy.EventRemoveBraceStyle);
					generator.AppendIndent (result);
					result.Append ("// TODO");
					generator.AppendLine (result);
					
					generator.AppendBraceEnd (result, generator.policy.EventRemoveBraceStyle);
					generator.AppendBraceEnd (result, generator.policy.EventBraceStyle);
				} else {
					result.Append (";");
				}
				return new CodeGeneratorMemberResult (result.ToString ());
			}
			
			public void AppendNotImplementedException (StringBuilder result, CodeGenerationOptions options,
			                                           out int bodyStartOffset, out int bodyEndOffset)
			{
				generator.AppendIndent (result);
				bodyStartOffset = result.Length;
				result.Append ("throw new ");
				AppendReturnType (result, options.ImplementingType, new DomReturnType ("System.NotImplementedException"));
				if (generator.policy.BeforeMethodCallParentheses)
					result.Append (" ");
				result.Append ("();");
				bodyEndOffset = result.Length;
				generator.AppendLine (result);
			}
			
			public void AppendMonoTouchTodo (StringBuilder result, out int bodyStartOffset, out int bodyEndOffset)
			{
				generator.AppendIndent (result);
				bodyStartOffset = result.Length;
				result.Append ("// TODO: Implement - see: http://go-mono.com/docs/index.aspx?link=T%3aMonoTouch.Foundation.ModelAttribute");
				bodyEndOffset = result.Length;
				generator.AppendLine (result);
			}
			
			public override CodeGeneratorMemberResult Visit (IMethod method, CodeGenerationOptions options)
			{
				int bodyStartOffset = -1, bodyEndOffset = -1;
				StringBuilder result = new StringBuilder ();
				AppendModifiers (result, options, method);
				AppendReturnType (result, options.ImplementingType, method.ReturnType);
				result.Append (" ");
				if (options.ExplicitDeclaration) {
					AppendReturnType (result, options.ImplementingType, new DomReturnType (method.DeclaringType));
					result.Append (".");
				}
				result.Append (method.Name);
				if (method.TypeParameters.Count > 0) {
					result.Append ("<");
					for (int i = 0; i < method.TypeParameters.Count; i++) {
						if (i > 0)
							result.Append (", ");
						var p = method.TypeParameters[i];
						result.Append (p.Name);
					}
					result.Append (">");
				}
				if (generator.policy.BeforeMethodDeclarationParentheses)
					result.Append (" ");
				result.Append ("(");
				AppendParameterList (result, options.ImplementingType, method.Parameters);
				result.Append (")");
				
				var typeParameters = method.TypeParameters;
				if (typeParameters.Any (p => p.Constraints.Any () || (p.TypeParameterModifier & TypeParameterModifier.HasDefaultConstructorConstraint) != 0)) {
					result.Append (" where ");
					int typeParameterCount = 0;
					foreach (var p in typeParameters) {
						if (!p.Constraints.Any () && (p.TypeParameterModifier & TypeParameterModifier.HasDefaultConstructorConstraint) == 0)
							continue;
						if (typeParameterCount != 0)
							result.Append (", ");
						
						typeParameterCount++;
						result.Append (p.Name);
						result.Append (" : ");
						int constraintCount = 0;
				
						if ((p.TypeParameterModifier & TypeParameterModifier.HasDefaultConstructorConstraint) != 0) {
							result.Append ("new ()");
							constraintCount++;
						}
						foreach (var c in p.Constraints) {
							if (constraintCount != 0)
								result.Append (", ");
							constraintCount++;
							if (c.DecoratedFullName == DomReturnType.ValueType.DecoratedFullName) {
								result.Append ("struct");
								continue;
							}
							if (c.DecoratedFullName == DomReturnType.Object.DecoratedFullName) {
								result.Append ("class");
								continue;
							}
							AppendReturnType (result, options.ImplementingType, c);
						}
					}
				}
				
				if (options.ImplementingType.ClassType == MonoDevelop.Projects.Dom.ClassType.Interface) {
					result.Append (";");
				} else {
					generator.AppendBraceStart (result, generator.policy.MethodBraceStyle);
					if (method.Name == "ToString" && (method.Parameters == null || method.Parameters.Count == 0) && method.ReturnType != null && method.ReturnType.FullName == "System.String") {
						generator.AppendIndent (result);
						bodyStartOffset = result.Length;
						result.Append ("return string.Format");
						if (generator.policy.BeforeMethodDeclarationParentheses)
							result.Append (" ");
						result.Append ("(\"[");
						result.Append (options.ImplementingType.Name);
						if (options.ImplementingType.PropertyCount > 0) 
							result.Append (": ");
						int i = 0;
						foreach (IProperty property in options.ImplementingType.Properties) {
							if (property.IsStatic || !property.IsPublic)
								continue;
							if (i > 0)
								result.Append (", ");
							result.Append (property.Name);
							result.Append ("={");
							result.Append (i++);
							result.Append ("}");
						}
						result.Append ("]\"");
						foreach (IProperty property in options.ImplementingType.Properties) {
							if (property.IsStatic || !property.IsPublic)
								continue;
							result.Append (", ");
							result.Append (property.Name);
						}
						result.Append (");");
						bodyEndOffset = result.Length;
						generator.AppendLine (result);
					} else if (IsMonoTouchModelMember (method)) {
						AppendMonoTouchTodo (result, out bodyStartOffset, out bodyEndOffset);
					} else if (method.IsAbstract || !(method.IsVirtual || method.IsOverride) || method.DeclaringType.ClassType == MonoDevelop.Projects.Dom.ClassType.Interface) {
						AppendNotImplementedException (result, options, out bodyStartOffset, out bodyEndOffset);
					} else {
						generator.AppendIndent (result);
						bodyStartOffset = result.Length;
						if (method.ReturnType.FullName != DomReturnType.Void.FullName)
							result.Append ("return ");
						result.Append ("base.");
						result.Append (method.Name);
						if (generator.policy.BeforeMethodCallParentheses)
							result.Append (" ");
						result.Append ("(");
						for (int i = 0; i < method.Parameters.Count; i++) {
							if (i > 0)
								result.Append (", ");
							
							var p = method.Parameters[i];
							if (p.IsOut)
								result.Append ("out ");
							if (p.IsRef)
								result.Append ("ref ");
							result.Append (p.Name);
						}
						result.Append (");");
						bodyEndOffset = result.Length;
						generator.AppendLine (result);
					}
					generator.AppendBraceEnd (result, generator.policy.MethodBraceStyle);
				}
				return new CodeGeneratorMemberResult (result.ToString (), bodyStartOffset, bodyEndOffset);
			}
			
			public static void AppendParameterList (StringBuilder result, IType implementingType, IList<IParameter> parameters)
			{
				for (int i = 0; i < parameters.Count; i++) {
					if (i > 0)
						result.Append (", ");
					
					var p = parameters[i];
					if (p.IsOut)
						result.Append ("out ");
					if (p.IsRef)
						result.Append ("ref ");
					if (p.IsParams)
						result.Append ("params ");
					AppendReturnType (result, implementingType, p.ReturnType);
					result.Append (" ");
					result.Append (p.Name);
				}
			}
			
			static string GetModifiers (IType implementingType, IMember member)
			{
				StringBuilder result = new StringBuilder ();
				if (member.IsPublic || (member.DeclaringType != null && member.DeclaringType.ClassType == MonoDevelop.Projects.Dom.ClassType.Interface)) {
					result.Append ("public ");
				} else if (member.IsProtectedAndInternal) {
					result.Append ("protected internal ");
				} else if (member.IsProtectedOrInternal && (member.DeclaringType != null && implementingType.SourceProjectDom == member.DeclaringType.SourceProjectDom)) {
					result.Append ("internal protected ");
				} else if (member.IsProtected) {
					result.Append ("protected ");
				} else if (member.IsInternal) {
					result.Append ("internal ");
				}
					
				if (member.IsStatic) 
					result.Append ("static ");
				
				return result.ToString ();
			}
			
			void AppendModifiers (StringBuilder result, CSharpCodeGenerator.CodeGenerationOptions options, IMember member)
			{
				generator.AppendIndent (result);
				if (options.ExplicitDeclaration || options.ImplementingType.ClassType == MonoDevelop.Projects.Dom.ClassType.Interface)
					return;
				result.Append (GetModifiers (options.ImplementingType, member));
				
				bool isFromInterface = false;
				if (member.DeclaringType != null && member.DeclaringType.ClassType == MonoDevelop.Projects.Dom.ClassType.Interface) {
					isFromInterface = true;
					if (options.ImplementingType != null) {
						foreach (IType type in options.ImplementingType.SourceProjectDom.GetInheritanceTree (options.ImplementingType)) {
							if (type.ClassType == MonoDevelop.Projects.Dom.ClassType.Interface)
								continue;
							if (type.SearchMember (member.Name, true).Any (m => m.Name == member.Name && member.MemberType == m.MemberType && DomMethod.ParameterListEquals (member.Parameters, m.Parameters))) {
								isFromInterface = false;
								break;
							}
						}
					}
				}
				if (!isFromInterface && ((member.Modifiers & MonoDevelop.Projects.Dom.Modifiers.Virtual) == MonoDevelop.Projects.Dom.Modifiers.Virtual || 
					(member.Modifiers & MonoDevelop.Projects.Dom.Modifiers.Abstract) == MonoDevelop.Projects.Dom.Modifiers.Abstract))
					result.Append ("override ");
			}
			
			public override CodeGeneratorMemberResult Visit (IProperty property, CodeGenerationOptions options)
			{
				var regions = new List<CodeGeneratorBodyRegion> ();
				var result = new StringBuilder ();
				AppendModifiers (result, options, property);
				AppendReturnType (result, options.ImplementingType, property.ReturnType);
				result.Append (" ");
				if (property.IsIndexer) {
					result.Append ("this[");
					AppendParameterList (result, options.ImplementingType, property.Parameters);
					result.Append ("]");
				} else {
					if (options.ExplicitDeclaration) {
						result.Append (ambience.GetString (new DomReturnType (property.DeclaringType), OutputFlags.IncludeGenerics));
						result.Append (".");
					}
					result.Append (property.Name);
				}
				generator.AppendBraceStart (result, generator.policy.PropertyBraceStyle);
				if (property.HasGet) {
					int bodyStartOffset, bodyEndOffset;
					generator.AppendIndent (result);
					result.Append ("get");
					if (options.ImplementingType.ClassType == MonoDevelop.Projects.Dom.ClassType.Interface) {
						result.AppendLine (";");
					} else {
						generator.AppendBraceStart (result, generator.policy.PropertyGetBraceStyle);
						if (IsMonoTouchModelMember (property)) {
							AppendMonoTouchTodo (result, out bodyStartOffset, out bodyEndOffset);
						} else if (property.IsAbstract || property.DeclaringType.ClassType == MonoDevelop.Projects.Dom.ClassType.Interface) {
							AppendNotImplementedException (result, options, out bodyStartOffset, out bodyEndOffset);
						} else {
							generator.AppendIndent (result);
							bodyStartOffset = result.Length;
							result.Append ("return base.");
							result.Append (property.Name);
							result.Append (";");
							bodyEndOffset = result.Length;
							generator.AppendLine (result);
						}
						generator.AppendBraceEnd (result, generator.policy.PropertyGetBraceStyle);
						generator.AppendLine (result);
						regions.Add (new CodeGeneratorBodyRegion (bodyStartOffset, bodyEndOffset));
					}
				}
				
				if (property.HasSet) {
					int bodyStartOffset, bodyEndOffset;
					generator.AppendIndent (result);
					result.Append ("set");
					if (options.ImplementingType.ClassType == MonoDevelop.Projects.Dom.ClassType.Interface) {
						result.AppendLine (";");
					} else {
						generator.AppendBraceStart (result, generator.policy.PropertyGetBraceStyle);
						if (IsMonoTouchModelMember (property)) {
							AppendMonoTouchTodo (result, out bodyStartOffset, out bodyEndOffset);
						} else if (property.IsAbstract || property.DeclaringType.ClassType == MonoDevelop.Projects.Dom.ClassType.Interface) {
							AppendNotImplementedException (result, options, out bodyStartOffset, out bodyEndOffset);
						} else {
							generator.AppendIndent (result);
							bodyStartOffset = result.Length;
							result.Append ("base.");
							result.Append (property.Name);
							result.Append (" = value;");
							bodyEndOffset = result.Length;
							generator.AppendLine (result);
						}
						generator.AppendBraceEnd (result, generator.policy.PropertyGetBraceStyle);
						generator.AppendLine (result);
						regions.Add (new CodeGeneratorBodyRegion (bodyStartOffset, bodyEndOffset));
					}
				}
				generator.AppendBraceEnd (result, generator.policy.PropertyBraceStyle);
				return new CodeGeneratorMemberResult (result.ToString (), regions);
			}
			
			public static bool IsMonoTouchModelMember (IMember member)
			{
				if (member == null || member.DeclaringType == null)
					return false;
				return member.DeclaringType.Attributes.Any (attr => attr.AttributeType != null && attr.AttributeType.FullName == "MonoTouch.Foundation.ModelAttribute");
			}
		}
		
		public override string CreateFieldEncapsulation (IType implementingType, IField field, string propertyName, MonoDevelop.Projects.Dom.Modifiers modifiers, bool readOnly)
		{
			SetIndentTo (implementingType);
			StringBuilder result = new StringBuilder ();
			AppendIndent (result);
			
			if (modifiers != MonoDevelop.Projects.Dom.Modifiers.None) {
				result.Append (ambience.GetString (modifiers));
				result.Append (" ");
			}
			
			AppendReturnType (result, implementingType, field.ReturnType);
			result.Append (" ");
			result.Append (propertyName);
			AppendBraceStart (result, policy.PropertyBraceStyle);
			AppendIndent (result);
			
			result.Append ("get");
			AppendBraceStart (result, policy.PropertyGetBraceStyle);
			AppendIndent (result);
			result.Append ("return this.");
			result.Append (field.Name);
			result.Append (";");
			AppendLine (result);
			AppendBraceEnd (result, policy.PropertyGetBraceStyle);
			AppendLine (result);

			if (!readOnly) {
				AppendIndent (result);
				result.Append ("set");
				AppendBraceStart (result, policy.PropertyGetBraceStyle);
				AppendIndent (result);
				result.Append (field.Name);
				result.Append (" = value;");
				AppendLine (result);
				AppendBraceEnd (result, policy.PropertyGetBraceStyle);
				AppendLine (result);
			}
			
			AppendBraceEnd (result, policy.PropertyBraceStyle);
			return result.ToString ();
		}
	}
}