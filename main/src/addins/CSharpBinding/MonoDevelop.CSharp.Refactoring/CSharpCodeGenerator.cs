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
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory;
using Mono.TextEditor;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using MonoDevelop.Projects.Policies;


namespace MonoDevelop.CSharp.Refactoring
{
	public class CSharpCodeGenerator : CodeGenerator
	{
		static CSharpAmbience ambience = new CSharpAmbience ();
		
		CSharpFormattingPolicy policy;
		
		public MonoDevelop.CSharp.Formatting.CSharpFormattingPolicy Policy {
			get {
				if (policy == null) {
					var types = MonoDevelop.Ide.DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);
					if (PolicyParent != null)
						policy = PolicyParent.Get<CSharpFormattingPolicy> (types);
					if (policy == null) {
						
						policy = MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<CSharpFormattingPolicy> (types);
					}
				}
				return this.policy;
			}
		}
		
		public override PolicyContainer PolicyParent {
			get {
				return base.PolicyParent;
			}
			set {
				base.PolicyParent = value;
				var types = MonoDevelop.Ide.DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);
				policy = value.Get<CSharpFormattingPolicy> (types);
			}
		}
		
		
		class CodeGenerationOptions
		{
			public bool ExplicitDeclaration { get; set; }
			public ITypeDefinition ImplementingType { get; set; }
			public IUnresolvedTypeDefinition Part { get; set; }
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
		
		public override CodeGeneratorMemberResult CreateMemberImplementation (ITypeDefinition implementingType,
		                                                                      IUnresolvedTypeDefinition part,
		                                                                      IUnresolvedMember member,
		                                                                      bool explicitDeclaration)
		{
			SetIndentTo (part);
			var options = new CodeGenerationOptions () {
				ExplicitDeclaration = explicitDeclaration,
				ImplementingType = implementingType,
				Part = part
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
		
		public override CodeGeneratorMemberResult CreateMemberImplementation (ITypeDefinition implementingType,
		                                                                      IUnresolvedTypeDefinition part,
		                                                                      IMember member,
		                                                                      bool explicitDeclaration)
		{
			SetIndentTo (part);
			var options = new CodeGenerationOptions () {
				ExplicitDeclaration = explicitDeclaration,
				ImplementingType = implementingType,
				Part = part
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
		
		void AppendReturnType (StringBuilder result, CodeGenerationOptions options, IType type)
		{
			if (type == null)
				throw new ArgumentNullException ("type");
			var implementingType = options.Part;
			var loc = implementingType.Region.End;
			
			var pf = implementingType.ParsedFile;
			var file = pf as CSharpParsedFile;
			var resolved = type;
			if (resolved.Kind == TypeKind.Unknown) {
				result.Append (type.ToString ());
				return;
			}
			var def = type.GetDefinition ();
			if (def != null) {
				using (var stringWriter = new System.IO.StringWriter ()) {
					var formatter = new TextWriterOutputFormatter (stringWriter);
					stringWriter.NewLine = EolMarker; 
					var visitor = new CSharpOutputVisitor (formatter, new CSharpFormattingOptions ());
					var shortType = CreateShortType (def.Compilation, file, loc, resolved);
					shortType.AcceptVisitor (visitor, null);
					
					var typeString = stringWriter.ToString ();
					if (typeString.StartsWith ("global::"))
						typeString = typeString.Substring ("global::".Length);
					result.Append (typeString);
				}
			} else {
				result.Append (new ICSharpCode.NRefactory.CSharp.CSharpAmbience ().ConvertType (type));
			}
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
			AppendReturnType (result, options, field.ReturnType);
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
			AppendReturnType (result, options, evt.ReturnType);
			result.Append (" ");
			if (options.ExplicitDeclaration) {
				result.Append (ambience.GetString (evt.DeclaringTypeDefinition, OutputFlags.IncludeGenerics));
				result.Append (".");
			}
			result.Append (evt.Name);
			if (options.ExplicitDeclaration) {
				AppendBraceStart (result, Policy.EventBraceStyle);
				AppendIndent (result);
				result.Append ("add");
				AppendBraceStart (result, Policy.EventAddBraceStyle);
				AppendIndent (result);
				result.Append ("// TODO");
				AppendLine (result);
				AppendBraceEnd (result, Policy.EventAddBraceStyle);
				
				AppendIndent (result);
				result.Append ("remove");
				AppendBraceStart (result, Policy.EventRemoveBraceStyle);
				AppendIndent (result);
				result.Append ("// TODO");
				AppendLine (result);
				
				AppendBraceEnd (result, Policy.EventRemoveBraceStyle);
				AppendBraceEnd (result, Policy.EventBraceStyle);
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
			// TODO: Type system conversion.
			result.Append ("System.NotImplementedException");
//			AppendReturnType (result, options.ImplementingType, options.Ctx.GetTypeDefinition (typeof (System.NotImplementedException)));
			if (Policy.BeforeMethodCallParentheses)
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
			AppendReturnType (result, options, method.ReturnType);
			result.Append (" ");
			if (options.ExplicitDeclaration) {
				AppendReturnType (result, options, method.DeclaringType);
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
			if (Policy.BeforeMethodDeclarationParentheses)
				result.Append (" ");
			result.Append ("(");
			AppendParameterList (result, options, method.Parameters);
			result.Append (")");
			
			var typeParameters = method.TypeParameters;
			
			// This should also check the types are in the correct mscorlib
			Func<IType, bool> validBaseType = t => t.FullName != "System.Object" && t.FullName != "System.ValueType";
			if (!options.ExplicitDeclaration && typeParameters.Any (p => p.HasDefaultConstructorConstraint || p.HasReferenceTypeConstraint || p.HasValueTypeConstraint || p.DirectBaseTypes.Any (validBaseType))) {
				result.Append (" where ");
				int typeParameterCount = 0;
				foreach (var p in typeParameters) {
					if (typeParameterCount != 0)
						result.Append (", ");
					
					typeParameterCount++;
					result.Append (p.Name);
					result.Append (" : ");
					int constraintCount = 0;
					
					if (p.HasDefaultConstructorConstraint) {
						result.Append ("new ()");
						constraintCount++;
					}
					
					if (p.HasValueTypeConstraint) {
						if (constraintCount != 0)
							result.Append (", ");
						result.Append ("struct");
						constraintCount++;
					}
					
					if (p.HasReferenceTypeConstraint) {
						if (constraintCount != 0)
							result.Append (", ");
						result.Append ("class");
						constraintCount++;
					}
//					bool hadInterfaces = false;
					foreach (var c in p.DirectBaseTypes.Where (validBaseType)) {
						if (constraintCount != 0)
							result.Append (", ");
						constraintCount++;
						AppendReturnType (result, options, c);
//						if (c.Kind == TypeKind.Interface)
//							hadInterfaces = true;
					}
				}
			}
			
			if (options.ImplementingType.Kind == TypeKind.Interface) {
				result.Append (";");
			} else {
				AppendBraceStart (result, Policy.MethodBraceStyle);
				if (method.Name == "ToString" && (method.Parameters == null || method.Parameters.Count == 0) && method.ReturnType != null/* && method.ReturnType.FullName == "System.String"*/) {
					AppendIndent (result);
					bodyStartOffset = result.Length;
					result.Append ("return string.Format");
					if (Policy.BeforeMethodDeclarationParentheses)
						result.Append (" ");
					result.Append ("(\"[");
					result.Append (options.ImplementingType.Name);
					if (options.ImplementingType.Properties.Any ()) 
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
				} else if (IsMonoTouchModelMember (method)) {
					AppendMonoTouchTodo (result, out bodyStartOffset, out bodyEndOffset);
				} else if (method.IsAbstract || !(method.IsVirtual || method.IsOverride) || method.DeclaringTypeDefinition.Kind == TypeKind.Interface) {
					AppendNotImplementedException (result, options, out bodyStartOffset, out bodyEndOffset);
				} else {
					AppendIndent (result);
					bodyStartOffset = result.Length;
					if (method.ReturnType.ReflectionName != typeof(void).FullName)
						result.Append ("return ");
					result.Append ("base.");
					result.Append (method.Name);
					if (Policy.BeforeMethodCallParentheses)
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
				AppendBraceEnd (result, Policy.MethodBraceStyle);
			}
			return new CodeGeneratorMemberResult (result.ToString (), bodyStartOffset, bodyEndOffset);
		}
		
		void AppendParameterList (StringBuilder result, CodeGenerationOptions options, IList<IParameter> parameters)
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
				AppendReturnType (result, options, p.Type);
				result.Append (" ");
				result.Append (p.Name);
			}
		}
		
		static string GetModifiers (IUnresolvedTypeDefinition implementingType, IMember member)
		{
			StringBuilder result = new StringBuilder ();
			if (member.IsPublic || (member.DeclaringType != null && member.DeclaringTypeDefinition.Kind == TypeKind.Interface)) {
				result.Append ("public ");
			} else if (member.IsProtectedAndInternal) {
				result.Append ("protected internal ");
			} else if (member.IsProtectedOrInternal) {
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
			if (options.ExplicitDeclaration || options.ImplementingType.Kind == TypeKind.Interface)
				return;
			result.Append (GetModifiers (options.Part, member));
			
			bool isFromInterface = false;
			if (member.DeclaringType != null && member.DeclaringTypeDefinition.Kind == TypeKind.Interface) {
				isFromInterface = true;
// TODO: Type system conversion.
//				if (options.ImplementingType != null) {
//					foreach (var type in options.ImplementingType.BaseTypes) {
//						if (type.Kind == TypeKind.Interface)
//							continue;
//						if (type.Members.Any (m => m.Name == member.Name && member.EntityType == m.EntityType /* && DomMethod.ParameterListEquals (member.Parameters, m.Parameters)*/ )) {
//							isFromInterface = false;
//							break;
//						}
//					}
//				}
			}
			
			if (!isFromInterface && member.IsOverridable)
				result.Append ("override ");
		}
		
		CodeGeneratorMemberResult GenerateCode (IProperty property, CodeGenerationOptions options)
		{
			var regions = new List<CodeGeneratorBodyRegion> ();
			var result = new StringBuilder ();
			AppendModifiers (result, options, property);
			AppendReturnType (result, options, property.ReturnType);
			result.Append (" ");
			if (property.IsIndexer) {
				result.Append ("this[");
				AppendParameterList (result, options, property.Parameters);
				result.Append ("]");
			} else {
				if (options.ExplicitDeclaration) {
					result.Append (ambience.GetString (property.DeclaringType, OutputFlags.IncludeGenerics));
					result.Append (".");
				}
				result.Append (property.Name);
			}
			AppendBraceStart (result, Policy.PropertyBraceStyle);
			if (property.CanGet) {
				int bodyStartOffset, bodyEndOffset;
				AppendIndent (result);
				result.Append ("get");
				if (options.ImplementingType.Kind == TypeKind.Interface) {
					result.AppendLine (";");
				} else {
					AppendBraceStart (result, Policy.PropertyGetBraceStyle);
					if (IsMonoTouchModelMember (property)) {
						AppendMonoTouchTodo (result, out bodyStartOffset, out bodyEndOffset);
					} else if (property.IsAbstract || property.DeclaringTypeDefinition.Kind == TypeKind.Interface) {
						AppendNotImplementedException (result, options, out bodyStartOffset, out bodyEndOffset);
					} else {
						AppendIndent (result);
						bodyStartOffset = result.Length;
						if (property.EntityType == EntityType.Indexer) {
							result.Append ("return base[");
							if (property.Parameters.Count > 0)
								result.Append (property.Parameters.First ().Name);
							result.Append ("];");
						} else {
							result.Append ("return base.");
							result.Append (property.Name);
							result.Append (";");
						}
						bodyEndOffset = result.Length;
						AppendLine (result);
					}
					AppendBraceEnd (result, Policy.PropertyGetBraceStyle);
					AppendLine (result);
					regions.Add (new CodeGeneratorBodyRegion (bodyStartOffset, bodyEndOffset));
				}
			}
			
			if (property.CanSet) {
				int bodyStartOffset, bodyEndOffset;
				AppendIndent (result);
				result.Append ("set");
				if (options.ImplementingType.Kind == TypeKind.Interface) {
					result.AppendLine (";");
				} else {
					AppendBraceStart (result, Policy.PropertyGetBraceStyle);
					if (IsMonoTouchModelMember (property)) {
						AppendMonoTouchTodo (result, out bodyStartOffset, out bodyEndOffset);
					} else if (property.IsAbstract || property.DeclaringTypeDefinition.Kind == TypeKind.Interface) {
						AppendNotImplementedException (result, options, out bodyStartOffset, out bodyEndOffset);
					} else {
						AppendIndent (result);
						bodyStartOffset = result.Length;
						if (property.EntityType == EntityType.Indexer) {
							result.Append ("base[");
							if (property.Parameters.Count > 0)
								result.Append (property.Parameters.First ().Name);
							result.Append ("] = value;");
						} else { 
							result.Append ("base.");
							result.Append (property.Name);
							result.Append (" = value;");
						}
						bodyEndOffset = result.Length;
						AppendLine (result);
					}
					AppendBraceEnd (result, Policy.PropertyGetBraceStyle);
					AppendLine (result);
					regions.Add (new CodeGeneratorBodyRegion (bodyStartOffset, bodyEndOffset));
				}
			}
			AppendBraceEnd (result, Policy.PropertyBraceStyle);
			return new CodeGeneratorMemberResult (result.ToString (), regions);
		}
		
		static bool IsMonoTouchModelMember (IMember member)
		{
			if (member == null || member.DeclaringType == null)
				return false;
			return member.DeclaringTypeDefinition.Attributes.Any (attr => attr.AttributeType != null && attr.AttributeType.ReflectionName == "MonoTouch.Foundation.ModelAttribute");
		}
		
		public override string CreateFieldEncapsulation (IUnresolvedTypeDefinition implementingType, IField field, string propertyName, Accessibility modifiers, bool readOnly)
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
			var options = new CodeGenerationOptions () {
				ImplementingType = field.DeclaringTypeDefinition,
				Part = implementingType
			};
			
			AppendReturnType (result, options, field.ReturnType);
			result.Append (" ");
			result.Append (propertyName);
			AppendBraceStart (result, Policy.PropertyBraceStyle);
			AppendIndent (result);
			
			result.Append ("get");
			AppendBraceStart (result, Policy.PropertyGetBraceStyle);
			AppendIndent (result);
			result.Append ("return this.");
			result.Append (field.Name);
			result.Append (";");
			AppendLine (result);
			AppendBraceEnd (result, Policy.PropertyGetBraceStyle);
			AppendLine (result);

			if (!readOnly) {
				AppendIndent (result);
				result.Append ("set");
				AppendBraceStart (result, Policy.PropertyGetBraceStyle);
				AppendIndent (result);
				result.Append (field.Name);
				result.Append (" = value;");
				AppendLine (result);
				AppendBraceEnd (result, Policy.PropertyGetBraceStyle);
				AppendLine (result);
			}
			
			AppendBraceEnd (result, Policy.PropertyBraceStyle);
			return result.ToString ();
		}
		
		int CountBlankLines (MonoDevelop.Ide.Gui.Document doc, int startLine)
		{
			int result = 0;
			LineSegment line;
			while ((line = doc.Editor.GetLine (startLine + result)) != null && doc.Editor.GetLineIndent (line).Length == line.EditableLength) {
				result++;
			}
		
			return result;
		}
		
		static bool InsertUsingAfter (AstNode node)
		{
			return node is ICSharpCode.NRefactory.CSharp.Comment ||
				node is UsingDeclaration ||
				node is UsingAliasDeclaration;
		}
		
		static AstNode SearchUsingInsertionPoint (AstNode parent)
		{
			var node = parent.FirstChild;
			while (true) {
				var next = node.NextSibling;
				if (!InsertUsingAfter (next))
					break;
				node = next;
			}
			return node;
		}
		
		public override void AddGlobalNamespaceImport (MonoDevelop.Ide.Gui.Document doc, string nsName)
		{
			var parsedDocument = doc.ParsedDocument;
			var unit = parsedDocument.GetAst<CompilationUnit> ();
			if (unit == null)
				return;
			
			var policy = doc.Project != null ? doc.Project.Policies.Get <CSharpFormattingPolicy> () : null;
			if (policy == null)
				policy = this.Policy;
			
			var node = SearchUsingInsertionPoint (unit);
			
			var text = new StringBuilder ();
			int lines = 0;
			
			if (InsertUsingAfter (node)) {
				lines = policy.BlankLinesBeforeUsings + 1;
				while (lines-- > 0) {
					text.Append (doc.Editor.EolMarker);
				}
			}
			
			text.Append ("using ");
			text.Append (nsName);
			text.Append (";");
			
			int offset = 0;
			if (node != null) {
				var loc = InsertUsingAfter (node) ? node.EndLocation : node.StartLocation;
				offset = doc.Editor.LocationToOffset (loc.Line, loc.Column);
			}
			
			lines = policy.BlankLinesAfterUsings;
			lines -= CountBlankLines (doc, doc.Editor.OffsetToLineNumber (offset) + 1);
			if (lines > 0)
				text.Append (doc.Editor.EolMarker);
			while (lines-- > 0) {
				text.Append (doc.Editor.EolMarker);
			}
			using (var undo = doc.Editor.OpenUndoGroup ()) {
				int caretOffset = doc.Editor.Caret.Offset;
				int inserted = doc.Editor.Insert (offset, text.ToString ());
				if (offset < caretOffset)
					doc.Editor.Caret.Offset = caretOffset + inserted;
			}
			doc.Editor.Document.CommitUpdateAll ();
		}
		
		public override void AddLocalNamespaceImport (MonoDevelop.Ide.Gui.Document doc, string nsName, TextLocation caretLocation)
		{
			var parsedDocument = doc.ParsedDocument;
			var unit = parsedDocument.GetAst<CompilationUnit> ();
			if (unit == null)
				return;
			
			var nsDecl = unit.GetNodeAt<NamespaceDeclaration> (caretLocation);
			if (nsDecl == null) {
				AddGlobalNamespaceImport (doc, nsName);
				return;
			}
			
			var policy = doc.Project != null ? doc.Project.Policies.Get <CSharpFormattingPolicy> () : null;
			if (policy == null)
				policy = this.Policy;
			
			
			var node = SearchUsingInsertionPoint (nsDecl);
			
			var text = new StringBuilder ();
			int lines = 0;
			
			if (InsertUsingAfter (node)) {
				lines = policy.BlankLinesBeforeUsings + 1;
				while (lines-- > 0) {
					text.Append (doc.Editor.EolMarker);
				}
			}
			
			string indent = doc.Editor.GetLineIndent (nsDecl.StartLocation.Line) + "\t";
			text.Append (indent);
			text.Append ("using ");
			text.Append (nsName);
			text.Append (";");
			
			int offset;
			TextLocation loc;
			if (node != null) {
				loc = InsertUsingAfter (node) ? node.EndLocation : node.StartLocation;
			} else {
				loc = nsDecl.LBraceToken.EndLocation;
			}
			offset = doc.Editor.LocationToOffset (loc.Line, loc.Column);
			
			lines = policy.BlankLinesAfterUsings;
			lines -= CountBlankLines (doc, doc.Editor.OffsetToLineNumber (offset) + 1);
			if (lines > 0)
				text.Append (doc.Editor.EolMarker);
			while (lines-- > 0) {
				text.Append (doc.Editor.EolMarker);
			}
			
			using (var undo = doc.Editor.OpenUndoGroup ()) {
				int caretOffset = doc.Editor.Caret.Offset;
				int inserted = doc.Editor.Insert (offset, text.ToString ());
				if (offset < caretOffset)
					doc.Editor.Caret.Offset = caretOffset + inserted;
			}
		}
		
		public override string GetShortTypeString (MonoDevelop.Ide.Gui.Document doc, IType type)
		{
			var shortType = CreateShortType (doc.Compilation, doc.ParsedDocument.ParsedFile as CSharpParsedFile, doc.Editor.Caret.Location, type);
			return OutputNode (doc, shortType);
		}
		
		static string OutputNode (MonoDevelop.Ide.Gui.Document doc, AstNode node)
		{
			using (var stringWriter = new System.IO.StringWriter ()) {
//				formatter.Indentation = indentLevel;
				var formatter = new TextWriterOutputFormatter (stringWriter);
				stringWriter.NewLine = doc.Editor.EolMarker;
				
				var visitor = new CSharpOutputVisitor (formatter, doc.GetFormattingOptions ());
				node.AcceptVisitor (visitor, null);
				return stringWriter.ToString ();
			}
		}
		
		
		public AstType CreateShortType (ICompilation compilation, CSharpParsedFile parsedFile, TextLocation loc, IType fullType)
		{
			var csResolver = parsedFile.GetResolver (compilation, loc);
			var builder = new ICSharpCode.NRefactory.CSharp.Refactoring.TypeSystemAstBuilder (csResolver);
			return builder.ConvertType (fullType);			
		}
		
		public override void CompleteStatement (MonoDevelop.Ide.Gui.Document doc)
		{
			var file = doc.Editor;
			var caretLocation = file.Caret.Location;
			
			int pos = file.LocationToOffset (caretLocation.Line + 1, 1);
			StringBuilder line = new StringBuilder ();
			int lineNr = caretLocation.Line + 1, column = 1, maxColumn = 1, lastPos = pos;
			if (true) 
				while (lineNr == caretLocation.Line + 1) {
					maxColumn = column;
					lastPos = pos;
					line.Append (file.GetCharAt (pos));
					pos++;
					var loc = file.OffsetToLocation (pos);
					lineNr = loc.Line;
					column = loc.Column;
				}
			string trimmedline = line.ToString ().Trim ();
			string indent = line.ToString ().Substring (0, line.Length - line.ToString ().TrimStart (' ', '\t').Length);
			if (trimmedline.EndsWith (";") || trimmedline.EndsWith ("{")) {
				file.Caret.Location = caretLocation;
				return;
			}
			
			if (trimmedline.StartsWith ("if") || 
			    trimmedline.StartsWith ("while") ||
			    trimmedline.StartsWith ("switch") ||
			    trimmedline.StartsWith ("for") ||
			    trimmedline.StartsWith ("foreach")) {
				if (!trimmedline.EndsWith (")")) {
					file.Insert (lastPos, " () {" + file.EolMarker + indent + file.Options.IndentationString + file.EolMarker + indent + "}");
					caretLocation.Column = maxColumn + 1;
				} else {
					file.Insert (lastPos, " {" + file.EolMarker + indent + file.Options.IndentationString + file.EolMarker + indent + "}");
					caretLocation.Column = indent.Length + 1;
					caretLocation.Line++;
				}
			} else if (trimmedline.StartsWith ("do")) {
				file.Insert (lastPos, " {" + file.EolMarker + indent + file.Options.IndentationString + file.EolMarker + indent + "} while ();");
				caretLocation.Column = indent.Length + 1;
				caretLocation.Line++;
			} else {
				file.Insert (lastPos, ";" + file.EolMarker + indent);
				caretLocation.Column = indent.Length;
				caretLocation.Line++;
			}
			file.Caret.Location = caretLocation;
		}
		
		
	}
}