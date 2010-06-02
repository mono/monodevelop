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
using MonoDevelop.CSharp.Dom;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Projects.Dom;
using System.Text;
using MonoDevelop.CSharp.Formatting;
using System.Collections.Generic;
using System.Linq;

namespace MonoDevelop.CSharp.Refactoring
{
	public class CSharpCodeGenerator : MonoDevelop.Projects.CodeGeneration.CodeGenerator
	{
		static CSharpAmbience ambience = new CSharpAmbience ();
		
		OutputVisitor visitor;
		CSharpFormattingPolicy policy;
		
		public CSharpCodeGenerator ()
		{
			visitor = new OutputVisitor (this);
			
			IEnumerable<string> types = MonoDevelop.Ide.DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);
			policy = MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<CSharpFormattingPolicy> (types);
		}
		
		
		class CodeGenerationOptions
		{
			public bool ExplicitDeclaration { get; set; }
		}
		
		public override string WrapInRegions (string regionName, string text)
		{
			StringBuilder result = new StringBuilder ();
			AppendIndent (result);
			result.Append ("#region ");
			result.AppendLine (regionName);
			result.AppendLine (text);
			AppendIndent (result);
			result.Append ("#endregion");
			return result.ToString ();
		}

		public override string CreateMemberImplementation (IMember member, bool explicitDeclaration)
		{
			CodeGenerationOptions options = new CodeGenerationOptions () {
				ExplicitDeclaration = explicitDeclaration
			};
			
			return member.AcceptVisitor (visitor, options);
		}
		
		void AppendBraceStart (StringBuilder result, BraceStyle braceStyle)
		{
			switch (braceStyle) {
			case BraceStyle.EndOfLine:
				result.AppendLine (" {");
				break;
			case BraceStyle.EndOfLineWithoutSpace:
				result.AppendLine ("{");
				break;
			case BraceStyle.NextLine:
				result.AppendLine ();
				AppendIndent (result);
				result.AppendLine ("{");
				break;
			case BraceStyle.NextLineShifted:
				result.AppendLine ();
				result.Append (GetIndent (IndentLevel + 1));
				result.AppendLine ("{");
				break;
			case BraceStyle.NextLineShifted2:
				result.AppendLine ();
				result.Append (GetIndent (IndentLevel + 1));
				result.AppendLine ("{");
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
		
			
		
		class OutputVisitor : AbstractDomVistitor<CodeGenerationOptions, string>
		{
			static string indent = "\t";
			CSharpCodeGenerator generator;
			
			public OutputVisitor (CSharpCodeGenerator generator)
			{
				this.generator = generator;
			}
			
			public override string Visit (IField field, CodeGenerationOptions options)
			{
				StringBuilder result = new StringBuilder ();
				generator.AppendIndent (result);
				result.Append (ambience.GetString (field.Modifiers));
				result.Append (" ");
				result.Append (ambience.GetString (field.ReturnType, OutputFlags.IncludeGenerics));
				result.Append (" ");
				result.Append (field.Name);
				result.Append (";");
				return result.ToString ();
			}
			
			public override string Visit (IEvent evt, CodeGenerationOptions options)
			{
				StringBuilder result = new StringBuilder ();
				
				AppendModifiers (result, options, evt);
				
				result.Append ("event ");
				result.Append (ambience.GetString (evt.ReturnType, OutputFlags.IncludeGenerics));
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
					result.AppendLine ("// TODO");
					generator.AppendBraceEnd (result, generator.policy.EventAddBraceStyle);
					
					generator.AppendIndent (result);
					result.Append ("remove");
					generator.AppendBraceStart (result, generator.policy.EventRemoveBraceStyle);
					generator.AppendIndent (result);
					result.AppendLine ("// TODO");
					generator.AppendBraceEnd (result, generator.policy.EventRemoveBraceStyle);
					generator.AppendBraceEnd (result, generator.policy.EventBraceStyle);
				} else {
					result.Append (";");
				}
				return result.ToString ();
			}
			
		
			
			public override string Visit (IMethod method, CodeGenerationOptions options)
			{
				StringBuilder result = new StringBuilder ();
				AppendModifiers (result, options, method);
				result.Append (ambience.GetString (method.ReturnType, OutputFlags.IncludeGenerics));
				result.Append (" ");
				if (options.ExplicitDeclaration) {
					result.Append (ambience.GetString (new DomReturnType (method.DeclaringType), OutputFlags.IncludeGenerics));
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
				AppendParameterList (result, method.Parameters);
				result.Append (")");
				
				generator.AppendBraceStart (result, generator.policy.MethodBraceStyle);
				if (IsMonoTouchModelMember (method)) {
					generator.AppendIndent (result);
					result.AppendLine ("// TODO: Implement - see: http://go-mono.com/docs/index.aspx?link=T%3aMonoTouch.Foundation.ModelAttribute");
				} else if (method.IsAbstract || method.DeclaringType.ClassType == ClassType.Interface) {
					generator.AppendIndent (result);
					result.AppendLine ("throw new System.NotImplementedException ();");
				} else {
					generator.AppendIndent (result);
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
					result.AppendLine (");");
				}
				generator.AppendBraceEnd (result, generator.policy.MethodBraceStyle);
				return result.ToString ();
			}
			
			public static void AppendParameterList (StringBuilder result, IList<IParameter> parameters)
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
					result.Append (ambience.GetString (p.ReturnType, OutputFlags.IncludeGenerics));
					result.Append (" ");
					result.Append (p.Name);
				}
			}
			
			void AppendModifiers (StringBuilder result, CSharpCodeGenerator.CodeGenerationOptions options, IMember member)
			{
				generator.AppendIndent (result);
				if (member.DeclaringType.ClassType == ClassType.Interface) {
					if (!options.ExplicitDeclaration)
						result.Append ("public ");
				} else {
					Modifiers modifiers = member.Modifiers;
					if ((modifiers & Modifiers.Virtual) == Modifiers.Virtual) {
						modifiers = modifiers & ~Modifiers.Virtual | Modifiers.Override;
					}
					result.Append (ambience.GetString (modifiers));
					result.Append (" ");
				}
			}
			
			public override string Visit (IProperty property, CodeGenerationOptions options)
			{
				StringBuilder result = new StringBuilder ();
				AppendModifiers (result, options, property);
				result.Append (ambience.GetString (property.ReturnType, OutputFlags.IncludeGenerics));
				result.Append (" ");
				if (property.IsIndexer) {
					result.Append ("this[");
					AppendParameterList (result, property.Parameters);
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
					generator.AppendIndent (result);
					result.Append ("get");
					generator.AppendBraceStart (result, generator.policy.PropertyGetBraceStyle);
					if (IsMonoTouchModelMember (property)) {
						generator.AppendIndent (result);
						result.AppendLine ("// TODO: Implement - see: http://go-mono.com/docs/index.aspx?link=T%3aMonoTouch.Foundation.ModelAttribute");
					} else if (property.IsAbstract || property.DeclaringType.ClassType == ClassType.Interface) {
						generator.AppendIndent (result);
						result.AppendLine ("throw new System.NotImplementedException ();");
					} else {
						generator.AppendIndent (result);
						result.Append ("return base.");
						result.Append (property.Name);
						result.AppendLine ();
					}
					generator.AppendBraceEnd (result, generator.policy.PropertyGetBraceStyle);
					result.AppendLine ();
				}
				
				if (property.HasSet) {
					generator.AppendIndent (result);
					result.Append ("set");
					generator.AppendBraceStart (result, generator.policy.PropertyGetBraceStyle);
					if (IsMonoTouchModelMember (property)) {
						generator.AppendIndent (result);
						result.AppendLine ("// TODO: Implement - see: http://go-mono.com/docs/index.aspx?link=T%3aMonoTouch.Foundation.ModelAttribute");
					} else if (property.IsAbstract || property.DeclaringType.ClassType == ClassType.Interface) {
						generator.AppendIndent (result);
						result.AppendLine ("throw new System.NotImplementedException ();");
					} else {
						generator.AppendIndent (result);
						result.Append ("base.");
						result.Append (property.Name);
						result.Append (" = value;");
						result.AppendLine ();
					}
					generator.AppendBraceEnd (result, generator.policy.PropertyGetBraceStyle);
					result.AppendLine ();
				}
				generator.AppendBraceEnd (result, generator.policy.PropertyBraceStyle);
				return result.ToString ();
			}
			
			public static bool IsMonoTouchModelMember (IMember member)
			{
				if (member == null || member.DeclaringType == null)
					return false;
				return member.DeclaringType.Attributes.Any (attr => attr.AttributeType != null && attr.AttributeType.FullName == "MonoTouch.Foundation.ModelAttribute");
			}
		}
	
		internal static string GetIndent (int indentLevel)
		{
			return new string ('\t', indentLevel);
		}
		
		public override string CreateFieldEncapsulation (IField field, string propertyName, Modifiers modifiers, bool readOnly)
		{
			StringBuilder result = new StringBuilder ();
			AppendIndent (result);
			
			if (modifiers != Modifiers.None) {
				result.Append (ambience.GetString (modifiers));
				result.Append (" ");
			}
			result.Append (ambience.GetString (field.ReturnType, OutputFlags.IncludeGenerics));
			result.Append (" ");
			result.Append (propertyName);
			AppendBraceStart (result, policy.PropertyBraceStyle);
			AppendIndent (result);
			
			result.Append ("get");
			AppendBraceStart (result, policy.PropertyGetBraceStyle);
			AppendIndent (result);
			result.Append ("return this.");
			result.Append (field.Name);
			result.AppendLine (";");
			AppendBraceEnd (result, policy.PropertyGetBraceStyle);
			result.AppendLine ();

			if (!readOnly) {
				AppendIndent (result);
				result.Append ("set");
				AppendBraceStart (result, policy.PropertyGetBraceStyle);
				AppendIndent (result);
				result.Append (field.Name);
				result.AppendLine (" = value;");
				AppendBraceEnd (result, policy.PropertyGetBraceStyle);
				result.AppendLine ();
			}
			
			AppendBraceEnd (result, policy.PropertyBraceStyle);
			return result.ToString ();
		}

		
	}
}