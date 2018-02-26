//
// SignatureMarkupCreator.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Projects;
using System.ComponentModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Components;
using MonoDevelop.Ide.Editor.Highlighting;
using ICSharpCode.NRefactory6.CSharp;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.CSharp.Completion;
using System.Threading;

namespace MonoDevelop.CSharp
{
	class SignatureMarkupCreator
	{
		const double optionalAlpha = 0.7;
		readonly DocumentContext ctx;
		readonly OptionSet options;
		readonly EditorTheme colorStyle;
		readonly int offset;

		public bool BreakLineAfterReturnType
		{
			get;
			set;
		}

		int highlightParameter = -1;

		public int HighlightParameter {
			get {
				return highlightParameter;
			}
			set {
				highlightParameter = value;
			}
		}

		public SignatureMarkupCreator (DocumentContext ctx, int offset)
		{
			this.offset = offset;
			this.colorStyle = SyntaxHighlightingService.GetIdeFittingTheme ();
			this.ctx = ctx;
			if (ctx != null) {
				this.options = ctx.GetOptionSet ();
			} else {
				this.options = MonoDevelop.Ide.TypeSystem.TypeSystemService.Workspace.Options;
			}
		}

		public string GetTypeReferenceString (ITypeSymbol type, bool highlight = true)
		{
			if (type == null)
				throw new ArgumentNullException (nameof (type));
			if (type.TypeKind == TypeKind.Error) {
				var typeSyntax = type.GenerateTypeSyntax ();
				string generatedTypeSyntaxString;
				try {
					var oldDoc = ctx.AnalysisDocument;
					var newDoc = oldDoc.WithSyntaxRoot (SyntaxFactory.ParseCompilationUnit (typeSyntax.ToString ()).WithAdditionalAnnotations (Simplifier.Annotation));
					var reducedDoc = Simplifier.ReduceAsync (newDoc, options);
					generatedTypeSyntaxString = Ambience.EscapeText (reducedDoc.Result.GetSyntaxRootAsync ().Result.ToString ());
				} catch {
					generatedTypeSyntaxString = typeSyntax != null ? Ambience.EscapeText (typeSyntax.ToString ()) : "?";
				}
				return highlight ? HighlightSemantically (generatedTypeSyntaxString, GetThemeColor (userTypes)) : generatedTypeSyntaxString;
			}
			if (type.TypeKind == TypeKind.Array) {
				var arrayType = (IArrayTypeSymbol)type;
				return GetTypeReferenceString (arrayType.ElementType, highlight) + "[" + new string (',', arrayType.Rank - 1) + "]";
			}
			if (type.TypeKind == TypeKind.Pointer)
				return GetTypeReferenceString (((IPointerTypeSymbol)type).PointedAtType, highlight) + "*";
			if (type.IsTupleType ()) {
				var sb = StringBuilderCache.Allocate ();
				sb.Append ("(");
				foreach (var member in type.GetMembers ().OfType<IFieldSymbol> ()) {
					if (member.CorrespondingTupleField == null ||
						member.CorrespondingTupleField == member)
						continue;
					if (sb.Length > 1)
						sb.Append (", ");
					sb.Append (GetTypeReferenceString (member.Type));
					sb.Append (" ");
					sb.Append (Ambience.EscapeText (member.Name));
				}
				sb.Append (")");
				return StringBuilderCache.ReturnAndFree (sb);
			}
			string displayString;
			if (ctx != null) {
				SemanticModel model = SemanticModel;
				if (model == null) {
					var parsedDocument = ctx.ParsedDocument;
					if (parsedDocument != null) {
						model = parsedDocument.GetAst<SemanticModel> () ?? ctx.AnalysisDocument?.GetSemanticModelAsync ().Result;
					}
				}
				//Math.Min (model.SyntaxTree.Length, offset)) is needed in case parsedDocument.GetAst<SemanticModel> () is outdated
				//this is tradeoff between performance and consistency between editor text(offset) and model, since
				//ToMinimalDisplayString can use little outdated model this is fine
				//but in case of Sketches where user usually is at end of document when typing text this can throw exception
				//because offset can be >= Length
				displayString = model != null ? CSharpAmbience.SafeMinimalDisplayString (type, model, Math.Min (model.SyntaxTree.Length - 1, offset), MonoDevelop.Ide.TypeSystem.Ambience.LabelFormat) : type.Name;
			} else {
				displayString = type.ToDisplayString (MonoDevelop.Ide.TypeSystem.Ambience.LabelFormat);
			}

			if (SyntaxFacts.IsPredefinedType(SyntaxFacts.GetKeywordKind (displayString)))
				return Highlight (displayString, GetThemeColor (keywordType));
			var text = MonoDevelop.Ide.TypeSystem.Ambience.EscapeText (displayString);
			return highlight ? HighlightSemantically (text, GetThemeColor (userTypes)) : text;
		}

		//		static ICompilation GetCompilation (IType type)
		//		{
		//			var def = type.GetDefinition ();
		//			if (def == null) {	
		//				var t = type;
		//				while (t is TypeWithElementType) {
		//					t = ((TypeWithElementType)t).ElementType;
		//				}
		//				if (t != null)
		//					def = t.GetDefinition ();
		//			}
		//			if (def != null)
		//				return def.Compilation;
		//			return null;
		//		}

		public string GetMarkup (ITypeSymbol type)
		{
			if (type == null)
				throw new ArgumentNullException ("entity");
			return GetTypeMarkup (type);
		}


		public string GetMarkup (Microsoft.CodeAnalysis.ISymbol entity)
		{
			if (entity == null)
				throw new ArgumentNullException ("entity");
			string result;
			try {
				switch (entity.Kind) {
				case Microsoft.CodeAnalysis.SymbolKind.ArrayType:
				case Microsoft.CodeAnalysis.SymbolKind.PointerType:
				case Microsoft.CodeAnalysis.SymbolKind.NamedType:
					result = GetTypeMarkup ((ITypeSymbol)entity);
					break;
				case Microsoft.CodeAnalysis.SymbolKind.Field:
					result = GetFieldMarkup ((IFieldSymbol)entity);
					break;
				case Microsoft.CodeAnalysis.SymbolKind.Property:
					result = GetPropertyMarkup ((IPropertySymbol)entity);
					break;
				case Microsoft.CodeAnalysis.SymbolKind.Event:
					result = GetEventMarkup ((IEventSymbol)entity);
					break;
				case Microsoft.CodeAnalysis.SymbolKind.Method:
					var method = (IMethodSymbol)entity;
					switch (method.MethodKind) {
					case MethodKind.Constructor:
						result = GetConstructorMarkup (method);
						break;
					case MethodKind.Destructor:
						result = GetDestructorMarkup (method);
						break;
					default:
						result = GetMethodMarkup (method);
						break;
					}
					break;
				case Microsoft.CodeAnalysis.SymbolKind.Namespace:
					result = GetNamespaceMarkup ((INamespaceSymbol)entity);
					break;
				case Microsoft.CodeAnalysis.SymbolKind.Local:
					result = GetLocalVariableMarkup ((ILocalSymbol)entity);
					break;
				case Microsoft.CodeAnalysis.SymbolKind.Parameter:
					result = GetParameterVariableMarkup ((IParameterSymbol)entity);
					break;
				default:
					Console.WriteLine (entity.Kind);
					return null;
				}
			} catch (Exception e) {
				LoggingService.LogError ("Error while getting markup for " + entity, e);
				return entity.Name;
			}
			// TODO
			//			if (entity.IsObsolete (out reason)) {
			//				var attr = reason == null ? "[Obsolete]" : "[Obsolete(\"" + reason + "\")]";
			//				result = "<span size=\"smaller\">" + attr + "</span>" + Environment.NewLine + result;
			//			}
			return result;
		}

		string GetNamespaceMarkup (INamespaceSymbol ns)
		{
			var result = StringBuilderCache.Allocate ();
			result.Append (Highlight ("namespace ", GetThemeColor (keywordOther)));
			result.Append (ns.Name);

			return StringBuilderCache.ReturnAndFree (result);
		}

		const string modifierColor      = "storage.modifier.source.cs";
		const string keywordDeclaration = "storage.type.source.cs";
		const string keywordOther       = "keyword.other.source.cs";
		const string keywordType        = "keyword.type.source.cs";
		const string keywordOperator    = "keyword.operator.source.cs";
		const string keywordConstant    = "constant.language.source.cs";
		const string userTypes = "entity.name.type.class.source.cs";

		const string numericConstants = "constant.numeric.source.cs";
		const string stringConstants = "punctuation.definition.string.begin.source.cs";


		void AppendModifiers (StringBuilder result, ISymbol entity)
		{
			if (entity.ContainingType != null && entity.ContainingType.TypeKind == TypeKind.Interface)
				return;
			
			switch (entity.DeclaredAccessibility) {
				case Accessibility.Internal:
				if (entity.Kind != SymbolKind.NamedType)
					result.Append (Highlight ("internal ", GetThemeColor (modifierColor)));
				break;
			case Accessibility.ProtectedAndInternal:
				result.Append (Highlight ("protected internal ", GetThemeColor (modifierColor)));
				break;
			case Accessibility.ProtectedOrInternal:
				result.Append (Highlight ("internal protected ", GetThemeColor (modifierColor)));
				break;
			case Accessibility.Protected:
				result.Append (Highlight ("protected ", GetThemeColor (modifierColor)));
				break;
			case Accessibility.Private:
				// private is the default modifier - no need to show that
				//				result.Append (Highlight (" private", SyntaxModeService.GetColor (colorStyle, modifierColor)));
				break;
			case Accessibility.Public:
				result.Append (Highlight ("public ", GetThemeColor (modifierColor)));
				break;
			}
			var field = entity as IFieldSymbol;

			if (field != null) {
				//  TODO!!!!
				/*if (field.IsFixed) {
					result.Append (Highlight ("fixed ", SyntaxModeService.GetColor (colorStyle, modifierColor)));
				} else*/
				if (field.IsConst) {
					result.Append (Highlight ("const ", GetThemeColor (modifierColor)));
				}
			} else if (entity.IsStatic) {
				result.Append (Highlight ("static ", GetThemeColor (modifierColor)));
			} else if (entity.IsSealed) {
				if (!(entity is ITypeSymbol && ((ITypeSymbol)entity).TypeKind == TypeKind.Delegate))
					result.Append (Highlight ("sealed ", GetThemeColor (modifierColor)));
			} else if (entity.IsAbstract) {
				if (!(entity is ITypeSymbol && ((ITypeSymbol)entity).TypeKind == TypeKind.Interface))
					result.Append (Highlight ("abstract ", GetThemeColor (modifierColor)));
			}

			//  TODO!!!!
			//			if (entity.IsShadowing)
			//				result.Append (Highlight ("new ", SyntaxModeService.GetColor (colorStyle, modifierColor)));

			var method = entity as IMethodSymbol;
			if (method != null) {
				if (method.IsOverride) {
					result.Append (Highlight ("override ", GetThemeColor (modifierColor)));
				} else if (method.IsVirtual) {
					result.Append (Highlight ("virtual ", GetThemeColor (modifierColor)));
				}
				if (method.IsAsync)
					result.Append (Highlight ("async ", GetThemeColor (modifierColor)));
				if (method.PartialDefinitionPart != null || method.PartialImplementationPart != null)
					result.Append (Highlight ("partial ", GetThemeColor (modifierColor)));
			}
			if (field != null) {
				if (field.IsVolatile)
					result.Append (Highlight ("volatile ", GetThemeColor (modifierColor)));
				if (field.IsReadOnly)
					result.Append (Highlight ("readonly ", GetThemeColor (modifierColor)));
			}

		}

		void AppendAccessibility (StringBuilder result, IMethodSymbol entity)
		{
			switch (entity.DeclaredAccessibility) {
			case Accessibility.Internal:
				result.Append (Highlight ("internal", GetThemeColor (modifierColor)));
				break;
			case Accessibility.ProtectedAndInternal:
				result.Append (Highlight ("protected internal", GetThemeColor (modifierColor)));
				break;
			case Accessibility.ProtectedOrInternal:
				result.Append (Highlight ("internal protected", GetThemeColor (modifierColor)));
				break;
			case Accessibility.Protected:
				result.Append (Highlight ("protected", GetThemeColor (modifierColor)));
				break;
			case Accessibility.Private:
				result.Append (Highlight ("private", GetThemeColor (modifierColor)));
				break;
			case Accessibility.Public:
				result.Append (Highlight ("public", GetThemeColor (modifierColor)));
				break;
			}
		}

		static int GetMarkupLength (string str)
		{
			int result = 0;
			bool inTag = false, inAbbrev = false;
			foreach (var ch in str) {
				switch (ch) {
				case '&':
					inAbbrev = true;
					break;
				case ';':
					if (!inAbbrev)
						goto default;
					inAbbrev = false;
					break;
				case '<':
					inTag = true;
					break;
				case '>':
					inTag = false;
					break;
				default:
					if (!inTag)
						result++;
					break;
				}
			}
			return result;
		}

		static bool IsObjectOrValueType (ITypeSymbol type)
		{
			return type != null && (type.SpecialType == SpecialType.System_Object || type.IsValueType);
		}

		string GetTypeParameterMarkup (ITypeSymbol t)
		{
			if (t == null)
				throw new ArgumentNullException ("t");
			var result = StringBuilderCache.Allocate ();
			var highlightedTypeName = Highlight (FilterEntityName (t.Name), GetThemeColor (userTypes));
			result.Append (highlightedTypeName);

			var color = AlphaBlend (SyntaxHighlightingService.GetColor (colorStyle, EditorThemeColors.Foreground), SyntaxHighlightingService.GetColor (colorStyle,EditorThemeColors.Background), optionalAlpha);
			var colorString = MonoDevelop.Components.HelperMethods.GetColorString (color);

			result.Append ("<span foreground=\"").Append (colorString).Append ("\">").Append (" (type parameter)</span>");
			var tp = t as ITypeParameterSymbol;
			if (tp != null) {
				if (!tp.HasConstructorConstraint && !tp.HasReferenceTypeConstraint && !tp.HasValueTypeConstraint && tp.ConstraintTypes.All (IsObjectOrValueType))
					return result.ToString ();
				result.AppendLine ();
				result.Append (Highlight (" where ", GetThemeColor (keywordOther)));
				result.Append (highlightedTypeName);
				result.Append (" : ");
				int constraints = 0;

				if (tp.HasReferenceTypeConstraint) {
					constraints++;
					result.Append (Highlight ("class", GetThemeColor (keywordDeclaration)));
				} else if (tp.HasValueTypeConstraint) {
					constraints++;
					result.Append (Highlight ("struct", GetThemeColor (keywordDeclaration)));
				}
				foreach (var bt in tp.ConstraintTypes) {
					if (!IsObjectOrValueType (bt)) {
						if (constraints > 0) {
							result.Append (",");
							if (constraints % 5 == 0) {
								result.AppendLine ();
								result.Append ("\t");
							}
						}
						constraints++;
						result.Append (GetTypeReferenceString (bt));
					}
				}
				if (tp.HasConstructorConstraint) {
					if (constraints > 0)
						result.Append (",");
					result.Append (Highlight ("new", GetThemeColor (keywordOperator)));
				}

			}
			return StringBuilderCache.ReturnAndFree (result);
		}

		string GetNullableMarkup (ITypeSymbol t)
		{
			return GetTypeReferenceString (t);
		}

		void AppendTypeParameterList (StringBuilder result, INamedTypeSymbol def)
		{
			var parameters = def.TypeParameters;
			//			if (def.ContainingType != null)
			//				parameters = parameters.Skip (def.DeclaringTypeDefinition.TypeParameterCount);
			AppendTypeParameters (result, parameters);
		}

		void AppendTypeArgumentList (StringBuilder result, INamedTypeSymbol def)
		{
			var parameters = def.TypeArguments;
			//			if (def.DeclaringType != null)
			//				parameters = parameters.Skip (def.DeclaringType.TypeParameterCount);
			AppendTypeParameters (result, parameters);
		}

		string GetTypeNameWithParameters (ITypeSymbol t)
		{
			StringBuilder result = StringBuilderCache.Allocate ();
			result.Append (Highlight (FilterEntityName (t.Name), GetThemeColor (userTypes)));
			var namedTypeSymbol = t as INamedTypeSymbol;
			if (namedTypeSymbol != null) {
				if (namedTypeSymbol.IsGenericType) {
					AppendTypeParameterList (result, namedTypeSymbol);
				} else if (namedTypeSymbol.IsUnboundGenericType) {
					AppendTypeArgumentList (result, namedTypeSymbol);
				}
			}
			return StringBuilderCache.ReturnAndFree (result);
		}

		public static bool IsNullableType (ITypeSymbol type)
		{
			var original = type.OriginalDefinition;
			return original.SpecialType == SpecialType.System_Nullable_T;
		}


		string GetTypeMarkup (ITypeSymbol t, bool includeDeclaringTypes = false)
		{
			if (t == null)
				throw new ArgumentNullException ("t");
			if (t.TypeKind == TypeKind.Error)
				return GettextCatalog.GetString ("Type can not be resolved.");
			if (t.TypeKind == TypeKind.Delegate)
				return GetDelegateMarkup ((INamedTypeSymbol)t);
			if (t.TypeKind == TypeKind.TypeParameter)
				return GetTypeParameterMarkup (t);
			if (t.TypeKind == TypeKind.Array || t.TypeKind == TypeKind.Pointer || t.IsTupleType)
				return GetTypeReferenceString (t);
			if (t.IsNullable ())
				return GetNullableMarkup (t);
			var result = StringBuilderCache.Allocate ();
			if (IsNullableType (t))
				AppendModifiers (result, t);

			switch (t.TypeKind) {
			case TypeKind.Class:
				result.Append (Highlight ("class ", GetThemeColor (keywordDeclaration)));
				break;
			case TypeKind.Interface:
				result.Append (Highlight ("interface ", GetThemeColor (keywordDeclaration)));
				break;
			case TypeKind.Struct:
				result.Append (Highlight ("struct ", GetThemeColor (keywordDeclaration)));
				break;
			case TypeKind.Enum:
				result.Append (Highlight ("enum ", GetThemeColor (keywordDeclaration)));
				break;
			}

			if (includeDeclaringTypes) {
				var typeNames = new List<string> ();
				var curType = t;
				while (curType != null) {
					typeNames.Add (GetTypeNameWithParameters (curType));
					curType = curType.ContainingType;
				}
				typeNames.Reverse ();
				result.Append (string.Join (".", typeNames));
			} else {
				result.Append (GetTypeNameWithParameters (t));
			}

			if (t.TypeKind == TypeKind.Array)
				return StringBuilderCache.ReturnAndFree (result);

			bool first = true;
			int maxLength = GetMarkupLength (result.ToString ());
			int length = maxLength;
			var sortedTypes = new List<INamedTypeSymbol> (t.Interfaces);

			sortedTypes.Sort ((x, y) => GetTypeReferenceString (y).Length.CompareTo (GetTypeReferenceString (x).Length));

			if (t.BaseType != null && t.BaseType.SpecialType != SpecialType.System_Object)
				sortedTypes.Insert (0, t.BaseType);

			if (t.TypeKind != TypeKind.Enum) {
				foreach (var directBaseType in sortedTypes) {
					if (first) {
						result.AppendLine (" :");
						result.Append ("  ");
						length = 2;
					} else { // 5.5. um 10:45
						result.Append (", ");
						length += 2;
					}
					var typeRef = GetTypeReferenceString (directBaseType, false);

					if (!first && length + typeRef.Length >= maxLength) {
						result.AppendLine ();
						result.Append ("  ");
						length = 2;
					}

					result.Append (typeRef);
					length += GetMarkupLength (typeRef);
					first = false;
				}
			} else {
				var enumBase = t.BaseType;
				if (enumBase.SpecialType != SpecialType.System_Int32) {
					result.AppendLine (" :");
					result.Append ("  ");
					result.Append (GetTypeReferenceString (enumBase, false));
				}
			}

			return StringBuilderCache.ReturnAndFree (result);
		}

		void AppendTypeParameters (StringBuilder result, ImmutableArray<ITypeParameterSymbol> typeParameters)
		{
			if (!typeParameters.Any ())
				return;
			result.Append ("&lt;");
			int i = 0;
			foreach (var typeParameter in typeParameters) {
				if (i > 0) {
					if (i % 5 == 0) {
						result.AppendLine (",");
						result.Append ("\t");
					} else {
						result.Append (", ");
					}
				}
				AppendVariance (result, typeParameter.Variance);
				result.Append (HighlightSemantically (CSharpAmbience.NetToCSharpTypeName (typeParameter.Name), GetThemeColor (userTypes)));
				i++;
			}
			result.Append ("&gt;");
		}

		void AppendTypeParameters (StringBuilder result, ImmutableArray<ITypeSymbol> typeParameters)
		{
			if (!typeParameters.Any ())
				return;
			result.Append ("&lt;");
			int i = 0;
			foreach (var typeParameter in typeParameters) {
				if (i > 0) {
					if (i % 5 == 0) {
						result.AppendLine (",");
						result.Append ("\t");
					} else {
						result.Append (", ");
					}
				}
				if (typeParameter is ITypeParameterSymbol)
					AppendVariance (result, ((ITypeParameterSymbol)typeParameter).Variance);
				result.Append (GetTypeReferenceString (typeParameter, false));
				i++;
			}
			result.Append ("&gt;");
		}

		static string FilterEntityName (string name)
		{
			return MonoDevelop.Ide.TypeSystem.Ambience.EscapeText (CSharpAmbience.FilterName (name));
		}

		public string GetDelegateInfo (ITypeSymbol type)
		{
			if (type == null)
				throw new ArgumentNullException ("type");
			var t = type;

			var result = StringBuilderCache.Allocate ();

			var method = t.GetDelegateInvokeMethod ();
			result.Append (GetTypeReferenceString (method.ReturnType));
			if (BreakLineAfterReturnType) {
				result.AppendLine ();
			} else {
				result.Append (" ");
			}


			result.Append (FilterEntityName (t.Name));

			AppendTypeParameters (result, method.TypeParameters);

			// TODO:
			//			if (document.GetOptionSet ().GetOption (CSharpFormattingOptions.SpaceBeforeDelegateDeclarationParentheses))
			//				result.Append (" ");

			result.Append ('(');
			AppendParameterList (
				result,
				method.Parameters,
				false /* formattingOptions.SpaceBeforeDelegateDeclarationParameterComma */,
				true /* formattingOptions.SpaceAfterDelegateDeclarationParameterComma*/,
				false
			);
			result.Append (')');
			return StringBuilderCache.ReturnAndFree (result);
		}

		string GetDelegateMarkup (INamedTypeSymbol delegateType)
		{
			var result = StringBuilderCache.Allocate ();
			var type = delegateType.IsUnboundGenericType ? delegateType.OriginalDefinition : delegateType;
			var method = type.GetDelegateInvokeMethod ();

			AppendModifiers (result, type);
			result.Append (Highlight ("delegate ", GetThemeColor (keywordDeclaration)));
			if (method != null)
				result.Append (GetTypeReferenceString (method.ReturnType));
			if (BreakLineAfterReturnType) {
				result.AppendLine ();
			} else {
				result.Append (" ");
			}


			result.Append (FilterEntityName (type.Name));

			if (type.TypeArguments.Length > 0) {
				AppendTypeArgumentList (result, type);
			} else {
				AppendTypeParameterList (result, type);
			}
			//  TODO
			//			if (formattingOptions.SpaceBeforeMethodDeclarationParameterComma)
			//				result.Append (" ");

			result.Append ('(');
			AppendParameterList (
				result,
				method.Parameters,
				false /* formattingOptions.SpaceBeforeDelegateDeclarationParameterComma */,
				false /* formattingOptions.SpaceAfterDelegateDeclarationParameterComma */);
			result.Append (')');
			return StringBuilderCache.ReturnAndFree (result);
		}

		string GetLocalVariableMarkup (ILocalSymbol local)
		{
			if (local == null)
				throw new ArgumentNullException ("local");

			var result = StringBuilderCache.Allocate ();

			if (local.IsConst)
				result.Append (Highlight ("const ", GetThemeColor (modifierColor)));

			result.Append (GetTypeReferenceString (local.Type));
			if (BreakLineAfterReturnType) {
				result.AppendLine ();
			} else {
				result.Append (" ");
			}

			result.Append (FilterEntityName (local.Name));

			if (local.IsConst) {
				if (options.GetOption (CSharpFormattingOptions.SpacingAroundBinaryOperator) == BinaryOperatorSpacingOptions.Single) {
					result.Append (" = ");
				} else {
					result.Append ("=");
				}
				AppendConstant (result, local.Type, local.ConstantValue);
			}

			return StringBuilderCache.ReturnAndFree (result);
		}

		string GetParameterVariableMarkup (IParameterSymbol parameter)
		{
			if (parameter == null)
				throw new ArgumentNullException ("parameter");

			var result = StringBuilderCache.Allocate ();
			AppendParameter (result, parameter);

			if (parameter.HasExplicitDefaultValue) {
				if (options.GetOption (CSharpFormattingOptions.SpacingAroundBinaryOperator) == BinaryOperatorSpacingOptions.Single) {
					result.Append (" = ");
				} else {
					result.Append ("=");
				}
				AppendConstant (result, parameter.Type, parameter.ExplicitDefaultValue);
			}

			return StringBuilderCache.ReturnAndFree (result);
		}


		string GetFieldMarkup (IFieldSymbol field)
		{
			if (field == null)
				throw new ArgumentNullException ("field");

			var result = StringBuilderCache.Allocate ();
			bool isEnum = field.ContainingType.TypeKind == TypeKind.Enum;
			if (!isEnum) {
				AppendModifiers (result, field);
				result.Append (GetTypeReferenceString (field.Type));
			} else {
				result.Append (GetTypeReferenceString (field.ContainingType));
			}
			if (BreakLineAfterReturnType) {
				result.AppendLine ();
			} else {
				result.Append (" ");
			}

			result.Append (HighlightSemantically (FilterEntityName (field.Name), GetThemeColor ("entity.name.field.source.cs")));

			//			if (field.IsFixed) {
			//				if (formattingOptions.SpaceBeforeArrayDeclarationBrackets) {
			//					result.Append (" [");
			//				} else {
			//					result.Append ("[");
			//				}
			//				if (formattingOptions.SpacesWithinBrackets)
			//					result.Append (" ");
			//				AppendConstant (result, field.Type, field.ConstantValue);
			//				if (formattingOptions.SpacesWithinBrackets)
			//					result.Append (" ");
			//				result.Append ("]");
			//			} else 

			if (field.IsConst) {
				if (isEnum && !(field.ContainingType.GetAttributes ().Any ((AttributeData attr) => attr.AttributeClass.Name == "FlagsAttribute" && attr.AttributeClass.ContainingNamespace.Name == "System"))) {
					return result.ToString ();
				}
				if (options.GetOption (CSharpFormattingOptions.SpacingAroundBinaryOperator) == BinaryOperatorSpacingOptions.Single) {
					result.Append (" = ");
				} else {
					result.Append ("=");
				}
				AppendConstant (result, field.Type, field.ConstantValue, isEnum);
			}

			return StringBuilderCache.ReturnAndFree (result);
		}

		string GetMethodMarkup (IMethodSymbol method)
		{
			if (method == null)
				throw new ArgumentNullException ("method");

			var result = StringBuilderCache.Allocate ();
			AppendModifiers (result, method);
			result.Append (GetTypeReferenceString (method.ReturnType));
			if (BreakLineAfterReturnType) {
				result.AppendLine ();
			} else {
				result.Append (" ");
			}

			AppendExplicitInterfaces (result, method.ExplicitInterfaceImplementations.Cast<ISymbol> ());

			if (method.MethodKind == MethodKind.BuiltinOperator || method.MethodKind == MethodKind.UserDefinedOperator) {
				result.Append ("operator ");
				result.Append (CSharpAmbience.GetOperator (method.Name));
			} else {
				result.Append (HighlightSemantically (FilterEntityName (method.Name), GetThemeColor ("entity.name.function.source.cs")));
			}
			if (method.TypeArguments.Length > 0) {
				result.Append ("&lt;");
				for (int i = 0; i < method.TypeArguments.Length; i++) {
					if (i > 0)
						result.Append (", ");
					result.Append (HighlightSemantically (GetTypeReferenceString (method.TypeArguments [i], false), GetThemeColor (userTypes)));
				}
				result.Append ("&gt;");
			} else {
				AppendTypeParameters (result, method.TypeParameters);
			}
			// TODO!
			//			if (formattingOptions.SpaceBeforeMethodDeclarationParentheses)
			//				result.Append (" ");

			result.Append ('(');
			var parameters = method.Parameters;
			AppendParameterList (result, parameters,
				false /* formattingOptions.SpaceBeforeMethodDeclarationParameterComma*/,
				false /* formattingOptions.SpaceAfterMethodDeclarationParameterComma*/);
			result.Append (')');
			return StringBuilderCache.ReturnAndFree (result);
		}

		string GetConstructorMarkup (IMethodSymbol method)
		{
			if (method == null)
				throw new ArgumentNullException ("method");


			var result = StringBuilderCache.Allocate ();
			AppendModifiers (result, method);

			result.Append (FilterEntityName (method.ContainingType.Name));
			//
			//			if (formattingOptions.SpaceBeforeConstructorDeclarationParentheses)
			//				result.Append (" ");

			result.Append ('(');
			if (method.ContainingType.TypeKind == TypeKind.Delegate) {
				result.Append (Highlight ("delegate", GetThemeColor (keywordDeclaration))).Append (" (");
				AppendParameterList (result, method.ContainingType.GetDelegateInvokeMethod ().Parameters,
					false /* formattingOptions.SpaceBeforeConstructorDeclarationParameterComma */,
					false /* formattingOptions.SpaceAfterConstructorDeclarationParameterComma */);
				result.Append (")");
			} else {
				AppendParameterList (result, method.Parameters,
					false /* formattingOptions.SpaceBeforeConstructorDeclarationParameterComma */,
					false /* formattingOptions.SpaceAfterConstructorDeclarationParameterComma */);
			}
			result.Append (')');
			return StringBuilderCache.ReturnAndFree (result);
		}

		string GetDestructorMarkup (IMethodSymbol method)
		{
			if (method == null)
				throw new ArgumentNullException ("method");

			var result = StringBuilderCache.Allocate ();
			AppendModifiers (result, method);
			if (BreakLineAfterReturnType) {
				result.AppendLine ();
			} else {
				result.Append (" ");
			}

			result.Append ("~");
			result.Append (FilterEntityName (method.ContainingType.Name));

			//			if (formattingOptions.SpaceBeforeConstructorDeclarationParentheses)
			//				result.Append (" ");

			result.Append ('(');
			AppendParameterList (result, method.Parameters,
				false /* formattingOptions.SpaceBeforeConstructorDeclarationParameterComma */,
				false /* formattingOptions.SpaceAfterConstructorDeclarationParameterComma */);
			result.Append (')');
			return StringBuilderCache.ReturnAndFree (result);
		}

		bool IsAccessibleOrHasSourceCode (ISymbol entity)
		{
			if (entity.DeclaredAccessibility == Accessibility.Public)
				return true;
			return entity.IsDefinedInSource ();
			//			if (!entity.Region.Begin.IsEmpty)
			//				return true;
			//			var lookup = new MemberLookup (resolver.CurrentTypeDefinition, resolver.Compilation.MainAssembly);
			//			return lookup.IsAccessible (entity, false);
		}

		string GetPropertyMarkup (IPropertySymbol property)
		{
			if (property == null)
				throw new ArgumentNullException ("property");
			var result = StringBuilderCache.Allocate ();
			AppendModifiers (result, property);
			result.Append (GetTypeReferenceString (property.Type));
			if (BreakLineAfterReturnType) {
				result.AppendLine ();
			} else {
				result.Append (" ");
			}

			AppendExplicitInterfaces (result, property.ExplicitInterfaceImplementations.Cast<ISymbol> ());

			if (property.IsIndexer) {
				result.Append (Highlight ("this", GetThemeColor (keywordOther)));
			} else {
				result.Append (HighlightSemantically (FilterEntityName (property.Name), GetThemeColor ("entity.name.property.source.cs")));
			}

			if (property.Parameters.Length > 0) {
				//				if (formattingOptions.SpaceBeforeIndexerDeclarationBracket)
				//					result.Append (" ");
				result.Append ("[");
				AppendParameterList (result, property.Parameters,
					false /*formattingOptions.SpaceBeforeIndexerDeclarationParameterComma*/,
					false /*formattingOptions.SpaceAfterIndexerDeclarationParameterComma*/);
				result.Append ("]");
			}

			result.Append (" {");
			if (property.GetMethod != null && IsAccessibleOrHasSourceCode (property.GetMethod)) {
				if (property.GetMethod.DeclaredAccessibility != property.DeclaredAccessibility) {

					result.Append (" ");
					AppendAccessibility (result, property.GetMethod);
				}
				result.Append (Highlight (" get", GetThemeColor (keywordOther))).Append (";");
			}

			if (property.SetMethod != null && IsAccessibleOrHasSourceCode (property.SetMethod)) {
				if (property.SetMethod.DeclaredAccessibility != property.DeclaredAccessibility) {
					result.Append (" ");
					AppendAccessibility (result, property.SetMethod);
				}
				result.Append (Highlight (" set", GetThemeColor (keywordOther))).Append (";");
			}
			result.Append (" }");

			return StringBuilderCache.ReturnAndFree (result);
		}


		public TooltipInformation GetExternAliasTooltip (ExternAliasDirectiveSyntax externAliasDeclaration, DotNetProject project)
		{
			var result = new TooltipInformation ();
			result.SignatureMarkup = Highlight ("extern ", GetThemeColor (modifierColor)) + Highlight ("alias ", GetThemeColor (keywordOther)) + externAliasDeclaration.Identifier;
			if (project == null)
				return result;
			foreach (var r in project.References) {
				if (string.IsNullOrEmpty (r.Aliases))
					continue;
				foreach (var alias in r.Aliases.Split (',', ';')) {
					if (alias == externAliasDeclaration.Identifier.ToFullString ())
						result.AddCategory (GettextCatalog.GetString ("Reference"), r.StoredReference);
				}
			}

			return result;
		}

		public TooltipInformation GetKeywordTooltip (SyntaxToken node)
		{
			var result = new TooltipInformation ();

			var color = AlphaBlend (SyntaxHighlightingService.GetColor (colorStyle, EditorThemeColors.Foreground), SyntaxHighlightingService.GetColor (colorStyle, EditorThemeColors.Background), optionalAlpha);
			var colorString = MonoDevelop.Components.HelperMethods.GetColorString (color);

			var keywordSign = "<span foreground=\"" + colorString + "\"> " + GettextCatalog.GetString ("(keyword)") + "</span>";

			switch (node.Kind ()) {
			case SyntaxKind.AbstractKeyword:
				result.SignatureMarkup = Highlight ("abstract", GetThemeColor (modifierColor)) + keywordSign;
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} modifier can be used with classes, methods, properties, indexers, and events.", Highlight ("abstract", GetThemeColor (modifierColor)));
				break;
			case SyntaxKind.AddKeyword:
				result.SignatureMarkup = Highlight ("add", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("[modifiers] {0} {{ accessor-body }}", Highlight ("add", GetThemeColor (keywordOther))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} keyword is used to define a custom accessor for when an event is subscribed to. If supplied, a remove accessor must also be supplied.", Highlight ("add", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.AscendingKeyword:
				result.SignatureMarkup = Highlight ("ascending", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Query Form"), GettextCatalog.GetString ("{0} ordering-statement {1}", Highlight ("orderby", GetThemeColor (keywordOther)), Highlight ("ascending", GetThemeColor (keywordOther))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} keyword is used to set the sorting order from smallest to largest in a query expression. This is the default behaviour.", Highlight ("ascending", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.AsyncKeyword:
				result.SignatureMarkup = Highlight ("async", GetThemeColor (keywordOther)) + keywordSign;
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} modifier is used to specify that a class method, anonymous method, or lambda expression is asynchronous.", Highlight ("async", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.AsKeyword:
				result.SignatureMarkup = Highlight ("as", GetThemeColor (keywordOperator)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("expression {0} type", Highlight ("as", GetThemeColor (keywordOperator))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} operator is used to perform conversions between compatible types.", Highlight ("as", GetThemeColor (keywordOperator)));
				break;
			case SyntaxKind.AwaitKeyword:
				result.SignatureMarkup = Highlight ("await", GetThemeColor (keywordOther)) + keywordSign;
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} operator is used to specify that an {1} method is to have its execution suspended until the {0} task has completed.", Highlight ("await", GetThemeColor (keywordOther)), Highlight ("async", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.BaseKeyword:
				result.SignatureMarkup = Highlight ("base", GetThemeColor (keywordOther)) + keywordSign;
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} keyword is used to access members of the base class from within a derived class.", Highlight ("base", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.BreakKeyword:
				result.SignatureMarkup = Highlight ("break", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), Highlight ("break", GetThemeColor (keywordOther)) + ";");
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} statement terminates the closest enclosing loop or switch statement in which it appears.", Highlight ("break", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.CaseKeyword:
				result.SignatureMarkup = Highlight ("case", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), 
				                    GettextCatalog.GetString ("{0} constant-expression:\n  statement\n  jump-statement", Highlight ("case", GetThemeColor (keywordOther))));
				result.SummaryMarkup = "";
				break;
			case SyntaxKind.CatchKeyword:
				result.SignatureMarkup = Highlight ("catch", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("{0} try-block\n  {1} (exception-declaration-1) catch-block-1\n  {1} (exception-declaration-2) catch-block-2\n  ...\n{0} try-block {1} catch-block", Highlight ("try", GetThemeColor (keywordOther)), Highlight ("catch", GetThemeColor (keywordOther))));
				result.SummaryMarkup = "";
				break;
			case SyntaxKind.CheckedKeyword:
				result.SignatureMarkup = Highlight ("checked", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("{0} block\nor\n{0} (expression)", Highlight ("checked", GetThemeColor (keywordOther))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} keyword is used to control the overflow-checking context for integral-type arithmetic operations and conversions. It can be used as an operator or a statement.", Highlight ("checked", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.ClassKeyword:
				result.SignatureMarkup = Highlight ("class", GetThemeColor (keywordDeclaration)) + keywordSign;
				if (node.Parent != null && node.Parent.IsKind (SyntaxKind.ConstructorConstraint)) {
					result.SummaryMarkup = GettextCatalog.GetString ("The {0} constraint specifies that the type argument must be a reference type; this applies also to any class, interface, delegate, or array type.", Highlight ("class", GetThemeColor (keywordDeclaration)));
				} else {
					result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("[attributes] [modifiers] {0} identifier [:base-list] {{ class-body }}[;]", Highlight ("class", GetThemeColor (keywordDeclaration))));
					result.SummaryMarkup = GettextCatalog.GetString ("Classes are declared using the keyword {0}.", Highlight ("class", GetThemeColor (keywordDeclaration)));
				}
				break;
			case SyntaxKind.ConstKeyword:
				result.SignatureMarkup = Highlight ("const", GetThemeColor (modifierColor)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("[attributes] [modifiers] {0} type declarators;", Highlight ("const", GetThemeColor (modifierColor))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} keyword is used to modify a declaration of a field or local variable. It specifies that the value of the field or the local variable cannot be modified.", Highlight ("const", GetThemeColor (modifierColor)));
				break;
			case SyntaxKind.ContinueKeyword:
				result.SignatureMarkup = Highlight ("continue", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), Highlight ("continue", GetThemeColor (keywordOther)) + ";");
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} statement passes control to the next iteration of the enclosing iteration statement in which it appears.", Highlight ("continue", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.DefaultKeyword:
				result.SignatureMarkup = Highlight ("default", GetThemeColor (keywordOther)) + keywordSign;
				result.SummaryMarkup = "";
				if (node.Parent != null) {
					if (node.Parent is DefaultExpressionSyntax) {
						result.AddCategory (GettextCatalog.GetString ("Form"),
						                    GettextCatalog.GetString ("{0} (Type)", Highlight ("default", GetThemeColor (keywordOther))));
						break;
					} else if (node.Parent is SwitchStatementSyntax) {
						result.AddCategory (GettextCatalog.GetString ("Form"),
						                    GettextCatalog.GetString ("{0} (expression) { \n  {1} constant-expression:\n    statement\n    jump-statement\n  [{2}:\n    statement\n    jump-statement]\n}",
						                    Highlight ("switch", GetThemeColor (keywordOther)), Highlight ("case", GetThemeColor (keywordOther)), Highlight ("default", GetThemeColor (keywordOther))));
						break;
					}
				}
				result.AddCategory (GettextCatalog.GetString ("Form"),
						            GettextCatalog.GetString ("{0} (Type)\n\nor\n\n{1} (expression) { \n  {2} constant-expression:\n    statement\n    jump-statement\n  [{3}:\n    statement\n    jump-statement]\n}", 
						                                      Highlight ("default", GetThemeColor (keywordOther)), Highlight ("switch", GetThemeColor (keywordOther)), Highlight ("case", GetThemeColor (keywordOther)), Highlight ("default", GetThemeColor (keywordOther)))
						           );
				break;
			case SyntaxKind.DelegateKeyword:
				result.SignatureMarkup = Highlight ("delegate", GetThemeColor (keywordDeclaration)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("[attributes] [modifiers] {0} result-type identifier ([formal-parameters]);", Highlight ("delegate", GetThemeColor (keywordDeclaration))));
				result.SummaryMarkup = GettextCatalog.GetString ("A {0} declaration defines a reference type that can be used to encapsulate a method with a specific signature.", Highlight ("delegate", GetThemeColor (keywordDeclaration)));
				break;
			case SyntaxKind.IdentifierToken:
				if (node.ToFullString () == "nameof" && node.Parent?.Parent?.Kind () == SyntaxKind.InvocationExpression)
					goto case SyntaxKind.NameOfKeyword;

				if (node.ToFullString () == "dynamic") {
					result.SignatureMarkup = Highlight ("dynamic", GetThemeColor (keywordOther)) + keywordSign;
					result.SummaryMarkup = GettextCatalog.GetString ("The {0} type allows for an object to bypass compile-time type checking and resolve type checking during run-time.", Highlight ("dynamic", GetThemeColor (keywordOther)));
				} else {
					return null;
				}
				break;
			case SyntaxKind.DescendingKeyword:
				result.SignatureMarkup = Highlight ("descending", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Query Form"), GettextCatalog.GetString ("{0} ordering-statement {1}", Highlight ("orderby", GetThemeColor (keywordOther)), Highlight ("descending", GetThemeColor (keywordOther))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} keyword is used to set the sorting order from largest to smallest in a query expression.", Highlight ("descending", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.DoKeyword:
				result.SignatureMarkup = Highlight ("do", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("{0} statement {1} (expression);", Highlight ("do", GetThemeColor (keywordOther)), Highlight ("while", GetThemeColor (keywordOther))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} statement executes a statement or a block of statements repeatedly until a specified expression evaluates to false.", Highlight ("do", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.ElseKeyword:
				result.SignatureMarkup = Highlight ("else", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("{0} (expression)\n  statement1\n  [{1}\n  statement2]", Highlight ("if", GetThemeColor (keywordOther)), Highlight ("else", GetThemeColor (keywordOther))));
				result.SummaryMarkup = "";
				break;
			case SyntaxKind.EnumKeyword:
				result.SignatureMarkup = Highlight ("enum", GetThemeColor (keywordDeclaration)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("[attributes] [modifiers] {0} identifier [:base-type] {{ enumerator-list }} [;]", Highlight ("enum", GetThemeColor (keywordDeclaration))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} keyword is used to declare an enumeration, a distinct type consisting of a set of named constants called the enumerator list.", Highlight ("enum", GetThemeColor (keywordDeclaration)));
				break;
			case SyntaxKind.EventKeyword:
				result.SignatureMarkup = Highlight ("event", GetThemeColor (modifierColor)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("[attributes] [modifiers] {0} type declarator;\n[attributes] [modifiers] {0} type member-name {{ accessor-declarations }};", Highlight ("event", GetThemeColor (modifierColor))));
				result.SummaryMarkup = GettextCatalog.GetString ("Specifies an event.");
				break;
			case SyntaxKind.ExplicitKeyword:
				result.SignatureMarkup = Highlight ("explicit", GetThemeColor (keywordOther)) + keywordSign;
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} keyword is used to declare an explicit user-defined type conversion operator.", Highlight ("explicit", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.ExternKeyword:
				result.SignatureMarkup = Highlight ("extern", GetThemeColor (modifierColor)) + keywordSign;
				result.SummaryMarkup = GettextCatalog.GetString ("Use the {0} modifier in a method declaration to indicate that the method is implemented externally. A common use of the extern modifier is with the DllImport attribute.", Highlight ("extern", GetThemeColor (modifierColor)));
				break;
			case SyntaxKind.FinallyKeyword:
				result.SignatureMarkup = Highlight ("finally", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("{0} try-block {1} finally-block",Highlight ("try", GetThemeColor (keywordOther)), Highlight ("finally", GetThemeColor (keywordOther))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} block is useful for cleaning up any resources allocated in the try block. Control is always passed to the finally block regardless of how the try block exits.", Highlight ("finally", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.FixedKeyword:
				result.SignatureMarkup = Highlight ("fixed", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("{0} ( type* ptr = expr ) statement", Highlight ("fixed", GetThemeColor (keywordOther))));
				result.SummaryMarkup = GettextCatalog.GetString ("Prevents relocation of a variable by the garbage collector.");
				break;
			case SyntaxKind.ForKeyword:
				result.SignatureMarkup = Highlight ("for", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("{0} ([initializers]; [expression]; [iterators]) statement", Highlight ("for", GetThemeColor (keywordOther))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} loop executes a statement or a block of statements repeatedly until a specified expression evaluates to false.", Highlight ("for", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.ForEachKeyword:
				result.SignatureMarkup = Highlight ("foreach", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), 
				                    GettextCatalog.GetString ("{0} (type identifier {1} expression) statement", Highlight ("foreach", GetThemeColor (keywordOther)), Highlight ("in", GetThemeColor (keywordOther))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} statement repeats a group of embedded statements for each element in an array or an object collection.", Highlight ("foreach", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.FromKeyword:
				result.SignatureMarkup = Highlight ("from", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("{0} range-variable {1} data-source [query clauses] {2} product-expression", Highlight ("from", GetThemeColor (keywordOther)), Highlight ("in", GetThemeColor (keywordOther)), Highlight ("select", GetThemeColor (keywordOther))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} keyword marks the beginning of a query expression and defines the data source and local variable to represent the elements in the sequence.", Highlight ("from", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.GetKeyword:
				result.SignatureMarkup = Highlight ("get", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("[modifiers] {0} [ {{ accessor-body }} ]", Highlight ("get", GetThemeColor (keywordOther))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} keyword is used to define an accessor method to retrieve the value of the property or indexer element.", Highlight ("get", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.GlobalKeyword:
				result.SignatureMarkup = Highlight ("global", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("{0} :: type", Highlight ("global", GetThemeColor (keywordOther))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} keyword is used to specify a type is within the global namespace.", Highlight ("global", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.GotoKeyword:
				result.SignatureMarkup = Highlight ("goto", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (
					GettextCatalog.GetString ("Form"), 
					GettextCatalog.GetString ("{0} identifier;\n{0} {1} constant-expression;\n {0} {2};", Highlight ("goto", GetThemeColor (keywordOther)), Highlight ("case", GetThemeColor (keywordOther)), Highlight ("default", GetThemeColor (keywordOther))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} statement transfers the program control directly to a labeled statement. ", Highlight ("goto", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.GroupKeyword:
				result.SignatureMarkup = Highlight ("group", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Query Form"), 
				                    GettextCatalog.GetString ("{0} range-variable {1} key-value\n\nor\n\n{2} range-variable {1} key-value {3} group-name ", Highlight ("group", GetThemeColor (keywordOther)), Highlight ("by", GetThemeColor (keywordOther)), Highlight ("group", GetThemeColor (keywordOther)), Highlight ("into", GetThemeColor (keywordOther))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} keyword groups elements together from a query which match the key value and stores the result in an {1}. It can also be stored in a group for further use in the query with 'into'.", Highlight ("group", GetThemeColor (keywordOther)), Highlight ("IGrouping&lt;TKey, TElement&gt;", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.IfKeyword:
				result.SignatureMarkup = Highlight ("if", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), 
				                    GettextCatalog.GetString ("{0} (expression)\n  statement1\n  [{1}\n  statement2]", Highlight ("if", GetThemeColor (keywordOther)), Highlight ("else", GetThemeColor (keywordOther))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} statement selects a statement for execution based on the value of a Boolean expression.", Highlight ("if", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.IntoKeyword:
				result.SignatureMarkup = Highlight ("into", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Query Form"), GettextCatalog.GetString ("{0} range-variable {1} key-value {2} group-name ", Highlight ("group", GetThemeColor (keywordOther)), Highlight ("by", GetThemeColor (keywordOther)), Highlight ("into", GetThemeColor (keywordOther))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} keyword stores the result of a group statement for further use in the query.", Highlight ("into", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.ImplicitKeyword:
				result.SignatureMarkup = Highlight ("implicit", GetThemeColor (keywordOther)) + keywordSign;
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} keyword is used to declare an implicit user-defined type conversion operator.", Highlight ("implicit", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.InKeyword:
				result.SignatureMarkup = Highlight ("in", GetThemeColor (keywordOther)) + keywordSign;
				if (node.Parent != null) {
					if (node.Parent is ForEachStatementSyntax) {
						result.AddCategory (GettextCatalog.GetString ("Form"),
						                    GettextCatalog.GetString ("{0} (type identifier {1} expression) statement", Highlight ("foreach", GetThemeColor (keywordOther)), Highlight ("in", GetThemeColor (keywordOther))));
						break;
					}
					if (node.Parent is FromClauseSyntax) {
						result.AddCategory (GettextCatalog.GetString ("Form"),
						                    GettextCatalog.GetString ("{0} range-variable {1} data-source [query clauses] {2} product-expression", Highlight ("from", GetThemeColor (keywordOther)), Highlight ("in", GetThemeColor (keywordOther)), Highlight ("select", GetThemeColor (keywordOther))));
						break;
					}
					if (node.Parent is TypeParameterConstraintClauseSyntax) {
						result.AddCategory (GettextCatalog.GetString ("Form"),
						                    GettextCatalog.GetString ("{0} IMyInterface&lt; {1} T&gt; {}", Highlight ("interface", GetThemeColor (keywordDeclaration)), Highlight ("in", GetThemeColor (keywordOther))));
						break;
					}
				}
				result.AddCategory (GettextCatalog.GetString ("Form"),
				                    GettextCatalog.GetString ("{0} (type identifier {1} expression) statement\n\nor\n\n{0} range-variable {1} data-source [query clauses] {2} product-expression\n\nor\n\n{3} IMyInterface&lt;{1} T&gt; {{}}",
															 Highlight ("foreach", GetThemeColor (keywordOther)), Highlight ("in", GetThemeColor (keywordOther)), Highlight ("select", GetThemeColor (keywordOther)), Highlight ("interface", GetThemeColor (keywordDeclaration)))
				);
				break;
			case SyntaxKind.InterfaceKeyword:
				result.SignatureMarkup = Highlight ("interface", GetThemeColor (keywordDeclaration)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("[attributes] [modifiers] {0} identifier [:base-list] {{interface-body}}[;]", Highlight ("interface", GetThemeColor (keywordDeclaration))));
				result.SummaryMarkup = GettextCatalog.GetString ("An interface defines a contract. A class or struct that implements an interface must adhere to its contract.");
				break;
			case SyntaxKind.InternalKeyword:
				result.SignatureMarkup = Highlight ("internal", GetThemeColor (modifierColor)) + keywordSign;
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} keyword is an access modifier for types and type members. Internal members are accessible only within files in the same assembly.", Highlight ("internal", GetThemeColor (modifierColor)));
				break;
			case SyntaxKind.IsKeyword:
				result.SignatureMarkup = Highlight ("is", GetThemeColor (keywordOperator)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("expression {0} type", Highlight ("is", GetThemeColor (keywordOperator))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} operator is used to check whether the run-time type of an object is compatible with a given type.", Highlight ("is", GetThemeColor (keywordOperator)));
				break;
			case SyntaxKind.JoinKeyword:
				result.SignatureMarkup = Highlight ("join", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Query Form"), GettextCatalog.GetString ("{0} range-variable2 {1} range2 {2} statement1 {3} statement2 [ {4} group-name ]", 
				                                                                                       Highlight ("join", GetThemeColor (keywordOther)), Highlight ("in", GetThemeColor (keywordOther)), Highlight ("on", GetThemeColor (keywordOther)), Highlight ("equals", GetThemeColor (keywordOther)), Highlight ("into", GetThemeColor (keywordOther))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} clause produces a new sequence of elements from two source sequences on a given equality condition.", Highlight ("join", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.LetKeyword:
				result.SignatureMarkup = Highlight ("let", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Query Form"), GettextCatalog.GetString ("{0} range-variable = expression", Highlight ("let", GetThemeColor (keywordOther))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} clause allows for a sub-expression to have its value stored in a new range variable for use later in the query.", Highlight ("let", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.LockKeyword:
				result.SignatureMarkup = Highlight ("lock", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("{0} (expression) statement_block", Highlight ("lock", GetThemeColor (keywordOther))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} keyword marks a statement block as a critical section by obtaining the mutual-exclusion lock for a given object, executing a statement, and then releasing the lock.", Highlight ("lock", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.NamespaceKeyword:
				result.SignatureMarkup = Highlight ("namespace", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("{0} name[.name1] ...] {{\ntype-declarations\n }}", Highlight ("namespace", GetThemeColor (keywordOther))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} keyword is used to declare a scope.", Highlight ("namespace", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.NewKeyword:
				result.SignatureMarkup = Highlight ("new", GetThemeColor (keywordOperator)) + keywordSign;
				if (node.Parent != null && node.Parent.IsKind (SyntaxKind.ConstructorConstraint)) {
					result.SummaryMarkup = GettextCatalog.GetString ("The {0} constraint specifies that any type argument in a generic class declaration must have a public parameterless constructor. To use the new constraint, the type cannot be abstract.", Highlight ("new", GetThemeColor (keywordOperator)));
				} else {
					result.SummaryMarkup = GettextCatalog.GetString ("The {0} keyword can be used as an operator or as a modifier. The operator is used to create objects on the heap and invoke constructors. The modifier is used to hide an inherited member from a base class member.", Highlight ("new", GetThemeColor (keywordOperator)));
				}
				break;
			case SyntaxKind.NullKeyword:
				result.SignatureMarkup = Highlight ("null", GetThemeColor (keywordConstant)) + keywordSign;
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} keyword is a literal that represents a null reference, one that does not refer to any object. {0} is the default value of reference-type variables.", Highlight ("null", GetThemeColor (keywordConstant)));
				break;
			case SyntaxKind.OperatorKeyword:
				result.SignatureMarkup = Highlight ("operator", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"),
									GettextCatalog.GetString ("{0} result-type {1} unary-operator ( op-type operand )\n{0} result-type {1} binary-operator (\nop-type operand,\nop-type2 operand2\n )\n{0} {2} {1} conv-type-out ( conv-type-in operand )\n{0} {3} {1} conv-type-out ( conv-type-in operand )",
															  Highlight ("public static", GetThemeColor (modifierColor)), Highlight ("operator", GetThemeColor (keywordOther)), Highlight ("implicit", GetThemeColor (keywordOther)), Highlight ("explicit", GetThemeColor (keywordOther)))
				                   );
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} keyword is used to declare an operator in a class or struct declaration.", Highlight ("operator", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.OrderByKeyword:
				result.SignatureMarkup = Highlight ("orderby", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Query Form"), 
				                    GettextCatalog.GetString ("{0} order-key1 [ {1}|{2}, [order-key2, ...]", Highlight ("orderby", GetThemeColor (keywordOther)), Highlight ("ascending", GetThemeColor (keywordOther)), Highlight ("descending", GetThemeColor (keywordOther))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} clause specifies for the returned sequence to be sorted on a given element in either ascending or descending order.", Highlight ("orderby", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.OutKeyword:
				result.SignatureMarkup = Highlight ("out", GetThemeColor (keywordOther)) + keywordSign;
				if (node.Parent != null) {
					if (node.Parent is TypeParameterSyntax) {
						result.AddCategory (GettextCatalog.GetString ("Form"),
						                    GettextCatalog.GetString ("{0} IMyInterface&lt;{1} T&gt; {}", Highlight ("interface", GetThemeColor (keywordDeclaration)), Highlight ("out", GetThemeColor (keywordOther))));
						break;
					}
					if (node.Parent is ParameterSyntax) {
						result.AddCategory (GettextCatalog.GetString ("Form"),
						                    GettextCatalog.GetString ("{0} parameter-name", Highlight ("out", GetThemeColor (keywordOther))));
						result.SummaryMarkup = GettextCatalog.GetString ("The {0} method parameter keyword on a method parameter causes a method to refer to the same variable that was passed into the method.", Highlight ("out", GetThemeColor (keywordOther)));
						break;
					}
				}

				result.AddCategory (GettextCatalog.GetString ("Form"),
				                    GettextCatalog.GetString ("{0} parameter-name\n\nor\n\n{1} IMyInterface&lt;{0} T&gt; {{}}",
				                                              Highlight ("out", GetThemeColor (keywordOther)), Highlight ("interface", GetThemeColor (keywordDeclaration))));
				break;
			case SyntaxKind.OverrideKeyword:
				result.SignatureMarkup = Highlight ("override", GetThemeColor (modifierColor)) + keywordSign;
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} modifier is used to override a method, a property, an indexer, or an event.", Highlight ("override", GetThemeColor (modifierColor)));
				break;
			case SyntaxKind.ParamKeyword:
				result.SignatureMarkup = Highlight ("params", GetThemeColor (keywordOther)) + keywordSign;
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} keyword lets you specify a method parameter that takes an argument where the number of arguments is variable.", Highlight ("params", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.PartialKeyword:
				result.SignatureMarkup = Highlight ("partial", GetThemeColor (keywordOther)) + keywordSign;
				if (node.Parent != null) {
					if (node.Parent is TypeDeclarationSyntax) {
						result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("[modifiers] {0} type-declaration", Highlight ("partial", GetThemeColor (keywordOther))));
						result.SummaryMarkup = GettextCatalog.GetString ("The {0} keyword on a type declaration allows for the definition to be split into multiple files.", Highlight ("partial", GetThemeColor (keywordOther)));
						break;
					} else if (node.Parent is MethodDeclarationSyntax) {
						result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("{0} method-declaration", Highlight ("partial", GetThemeColor (keywordOther))));
						result.SummaryMarkup = GettextCatalog.GetString ("The {0} keyword on a method declaration allows for the implementation of a method to be defined in another part of the partial class.", Highlight ("partial", GetThemeColor (keywordOther)));
					}
				} else
					result.AddCategory (GettextCatalog.GetString ("Form"), 
					                    GettextCatalog.GetString ("[modifiers] {0} type-declaration\n\nor\n\n{0} method-declaration", Highlight ("partial", GetThemeColor (keywordOther))));
				break;
			case SyntaxKind.PrivateKeyword:
				result.SignatureMarkup = Highlight ("private", GetThemeColor (modifierColor)) + keywordSign;
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} keyword is a member access modifier. Private access is the least permissive access level. Private members are accessible only within the body of the class or the struct in which they are declared.", Highlight ("private", GetThemeColor (modifierColor)));
				break;
			case SyntaxKind.ProtectedKeyword:
				result.SignatureMarkup = Highlight ("protected", GetThemeColor (modifierColor)) + keywordSign;
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} keyword is a member access modifier. A protected member is accessible from within the class in which it is declared, and from within any class derived from the class that declared this member.", Highlight ("protected", GetThemeColor (modifierColor)));
				break;
			case SyntaxKind.PublicKeyword:
				Console.WriteLine ("modifier color:  "+GetThemeColor (modifierColor).ToPangoString ());
				result.SignatureMarkup = Highlight ("public", GetThemeColor (modifierColor)) + keywordSign;
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} keyword is an access modifier for types and type members. Public access is the most permissive access level. There are no restrictions on accessing public members.", Highlight ("public", GetThemeColor (modifierColor)));
				break;
			case SyntaxKind.ReadOnlyKeyword:
				result.SignatureMarkup = Highlight ("readonly", GetThemeColor (modifierColor)) + keywordSign;
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} keyword is a modifier that you can use on fields. When a field declaration includes a {0} modifier, assignments to the fields introduced by the declaration can only occur as part of the declaration or in a constructor in the same class.", Highlight ("readonly", GetThemeColor (modifierColor)));
				break;
			case SyntaxKind.RefKeyword:
				result.SignatureMarkup = Highlight ("ref", GetThemeColor (keywordOther)) + keywordSign;
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} method parameter keyword on a method parameter causes a method to refer to the same variable that was passed into the method.", Highlight ("ref ", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.RemoveKeyword:
				result.SignatureMarkup = Highlight ("remove", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("[modifiers] {0} {{ accessor-body }}", Highlight ("remove", GetThemeColor (keywordOther))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} keyword is used to define a custom accessor for when an event is unsubscribed from. If supplied, an add accessor must also be supplied.", Highlight ("remove", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.ReturnKeyword:
				result.SignatureMarkup = Highlight ("return", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("{0} [expression];", Highlight ("return", GetThemeColor (keywordOther))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} statement terminates execution of the method in which it appears and returns control to the calling method.", Highlight ("return ", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.SelectKeyword:
				result.SignatureMarkup = Highlight ("select", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Query Form"), GettextCatalog.GetString ("{0} return-type", Highlight ("select", GetThemeColor (keywordOther))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} clause specifies the type of value to return from the query.", Highlight ("select", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.SealedKeyword:
				result.SignatureMarkup = Highlight ("sealed", GetThemeColor (modifierColor)) + keywordSign;
				result.SummaryMarkup = GettextCatalog.GetString ("A sealed class cannot be inherited.");
				break;
			case SyntaxKind.SetKeyword:
				result.SignatureMarkup = Highlight ("set", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("[modifiers] {0} [ {{ accessor-body }} ]", Highlight ("set", GetThemeColor (keywordOther))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} keyword is used to define an accessor method to assign to the value of the property or indexer element.", Highlight ("set", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.SizeOfKeyword:
				result.SignatureMarkup = Highlight ("sizeof", GetThemeColor (keywordOperator)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("{0} (type)", Highlight ("sizeof", GetThemeColor (keywordOperator))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} operator is used to obtain the size in bytes for a value type.", Highlight ("sizeof", GetThemeColor (keywordOperator)));
				break;
			case SyntaxKind.StackAllocKeyword:
				result.SignatureMarkup = Highlight ("stackalloc", GetThemeColor (keywordOperator)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("type * ptr = {0} type [ expr ];", Highlight ("stackalloc", GetThemeColor (keywordOperator))));
				result.SummaryMarkup = GettextCatalog.GetString ("Allocates a block of memory on the stack.");
				break;
			case SyntaxKind.StaticKeyword:
				result.SignatureMarkup = Highlight ("static", GetThemeColor (modifierColor)) + keywordSign;
				result.SummaryMarkup = GettextCatalog.GetString ("Use the {0} modifier to declare a static member, which belongs to the type itself rather than to a specific object.", Highlight ("static", GetThemeColor (modifierColor)));
				break;
			case SyntaxKind.StructKeyword:
				result.SignatureMarkup = Highlight ("struct", GetThemeColor (keywordDeclaration)) + keywordSign;
				if (node.Parent != null && node.Parent.IsKind (SyntaxKind.ConstructorConstraint)) {
					result.SummaryMarkup = GettextCatalog.GetString ("The {0} constraint specifies that the type argument must be a value type. Any value type except Nullable can be specified.", Highlight ("struct", GetThemeColor (keywordDeclaration)));
				} else {
					result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("[attributes] [modifiers] {0} identifier [:interfaces] body [;]", Highlight ("struct", GetThemeColor (keywordDeclaration))));
					result.SummaryMarkup = GettextCatalog.GetString ("A {0} type is a value type that can contain constructors, constants, fields, methods, properties, indexers, operators, events, and nested types.", Highlight ("struct", GetThemeColor (keywordDeclaration)));
				}
				break;
			case SyntaxKind.SwitchKeyword:
				result.SignatureMarkup = Highlight ("switch", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("{0} (expression)\n {{\n  {1} constant-expression:\n  statement\n  jump-statement\n  [{2}:\n  statement\n  jump-statement]\n }}", Highlight ("switch", GetThemeColor (keywordOther)), Highlight ("case ", GetThemeColor (keywordOther)), Highlight ("default", GetThemeColor (keywordOther))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} statement is a control statement that handles multiple selections by passing control to one of the {1} statements within its body.", Highlight ("switch", GetThemeColor (keywordOther)), Highlight ("case", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.ThisKeyword:
				result.SignatureMarkup = Highlight ("this", GetThemeColor (keywordOther)) + keywordSign;
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} keyword refers to the current instance of the class.", Highlight ("this", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.ThrowKeyword:
				result.SignatureMarkup = Highlight ("throw", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("{0} [expression];", Highlight ("throw", GetThemeColor (keywordOther))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} statement is used to signal the occurrence of an anomalous situation (exception) during the program execution.", Highlight ("throw ", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.TryKeyword:
				result.SignatureMarkup = Highlight ("try", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), 
				                    GettextCatalog.GetString ("{0} try-block\n  {1} (exception-declaration-1) catch-block-1 \n  {1} (exception-declaration-2) catch-block-2 \n...\n{0} try-block {1} catch-block",
				                                              Highlight ("try", GetThemeColor (keywordOther)), Highlight ("catch", GetThemeColor (keywordOther))));
				result.SummaryMarkup = GettextCatalog.GetString ("The try-catch statement consists of a {0} block followed by one or more {1} clauses, which specify handlers for different exceptions.", Highlight ("try", GetThemeColor (keywordOther)), Highlight ("catch", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.TypeOfKeyword:
				result.SignatureMarkup = Highlight ("typeof", GetThemeColor (keywordOperator)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("{0} (type)", Highlight ("typeof", GetThemeColor (keywordOperator))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} operator is used to obtain the System.Type object for a type.", Highlight ("typeof", GetThemeColor (keywordOperator)));
				break;
			case SyntaxKind.UncheckedKeyword:
				result.SignatureMarkup = Highlight ("unchecked", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("{0} block\n{0} (expression)", Highlight ("unchecked", GetThemeColor (keywordOther))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} keyword is used to control the overflow-checking context for integral-type arithmetic operations and conversions.", Highlight ("unchecked", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.UnsafeKeyword:
				result.SignatureMarkup = Highlight ("unsafe", GetThemeColor (keywordOther)) + keywordSign;
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} keyword denotes an unsafe context, which is required for any operation involving pointers.", Highlight ("unsafe", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.UsingKeyword:
				result.SignatureMarkup = Highlight ("using", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("{0} (expression | type identifier = initializer) statement\n{0} [alias = ]class_or_namespace;", Highlight ("using", GetThemeColor (keywordOther))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} directive creates an alias for a namespace or imports types defined in other namespaces. The {0} statement defines a scope at the end of which an object will be disposed.", Highlight ("using", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.VirtualKeyword:
				result.SignatureMarkup = Highlight ("virtual", GetThemeColor (modifierColor)) + keywordSign;
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} keyword is used to modify a method, property, indexer, or event declaration and allow for it to be overridden in a derived class.", Highlight ("virtual", GetThemeColor (modifierColor)));
				break;
			case SyntaxKind.VolatileKeyword:
				result.SignatureMarkup = Highlight ("volatile", GetThemeColor (modifierColor)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("{0} declaration", Highlight ("volatile", GetThemeColor (modifierColor))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} keyword indicates that a field can be modified in the program by something such as the operating system, the hardware, or a concurrently executing thread.", Highlight ("volatile", GetThemeColor (modifierColor)));
				break;
			case SyntaxKind.VoidKeyword:
				result.SignatureMarkup = Highlight ("void", GetThemeColor (keywordOther)) + keywordSign;
				break;
			case SyntaxKind.WhereKeyword:
				result.SignatureMarkup = Highlight ("where", GetThemeColor (keywordOther)) + keywordSign;
				if (node.Parent != null) {
					if (node.Parent is WhereClauseSyntax) {
						result.AddCategory (GettextCatalog.GetString ("Query Form"), GettextCatalog.GetString ("{0} condition", Highlight ("where", GetThemeColor (keywordOther))));
						result.SummaryMarkup = GettextCatalog.GetString ("The {0} clause specifies which elements from the data source to be returned according to a given condition.", Highlight ("where", GetThemeColor (keywordOther)));
						break;
					}
					if (node.Parent is TypeConstraintSyntax) {
						result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("generic-class-declaration {0} type-parameter : type-constraint", Highlight ("where", GetThemeColor (keywordOther))));
						result.SummaryMarkup = GettextCatalog.GetString ("The {0} clause constrains which types can be used as the type parameter in a generic declaration.", Highlight ("where", GetThemeColor (keywordOther)));
						break;
					}
				} else {
					result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("generic-class-declaration {0} type-parameter : type-constraint\n\nor\n\nquery-clauses {0} condition [query-clauses]", Highlight ("where", GetThemeColor (keywordOther))));
				}
				break;
			case SyntaxKind.YieldKeyword:
				result.SignatureMarkup = Highlight ("yield", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("{0} {1}\n\nor\n\n{0} {2} expression", Highlight ("yield", GetThemeColor (keywordOther)), Highlight ("break", GetThemeColor (keywordOther)), Highlight ("return", GetThemeColor (keywordOther))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} keyword is used to indicate that a method, get accessor, or operator is an iterator.", Highlight ("yield", GetThemeColor (keywordOther)));
				break;
			case SyntaxKind.WhileKeyword:
				result.SignatureMarkup = Highlight ("while", GetThemeColor (keywordOther)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("{0} (expression) statement", Highlight ("while", GetThemeColor (keywordOther))));
				result.SummaryMarkup = GettextCatalog.GetString ("The {0} statement executes a statement or a block of statements until a specified expression evaluates to false.", Highlight ("while", GetThemeColor (keywordOther)));
				break;
				case SyntaxKind.NameOfKeyword:
				result.SignatureMarkup = Highlight ("nameof", GetThemeColor (keywordDeclaration)) + keywordSign;
				result.AddCategory (GettextCatalog.GetString ("Form"), GettextCatalog.GetString ("{0}(identifier)", Highlight ("nameof", GetThemeColor (keywordDeclaration))));
				result.SummaryMarkup = GettextCatalog.GetString ("Used to obtain the simple (unqualified) string name of a variable, type, or member.");
				break;
			default:
				return null;
			}
			return result;
		}

		public TooltipInformation GetConstraintTooltip (SyntaxToken keyword)
		{
			var result = new TooltipInformation ();

			var color = AlphaBlend (SyntaxHighlightingService.GetColor (colorStyle,EditorThemeColors.Foreground), SyntaxHighlightingService.GetColor (colorStyle,EditorThemeColors.Background), optionalAlpha);
			var colorString = MonoDevelop.Components.HelperMethods.GetColorString (color);

			var keywordSign = "<span foreground=\"" + colorString + "\"> " + GettextCatalog.GetString ("(keyword)") + "</span>";

			result.SignatureMarkup = Highlight (keyword.ToFullString (), GetThemeColor (keywordOther)) + keywordSign;

			switch (keyword.Parent.Kind ()) {
			case SyntaxKind.ClassConstraint:
				result.AddCategory (GettextCatalog.GetString ("Constraint"), GettextCatalog.GetString ("The type argument must be a reference type; this applies also to any class, interface, delegate, or array type."));
				break;
			case SyntaxKind.ConstructorConstraint:
				result.AddCategory (GettextCatalog.GetString ("Constraint"), GettextCatalog.GetString ("The type argument must have a public parameterless constructor. When used together with other constraints, the new() constraint must be specified last."));
				break;
			case SyntaxKind.StructConstraint:
				result.AddCategory (GettextCatalog.GetString ("Constraint"), GettextCatalog.GetString ("The type argument must be a value type. Any value type except Nullable can be specified. See Using Nullable Types (C# Programming Guide) for more information."));
				break;
			}

			return result;
		}

		public TooltipInformation GetTypeOfTooltip (TypeOfExpressionSyntax typeOfExpression, ITypeSymbol resolveResult)
		{
			var result = new TooltipInformation ();
			if (resolveResult == null) {
				result.SignatureMarkup = MonoDevelop.Ide.TypeSystem.Ambience.EscapeText (typeOfExpression.Type.ToString ());
			} else {
				result.SignatureMarkup = GetTypeMarkup (resolveResult, true);
			}
			return result;
		}

		//		public TooltipInformation GetAliasedNamespaceTooltip (AliasNamespaceResolveResult resolveResult)
		//		{
		//			var result = new TooltipInformation ();
		//			result.SignatureMarkup = GetMarkup (resolveResult.Namespace);
		//			result.AddCategory (GettextCatalog.GetString ("Alias information"), GettextCatalog.GetString ("Resolved using alias '{0}'", resolveResult.Alias));
		//			return result;
		//		}
		//
		//		public TooltipInformation GetAliasedTypeTooltip (AliasTypeResolveResult resolveResult)
		//		{
		//			var result = new TooltipInformation ();
		//			result.SignatureMarkup = GetTypeMarkup (resolveResult.Type, true);
		//			result.AddCategory (GettextCatalog.GetString ("Alias information"), GettextCatalog.GetString ("Resolved using alias '{0}'", resolveResult.Alias));
		//			return result;
		//		}

		string GetEventMarkup (IEventSymbol evt)
		{
			if (evt == null)
				throw new ArgumentNullException ("evt");
			var result = StringBuilderCache.Allocate ();
			AppendModifiers (result, evt);
			result.Append (Highlight ("event ", GetThemeColor (modifierColor)));
			result.Append (GetTypeReferenceString (evt.Type));
			if (BreakLineAfterReturnType) {
				result.AppendLine ();
			} else {
				result.Append (" ");
			}

			AppendExplicitInterfaces (result, evt.ExplicitInterfaceImplementations.Cast<ISymbol> ());
			result.Append (HighlightSemantically (FilterEntityName (evt.Name), GetThemeColor ("entity.name.event.source.cs")));
			return StringBuilderCache.ReturnAndFree (result);
		}

		bool grayOut;

		bool GrayOut
		{
			get
			{
				return grayOut;
			}
			set
			{
				grayOut = value;
			}
		}

		public SemanticModel SemanticModel { get; internal set; }

		void AppendParameterList (StringBuilder result, ImmutableArray<IParameterSymbol> parameterList, bool spaceBefore, bool spaceAfter, bool newLine = true)
		{
			if (parameterList == null || parameterList.Length == 0)
				return;
			if (newLine)
				result.AppendLine ();
			for (int i = 0; i < parameterList.Length; i++) {
				var parameter = parameterList [i];
				if (newLine)
					result.Append (new string (' ', 2));
				var doHighightParameter = i == HighlightParameter || HighlightParameter >= i && i == parameterList.Length - 1 && parameter.IsParams;
				if (doHighightParameter)
					result.Append ("<u>");
				/*				if (parameter.IsOptional) {
					GrayOut = true;
					var color = AlphaBlend (colorStyle.Default.Color, colorStyle.Default.BackgroundColor, optionalAlpha);
					var colorString = Mono.TextEditor.HelperMethods.GetColorString (color);
					result.Append ("<span foreground=\"" + colorString + "\">");
				}*/
				AppendParameter (result, parameter);
				if (parameter.IsOptional) {
					if (options.GetOption (CSharpFormattingOptions.SpacingAroundBinaryOperator) == BinaryOperatorSpacingOptions.Single) {
						result.Append (" = ");
					} else {
						result.Append ("=");
					}
					AppendConstant (result, parameter.Type, parameter.ExplicitDefaultValue);
					//					GrayOut = false;
					//					result.Append ("</span>");
				}
				if (doHighightParameter)
					result.Append ("</u>");
				if (i + 1 < parameterList.Length) {
					if (spaceBefore)
						result.Append (' ');
					result.Append (',');
					if (newLine) {
						result.AppendLine ();
					} else {
						if (spaceAfter)
							result.Append (' ');
					}
				}
			}
			if (newLine)
				result.AppendLine ();
		}

		void AppendParameter (StringBuilder result, IParameterSymbol parameter)
		{
			if (parameter == null)
				return;
			if (parameter.RefKind == RefKind.Out) {
				result.Append (Highlight ("out ", GetThemeColor (keywordOther)));
			} else if (parameter.RefKind == RefKind.Ref) {
				result.Append (Highlight ("ref ", GetThemeColor (keywordOther)));
			} else if (parameter.IsParams) {
				result.Append (Highlight ("params ", GetThemeColor (keywordOther)));
			}
			result.Append (GetTypeReferenceString (parameter.Type));
			result.Append (" ");
			result.Append (FilterEntityName (parameter.Name));
		}

		void AppendExplicitInterfaces (StringBuilder sb, IEnumerable<Microsoft.CodeAnalysis.ISymbol> member)
		{
			foreach (var implementedInterfaceMember in member) {
				sb.Append (GetTypeReferenceString (implementedInterfaceMember.ContainingType));
				sb.Append (".");
			}
		}

		static ulong GetUlong (string str)
		{
			try {
				if (str [0] == '-')
					return (ulong)long.Parse (str);
				return ulong.Parse (str);
			} catch (Exception e) {
				LoggingService.LogError ("Error while converting " + str + " to a number.", e);
				return 0;
			}
		}

		void AppendConstant (StringBuilder sb, ITypeSymbol constantType, object constantValue, bool useNumericalEnumValue = false)
		{
			
			if (constantValue is string) {
				
				sb.Append (Highlight ("\"" + Ambience.EscapeText (ConvertString ((string)constantValue)) + "\"", GetThemeColor (stringConstants)));
				return;
			}
			if (constantValue is char) {
				sb.Append (Highlight ("'" + ConvertChar ((char)constantValue) + "'", GetThemeColor (stringConstants)));
				return;
			}
			if (constantValue is bool) {
				sb.Append (Highlight ((bool)constantValue ? "true" : "false", GetThemeColor (keywordConstant)));
				return;
			}

			if (constantValue == null) {
				if (constantType.IsValueType) {
					// structs can never be == null, therefore it's the default value.
					sb.Append (Highlight ("default", GetThemeColor (keywordOther)))
					  .Append ("(")
					  .Append (GetTypeReferenceString (constantType))
					  .Append (")");
				} else {
					sb.Append (Highlight ("null", GetThemeColor (keywordConstant)));
				}
				return;
			}
			//			TODOδ
			//			while (IsNullableType (constantType))
			//				constantType = NullableType.GetUnderlyingType (constantType);
			if (constantType.TypeKind == TypeKind.Enum) {
				foreach (var field in constantType.GetMembers ().OfType<IFieldSymbol> ()) {
					if (field.ConstantValue == constantValue) {
						if (useNumericalEnumValue) {
							sb.Append (Highlight (string.Format ("0x{0:X}", field.ConstantValue), GetThemeColor (numericConstants)));
						} else {
							sb.Append (GetTypeReferenceString (constantType))
							  .Append (".")
							  .Append (FilterEntityName (field.Name));
						}
						return;
					}
				}
				// try to decompose flags
				if (constantType.GetAttributes ().Any (attr => attr.AttributeClass.Name == "FlagsAttribute" && attr.AttributeClass.ContainingNamespace.Name == "System")) {
					var val = GetUlong (constantValue.ToString ());
					var outVal = 0UL;
					var fields = new List<IFieldSymbol> ();
					foreach (var field in constantType.GetMembers ().OfType<IFieldSymbol> ()) {
						if (field.ConstantValue == null)
							continue;
						var val2 = GetUlong (field.ConstantValue.ToString ());
						if ((val & val2) == val2) {
							fields.Add (field);
							outVal |= val2;
						}
					}

					if (val == outVal && fields.Count > 1) {
						for (int i = 0; i < fields.Count; i++) {
							if (i > 0)
								sb.Append (" | ");
							var field = fields [i];
							sb.Append (GetTypeReferenceString (constantType))
							  .Append (".")
							  .Append (FilterEntityName (field.Name));
						}
						return;
					}
				}

				sb.Append ("(")
				  .Append (GetTypeReferenceString (constantType))
				  .Append (")")
				  .Append (Highlight (constantValue.ToString (), GetThemeColor (numericConstants)));
				return;
			}
			sb.Append (Highlight (MonoDevelop.Ide.TypeSystem.Ambience.EscapeText (constantValue.ToString ()), GetThemeColor (numericConstants)));
		}

		static string ConvertChar (char ch)
		{
			switch (ch) {
			case '\\':
				return "\\\\";
			case '\0':
				return "\\0";
			case '\a':
				return "\\a";
			case '\b':
				return "\\b";
			case '\f':
				return "\\f";
			case '\n':
				return "\\n";
			case '\r':
				return "\\r";
			case '\t':
				return "\\t";
			case '\v':
				return "\\v";
			default:
				if (char.IsControl (ch) || char.IsSurrogate (ch) ||
					// print all uncommon white spaces as numbers
					(char.IsWhiteSpace (ch) && ch != ' ')) {
					return "\\u" + ((int)ch).ToString ("x4");
				} else {
					return ch.ToString ();
				}
			}
		}

		static string ConvertString (string str)
		{
			StringBuilder sb = StringBuilderCache.Allocate ();
			foreach (char ch in str) {
				if (ch == '"') {
					sb.Append ("\\\"");
				} else {
					sb.Append (ConvertChar (ch));
				}
			}
			return StringBuilderCache.ReturnAndFree (sb);
		}

		HslColor GetThemeColor (string scope)
		{
			return SyntaxHighlightingService.GetColorFromScope (this.colorStyle, scope, EditorThemeColors.Foreground);
		}

		void AppendVariance (StringBuilder sb, VarianceKind variance)
		{
			if (variance == VarianceKind.In) {
				sb.Append (Highlight ("in ", GetThemeColor (keywordOther)));
			} else if (variance == VarianceKind.Out) {
				sb.Append (Highlight ("out ", GetThemeColor (keywordOther)));
			}
		}

		Gdk.Color AlphaBlend (Gdk.Color color, Gdk.Color color2, double alpha)
		{
			return new Gdk.Color (
				(byte)((alpha * color.Red + (1 - alpha) * color2.Red) / 256),
				(byte)((alpha * color.Green + (1 - alpha) * color2.Green) / 256),
				(byte)((alpha * color.Blue + (1 - alpha) * color2.Blue) / 256)
			);
		}

		Gdk.Color AlphaBlend (Cairo.Color color, Cairo.Color color2, double alpha)
		{
			return AlphaBlend ((Gdk.Color)((HslColor)color), (Gdk.Color)((HslColor)color2), alpha);
		}

		HslColor AlphaBlend (HslColor color, HslColor color2, double alpha)
		{
			return (HslColor)AlphaBlend ((Gdk.Color)color, (Gdk.Color)color2, alpha);
		}

		public string GetArrayIndexerMarkup (IArrayTypeSymbol arrayType)
		{
			if (arrayType == null)
				throw new ArgumentNullException ("arrayType");
			var result = StringBuilderCache.Allocate ();
			result.Append (GetTypeReferenceString (arrayType.ElementType));
			if (BreakLineAfterReturnType) {
				result.AppendLine ();
			} else {
				result.Append (" ");
			}
			result.Append (Highlight ("this", GetThemeColor (keywordOther)));
			result.Append ("[");
			for (int i = 0; i < arrayType.Rank; i++) {
				if (i > 0)
					result.Append (", ");
				var doHighightParameter = i == HighlightParameter;
				if (doHighightParameter)
					result.Append ("<u>");

				result.Append (Highlight ("int ", GetThemeColor (keywordOther)));
				if (arrayType.Rank == 1)
					result.Append ("index");
				else
					result.Append ("i").Append (i + 1);
				if (doHighightParameter)
					result.Append ("</u>");
			}
			result.Append ("]");

			result.Append (" {");
			result.Append (Highlight (" get", GetThemeColor (keywordOther))).Append (";");
			result.Append (Highlight (" set", GetThemeColor (keywordOther))).Append (";");
			result.Append (" }");

			return StringBuilderCache.ReturnAndFree (result);
		}


		string Highlight (string str, HslColor color)
		{
			if (grayOut) {
				color = AlphaBlend ((Gdk.Color)color, (Gdk.Color)GetThemeColor (EditorThemeColors.Background), optionalAlpha);
			}
			return "<span foreground=\"" + color.ToPangoString () + "\">" + str + "</span>";
		}

		string HighlightSemantically (string str, HslColor color)
		{
			if (!DefaultSourceEditorOptions.Instance.EnableSemanticHighlighting)
				return str;
			return Highlight (str, color);
		}

		public string CreateFooter (ISymbol entity)
		{
			var type = entity as ITypeSymbol;
			if (type != null && type.Locations.Any ()) {
				var loc = type.Locations.First ();
				if (loc.IsInSource) {// TODO:
									 //					MonoDevelop.Projects.Project project;
									 //					
									 //					if (type.TryGetSourceProject (out project)) {
									 //						var relPath = FileService.AbsoluteToRelativePath (project.BaseDirectory, loc.SourceTree.FilePath);
									 //						var line = loc.SourceTree.GetLineSpan (loc.SourceSpan, true).StartLinePosition.Line;
									 //						
									 //						return (type.ContainingNamespace.IsGlobalNamespace ? "" : "<span font='11'>" + GettextCatalog.GetString ("Namespace:\t{0}", AmbienceService.EscapeText (type.ContainingNamespace.Name)) + "</span>" + Environment.NewLine) +
									 //							"<span font='11'>" + GettextCatalog.GetString ("Project:\t{0}", AmbienceService.EscapeText (type.ContainingAssembly.Name)) + "</span>" + Environment.NewLine +
									 //							"<span font='11'>" + GettextCatalog.GetString ("File:\t\t{0} (line {1})", AmbienceService.EscapeText (relPath), line) + "</span>";
									 //					}
				}
				return (type.ContainingNamespace.IsGlobalNamespace ? "" : "<span font='11'>" + GettextCatalog.GetString ("Namespace:\t{0}", MonoDevelop.Ide.TypeSystem.Ambience.EscapeText (type.ContainingNamespace.GetFullName ())) + "</span>" + Environment.NewLine) +
					"<span font='11'>" + GettextCatalog.GetString ("Assembly:\t{0}", MonoDevelop.Ide.TypeSystem.Ambience.EscapeText (type.ContainingAssembly.Name)) + "</span>";
			}

			var method = entity as IMethodSymbol;
			if (method != null && (method.MethodKind == MethodKind.Constructor || method.MethodKind == MethodKind.StaticConstructor || method.MethodKind == MethodKind.Destructor)) {
				return (method.ContainingNamespace.IsGlobalNamespace ? "" : "<span font='11'>" + GettextCatalog.GetString ("Namespace:\t{0}", MonoDevelop.Ide.TypeSystem.Ambience.EscapeText (method.ContainingNamespace.GetFullName ())) + "</span>" + Environment.NewLine) +
					"<span font='11'>" + GettextCatalog.GetString ("Assembly:\t{0}", MonoDevelop.Ide.TypeSystem.Ambience.EscapeText (method.ContainingAssembly.Name)) + "</span>";
			}


			if (entity.ContainingType != null && entity.Locations.Any ()) {
				var loc = entity.Locations.First ();
				if (!loc.IsInSource) {
					// TODO:
					//					MonoDevelop.Projects.Project project;
					//					if (entity.ContainingType.TryGetSourceProject (out project)) {
					//						var relPath = FileService.AbsoluteToRelativePath (project.BaseDirectory, loc.SourceTree.FilePath);
					//						var line = loc.SourceTree.GetLineSpan (loc.SourceSpan, true).StartLinePosition.Line;
					//						return "<span font='11'>" + GettextCatalog.GetString ("Project:\t{0}", AmbienceService.EscapeText (project.Name)) + "</span>" + Environment.NewLine +
					//								"<span font='11'>" + GettextCatalog.GetString ("From type:\t{0}", AmbienceService.EscapeText (entity.ContainingType.Name)) + "</span>" + Environment.NewLine +
					//								"<span font='11'>" + GettextCatalog.GetString ("File:\t\t{0} (line {1})", AmbienceService.EscapeText (relPath), line) + "</span>";
					//					}
				}
				return "<span font='11'>" + GettextCatalog.GetString ("From type:\t{0}", MonoDevelop.Ide.TypeSystem.Ambience.EscapeText (entity.ContainingType.Name)) + "</span>" + Environment.NewLine +
					"<span font='11'>" + GettextCatalog.GetString ("Assembly:\t{0}", MonoDevelop.Ide.TypeSystem.Ambience.EscapeText (entity.ContainingAssembly.Name)) + "</span>";
			}
			return null;
		}
	}
}
