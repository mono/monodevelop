// 
// CSharpCodecs
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
using System.Text;
using MonoDevelop.CSharp.Formatting;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Ide;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.TypeSystem;

namespace MonoDevelop.CSharp.Refactoring
{
	public class CSharpCodeGenerator : CodeGenerator
	{
		static CSharpAmbience ambience = new CSharpAmbience ();
		
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
			IEnumerable<string> types = MonoDevelop.Ide.DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);
			policy = MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<CSharpFormattingPolicy> (types);
		}
		
		class CodeGenerationOptions
		{
			public bool ExplicitDeclaration { get; set; }
			public ITypeDefinition ImplementingType { get; set; }
			public ITypeResolveContext Ctx { get; set; }
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
		
		public override CodeGeneratorMemberResult CreateMemberImplementation (ITypeResolveContext ctx, ITypeDefinition implementingType, IMember member,
		                                                                      bool explicitDeclaration)
		{
			SetIndentTo (implementingType);
			var options = new CodeGenerationOptions () {
				ExplicitDeclaration = explicitDeclaration,
				ImplementingType = implementingType,
				Ctx = ctx
			};
			if (member is IMethod)
				return GenerateCode ((IMethod)member, options);
			if (member is IProperty)
				return GenerateCode ((IProperty)member, options);
			if (member is IField)
				return GenerateCode ((IField)member, options);
			if (member is IEvent)
				return GenerateCode ((IEvent)member, options);
			throw new NotSupportedException ("member " +  member + " is not supported.");
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
		
		static void AppendReturnType (StringBuilder result, IType implementingType, ITypeReference type)
		{
		//	var shortType = implementingType.CompilationUnit.ShortenTypeName (type, implementingType.BodyRegion.IsEmpty ? implementingType.Location : implementingType.BodyRegion.Start);
			var shortType = type.ToString ();
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

		
		CodeGeneratorMemberResult GenerateCode (IField field, CodeGenerationOptions options)
		{
			StringBuilder result = new StringBuilder ();
			AppendIndent (result);
			AppendModifiers (result, options, field);
			result.Append (" ");
			AppendReturnType (result, options.ImplementingType, field.ReturnType);
			result.Append (" ");
			result.Append (field.Name);
			result.Append (";");
			return new CodeGeneratorMemberResult (result.ToString (), -1, -1);
		}
		
		CodeGeneratorMemberResult GenerateCode (IEvent evt, CodeGenerationOptions options)
		{
			StringBuilder result = new StringBuilder ();
			
			AppendModifiers (result, options, evt);
			
			result.Append ("event ");
			AppendReturnType (result, options.ImplementingType, evt.ReturnType);
			result.Append (" ");
			if (options.ExplicitDeclaration) {
				result.Append (ambience.GetString (options.Ctx, (ITypeReference)evt.DeclaringTypeDefinition, OutputFlags.IncludeGenerics));
				result.Append (".");
			}
			result.Append (evt.Name);
			if (options.ExplicitDeclaration) {
				AppendBraceStart (result, policy.EventBraceStyle);
				AppendIndent (result);
				result.Append ("add");
				AppendBraceStart (result, policy.EventAddBraceStyle);
				AppendIndent (result);
				result.Append ("// TODO");
				AppendLine (result);
				AppendBraceEnd (result, policy.EventAddBraceStyle);
				
				AppendIndent (result);
				result.Append ("remove");
				AppendBraceStart (result, policy.EventRemoveBraceStyle);
				AppendIndent (result);
				result.Append ("// TODO");
				AppendLine (result);
				
				AppendBraceEnd (result, policy.EventRemoveBraceStyle);
				AppendBraceEnd (result, policy.EventBraceStyle);
			} else {
				result.Append (";");
			}
			return new CodeGeneratorMemberResult (result.ToString ());
		}
		
		void AppendNotImplementedException (StringBuilder result, CodeGenerationOptions options,
		                                           out int bodyStartOffset, out int bodyEndOffset)
		{
			AppendIndent (result);
			bodyStartOffset = result.Length;
			result.Append ("throw new ");
			AppendReturnType (result, options.ImplementingType, options.Ctx.GetTypeDefinition (typeof (System.NotImplementedException)));
			if (policy.BeforeMethodCallParentheses)
				result.Append (" ");
			result.Append ("();");
			bodyEndOffset = result.Length;
			AppendLine (result);
		}
		
		void AppendMonoTouchTodo (StringBuilder result, out int bodyStartOffset, out int bodyEndOffset)
		{
			AppendIndent (result);
			bodyStartOffset = result.Length;
			result.Append ("// TODO: Implement - see: http://go-mono.com/docs/index.aspx?link=T%3aMonoTouch.Foundation.ModelAttribute");
			bodyEndOffset = result.Length;
			AppendLine (result);
		}
		
		CodeGeneratorMemberResult GenerateCode (IMethod method, CodeGenerationOptions options)
		{
			int bodyStartOffset = -1, bodyEndOffset = -1;
			StringBuilder result = new StringBuilder ();
			AppendModifiers (result, options, method);
			AppendReturnType (result, options.ImplementingType, method.ReturnType);
			result.Append (" ");
			if (options.ExplicitDeclaration) {
				AppendReturnType (result, options.ImplementingType, method.DeclaringTypeDefinition);
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
			if (policy.BeforeMethodDeclarationParentheses)
				result.Append (" ");
			result.Append ("(");
			AppendParameterList (result, options.ImplementingType, method.Parameters);
			result.Append (")");
			
			var typeParameters = method.TypeParameters;
			if (typeParameters.Any (p => p.Constraints.Any () /*|| (p.TypeParameterModifier & TypeParameterModifier.HasDefaultConstructorConstraint) != 0*/)) {
				result.Append (" where ");
				int typeParameterCount = 0;
				foreach (var p in typeParameters) {
					if (!p.Constraints.Any () /*&& (p.TypeParameterModifier & TypeParameterModifier.HasDefaultConstructorConstraint) == 0*/)
						continue;
					if (typeParameterCount != 0)
						result.Append (", ");
					
					typeParameterCount++;
					result.Append (p.Name);
					result.Append (" : ");
					int constraintCount = 0;
			
//					if ((p.TypeParameterModifier & TypeParameterModifier.HasDefaultConstructorConstraint) != 0) {
//						result.Append ("new ()");
//						constraintCount++;
//					}
					foreach (var c in p.Constraints) {
						if (constraintCount != 0)
							result.Append (", ");
						constraintCount++;
						var ct = c.Resolve (options.Ctx);
						if (ct.Equals (options.Ctx.GetTypeDefinition (typeof (System.ValueType)))) {
							result.Append ("struct");
							continue;
						}
						if (ct.Equals (options.Ctx.GetTypeDefinition (typeof (System.Object)))) {
							result.Append ("class");
							continue;
						}
						AppendReturnType (result, options.ImplementingType, c);
					}
				}
			}
			
			if (options.ImplementingType.ClassType == ClassType.Interface) {
				result.Append (";");
			} else {
				AppendBraceStart (result, policy.MethodBraceStyle);
				if (method.Name == "ToString" && (method.Parameters == null || method.Parameters.Count == 0) && method.ReturnType != null/* && method.ReturnType.FullName == "System.String"*/) {
					AppendIndent (result);
					bodyStartOffset = result.Length;
					result.Append ("return string.Format");
					if (policy.BeforeMethodDeclarationParentheses)
						result.Append (" ");
					result.Append ("(\"[");
					result.Append (options.ImplementingType.Name);
					if (options.ImplementingType.Properties.Count > 0) 
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
					AppendLine (result);
				} else if (IsMonoTouchModelMember (options.Ctx, method)) {
					AppendMonoTouchTodo (result, out bodyStartOffset, out bodyEndOffset);
				} else if (method.IsAbstract || !(method.IsVirtual || method.IsOverride) || method.DeclaringTypeDefinition.ClassType == ClassType.Interface) {
					AppendNotImplementedException (result, options, out bodyStartOffset, out bodyEndOffset);
				} else {
					AppendIndent (result);
					bodyStartOffset = result.Length;
					if (method.ReturnType != null) // != void ? 
						result.Append ("return ");
					result.Append ("base.");
					result.Append (method.Name);
					if (policy.BeforeMethodCallParentheses)
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
					AppendLine (result);
				}
				AppendBraceEnd (result, policy.MethodBraceStyle);
			}
			return new CodeGeneratorMemberResult (result.ToString (), bodyStartOffset, bodyEndOffset);
		}
		
		void AppendParameterList (StringBuilder result, IType implementingType, IList<IParameter> parameters)
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
				AppendReturnType (result, implementingType, p.Type);
				result.Append (" ");
				result.Append (p.Name);
			}
		}
		
		static string GetModifiers (IType implementingType, IMember member)
		{
			StringBuilder result = new StringBuilder ();
			if (member.IsPublic || (member.DeclaringType != null && member.DeclaringTypeDefinition.ClassType == ClassType.Interface)) {
				result.Append ("public ");
			} else if (member.IsProtectedAndInternal) {
				result.Append ("protected internal ");
			} else if (member.IsProtectedOrInternal && (member.DeclaringType != null && implementingType.GetProjectContent () == member.DeclaringType.GetProjectContent ())) {
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
		
		void AppendModifiers (StringBuilder result, CodeGenerationOptions options, IMember member)
		{
			AppendIndent (result);
			if (options.ExplicitDeclaration || options.ImplementingType.ClassType == ClassType.Interface)
				return;
			result.Append (GetModifiers (options.ImplementingType, member));
			
			bool isFromInterface = false;
			if (member.DeclaringType != null && member.DeclaringTypeDefinition.ClassType == ClassType.Interface) {
				isFromInterface = true;
				if (options.ImplementingType != null) {
					foreach (var type in options.ImplementingType.GetAllBaseTypeDefinitions (options.Ctx)) {
						if (type.ClassType == ClassType.Interface)
							continue;
						if (type.Members.Any (m => m.Name == member.Name && member.EntityType == m.EntityType /* && DomMethod.ParameterListEquals (member.Parameters, m.Parameters)*/ )) {
							isFromInterface = false;
							break;
						}
					}
				}
			}
			if (!isFromInterface && member.IsVirtual || member.IsAbstract)
				result.Append ("override ");
		}
		
		CodeGeneratorMemberResult GenerateCode (IProperty property, CodeGenerationOptions options)
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
					result.Append (ambience.GetString (options.Ctx, (ITypeReference)property.DeclaringType, OutputFlags.IncludeGenerics));
					result.Append (".");
				}
				result.Append (property.Name);
			}
			AppendBraceStart (result, policy.PropertyBraceStyle);
			if (property.CanGet) {
				int bodyStartOffset, bodyEndOffset;
				AppendIndent (result);
				result.Append ("get");
				if (options.ImplementingType.ClassType == ClassType.Interface) {
					result.AppendLine (";");
				} else {
					AppendBraceStart (result, policy.PropertyGetBraceStyle);
					if (IsMonoTouchModelMember (options.Ctx, property)) {
						AppendMonoTouchTodo (result, out bodyStartOffset, out bodyEndOffset);
					} else if (property.IsAbstract || property.DeclaringTypeDefinition.ClassType == ClassType.Interface) {
						AppendNotImplementedException (result, options, out bodyStartOffset, out bodyEndOffset);
					} else {
						AppendIndent (result);
						bodyStartOffset = result.Length;
						result.Append ("return base.");
						result.Append (property.Name);
						result.Append (";");
						bodyEndOffset = result.Length;
						AppendLine (result);
					}
					AppendBraceEnd (result, policy.PropertyGetBraceStyle);
					AppendLine (result);
					regions.Add (new CodeGeneratorBodyRegion (bodyStartOffset, bodyEndOffset));
				}
			}
			
			if (property.CanSet) {
				int bodyStartOffset, bodyEndOffset;
				AppendIndent (result);
				result.Append ("set");
				if (options.ImplementingType.ClassType == ClassType.Interface) {
					result.AppendLine (";");
				} else {
					AppendBraceStart (result, policy.PropertyGetBraceStyle);
					if (IsMonoTouchModelMember (options.Ctx, property)) {
						AppendMonoTouchTodo (result, out bodyStartOffset, out bodyEndOffset);
					} else if (property.IsAbstract || property.DeclaringTypeDefinition.ClassType == ClassType.Interface) {
						AppendNotImplementedException (result, options, out bodyStartOffset, out bodyEndOffset);
					} else {
						AppendIndent (result);
						bodyStartOffset = result.Length;
						result.Append ("base.");
						result.Append (property.Name);
						result.Append (" = value;");
						bodyEndOffset = result.Length;
						AppendLine (result);
					}
					AppendBraceEnd (result, policy.PropertyGetBraceStyle);
					AppendLine (result);
					regions.Add (new CodeGeneratorBodyRegion (bodyStartOffset, bodyEndOffset));
				}
			}
			AppendBraceEnd (result, policy.PropertyBraceStyle);
			return new CodeGeneratorMemberResult (result.ToString (), regions);
		}
		
		static bool IsMonoTouchModelMember (ITypeResolveContext ctx, IMember member)
		{
			if (member == null || member.DeclaringType == null)
				return false;
			return member.DeclaringTypeDefinition.Attributes.Any (attr => attr.AttributeType != null && attr.AttributeType.Resolve (ctx).Equals (ctx.GetTypeDefinition ("MonoTouch.Foundation", "ModelAttribute", 0, StringComparer.Ordinal)));
		}
		
		public override string CreateFieldEncapsulation (ITypeDefinition implementingType, IField field, string propertyName, Accessibility modifiers, bool readOnly)
		{
			SetIndentTo (implementingType);
			StringBuilder result = new StringBuilder ();
			AppendIndent (result);
			
//			if (modifiers != MonoDevelop.Projects.Dom.Modifiers.None) {
//				switch (modifiers) {
//				}
//				result.Append (ambience.GetString (modifiers));
//				result.Append (" ");
//			}
			
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
		
		public override void AddGlobalNamespaceImport (MonoDevelop.Ide.Gui.Document doc, string nsName)
		{
			var parsedDocument = doc.ParsedDocument;
			var unit = parsedDocument.Annotation<CompilationUnit> ();
			if (unit == null)
				return;
			
			var node = unit.FirstChild;
			while (node.NextSibling is ICSharpCode.NRefactory.CSharp.Comment || node.NextSibling is UsingDeclaration || node.NextSibling is UsingAliasDeclaration) {
				node = node.NextSibling;
			}
			
			var text = new StringBuilder ();
			if (node != null && node != unit.FirstChild)
				text.Append (doc.Editor.EolMarker);
			text.Append ("using ");
			text.Append (nsName);
			text.Append (";");
			text.Append (doc.Editor.EolMarker);
			
			int offset = 0;
			if (node != null) {
				var loc = node == unit.FirstChild ? node.StartLocation : node.EndLocation;
				offset = doc.Editor.LocationToOffset (loc.Line, loc.Column);
			}
			doc.Editor.Document.BeginAtomicUndo ();
			int caretOffset = doc.Editor.Caret.Offset;
			int inserted = doc.Editor.Insert (offset, text.ToString ());
			if (offset < caretOffset)
				doc.Editor.Caret.Offset = caretOffset + inserted;
			doc.Editor.Document.EndAtomicUndo ();
			doc.Editor.Document.CommitUpdateAll ();
		}
		
		public override void AddLocalNamespaceImport (MonoDevelop.Ide.Gui.Document doc, string nsName, AstLocation caretLocation)
		{
			var parsedDocument = doc.ParsedDocument;
			var unit = parsedDocument.Annotation<CompilationUnit> ();
			if (unit == null)
				return;
			
			var nsDecl = unit.GetNodeAt<NamespaceDeclaration> (caretLocation);
			if (nsDecl == null) {
				AddGlobalNamespaceImport (doc, nsName);
				return;
			}
				
			
			var node = unit.FirstChild;
			while (node.NextSibling is ICSharpCode.NRefactory.CSharp.Comment || node.NextSibling is UsingDeclaration || node.NextSibling is UsingAliasDeclaration) {
				node = node.NextSibling;
			}
			
			var text = new StringBuilder ();
			
			text.Append (doc.Editor.EolMarker);
			string indent = doc.Editor.GetLineIndent (nsDecl.StartLocation.Line) + "\t";
			text.Append (indent);
			text.Append ("using ");
			text.Append (nsName);
			text.Append (";");
			text.Append (doc.Editor.EolMarker);
			
			int offset;
			AstLocation loc;
			if (node != null) {
				loc = node == unit.FirstChild ? node.StartLocation : node.EndLocation;
			} else {
				loc = nsDecl.LBraceToken.EndLocation;
			}
			offset = doc.Editor.LocationToOffset (loc.Line, loc.Column);
			
			doc.Editor.Document.BeginAtomicUndo ();
			int caretOffset = doc.Editor.Caret.Offset;
			int inserted = doc.Editor.Insert (offset, text.ToString ());
			if (offset < caretOffset)
				doc.Editor.Caret.Offset = caretOffset + inserted;
			doc.Editor.Document.EndAtomicUndo ();
		}
	}
}