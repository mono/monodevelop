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
using System.Linq;
using MonoDevelop.Ide.TypeSystem;
using Microsoft.CodeAnalysis;
using System.Text;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide;
using System.Collections.Generic;
using MonoDevelop.CSharp.Formatting;
using Microsoft.CodeAnalysis.Options;
using MonoDevelop.CSharp.Completion;
using System.Threading;
using ICSharpCode.NRefactory;

namespace MonoDevelop.CSharp.Refactoring
{
	class CSharpCodeGenerator : CodeGenerator
	{
		//		static CSharpAmbience ambience = new CSharpAmbience ();
		//		
		//		CSharpFormattingPolicy policy;
		//		
		//		public MonoDevelop.CSharp.Formatting.CSharpFormattingPolicy Policy {
		//			get {
		//				if (policy == null) {
		//					var types = MonoDevelop.Ide.DesktopService.GetMimeTypeInheritanceChain (MonoDevelop.CSharp.Formatting.CSharpFormatter.MimeType);
		//					if (PolicyParent != null)
		//						policy = PolicyParent.Get<CSharpFormattingPolicy> (types);
		//					if (policy == null) {
		//						
		//						policy = MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<CSharpFormattingPolicy> (types);
		//					}
		//				}
		//				return this.policy;
		//			}
		//		}
		//		
		//		public override PolicyContainer PolicyParent {
		//			get {
		//				return base.PolicyParent;
		//			}
		//			set {
		//				base.PolicyParent = value;
		//				var types = MonoDevelop.Ide.DesktopService.GetMimeTypeInheritanceChain (MonoDevelop.CSharp.Formatting.CSharpFormatter.MimeType);
		//				policy = value.Get<CSharpFormattingPolicy> (types);
		//			}
		//		}
		//		
		//		
		class CodeGenerationOptions
		{
			public bool ExplicitDeclaration { get; set; }
			public ITypeSymbol ImplementingType { get; set; }
			public Location Part { get; set; }

			public TextEditor Editor { get; set; }
			public DocumentContext DocumentContext { get; set; }

			public bool CreateProtocolMember { get; set; }

			SemanticModel semanticModel;
			public SemanticModel SemanticModel {
				get {
					if (semanticModel == null) {
						var model = DocumentContext.ParsedDocument.GetAst<SemanticModel> ();
						return model;
					}
					return semanticModel;
				}

				set {
					semanticModel = value;
				}
			}

			public string GetShortType(string ns, string name, int typeArguments = 0)
			{
				if (DocumentContext == null || Editor == null || SemanticModel == null || DocumentContext.ParsedDocument == null)
					return ns + "." + name;

				var model = DocumentContext.ParsedDocument.GetAst<SemanticModel>();

				if (model == null)
					return ns + "." + name;

				var type = model.Compilation.GetTypeByMetadataName(ns + "." + name);
				if (type == null)
					return ns + "." + name;


				return CSharpAmbience.SafeMinimalDisplayString (type, model, Editor.CaretOffset, Ambience.LabelFormat);
			}
		}

		static void AppendLine(StringBuilder sb)
		{
			sb.AppendLine();
		}

		public override string WrapInRegions (string regionName, string text)
		{
			StringBuilder result = Core.StringBuilderCache.Allocate ();
			AppendIndent (result);
			result.Append ("#region ");
			result.Append (regionName);
			AppendLine (result);
			result.Append (text);
			AppendLine (result);
			AppendIndent (result);
			result.Append ("#endregion");
			return Core.StringBuilderCache.ReturnAndFree (result);
		}

		static void AppendObsoleteAttribute(StringBuilder result, CodeGenerationOptions options, ISymbol entity)
		{
			// TODO: Roslyn port
			//			string reason;
			//			if (!entity.IsObsolete (out reason))
			//				return;
			//
			//			var implementingType = options.Part;
			//			var loc = implementingType.Region.End;
			//			
			//			var pf = implementingType.UnresolvedFile;
			//			var file = pf as CSharpUnresolvedFile;
			//
			//			result.Append ("[");
			//			var obsoleteRef = ReflectionHelper.ParseReflectionName ("System.ObsoleteAttribute");
			//			var resolvedType = obsoleteRef.Resolve (options.ImplementingType.Compilation);
			//			var shortType = resolvedType.Kind != TypeKind.Unknown ? CreateShortType (options.ImplementingType.Compilation, file, loc, resolvedType) : null;
			//			var text = shortType != null ? shortType.ToString () : "System.Obsolete";
			//			if (text.EndsWith ("Attribute", StringComparison.Ordinal))
			//				text = text.Substring (0, text.Length - "Attribute".Length);
			//			result.Append (text);
			//			if (!string.IsNullOrEmpty (reason)) {
			//				result.Append (" (\"");
			//				result.Append (reason);
			//				result.Append ("\")");
			//			}
			//			result.Append ("]");
			//			result.AppendLine ();
		}
		//		
		//		public override CodeGeneratorMemberResult CreateMemberImplementation (ITypeDefinition implementingType,
		//		                                                                      IUnresolvedTypeDefinition part,
		//		                                                                      IUnresolvedMember member,
		//		                                                                      bool explicitDeclaration)
		//		{
		//			SetIndentTo (part);
		//			var options = new CodeGenerationOptions () {
		//				ExplicitDeclaration = explicitDeclaration,
		//				ImplementingType = implementingType,
		//				Part = part
		//			};
		//			ITypeResolveContext ctx;
		//
		//			var doc = IdeApp.Workbench.GetDocument (part.Region.FileName);
		//			ctx = new CSharpTypeResolveContext (implementingType.Compilation.MainAssembly, null, implementingType, null);
		//			options.Document = doc;
		//
		//			if (member is IUnresolvedMethod)
		//				return GenerateCode ((IMethod) ((IUnresolvedMethod)member).CreateResolved (ctx), options);
		//			if (member is IUnresolvedProperty)
		//				return GenerateCode ((IProperty) ((IUnresolvedProperty)member).CreateResolved (ctx), options);
		//			if (member is IUnresolvedField)
		//				return GenerateCode ((IField) ((IUnresolvedField)member).CreateResolved (ctx), options);
		//			if (member is IUnresolvedEvent)
		//				return GenerateCode ((IEvent) ((IUnresolvedEvent)member).CreateResolved (ctx), options);
		//			throw new NotSupportedException ("member " +  member + " is not supported.");
		//		}

		public static CodeGeneratorMemberResult CreateOverridenMemberImplementation(DocumentContext document, TextEditor editor, ITypeSymbol implementingType, Location part, ISymbol member, bool explicitDeclaration, SemanticModel model)
		{
			var options = new CodeGenerationOptions {
				ExplicitDeclaration = explicitDeclaration,
				ImplementingType = implementingType,
				Part = part,
				DocumentContext = document,
				Editor = editor,
				SemanticModel = model
			};

			if (member is IMethodSymbol)
				return GenerateCode ((IMethodSymbol)member, options);
			if (member is IPropertySymbol)
				return GenerateCode ((IPropertySymbol)member, options);
			if (member is IFieldSymbol)
				return GenerateCode ((IFieldSymbol)member, options);
			if (member is IEventSymbol)
				return GenerateCode ((IEventSymbol)member, options);
			throw new NotSupportedException("member " + member + " is not supported.");
		}

		public static CodeGeneratorMemberResult CreateProtocolMemberImplementation(DocumentContext document, TextEditor editor, ITypeSymbol implementingType, Location part, ISymbol member, bool explicitDeclaration, SemanticModel model)
		{
			//			SetIndentTo (part);
			var options = new CodeGenerationOptions {
				ExplicitDeclaration = explicitDeclaration,
				ImplementingType = implementingType,
				Part = part,
				DocumentContext = document,
				Editor = editor,
				CreateProtocolMember = true,
				SemanticModel = model
			};

			if (member is IMethodSymbol)
				return GenerateProtocolCode ((IMethodSymbol)member, options);
			if (member is IPropertySymbol)
				return GenerateCode ((IPropertySymbol)member, options);
			if (member is IFieldSymbol)
				return GenerateCode ((IFieldSymbol)member, options);
			if (member is IEventSymbol)
				return GenerateCode ((IEventSymbol)member, options);
			throw new NotSupportedException("member " + member + " is not supported.");
		}

		public static CodeGeneratorMemberResult CreatePartialMemberImplementation(DocumentContext document, TextEditor editor, ITypeSymbol implementingType, Location part, ISymbol member, bool explicitDeclaration, SemanticModel model)
		{
			var options = new CodeGenerationOptions {
				ExplicitDeclaration = explicitDeclaration,
				ImplementingType = implementingType,
				Part = part,
				DocumentContext = document,
				Editor = editor,
				SemanticModel = model
			};

			if (member is IMethodSymbol)
				return GeneratePartialCode ((IMethodSymbol)member, options);
			throw new NotSupportedException("member " + member + " is not supported.");
		}
		//
		//		void AppendBraceStart (StringBuilder result, BraceStyle braceStyle)
		//		{
		//			switch (braceStyle) {
		//			case BraceStyle.BannerStyle:
		//			case BraceStyle.EndOfLine:
		//				result.Append (" {");
		//				AppendLine (result);
		//				break;
		//			case BraceStyle.EndOfLineWithoutSpace:
		//				result.Append ("{");
		//				AppendLine (result);
		//				break;
		//			case BraceStyle.NextLine:
		//				AppendLine (result);
		//				AppendIndent (result);
		//				result.Append ("{");
		//				AppendLine (result);
		//				break;
		//			case BraceStyle.NextLineShifted:
		//				AppendLine (result);
		//				result.Append (GetIndent (IndentLevel + 1));
		//				result.Append ("{");
		//				AppendLine (result);
		//				break;
		//			case BraceStyle.NextLineShifted2:
		//				AppendLine (result);
		//				result.Append (GetIndent (IndentLevel + 1));
		//				result.Append ("{");
		//				AppendLine (result);
		//				IndentLevel++;
		//				break;
		//			default:
		//				goto case BraceStyle.NextLine;
		//			}
		//			IndentLevel++;
		//		}
		//		
		//		void AppendBraceEnd (StringBuilder result, BraceStyle braceStyle)
		//		{
		//			switch (braceStyle) {
		//			case BraceStyle.EndOfLineWithoutSpace:
		//			case BraceStyle.NextLine:
		//			case BraceStyle.EndOfLine:
		//				IndentLevel --;
		//				AppendIndent (result);
		//				result.Append ("}");
		//				break;
		//			case BraceStyle.BannerStyle:
		//			case BraceStyle.NextLineShifted:
		//				AppendIndent (result);
		//				result.Append ("}");
		//				IndentLevel--;
		//				break;
		//			case BraceStyle.NextLineShifted2:
		//				IndentLevel--;
		//				AppendIndent (result);
		//				result.Append ("}");
		//				IndentLevel--;
		//				break;
		//			default:
		//				goto case BraceStyle.NextLine;
		//			}
		//		}
		//		
		//		void AppendIndent (StringBuilder result)
		//		{
		//			result.Append (GetIndent (IndentLevel));
		//		}
		//		
		static void AppendReturnType(StringBuilder result, CodeGenerationOptions options, ITypeSymbol type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			result.Append(CSharpAmbience.SafeMinimalDisplayString (type, options.SemanticModel, options.Part.SourceSpan.Start, Ambience.LabelFormat));

			//			var implementingType = options.Part;
			//			var loc = implementingType.Region.End;
			//			
			//			var pf = implementingType.UnresolvedFile;
			//			var file = pf as CSharpUnresolvedFile;
			//			var resolved = type;
			//			if (resolved.Kind == TypeKind.Unknown) {
			//				result.Append (type.FullName);
			//				return;
			//			}
			//			var def = type.GetDefinition ();
			//			if (def != null) {
			//				using (var stringWriter = new System.IO.StringWriter ()) {
			//					var formatter = new TextWriterTokenWriter (stringWriter);
			//					stringWriter.NewLine = EolMarker; 
			//					var visitor = new CSharpOutputVisitor (formatter, FormattingOptionsFactory.CreateMono ());
			//					var shortType = CreateShortType (def.Compilation, file, loc, resolved);
			//					shortType.AcceptVisitor (visitor);
			//					
			//					var typeString = stringWriter.ToString ();
			//					if (typeString.StartsWith ("global::"))
			//						typeString = typeString.Substring ("global::".Length);
			//					result.Append (typeString);
			//				}
			//			} else {
			//				result.Append (new ICSharpCode.NRefactory.CSharp.CSharpAmbience ().ConvertType (type));
			//			}
		}
		//		
		//		/*
		//		void ResolveReturnTypes ()
		//		{
		//			returnType = member.ReturnType;
		//			foreach (IUsing u in unit.Usings) {
		//				foreach (KeyValuePair<string, IReturnType> alias in u.Aliases) {
		//					if (alias.Key == member.ReturnType.FullName) {
		//						returnType = alias.Value;
		//						return;
		//					}
		//				}
		//			}
		//		}*/
		//
		//		
		static CodeGeneratorMemberResult GenerateCode (IFieldSymbol field, CodeGenerationOptions options)
		{
			StringBuilder result = Core.StringBuilderCache.Allocate ();
			AppendIndent (result);
			AppendModifiers (result, options, field);
			result.Append (" ");
			AppendReturnType (result, options, field.Type);
			result.Append (" ");
			result.Append (CSharpAmbience.FilterName (field.Name));
			result.Append (";");
			return new CodeGeneratorMemberResult (Core.StringBuilderCache.ReturnAndFree (result), -1, -1);
		}

		static void AppendIndent (StringBuilder result)
		{

		}

		static CodeGeneratorMemberResult GenerateCode (IEventSymbol evt, CodeGenerationOptions options)
		{
			StringBuilder result = Core.StringBuilderCache.Allocate ();
			AppendObsoleteAttribute (result, options, evt);
			AppendModifiers (result, options, evt);

			result.Append ("event ");
			AppendReturnType (result, options, evt.Type);
			result.Append (" ");
			if (options.ExplicitDeclaration) {
				AppendReturnType (result, options, evt.ContainingType);
				result.Append (".");
			}

			result.Append (CSharpAmbience.FilterName (evt.Name));
			if (options.ExplicitDeclaration) {
				result.Append ("{");
				AppendIndent (result);
				result.Append ("add {");
				AppendIndent (result);
				result.Append ("// TODO");
				AppendLine (result);
				result.Append ("}");

				AppendIndent (result);
				result.Append ("remove {");
				AppendIndent (result);
				result.Append ("// TODO");
				AppendLine (result);
				result.Append ("}}");
			} else {
				result.Append (";");
			}
			return new CodeGeneratorMemberResult (Core.StringBuilderCache.ReturnAndFree (result));
		}

		static void AppendNotImplementedException (StringBuilder result, CodeGenerationOptions options, out int bodyStartOffset, out int bodyEndOffset)
		{
			AppendIndent (result);
			bodyStartOffset = result.Length;
			result.Append ("throw new ");
			result.Append (options.GetShortType ("System", "NotImplementedException"));
			result.Append ("();");
			bodyEndOffset = result.Length;
			AppendLine (result);
		}

		internal static string[] MonoTouchComments = {
			" NOTE: Don't call the base implementation on a Model class",
			" see http://docs.xamarin.com/guides/ios/application_fundamentals/delegates,_protocols,_and_events"
		};

		static void AppendMonoTouchTodo(StringBuilder result, CodeGenerationOptions options, out int bodyStartOffset, out int bodyEndOffset)
		{
			AppendIndent (result);
			bodyStartOffset = result.Length;
			foreach (var cmt in MonoTouchComments) {
				result.Append("//").AppendLine (cmt);
				AppendIndent (result);
			}
			result.Append("throw new ");
			result.Append(options.GetShortType("System", "NotImplementedException"));
			result.Append("();");

			bodyEndOffset = result.Length;
			AppendLine (result);
		}

		static CodeGeneratorMemberResult GenerateCode(IMethodSymbol method, CodeGenerationOptions options)
		{
			int bodyStartOffset = -1, bodyEndOffset = -1;
			var result = Core.StringBuilderCache.Allocate ();
			AppendObsoleteAttribute (result, options, method);
			AppendModifiers (result, options, method);
			//			if (method.IsPartial)
			//				result.Append ("partial ");
			AppendReturnType (result, options, method.ReturnType);
			result.Append (" ");
			if (options.ExplicitDeclaration) {
				AppendReturnType (result, options, method.ContainingType);
				result.Append(".");
			}

			result.Append(CSharpAmbience.FilterName(method.Name));
			if (method.TypeParameters.Length > 0) {
				result.Append("<");
				for (int i = 0; i < method.TypeParameters.Length; i++) {
					if (i > 0)
						result.Append(", ");
					var p = method.TypeParameters[i];
					result.Append(CSharpAmbience.FilterName(p.Name));
				}
				result.Append(">");
			}
			result.Append("(");
			AppendParameterList (result, options, method.Parameters, true);
			result.Append(")");

			var typeParameters = method.TypeParameters;

			//			// This should also check the types are in the correct mscorlib
			//			Func<IType, bool> validBaseType = t => t.FullName != "System.Object" && t.FullName != "System.ValueType";
			//
			//			bool isFromInterface = method.DeclaringType != null && method.DeclaringTypeDefinition.Kind == TypeKind.Interface;
			//
			//			if (!options.ExplicitDeclaration && isFromInterface && typeParameters.Any (p => p.HasDefaultConstructorConstraint || p.HasReferenceTypeConstraint || p.HasValueTypeConstraint || p.DirectBaseTypes.Any (validBaseType))) {
			//				result.Append (" where ");
			//				int typeParameterCount = 0;
			//				foreach (var p in typeParameters) {
			//					if (typeParameterCount != 0)
			//						result.Append (", ");
			//					
			//					typeParameterCount++;
			//					result.Append (CSharpAmbience.FilterName (p.Name));
			//					result.Append (" : ");
			//					int constraintCount = 0;
			//					
			//					if (p.HasDefaultConstructorConstraint) {
			//						result.Append ("new ()");
			//						constraintCount++;
			//					}
			//					
			//					if (p.HasValueTypeConstraint) {
			//						if (constraintCount != 0)
			//							result.Append (", ");
			//						result.Append ("struct");
			//						constraintCount++;
			//					}
			//					
			//					if (p.HasReferenceTypeConstraint) {
			//						if (constraintCount != 0)
			//							result.Append (", ");
			//						result.Append ("class");
			//						constraintCount++;
			//					}
			//					//					bool hadInterfaces = false;
			//					foreach (var c in p.DirectBaseTypes.Where (validBaseType)) {
			//						if (constraintCount != 0)
			//							result.Append (", ");
			//						constraintCount++;
			//						AppendReturnType (result, options, c);
			//						//						if (c.Kind == TypeKind.Interface)
			//						//							hadInterfaces = true;
			//					}
			//				}
			//			}

			if (options.ImplementingType.TypeKind == TypeKind.Interface) {
				result.Append (";");
			} else {
				result.Append ("{");
				if (method.Name == "ToString" && method.Parameters.Length == 0 && method.ReturnType != null/* && method.ReturnType.FullName == "System.String"*/) {
					AppendIndent (result);
					bodyStartOffset = result.Length;
					result.Append ("return string.Format");
					result.Append ("(\"[");
					result.Append (options.ImplementingType.Name);
					if (options.ImplementingType.GetMembers ().OfType<IPropertySymbol> ().Any ())
						result.Append (": ");
					int i = 0;
					var properties = new List<IPropertySymbol> ();

					foreach (var property in options.ImplementingType.GetMembers ().OfType<IPropertySymbol> ()) {
						if (properties.Any (p => p.Name == property.Name))
							continue;
						properties.Add (property);
					}

					foreach (var property in properties) {
						if (property.IsStatic || property.DeclaredAccessibility != Accessibility.Public)
							continue;
						if (i > 0)
							result.Append (", ");
						result.Append (property.Name);
						result.Append ("={");
						result.Append (i++);
						result.Append ("}");
					}
					result.Append ("]\"");
					foreach (var property in properties) {
						if (property.IsStatic || property.DeclaredAccessibility != Accessibility.Public)
							continue;
						result.Append (", ");
						result.Append (property.Name);
					}
					result.Append (");");
					bodyEndOffset = result.Length;
					AppendLine (result);
				} else if (IsMonoTouchModelMember (method)) {
					AppendMonoTouchTodo (result, options, out bodyStartOffset, out bodyEndOffset);
				} else if (method.IsAbstract || !(method.IsVirtual || method.IsOverride) || method.ContainingType.TypeKind == TypeKind.Interface) {
					AppendNotImplementedException (result, options, out bodyStartOffset, out bodyEndOffset);
				} else {
					bool skipBody = false;
					// Analyze if the body consists just of a single throw instruction
					// See: Bug 1373 - overriding [Model] class methods shouldn't insert base.Methods
					// TODO: Extend this to user defined code.
					//					try {
					//						if (method.Region.FileName == null) {
					//							var asm = AssemblyDefinition.ReadAssembly (method.ParentAssembly.UnresolvedAssembly.Location);
					//							foreach (var type in asm.MainModule.Types) {
					//								if (type.FullName != method.DeclaringType.FullName)
					//									continue;
					//								foreach (var m  in type.Resolve ().Methods) {
					//									if (m.HasBody && m.Name == method.Name) {
					//										var context = new DecompilerContext (asm.MainModule);
					//										
					//										context.CurrentType = type;
					//				
					//										context.Settings = new DecompilerSettings () {
					//											AnonymousMethods = true,
					//											AutomaticEvents  = true,
					//											AutomaticProperties = true,
					//											ForEachStatement = true,
					//											LockStatement = true
					//										};
					//				
					//										var astBuilder = new AstBuilder (context);
					//										astBuilder.AddMethod (m);
					//										
					//										astBuilder.RunTransformations (o => false);
					//
					//										var visitor = new ThrowsExceptionVisitor ();
					//										astBuilder.SyntaxTree.AcceptVisitor (visitor);
					//										skipBody = visitor.Throws;
					//										if (skipBody)
					//											break;
					//									}
					//								}
					//								if (skipBody)
					//									break;
					//							}
					//						}
					//					} catch (Exception) {
					//					}
					AppendIndent (result);
					bodyStartOffset = result.Length;
					if (!skipBody) {
						if (method.ReturnType.SpecialType != SpecialType.System_Void)
							result.Append ("return ");
						result.Append ("base.");
						result.Append (CSharpAmbience.FilterName (method.Name));
						result.Append ("(");
						AppendParameterList (result, options, method.Parameters, false);
						result.Append (");");
					} else {
						result.Append ("throw new System.NotImplementedException ();");
					}
					bodyEndOffset = result.Length;
					AppendLine (result);
				}
				result.Append ("}");
			}
			return new CodeGeneratorMemberResult(Core.StringBuilderCache.ReturnAndFree (result), bodyStartOffset, bodyEndOffset);
		}


		static CodeGeneratorMemberResult GeneratePartialCode(IMethodSymbol method, CodeGenerationOptions options)
		{
			int bodyStartOffset = -1, bodyEndOffset = -1;
			var result = Core.StringBuilderCache.Allocate ();
			AppendObsoleteAttribute (result, options, method);
			result.Append("partial ");
			AppendReturnType (result, options, method.ReturnType);
			result.Append(" ");
			if (options.ExplicitDeclaration) {
				AppendReturnType (result, options, method.ContainingType);
				result.Append(".");
			}

			result.Append(CSharpAmbience.FilterName(method.Name));
			if (method.TypeParameters.Length > 0) {
				result.Append("<");
				for (int i = 0; i < method.TypeParameters.Length; i++) {
					if (i > 0)
						result.Append(", ");
					var p = method.TypeParameters[i];
					result.Append(CSharpAmbience.FilterName(p.Name));
				}
				result.Append(">");
			}
			result.Append("(");
			AppendParameterList (result, options, method.Parameters, true);
			result.Append(")");

			var typeParameters = method.TypeParameters;
			result.AppendLine("{");
			bodyStartOffset = result.Length;
			AppendLine (result);
			bodyEndOffset = result.Length;
			result.AppendLine("}");
			return new CodeGeneratorMemberResult(Core.StringBuilderCache.ReturnAndFree (result), bodyStartOffset, bodyEndOffset);
		}

		//		class ThrowsExceptionVisitor : DepthFirstAstVisitor
		//		{
		//			public bool Throws = false;
		//			
		//			public override void VisitBlockStatement (BlockStatement blockStatement)
		//			{
		//				if (blockStatement.Statements.Count == 1 && blockStatement.Statements.First () is ThrowStatement)
		//					Throws = true;
		//			}
		//		}

		static void AppendParameterList (StringBuilder result, CodeGenerationOptions options, IList<IParameterSymbol> parameters, bool asParameterList)
		{
			for (int i = 0; i < parameters.Count; i++) {
				if (i > 0)
					result.Append (", ");

				var p = parameters [i];
				if (p.RefKind == RefKind.Out)
					result.Append ("out ");
				if (p.RefKind == RefKind.Ref)
					result.Append ("ref ");
				if (asParameterList) {
					if (p.IsParams)
						result.Append ("params ");
					AppendReturnType (result, options, p.Type);
					result.Append (" ");
				}
				result.Append (CSharpAmbience.FilterName (p.Name));
				if (asParameterList && p.HasExplicitDefaultValue) {
					result.Append (" = ");
					if (p.ExplicitDefaultValue is Enum) {
						var name = Enum.GetName (p.ExplicitDefaultValue.GetType (), p.ExplicitDefaultValue);
						if (name != null) {
							AppendReturnType (result, options, p.Type);
							result.Append ("." + name);
						} else {
							result.Append ("(");
							AppendReturnType (result, options, p.Type);
							result.Append (")").Append (p.ExplicitDefaultValue);
						}
					} else if (p.ExplicitDefaultValue is char) {
						result.Append ("'").Append (p.ExplicitDefaultValue).Append ("'");
					} else if (p.ExplicitDefaultValue is string) {
						result.Append ("\"").Append (CSharpTextEditorIndentation.ConvertToStringLiteral ((string)p.ExplicitDefaultValue)).Append ("\"");
					} else if (p.ExplicitDefaultValue is bool) {
						result.Append ((bool)p.ExplicitDefaultValue ? "true" : "false");
					} else if (p.ExplicitDefaultValue == null) {
						if (p.Type.IsValueType && p.Type.SpecialType != SpecialType.System_String) {
							result.Append ("default(").Append (p.Type.ToMinimalDisplayString (options.SemanticModel, options.Part.SourceSpan.Start)).Append (")");
						} else {
							result.Append ("null");
						}
					} else {
						result.Append (p.ExplicitDefaultValue);
					}
				}
			}
		}

		public static IEnumerable<string> GetEnumLiterals (Type type)
		{
			return Enum.GetNames (type);
		}

		static string GetModifiers (ITypeSymbol implementingType, Location implementingPart, ISymbol member)
		{
			var result = Core.StringBuilderCache.Allocate ();

			if (member.DeclaredAccessibility == Accessibility.Public || (member.ContainingType != null && member.ContainingType.TypeKind == TypeKind.Interface)) {
				result.Append ("public ");
			} else if (member.DeclaredAccessibility == Accessibility.ProtectedOrInternal) {
				if (IdeApp.Workbench.ActiveDocument != null && member.ContainingAssembly != implementingType.ContainingAssembly) {
					result.Append ("protected ");
				} else {
					result.Append ("internal protected ");
				}
			} else if (member.DeclaredAccessibility == Accessibility.Protected) {
				result.Append ("protected ");
			} else if (member.DeclaredAccessibility == Accessibility.Internal) {
				result.Append ("internal ");
			}

			if (member.IsStatic)
				result.Append ("static ");

			return Core.StringBuilderCache.ReturnAndFree (result);
		}

		static void AppendModifiers (StringBuilder result, CodeGenerationOptions options, ISymbol member)
		{
			//AppendIndent (result);
			//if (options.ExplicitDeclaration || options.ImplementingType.Kind == TypeKind.Interface)
			//	return;
			result.Append (GetModifiers (options.ImplementingType, options.Part, member));
			//bool isFromInterface = false;
			if (member.ContainingType != null && member.ContainingType.TypeKind == TypeKind.Interface) {
				//isFromInterface = true;
				// TODO: Type system conversion.
				//				if (options.ImplementingType != null) {
				//					foreach (var type in options.ImplementingType.BaseTypes) {
				//						if (type.Kind == TypeKind.Interface)
				//							continue;
				//						if (type.Members.Any (m => m.Name == member.Name && member.SymbolKind == m.SymbolKind /* && DomMethod.ParameterListEquals (member.Parameters, m.Parameters)*/ )) {
				//							isFromInterface = false;
				//							break;
				//						}
				//					}
				//				}
			}
			if (member is IMethodSymbol) {
				if (!options.CreateProtocolMember)
					result.Append ("override ");
			}

			if (member is IPropertySymbol) {
				if (!options.CreateProtocolMember)
					result.Append ("override ");
			}
		}

		static CodeGeneratorMemberResult GenerateCode (IPropertySymbol property, CodeGenerationOptions options)
		{
			var regions = new List<CodeGeneratorBodyRegion> ();
			var result = Core.StringBuilderCache.Allocate ();
			AppendObsoleteAttribute (result, options, property);
			AppendModifiers (result, options, property);
			AppendReturnType (result, options, property.Type);
			result.Append (" ");
			if (property.IsIndexer) {
				result.Append ("this[");
				AppendParameterList (result, options, property.Parameters, true);
				result.Append ("]");
			} else {
				//				if (options.ExplicitDeclaration) {
				//					result.Append (ambience.GetString (property.DeclaringType, OutputFlags.IncludeGenerics));
				//					result.Append (".");
				//				}
				result.Append (CSharpAmbience.FilterName (property.Name));
			}
			result.AppendLine (" {");
			if (property.GetMethod != null) {
				int bodyStartOffset, bodyEndOffset;
				AppendIndent (result);
				result.Append ("get");
				if (options.ImplementingType.TypeKind == TypeKind.Interface) {
					result.AppendLine (";");
				} else {
					result.AppendLine (" {");
					if (IsMonoTouchModelMember (property)) {
						AppendMonoTouchTodo (result, options, out bodyStartOffset, out bodyEndOffset);
					} else if (property.IsAbstract || property.ContainingType.TypeKind == TypeKind.Interface) {
						AppendNotImplementedException (result, options, out bodyStartOffset, out bodyEndOffset);
					} else {
						AppendIndent (result);
						bodyStartOffset = result.Length;
						if (property.IsIndexer) {
							result.Append ("return base[");
							if (property.Parameters.Length > 0)
								result.Append (CSharpAmbience.FilterName (property.Parameters.First ().Name));
							result.Append ("];");
						} else {
							result.Append ("return base.");
							result.Append (CSharpAmbience.FilterName (property.Name));
							result.Append (";");
						}
						bodyEndOffset = result.Length;
						AppendLine (result);
					}
					result.Append ("}");
					AppendLine (result);
					regions.Add (new CodeGeneratorBodyRegion (bodyStartOffset, bodyEndOffset));
				}
			}

			if (property.SetMethod != null) {
				int bodyStartOffset, bodyEndOffset;
				AppendIndent (result);
				result.Append ("set");
				if (options.ImplementingType.TypeKind == TypeKind.Interface) {
					result.AppendLine (";");
				} else {
					result.AppendLine (" {");
					if (IsMonoTouchModelMember (property)) {
						AppendMonoTouchTodo (result, options, out bodyStartOffset, out bodyEndOffset);
					} else if (property.IsAbstract || property.ContainingType.TypeKind == TypeKind.Interface) {
						AppendNotImplementedException (result, options, out bodyStartOffset, out bodyEndOffset);
					} else {
						AppendIndent (result);
						bodyStartOffset = result.Length;
						if (property.IsIndexer) {
							result.Append ("base[");
							if (property.Parameters.Length > 0)
								result.Append (CSharpAmbience.FilterName (property.Parameters.First ().Name));
							result.Append ("] = value;");
						} else {
							result.Append ("base.");
							result.Append (CSharpAmbience.FilterName (property.Name));
							result.Append (" = value;");
						}
						bodyEndOffset = result.Length;
						AppendLine (result);
					}
					result.Append ("}");
					AppendLine (result);
					regions.Add (new CodeGeneratorBodyRegion (bodyStartOffset, bodyEndOffset));
				}
			}
			result.Append ("}");
			return new CodeGeneratorMemberResult (Core.StringBuilderCache.ReturnAndFree (result), regions);
		}

		internal static bool IsMonoTouchModelMember (ISymbol member)
		{
			if (member == null || member.ContainingType == null)
				return false;
			return member.ContainingType.GetAttributes ().Any (attr => attr.AttributeClass.MetadataName == "MonoTouch.Foundation.ModelAttribute");
		}

		////		public override string CreateFieldEncapsulation (IUnresolvedTypeDefinition implementingType, IField field, string propertyName, Accessibility modifiers, bool readOnly)
		////		{
		////			SetIndentTo (implementingType);
		////			StringBuilder result = new StringBuilder ();
		////			AppendIndent (result);
		////			
		//////			if (modifiers != MonoDevelop.Projects.Dom.Modifiers.None) {
		//////				switch (modifiers) {
		//////				}
		//////				result.Append (ambience.GetString (modifiers));
		//////				result.Append (" ");
		//////			}
		////			var options = new CodeGenerationOptions () {
		////				ImplementingType = field.DeclaringTypeDefinition,
		////				Part = implementingType
		////			};
		////			result.Append ("public ");
		////			AppendReturnType (result, options, field.ReturnType);
		////			result.Append (" ");
		////			result.Append (propertyName);
		////			AppendBraceStart (result, Policy.PropertyBraceStyle);
		////			AppendIndent (result);
		////			
		////			result.Append ("get");
		////			AppendBraceStart (result, Policy.PropertyGetBraceStyle);
		////			AppendIndent (result);
		////			result.Append ("return this.");
		////			result.Append (CSharpAmbience.FilterName (field.Name));
		////			result.Append (";");
		////			AppendLine (result);
		////			AppendBraceEnd (result, Policy.PropertyGetBraceStyle);
		////			AppendLine (result);
		////
		////			if (!readOnly) {
		////				AppendIndent (result);
		////				result.Append ("set");
		////				AppendBraceStart (result, Policy.PropertyGetBraceStyle);
		////				AppendIndent (result);
		////				result.Append (CSharpAmbience.FilterName (field.Name));
		////				result.Append (" = value;");
		////				AppendLine (result);
		////				AppendBraceEnd (result, Policy.PropertyGetBraceStyle);
		////				AppendLine (result);
		////			}
		////			
		////			AppendBraceEnd (result, Policy.PropertyBraceStyle);
		////			return result.ToString ();
		////		}
		//		
		//		int CountBlankLines (IReadonlyTextDocument doc, int startLine)
		//		{
		//			int result = 0;
		//			IDocumentLine line;
		//			while ((line = doc.GetLine (startLine + result)) != null && doc.GetLineIndent (line).Length == line.Length) {
		//				result++;
		//			}
		//		
		//			return result;
		//		}
		//		
		//		static bool InsertUsingAfter (AstNode node)
		//		{
		//			return node is NewLineNode && IsCommentOrUsing (node.GetNextSibling (s => !(s is NewLineNode))) ||
		//				IsCommentOrUsing (node) || (node is PreProcessorDirective);
		//		}
		//
		//		static bool IsCommentOrUsing (AstNode node)
		//		{
		//			return node is ICSharpCode.NRefactory.CSharp.Comment ||
		//				node is UsingDeclaration ||
		//				node is UsingAliasDeclaration;
		//		}
		//		

		//		
		//		static string OutputNode (TextEditor editor, DocumentContext context, AstNode node)
		//		{
		//			using (var stringWriter = new System.IO.StringWriter ()) {
		////				formatter.Indentation = indentLevel;
		//				var formatter = new TextWriterTokenWriter (stringWriter);
		//				stringWriter.NewLine = editor.EolMarker;
		//
		//				var visitor = new CSharpOutputVisitor (formatter, null /* TODO: BROKEN DUE ROSLYN PORT (note: that code should be unused) */ );
		//				node.AcceptVisitor (visitor);
		//				return stringWriter.ToString ();
		//			}
		//		}
		//		
		//		
		//		public AstType CreateShortType (ICompilation compilation, CSharpUnresolvedFile parsedFile, TextLocation loc, IType fullType)
		//		{
		//			var csResolver = parsedFile.GetResolver (compilation, loc);
		//			var builder = new ICSharpCode.NRefactory.CSharp.Refactoring.TypeSystemAstBuilder (csResolver);
		//			return builder.ConvertType (fullType);			
		//		}
		//

		public override async void CompleteStatement (MonoDevelop.Ide.Gui.Document doc)
		{
			var fixer = new ConstructFixer (doc.GetFormattingOptions ());
			int newOffset = await fixer.TryFix (doc, doc.Editor.CaretOffset, default(CancellationToken));
			if (newOffset != -1) {
				doc.Editor.CaretOffset = newOffset;
			}
		}

		static CodeGeneratorMemberResult GenerateProtocolCode(IMethodSymbol method, CodeGenerationOptions options)
		{
			int bodyStartOffset = -1, bodyEndOffset = -1;
			var result = Core.StringBuilderCache.Allocate ();
			var exportAttribute = method.GetAttributes ().FirstOrDefault (attr => attr.AttributeClass.Name == "ExportAttribute");
			if (exportAttribute != null) {
				result.Append ("[Export(\"");
				result.Append (exportAttribute.ConstructorArguments.First ().Value.ToString ());
				result.Append ("\")]");
				result.AppendLine ();
			}
			AppendModifiers (result, options, method);

			AppendReturnType (result, options, method.ReturnType);
			result.Append (" ");
			if (options.ExplicitDeclaration) {
				AppendReturnType (result, options, method.ContainingType);
				result.Append(".");
			}

			result.Append(CSharpAmbience.FilterName(method.Name));
			if (method.TypeParameters.Length > 0) {
				result.Append("<");
				for (int i = 0; i < method.TypeParameters.Length; i++) {
					if (i > 0)
						result.Append(", ");
					var p = method.TypeParameters[i];
					result.Append(CSharpAmbience.FilterName(p.Name));
				}
				result.Append(">");
			}
			result.Append("(");
			AppendParameterList (result, options, method.Parameters, true);
			result.Append(")");

			var typeParameters = method.TypeParameters;

			result.Append ("{");
			AppendIndent (result);
			bodyStartOffset = result.Length;
			result.Append ("throw new System.NotImplementedException ();");
			bodyEndOffset = result.Length;
			AppendLine (result);
			result.Append ("}");
			return new CodeGeneratorMemberResult(Core.StringBuilderCache.ReturnAndFree (result), bodyStartOffset, bodyEndOffset);
		}

		public override void AddGlobalNamespaceImport (TextEditor editor, DocumentContext context, string nsName)
		{
			// not used anymore
		}

		public override void AddLocalNamespaceImport (TextEditor editor, DocumentContext context, string nsName, TextLocation caretLocation)
		{
			// not used anymore
		}
	}
}