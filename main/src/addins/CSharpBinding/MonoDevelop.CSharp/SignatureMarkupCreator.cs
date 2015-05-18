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
using MonoDevelop.Ide.TypeSystem;
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
using MonoDevelop.NUnit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Components;
using MonoDevelop.Ide.Editor.Highlighting;
using ICSharpCode.NRefactory6.CSharp;

namespace MonoDevelop.CSharp
{
	class SignatureMarkupCreator
	{
		const double optionalAlpha = 0.7;
		readonly DocumentContext ctx;
		readonly OptionSet options;
		readonly ColorScheme colorStyle;
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
			this.colorStyle = SyntaxModeService.GetColorStyle (MonoDevelop.Ide.IdeApp.Preferences.ColorScheme);
			this.ctx = ctx;
			if (ctx != null) {
				if (ctx.ParsedDocument == null || ctx.AnalysisDocument == null) {
					LoggingService.LogError ("Signature markup creator created with invalid context." + Environment.NewLine + Environment.StackTrace);
				}
				this.options = ctx.GetOptionSet ();
			} else {
				this.options = TypeSystemService.Workspace.Options;
			}
		}

		public string GetTypeReferenceString (ITypeSymbol type, bool highlight = true)
		{
			if (type == null)
				throw new ArgumentNullException ("type");
			if (type.TypeKind == TypeKind.Error)
				return "?";
			if (type.TypeKind == TypeKind.Array) {
				var arrayType = (IArrayTypeSymbol)type;
				return GetTypeReferenceString (arrayType.ElementType, highlight) + "[" + new string (',', arrayType.Rank - 1) + "]";
			}
			if (type.TypeKind == TypeKind.Pointer)
				return GetTypeReferenceString (((IPointerTypeSymbol)type).PointedAtType, highlight) + "*";
			string displayString;
			if (ctx != null) {
				SemanticModel model = SemanticModel;
				if (model == null) {
					var parsedDocument = ctx.ParsedDocument;
					if (parsedDocument != null) {
						model = parsedDocument.GetAst<SemanticModel> () ?? ctx.AnalysisDocument.GetSemanticModelAsync ().Result;
					}
				}
				//Math.Min (model.SyntaxTree.Length, offset)) is needed in case parsedDocument.GetAst<SemanticModel> () is outdated
				//this is tradeoff between performance and consistency between editor text(offset) and model, since
				//ToMinimalDisplayString can use little outdated model this is fine
				//but in case of Sketches where user usually is at end of document when typing text this can throw exception
				//because offset can be >= Length
				displayString = model != null ? type.ToMinimalDisplayString (model, Math.Min (model.SyntaxTree.Length - 1, offset), Ambience.LabelFormat) : type.Name;
			} else {
				displayString = type.ToDisplayString (Ambience.LabelFormat);
			}
			var text = Ambience.EscapeText (displayString);
			return highlight ? HighlightSemantically (text, colorStyle.UserTypes) : text;
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
			// TODO
			//			if (entity.IsObsolete (out reason)) {
			//				var attr = reason == null ? "[Obsolete]" : "[Obsolete(\"" + reason + "\")]";
			//				result = "<span size=\"smaller\">" + attr + "</span>" + Environment.NewLine + result;
			//			}
			return result;
		}

		string GetNamespaceMarkup (INamespaceSymbol ns)
		{
			var result = new StringBuilder ();
			result.Append (Highlight ("namespace ", colorStyle.KeywordNamespace));
			result.Append (ns.Name);

			return result.ToString ();
		}
		void AppendModifiers (StringBuilder result, ISymbol entity)
		{
			if (entity.ContainingType != null && entity.ContainingType.TypeKind == TypeKind.Interface)
				return;

			switch (entity.DeclaredAccessibility) {
			case Accessibility.Internal:
				if (entity.Kind != SymbolKind.NamedType)
					result.Append (Highlight ("internal ", colorStyle.KeywordModifiers));
				break;
			case Accessibility.ProtectedAndInternal:
				result.Append (Highlight ("protected internal ", colorStyle.KeywordModifiers));
				break;
			case Accessibility.ProtectedOrInternal:
				result.Append (Highlight ("internal protected ", colorStyle.KeywordModifiers));
				break;
			case Accessibility.Protected:
				result.Append (Highlight ("protected ", colorStyle.KeywordModifiers));
				break;
			case Accessibility.Private:
				// private is the default modifier - no need to show that
				//				result.Append (Highlight (" private", colorStyle.KeywordModifiers));
				break;
			case Accessibility.Public:
				result.Append (Highlight ("public ", colorStyle.KeywordModifiers));
				break;
			}
			var field = entity as IFieldSymbol;

			if (field != null) {
				//  TODO!!!!
				/*if (field.IsFixed) {
					result.Append (Highlight ("fixed ", colorStyle.KeywordModifiers));
				} else*/
				if (field.IsConst) {
					result.Append (Highlight ("const ", colorStyle.KeywordModifiers));
				}
			} else if (entity.IsStatic) {
				result.Append (Highlight ("static ", colorStyle.KeywordModifiers));
			} else if (entity.IsSealed) {
				if (!(entity is ITypeSymbol && ((ITypeSymbol)entity).TypeKind == TypeKind.Delegate))
					result.Append (Highlight ("sealed ", colorStyle.KeywordModifiers));
			} else if (entity.IsAbstract) {
				if (!(entity is ITypeSymbol && ((ITypeSymbol)entity).TypeKind == TypeKind.Interface))
					result.Append (Highlight ("abstract ", colorStyle.KeywordModifiers));
			}

			//  TODO!!!!
			//			if (entity.IsShadowing)
			//				result.Append (Highlight ("new ", colorStyle.KeywordModifiers));

			var method = entity as IMethodSymbol;
			if (method != null) {
				if (method.IsOverride) {
					result.Append (Highlight ("override ", colorStyle.KeywordModifiers));
				} else if (method.IsVirtual) {
					result.Append (Highlight ("virtual ", colorStyle.KeywordModifiers));
				}
				if (method.IsAsync)
					result.Append (Highlight ("async ", colorStyle.KeywordModifiers));
				if (method.PartialDefinitionPart != null || method.PartialImplementationPart != null)
					result.Append (Highlight ("partial ", colorStyle.KeywordModifiers));
			}
			if (field != null) {
				if (field.IsVolatile)
					result.Append (Highlight ("volatile ", colorStyle.KeywordModifiers));
				if (field.IsReadOnly)
					result.Append (Highlight ("readonly ", colorStyle.KeywordModifiers));
			}

		}

		void AppendAccessibility (StringBuilder result, IMethodSymbol entity)
		{
			switch (entity.DeclaredAccessibility) {
			case Accessibility.Internal:
				result.Append (Highlight ("internal", colorStyle.KeywordModifiers));
				break;
			case Accessibility.ProtectedAndInternal:
				result.Append (Highlight ("protected internal", colorStyle.KeywordModifiers));
				break;
			case Accessibility.ProtectedOrInternal:
				result.Append (Highlight ("internal protected", colorStyle.KeywordModifiers));
				break;
			case Accessibility.Protected:
				result.Append (Highlight ("protected", colorStyle.KeywordModifiers));
				break;
			case Accessibility.Private:
				result.Append (Highlight ("private", colorStyle.KeywordModifiers));
				break;
			case Accessibility.Public:
				result.Append (Highlight ("public", colorStyle.KeywordModifiers));
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
			var result = new StringBuilder ();
			var highlightedTypeName = Highlight (FilterEntityName (t.Name), colorStyle.UserTypes);
			result.Append (highlightedTypeName);

			var color = AlphaBlend (colorStyle.PlainText.Foreground, colorStyle.PlainText.Background, optionalAlpha);
			var colorString = MonoDevelop.Components.HelperMethods.GetColorString (color);

			result.Append ("<span foreground=\"" + colorString + "\">" + " (type parameter)</span>");
			var tp = t as ITypeParameterSymbol;
			if (tp != null) {
				if (!tp.HasConstructorConstraint && !tp.HasReferenceTypeConstraint && !tp.HasValueTypeConstraint && tp.ConstraintTypes.All (IsObjectOrValueType))
					return result.ToString ();
				result.AppendLine ();
				result.Append (Highlight (" where ", colorStyle.KeywordContext));
				result.Append (highlightedTypeName);
				result.Append (" : ");
				int constraints = 0;

				if (tp.HasReferenceTypeConstraint) {
					constraints++;
					result.Append (Highlight ("class", colorStyle.KeywordDeclaration));
				} else if (tp.HasValueTypeConstraint) {
					constraints++;
					result.Append (Highlight ("struct", colorStyle.KeywordDeclaration));
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
					result.Append (Highlight ("new", colorStyle.KeywordOperators));
				}

			}
			return result.ToString ();
		}

		string GetNullableMarkup (ITypeSymbol t)
		{
			var result = new StringBuilder ();
			result.Append (GetTypeReferenceString (t));
			return result.ToString ();
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
			StringBuilder result = new StringBuilder ();
			result.Append (Highlight (FilterEntityName (t.Name), colorStyle.UserTypesTypeParameters));
			var namedTypeSymbol = t as INamedTypeSymbol;
			if (namedTypeSymbol != null) {
				if (namedTypeSymbol.IsGenericType) {
					AppendTypeParameterList (result, namedTypeSymbol);
				} else if (namedTypeSymbol.IsUnboundGenericType) {
					AppendTypeArgumentList (result, namedTypeSymbol);
				}
			}
			return result.ToString ();
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
				return "Type can not be resolved.";
			if (t.TypeKind == TypeKind.Delegate)
				return GetDelegateMarkup ((INamedTypeSymbol)t);
			if (t.TypeKind == TypeKind.TypeParameter)
				return GetTypeParameterMarkup (t);
			if (t.TypeKind == TypeKind.Array || t.TypeKind == TypeKind.Pointer)
				return GetTypeReferenceString (t);
			if (t.SpecialType == SpecialType.System_Nullable_T)
				return GetNullableMarkup (t);
			var result = new StringBuilder ();
			if (IsNullableType (t))
				AppendModifiers (result, t);

			switch (t.TypeKind) {
			case TypeKind.Class:
				result.Append (Highlight ("class ", colorStyle.KeywordDeclaration));
				break;
			case TypeKind.Interface:
				result.Append (Highlight ("interface ", colorStyle.KeywordDeclaration));
				break;
			case TypeKind.Struct:
				result.Append (Highlight ("struct ", colorStyle.KeywordDeclaration));
				break;
			case TypeKind.Enum:
				result.Append (Highlight ("enum ", colorStyle.KeywordDeclaration));
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
				return result.ToString ();

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

			return result.ToString ();
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
				result.Append (HighlightSemantically (CSharpAmbience.NetToCSharpTypeName (typeParameter.Name), colorStyle.UserTypes));
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
			return Ambience.EscapeText (CSharpAmbience.FilterName (name));
		}

		public string GetDelegateInfo (ITypeSymbol type)
		{
			if (type == null)
				throw new ArgumentNullException ("type");
			var t = type;

			var result = new StringBuilder ();

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
			return result.ToString ();
		}

		string GetDelegateMarkup (INamedTypeSymbol delegateType)
		{
			var result = new StringBuilder ();
			var type = delegateType.IsUnboundGenericType ? delegateType.OriginalDefinition : delegateType;
			var method = type.GetDelegateInvokeMethod ();

			AppendModifiers (result, type);
			result.Append (Highlight ("delegate ", colorStyle.KeywordDeclaration));
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
			return result.ToString ();
		}

		string GetLocalVariableMarkup (ILocalSymbol local)
		{
			if (local == null)
				throw new ArgumentNullException ("local");

			var result = new StringBuilder ();

			if (local.IsConst)
				result.Append (Highlight ("const ", colorStyle.KeywordModifiers));

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

			return result.ToString ();
		}

		string GetParameterVariableMarkup (IParameterSymbol parameter)
		{
			if (parameter == null)
				throw new ArgumentNullException ("parameter");

			var result = new StringBuilder ();
			AppendParameter (result, parameter);

			if (parameter.HasExplicitDefaultValue) {
				if (options.GetOption (CSharpFormattingOptions.SpacingAroundBinaryOperator) == BinaryOperatorSpacingOptions.Single) {
					result.Append (" = ");
				} else {
					result.Append ("=");
				}
				AppendConstant (result, parameter.Type, parameter.ExplicitDefaultValue);
			}

			return result.ToString ();
		}


		string GetFieldMarkup (IFieldSymbol field)
		{
			if (field == null)
				throw new ArgumentNullException ("field");

			var result = new StringBuilder ();
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

			result.Append (HighlightSemantically (FilterEntityName (field.Name), colorStyle.UserFieldDeclaration));

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

			return result.ToString ();
		}

		string GetMethodMarkup (IMethodSymbol method)
		{
			if (method == null)
				throw new ArgumentNullException ("method");

			var result = new StringBuilder ();
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
				result.Append (HighlightSemantically (FilterEntityName (method.Name), colorStyle.UserMethodDeclaration));
			}
			if (method.TypeArguments.Length > 0) {
				result.Append ("&lt;");
				for (int i = 0; i < method.TypeArguments.Length; i++) {
					if (i > 0)
						result.Append (", ");
					result.Append (HighlightSemantically (GetTypeReferenceString (method.TypeArguments [i], false), colorStyle.UserTypes));
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
			return result.ToString ();
		}

		string GetConstructorMarkup (IMethodSymbol method)
		{
			if (method == null)
				throw new ArgumentNullException ("method");


			var result = new StringBuilder ();
			AppendModifiers (result, method);

			result.Append (FilterEntityName (method.ContainingType.Name));
			//
			//			if (formattingOptions.SpaceBeforeConstructorDeclarationParentheses)
			//				result.Append (" ");

			result.Append ('(');
			if (method.ContainingType.TypeKind == TypeKind.Delegate) {
				result.Append (Highlight ("delegate", colorStyle.KeywordDeclaration) + " (");
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
			return result.ToString ();
		}

		string GetDestructorMarkup (IMethodSymbol method)
		{
			if (method == null)
				throw new ArgumentNullException ("method");

			var result = new StringBuilder ();
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
			return result.ToString ();
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
			var result = new StringBuilder ();
			AppendModifiers (result, property);
			result.Append (GetTypeReferenceString (property.Type));
			if (BreakLineAfterReturnType) {
				result.AppendLine ();
			} else {
				result.Append (" ");
			}

			AppendExplicitInterfaces (result, property.ExplicitInterfaceImplementations.Cast<ISymbol> ());

			if (property.IsIndexer) {
				result.Append (Highlight ("this", colorStyle.KeywordAccessors));
			} else {
				result.Append (HighlightSemantically (FilterEntityName (property.Name), colorStyle.UserPropertyDeclaration));
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
				result.Append (Highlight (" get", colorStyle.KeywordProperty) + ";");
			}

			if (property.SetMethod != null && IsAccessibleOrHasSourceCode (property.SetMethod)) {
				if (property.SetMethod.DeclaredAccessibility != property.DeclaredAccessibility) {
					result.Append (" ");
					AppendAccessibility (result, property.SetMethod);
				}
				result.Append (Highlight (" set", colorStyle.KeywordProperty) + ";");
			}
			result.Append (" }");

			return result.ToString ();
		}


		public TooltipInformation GetExternAliasTooltip (ExternAliasDirectiveSyntax externAliasDeclaration, DotNetProject project)
		{
			var result = new TooltipInformation ();
			result.SignatureMarkup = Highlight ("extern ", colorStyle.KeywordModifiers) + Highlight ("alias ", colorStyle.KeywordNamespace) + externAliasDeclaration.Identifier;
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

			var color = AlphaBlend (colorStyle.PlainText.Foreground, colorStyle.PlainText.Background, optionalAlpha);
			var colorString = MonoDevelop.Components.HelperMethods.GetColorString (color);

			var keywordSign = "<span foreground=\"" + colorString + "\">" + " (keyword)</span>";

			switch (node.Kind ()) {
			case SyntaxKind.AbstractKeyword:
				result.SignatureMarkup = Highlight ("abstract", colorStyle.KeywordModifiers) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("abstract", colorStyle.KeywordModifiers) + " modifier can be used with classes, methods, properties, indexers, and events.";
				break;
			case SyntaxKind.AddKeyword:
				result.SignatureMarkup = Highlight ("add", colorStyle.KeywordContext) + keywordSign;
				result.AddCategory ("Form", "[modifiers] " + Highlight ("add", colorStyle.KeywordContext) + " { accessor-body }");
				result.SummaryMarkup = "The " + Highlight ("add", colorStyle.KeywordContext) + " keyword is used to define a custom accessor for when an event is subscribed to. If supplied, a remove accessor must also be supplied.";
				break;
			case SyntaxKind.AscendingKeyword:
				result.SignatureMarkup = Highlight ("ascending", colorStyle.KeywordContext) + keywordSign;
				result.AddCategory ("Query Form", Highlight ("orderby", colorStyle.KeywordContext) + " ordering-statement " + Highlight ("ascending", colorStyle.KeywordContext));
				result.SummaryMarkup = "The " + Highlight ("ascending", colorStyle.KeywordContext) + " keyword is used to set the sorting order from smallest to largest in a query expression. This is the default behaviour.";
				break;
			case SyntaxKind.AsyncKeyword:
				result.SignatureMarkup = Highlight ("async", colorStyle.KeywordContext) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("async", colorStyle.KeywordContext) + " modifier is used to specify that a class method, anonymous method, or lambda expression is asynchronous.";
				break;
			case SyntaxKind.AsKeyword:
				result.SignatureMarkup = Highlight ("as", colorStyle.KeywordOperators) + keywordSign;
				result.AddCategory ("Form", "expression " + Highlight ("as", colorStyle.KeywordOperators) + " type");
				result.SummaryMarkup = "The " + Highlight ("as", colorStyle.KeywordOperators) + " operator is used to perform conversions between compatible types. ";
				break;
			case SyntaxKind.AwaitKeyword:
				result.SignatureMarkup = Highlight ("await", colorStyle.KeywordContext) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("await", colorStyle.KeywordContext) + " operator is used to specify that an " + Highlight ("async", colorStyle.KeywordContext) + " method is to have its execution suspended until the " + Highlight ("await", colorStyle.KeywordContext) +
				" task has completed.";
				break;
			case SyntaxKind.BaseKeyword:
				result.SignatureMarkup = Highlight ("base", colorStyle.KeywordAccessors) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("base", colorStyle.KeywordAccessors) + " keyword is used to access members of the base class from within a derived class.";
				break;
			case SyntaxKind.BreakKeyword:
				result.SignatureMarkup = Highlight ("break", colorStyle.KeywordJump) + keywordSign;
				result.AddCategory ("Form", Highlight ("break", colorStyle.KeywordJump) + ";");
				result.SummaryMarkup = "The " + Highlight ("break", colorStyle.KeywordJump) + " statement terminates the closest enclosing loop or switch statement in which it appears.";
				break;
			case SyntaxKind.CaseKeyword:
				result.SignatureMarkup = Highlight ("case", colorStyle.KeywordSelection) + keywordSign;
				result.AddCategory ("Form", Highlight ("case", colorStyle.KeywordSelection) + " constant-expression:" + Environment.NewLine +
				"  statement" + Environment.NewLine +
				"  jump-statement");
				result.SummaryMarkup = "";
				break;
			case SyntaxKind.CatchKeyword:
				result.SignatureMarkup = Highlight ("catch", colorStyle.KeywordException) + keywordSign;
				result.AddCategory ("Form", Highlight ("try", colorStyle.KeywordException) + " try-block" + Environment.NewLine +
				"  " + Highlight ("catch", colorStyle.KeywordException) + " (exception-declaration-1) catch-block-1" + Environment.NewLine +
				"  " + Highlight ("catch", colorStyle.KeywordException) + " (exception-declaration-2) catch-block-2" + Environment.NewLine +
				"  ..." + Environment.NewLine +
				Highlight ("try", colorStyle.KeywordException) + " try-block " + Highlight ("catch", colorStyle.KeywordException) + " catch-block");
				result.SummaryMarkup = "";
				break;
			case SyntaxKind.CheckedKeyword:
				result.SignatureMarkup = Highlight ("checked", colorStyle.KeywordOther) + keywordSign;
				result.AddCategory ("Form", Highlight ("checked", colorStyle.KeywordOther) + " block" + Environment.NewLine +
				"or" + Environment.NewLine +
				Highlight ("checked", colorStyle.KeywordOther) + " (expression)");
				result.SummaryMarkup = "The " + Highlight ("checked", colorStyle.KeywordOther) + " keyword is used to control the overflow-checking context for integral-type arithmetic operations and conversions. It can be used as an operator or a statement.";
				break;
			case SyntaxKind.ClassKeyword:
				result.SignatureMarkup = Highlight ("class", colorStyle.KeywordDeclaration) + keywordSign;
				result.AddCategory ("Form", "[attributes] [modifiers] " + Highlight ("class", colorStyle.KeywordDeclaration) + " identifier [:base-list] { class-body }[;]");
				result.SummaryMarkup = "Classes are declared using the keyword " + Highlight ("class", colorStyle.KeywordDeclaration) + ".";
				break;
			case SyntaxKind.ConstKeyword:
				result.SignatureMarkup = Highlight ("const", colorStyle.KeywordModifiers) + keywordSign;
				result.AddCategory ("Form", "[attributes] [modifiers] " + Highlight ("const", colorStyle.KeywordModifiers) + " type declarators;");
				result.SummaryMarkup = "The " + Highlight ("const", colorStyle.KeywordModifiers) + " keyword is used to modify a declaration of a field or local variable. It specifies that the value of the field or the local variable cannot be modified. ";
				break;
			case SyntaxKind.ContinueKeyword:
				result.SignatureMarkup = Highlight ("continue", colorStyle.KeywordJump) + keywordSign;
				result.AddCategory ("Form", Highlight ("continue", colorStyle.KeywordJump) + ";");
				result.SummaryMarkup = "The " + Highlight ("continue", colorStyle.KeywordJump) + " statement passes control to the next iteration of the enclosing iteration statement in which it appears.";
				break;
			case SyntaxKind.DefaultKeyword:
				result.SignatureMarkup = Highlight ("default", colorStyle.KeywordSelection) + keywordSign;
				result.SummaryMarkup = "";
				if (node.Parent != null) {
					if (node.Parent is DefaultExpressionSyntax) {
						result.AddCategory ("Form",
							Highlight ("default", colorStyle.KeywordSelection) + " (Type)");
						break;
					} else if (node.Parent is SwitchStatementSyntax) {
						result.AddCategory ("Form",
							Highlight ("switch", colorStyle.KeywordSelection) + " (expression) { " + Environment.NewLine +
							"  " + Highlight ("case", colorStyle.KeywordSelection) + " constant-expression:" + Environment.NewLine +
							"    statement" + Environment.NewLine +
							"    jump-statement" + Environment.NewLine +
							"  [" + Highlight ("default", colorStyle.KeywordSelection) + ":" + Environment.NewLine +
							"    statement" + Environment.NewLine +
							"    jump-statement]" + Environment.NewLine +
							"}");
						break;
					}
				}
				result.AddCategory ("Form",
					Highlight ("default", colorStyle.KeywordSelection) + " (Type)" + Environment.NewLine + Environment.NewLine +
					"or" + Environment.NewLine + Environment.NewLine +
					Highlight ("switch", colorStyle.KeywordSelection) + " (expression) { " + Environment.NewLine +
					"  " + Highlight ("case", colorStyle.KeywordSelection) + " constant-expression:" + Environment.NewLine +
					"    statement" + Environment.NewLine +
					"    jump-statement" + Environment.NewLine +
					"  [" + Highlight ("default", colorStyle.KeywordSelection) + ":" + Environment.NewLine +
					"    statement" + Environment.NewLine +
					"    jump-statement]" + Environment.NewLine +
					"}");
				break;
			case SyntaxKind.DelegateKeyword:
				result.SignatureMarkup = Highlight ("delegate", colorStyle.KeywordDeclaration) + keywordSign;
				result.AddCategory ("Form", "[attributes] [modifiers] " + Highlight ("delegate", colorStyle.KeywordDeclaration) + " result-type identifier ([formal-parameters]);");
				result.SummaryMarkup = "A " + Highlight ("delegate", colorStyle.KeywordDeclaration) + " declaration defines a reference type that can be used to encapsulate a method with a specific signature.";
				break;
			case SyntaxKind.IdentifierName:
				if (node.ToFullString () == "dynamic") {
					result.SignatureMarkup = Highlight ("dynamic", colorStyle.KeywordContext) + keywordSign;
					result.SummaryMarkup = "The " + Highlight ("dynamic", colorStyle.KeywordContext) + " type allows for an object to bypass compile-time type checking and resolve type checking during run-time.";
				} else {
					return null;
				}
				break;
			case SyntaxKind.DescendingKeyword:
				result.SignatureMarkup = Highlight ("descending", colorStyle.KeywordContext) + keywordSign;
				result.AddCategory ("Query Form", Highlight ("orderby", colorStyle.KeywordContext) + " ordering-statement " + Highlight ("descending", colorStyle.KeywordContext));
				result.SummaryMarkup = "The " + Highlight ("descending", colorStyle.KeywordContext) + " keyword is used to set the sorting order from largest to smallest in a query expression.";
				break;
			case SyntaxKind.DoKeyword:
				result.SignatureMarkup = Highlight ("do", colorStyle.KeywordIteration) + keywordSign;
				result.AddCategory ("Form", Highlight ("do", colorStyle.KeywordIteration) + " statement " + Highlight ("while", colorStyle.KeywordIteration) + " (expression);");
				result.SummaryMarkup = "The " + Highlight ("do", colorStyle.KeywordIteration) + " statement executes a statement or a block of statements repeatedly until a specified expression evaluates to false.";
				break;
			case SyntaxKind.ElseKeyword:
				result.SignatureMarkup = Highlight ("else", colorStyle.KeywordSelection) + keywordSign;
				result.AddCategory ("Form", Highlight ("if", colorStyle.KeywordSelection) + " (expression)" + Environment.NewLine +
					"  statement1" + Environment.NewLine +
					"  [" + Highlight ("else", colorStyle.KeywordSelection) + Environment.NewLine +
					"  statement2]");
				result.SummaryMarkup = "";
				break;
			case SyntaxKind.EnumKeyword:
				result.SignatureMarkup = Highlight ("enum", colorStyle.KeywordDeclaration) + keywordSign;
				result.AddCategory ("Form", "[attributes] [modifiers] " + Highlight ("enum", colorStyle.KeywordDeclaration) + " identifier [:base-type] {enumerator-list} [;]");
				result.SummaryMarkup = "The " + Highlight ("enum", colorStyle.KeywordDeclaration) + " keyword is used to declare an enumeration, a distinct type consisting of a set of named constants called the enumerator list.";
				break;
			case SyntaxKind.EventKeyword:
				result.SignatureMarkup = Highlight ("event", colorStyle.KeywordModifiers) + keywordSign;
				result.AddCategory ("Form", "[attributes] [modifiers] " + Highlight ("event", colorStyle.KeywordModifiers) + " type declarator;" + Environment.NewLine +
					"[attributes] [modifiers] " + Highlight ("event", colorStyle.KeywordModifiers) + " type member-name {accessor-declarations};");
				result.SummaryMarkup = "Specifies an event.";
				break;
			case SyntaxKind.ExplicitKeyword:
				result.SignatureMarkup = Highlight ("explicit", colorStyle.KeywordOperatorDeclaration) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("explicit", colorStyle.KeywordOperatorDeclaration) + " keyword is used to declare an explicit user-defined type conversion operator.";
				break;
			case SyntaxKind.ExternKeyword:
				result.SignatureMarkup = Highlight ("extern", colorStyle.KeywordModifiers) + keywordSign;
				result.SummaryMarkup = "Use the " + Highlight ("extern", colorStyle.KeywordModifiers) + " modifier in a method declaration to indicate that the method is implemented externally. A common use of the extern modifier is with the DllImport attribute.";
				break;
			case SyntaxKind.FinallyKeyword:
				result.SignatureMarkup = Highlight ("finally", colorStyle.KeywordException) + keywordSign;
				result.AddCategory ("Form", Highlight ("try", colorStyle.KeywordException) + " try-block " + Highlight ("finally", colorStyle.KeywordException) + " finally-block");
				result.SummaryMarkup = "The " + Highlight ("finally", colorStyle.KeywordException) + " block is useful for cleaning up any resources allocated in the try block. Control is always passed to the finally block regardless of how the try block exits.";
				break;
			case SyntaxKind.FixedKeyword:
				result.SignatureMarkup = Highlight ("fixed", colorStyle.KeywordOther) + keywordSign;
				result.AddCategory ("Form", Highlight ("fixed", colorStyle.KeywordOther) + " ( type* ptr = expr ) statement");
				result.SummaryMarkup = "Prevents relocation of a variable by the garbage collector.";
				break;
			case SyntaxKind.ForKeyword:
				result.SignatureMarkup = Highlight ("for", colorStyle.KeywordIteration) + keywordSign;
				result.AddCategory ("Form", Highlight ("for", colorStyle.KeywordIteration) + " ([initializers]; [expression]; [iterators]) statement");
				result.SummaryMarkup = "The " + Highlight ("for", colorStyle.KeywordIteration) + " loop executes a statement or a block of statements repeatedly until a specified expression evaluates to false.";
				break;
			case SyntaxKind.ForEachKeyword:
				result.SignatureMarkup = Highlight ("foreach", colorStyle.KeywordIteration) + keywordSign;
				result.AddCategory ("Form", Highlight ("foreach", colorStyle.KeywordIteration) + " (type identifier " + Highlight ("in", colorStyle.KeywordIteration) + " expression) statement");
				result.SummaryMarkup = "The " + Highlight ("foreach", colorStyle.KeywordIteration) + " statement repeats a group of embedded statements for each element in an array or an object collection. ";
				break;
			case SyntaxKind.FromKeyword:
				result.SignatureMarkup = Highlight ("from", colorStyle.KeywordContext) + keywordSign;
				result.AddCategory ("Form", Highlight ("from", colorStyle.KeywordContext) + " range-variable " + Highlight ("in", colorStyle.KeywordIteration)
				+ " data-source [query clauses] " + Highlight ("select", colorStyle.KeywordContext) + " product-expression");
				result.SummaryMarkup = "The " + Highlight ("from", colorStyle.KeywordContext) + " keyword marks the beginning of a query expression and defines the data source and local variable to represent the elements in the sequence.";
				break;
			case SyntaxKind.GetKeyword:
				result.SignatureMarkup = Highlight ("get", colorStyle.KeywordContext) + keywordSign;
				result.AddCategory ("Form", "[modifiers] " + Highlight ("get", colorStyle.KeywordContext) + " [ { accessor-body } ]");
				result.SummaryMarkup = "The " + Highlight ("get", colorStyle.KeywordContext) + " keyword is used to define an accessor method to retrieve the value of the property or indexer element.";
				break;
			case SyntaxKind.GlobalKeyword:
				result.SignatureMarkup = Highlight ("global", colorStyle.KeywordContext) + keywordSign;
				result.AddCategory ("Form", Highlight ("global", colorStyle.KeywordContext) + " :: type");
				result.SummaryMarkup = "The " + Highlight ("global", colorStyle.KeywordContext) + " keyword is used to specify a type is within the global namespace.";
				break;
			case SyntaxKind.GotoKeyword:
				result.SignatureMarkup = Highlight ("goto", colorStyle.KeywordJump) + keywordSign;
				result.AddCategory ("Form", Highlight ("goto", colorStyle.KeywordJump) + " identifier;" + Environment.NewLine +
				Highlight ("goto", colorStyle.KeywordJump) + " " + Highlight ("case", colorStyle.KeywordSelection) + " constant-expression;" + Environment.NewLine +
				Highlight ("goto", colorStyle.KeywordJump) + " " + Highlight ("default", colorStyle.KeywordSelection) + ";");
				result.SummaryMarkup = "The " + Highlight ("goto", colorStyle.KeywordJump) + " statement transfers the program control directly to a labeled statement. ";
				break;
			case SyntaxKind.GroupKeyword:
				result.SignatureMarkup = Highlight ("group", colorStyle.KeywordContext) + keywordSign;
				result.AddCategory ("Query Form", Highlight ("group", colorStyle.KeywordContext) + " range-variable " + Highlight ("by", colorStyle.KeywordContext) + "key-value"
					+ Environment.NewLine + Environment.NewLine + "or" + Environment.NewLine + Environment.NewLine +
				Highlight ("group", colorStyle.KeywordContext) + " range-variable " + Highlight ("by", colorStyle.KeywordContext) + " key-value " + Highlight ("into", colorStyle.KeywordContext) + " group-name ");
				result.SummaryMarkup = "The " + Highlight ("group", colorStyle.KeywordContext) + " keyword groups elements together from a query which match the key value and stores the result in an "
					+ Highlight ("IGrouping&lt;TKey, TElement&gt;", colorStyle.KeywordTypes) + ". It can also be stored in a group for further use in the query with 'into'.";
				break;
			case SyntaxKind.IfKeyword:
				result.SignatureMarkup = Highlight ("if", colorStyle.KeywordSelection) + keywordSign;
				result.AddCategory ("Form", Highlight ("if", colorStyle.KeywordSelection) + " (expression)" + Environment.NewLine +
					"  statement1" + Environment.NewLine +
					"  [" + Highlight ("else", colorStyle.KeywordSelection) + Environment.NewLine +
					"  statement2]");
				result.SummaryMarkup = "The " + Highlight ("if", colorStyle.KeywordSelection) + " statement selects a statement for execution based on the value of a Boolean expression. ";
				break;
			case SyntaxKind.IntoKeyword:
				result.SignatureMarkup = Highlight ("into", colorStyle.KeywordContext) + keywordSign;
				result.AddCategory ("Query Form", Highlight ("group", colorStyle.KeywordContext) + " range-variable " + Highlight ("by", colorStyle.KeywordContext) + " key-value " + Highlight ("into", colorStyle.KeywordContext) + " group-name ");
				result.SummaryMarkup = "The " + Highlight ("into", colorStyle.KeywordContext) + " keyword stores the result of a group statement for further use in the query.";
				break;
			case SyntaxKind.ImplicitKeyword:
				result.SignatureMarkup = Highlight ("implicit", colorStyle.KeywordOperatorDeclaration) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("implicit", colorStyle.KeywordOperatorDeclaration) + " keyword is used to declare an implicit user-defined type conversion operator.";
				break;
			case SyntaxKind.InKeyword:
				result.SignatureMarkup = Highlight ("in", colorStyle.KeywordIteration) + keywordSign;
				if (node.Parent != null) {
					if (node.Parent is ForEachStatementSyntax) {
						result.AddCategory ("Form",
							Highlight ("foreach", colorStyle.KeywordIteration) + " (type identifier " + Highlight ("in", colorStyle.KeywordIteration) + " expression) statement");
						break;
					}
					if (node.Parent is FromClauseSyntax) {
						result.AddCategory ("Form",
							Highlight ("from", colorStyle.KeywordContext) + " range-variable " + Highlight ("in", colorStyle.KeywordIteration) + " data-source [query clauses] " + Highlight ("select", colorStyle.KeywordContext) + " product-expression");
						break;
					}
					if (node.Parent is TypeParameterConstraintClauseSyntax) {
						result.AddCategory ("Form",
							Highlight ("interface", colorStyle.KeywordDeclaration) + " IMyInterface&lt;" + Highlight ("in", colorStyle.KeywordIteration) + " T&gt; {}");
						break;
					}
				}
				result.AddCategory ("Form", Highlight ("foreach", colorStyle.KeywordIteration) + " (type identifier " + Highlight ("in", colorStyle.KeywordIteration) + " expression) statement" + Environment.NewLine + Environment.NewLine +
					"or" + Environment.NewLine + Environment.NewLine +
				Highlight ("from", colorStyle.KeywordContext) + " range-variable " + Highlight ("in", colorStyle.KeywordIteration) + " data-source [query clauses] " + Highlight ("select", colorStyle.KeywordContext) + " product-expression" + Environment.NewLine + Environment.NewLine +
					"or" + Environment.NewLine + Environment.NewLine +
				Highlight ("interface", colorStyle.KeywordDeclaration) + " IMyInterface&lt;" + Highlight ("in", colorStyle.KeywordIteration) + " T&gt; {}"
				);
				break;
			case SyntaxKind.InterfaceKeyword:
				result.SignatureMarkup = Highlight ("interface", colorStyle.KeywordDeclaration) + keywordSign;
				result.AddCategory ("Form", "[attributes] [modifiers] " + Highlight ("interface", colorStyle.KeywordDeclaration) + " identifier [:base-list] {interface-body}[;]");
				result.SummaryMarkup = "An interface defines a contract. A class or struct that implements an interface must adhere to its contract.";
				break;
			case SyntaxKind.InternalKeyword:
				result.SignatureMarkup = Highlight ("internal", colorStyle.KeywordModifiers) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("internal", colorStyle.KeywordModifiers) + " keyword is an access modifier for types and type members. Internal members are accessible only within files in the same assembly.";
				break;
			case SyntaxKind.IsKeyword:
				result.SignatureMarkup = Highlight ("is", colorStyle.KeywordOperators) + keywordSign;
				result.AddCategory ("Form", "expression " + Highlight ("is", colorStyle.KeywordOperators) + " type");
				result.SummaryMarkup = "The " + Highlight ("is", colorStyle.KeywordOperators) + " operator is used to check whether the run-time type of an object is compatible with a given type.";
				break;
			case SyntaxKind.JoinKeyword:
				result.SignatureMarkup = Highlight ("join", colorStyle.KeywordContext) + keywordSign;
				result.AddCategory ("Query Form", Highlight ("join", colorStyle.KeywordContext) + " range-variable2 " + Highlight ("in", colorStyle.KeywordContext) + " range2 " + Highlight ("on", colorStyle.KeywordContext)
					+ " statement1 " + Highlight ("equals", colorStyle.KeywordContext) + " statement2 [ " + Highlight ("into", colorStyle.KeywordContext) + " group-name ]");
				result.SummaryMarkup = "The " + Highlight ("join", colorStyle.KeywordContext) + " clause produces a new sequence of elements from two source sequences on a given equality condition.";
				break;
			case SyntaxKind.LetKeyword:
				result.SignatureMarkup = Highlight ("let", colorStyle.KeywordContext) + keywordSign;
				result.AddCategory ("Query Form", Highlight ("let", colorStyle.KeywordContext) + " range-variable = expression");
				result.SummaryMarkup = "The " + Highlight ("let", colorStyle.KeywordContext) + " clause allows for a sub-expression to have its value stored in a new range variable for use later in the query.";
				break;
			case SyntaxKind.LockKeyword:
				result.SignatureMarkup = Highlight ("lock", colorStyle.KeywordOther) + keywordSign;
				result.AddCategory ("Form", Highlight ("lock", colorStyle.KeywordOther) + " (expression) statement_block");
				result.SummaryMarkup = "The " + Highlight ("lock", colorStyle.KeywordOther) + " keyword marks a statement block as a critical section by obtaining the mutual-exclusion lock for a given object, executing a statement, and then releasing the lock. ";
				break;
			case SyntaxKind.NamespaceKeyword:
				result.SignatureMarkup = Highlight ("namespace", colorStyle.KeywordNamespace) + keywordSign;
				result.AddCategory ("Form", Highlight ("namespace", colorStyle.KeywordNamespace) + " name[.name1] ...] {" + Environment.NewLine +
					"type-declarations" + Environment.NewLine +
					" }");
				result.SummaryMarkup = "The " + Highlight ("namespace", colorStyle.KeywordNamespace) + " keyword is used to declare a scope. ";
				break;
			case SyntaxKind.NewKeyword:
				result.SignatureMarkup = Highlight ("new", colorStyle.KeywordOperators) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("new", colorStyle.KeywordOperators) + " keyword can be used as an operator or as a modifier. The operator is used to create objects on the heap and invoke constructors. The modifier is used to hide an inherited member from a base class member.";
				break;
			case SyntaxKind.NullKeyword:
				result.SignatureMarkup = Highlight ("null", colorStyle.KeywordConstants) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("null", colorStyle.KeywordConstants) + " keyword is a literal that represents a null reference, one that does not refer to any object. " + Highlight ("null", colorStyle.KeywordConstants) + " is the default value of reference-type variables.";
				break;
			case SyntaxKind.OperatorKeyword:
				result.SignatureMarkup = Highlight ("operator", colorStyle.KeywordOperatorDeclaration) + keywordSign;
				result.AddCategory ("Form", Highlight ("public static ", colorStyle.KeywordModifiers) + "result-type " + Highlight ("operator", colorStyle.KeywordOperatorDeclaration) + " unary-operator ( op-type operand )" + Environment.NewLine +
				Highlight ("public static ", colorStyle.KeywordModifiers) + "result-type " + Highlight ("operator", colorStyle.KeywordOperatorDeclaration) + " binary-operator (" + Environment.NewLine +
					"op-type operand," + Environment.NewLine +
					"op-type2 operand2" + Environment.NewLine +
					" )" + Environment.NewLine +
					Highlight ("public static ", colorStyle.KeywordModifiers) + Highlight ("implicit operator", colorStyle.KeywordOperatorDeclaration) + " conv-type-out ( conv-type-in operand )" + Environment.NewLine +
					Highlight ("public static ", colorStyle.KeywordModifiers) + Highlight ("explicit operator", colorStyle.KeywordOperatorDeclaration) + " conv-type-out ( conv-type-in operand )"
				);
				result.SummaryMarkup = "The " + Highlight ("operator", colorStyle.KeywordOperatorDeclaration) + " keyword is used to declare an operator in a class or struct declaration.";
				break;
			case SyntaxKind.OrderByKeyword:
				result.SignatureMarkup = Highlight ("orderby", colorStyle.KeywordContext) + keywordSign;
				result.AddCategory ("Query Form", Highlight ("orderby", colorStyle.KeywordContext) + " order-key1 [ " + Highlight ("ascending", colorStyle.KeywordContext) + "|" + Highlight ("descending", colorStyle.KeywordContext) + " , [order-key2, ...]");
				result.SummaryMarkup = "The " + Highlight ("orderby", colorStyle.KeywordContext) + " clause specifies for the returned sequence to be sorted on a given element in either ascending or descending order.";
				break;
			case SyntaxKind.OutKeyword:
				result.SignatureMarkup = Highlight ("out", colorStyle.KeywordParameter) + keywordSign;
				if (node.Parent != null) {
					if (node.Parent is TypeParameterSyntax) {
						result.AddCategory ("Form",
							Highlight ("interface", colorStyle.KeywordDeclaration) + " IMyInterface&lt;" + Highlight ("out", colorStyle.KeywordParameter) + " T&gt; {}");
						break;
					}
					if (node.Parent is ParameterSyntax) {
						result.AddCategory ("Form",
							Highlight ("out", colorStyle.KeywordParameter) + " parameter-name");
						result.SummaryMarkup = "The " + Highlight ("out", colorStyle.KeywordParameter) + " method parameter keyword on a method parameter causes a method to refer to the same variable that was passed into the method.";
						break;
					}
				}

				result.AddCategory ("Form",
					Highlight ("out", colorStyle.KeywordParameter) + " parameter-name" + Environment.NewLine + Environment.NewLine +
					"or" + Environment.NewLine + Environment.NewLine +
					Highlight ("interface", colorStyle.KeywordDeclaration) + " IMyInterface&lt;" + Highlight ("out", colorStyle.KeywordParameter) + " T&gt; {}"
				);
				break;
			case SyntaxKind.OverrideKeyword:
				result.SignatureMarkup = Highlight ("override", colorStyle.KeywordModifiers) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("override", colorStyle.KeywordModifiers) + " modifier is used to override a method, a property, an indexer, or an event.";
				break;
			case SyntaxKind.ParamKeyword:
				result.SignatureMarkup = Highlight ("params", colorStyle.KeywordParameter) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("params", colorStyle.KeywordParameter) + " keyword lets you specify a method parameter that takes an argument where the number of arguments is variable.";
				break;
			case SyntaxKind.PartialKeyword:
				result.SignatureMarkup = Highlight ("partial", colorStyle.KeywordContext) + keywordSign;
				if (node.Parent != null) {
					if (node.Parent is TypeDeclarationSyntax) {
						result.AddCategory ("Form", "[modifiers] " + Highlight ("partial", colorStyle.KeywordContext) + " type-declaration");
						result.SummaryMarkup = "The " + Highlight ("partial", colorStyle.KeywordContext) + " keyword on a type declaration allows for the definition to be split into multiple files.";
						break;
					} else if (node.Parent is MethodDeclarationSyntax) {
						result.AddCategory ("Form", Highlight ("partial", colorStyle.KeywordContext) + " method-declaration");
						result.SummaryMarkup = "The " + Highlight ("partial", colorStyle.KeywordContext) + " keyword on a method declaration allows for the implementation of a method to be defined in another part of the partial class.";
					}
				} else
					result.AddCategory ("Form", "[modifiers] " + Highlight ("partial", colorStyle.KeywordContext) + " type-declaration" + Environment.NewLine + Environment.NewLine + "or" + Environment.NewLine + Environment.NewLine +
					Highlight ("partial", colorStyle.KeywordContext) + " method-declaration");
				break;
			case SyntaxKind.PrivateKeyword:
				result.SignatureMarkup = Highlight ("private", colorStyle.KeywordModifiers) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("private", colorStyle.KeywordModifiers) + " keyword is a member access modifier. Private access is the least permissive access level. Private members are accessible only within the body of the class or the struct in which they are declared.";
				break;
			case SyntaxKind.ProtectedKeyword:
				result.SignatureMarkup = Highlight ("protected", colorStyle.KeywordModifiers) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("protected", colorStyle.KeywordModifiers) + " keyword is a member access modifier. A protected member is accessible from within the class in which it is declared, and from within any class derived from the class that declared this member.";
				break;
			case SyntaxKind.PublicKeyword:
				result.SignatureMarkup = Highlight ("public", colorStyle.KeywordModifiers) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("public", colorStyle.KeywordModifiers) + " keyword is an access modifier for types and type members. Public access is the most permissive access level. There are no restrictions on accessing public members.";
				break;
			case SyntaxKind.ReadOnlyKeyword:
				result.SignatureMarkup = Highlight ("readonly", colorStyle.KeywordModifiers) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("readonly", colorStyle.KeywordModifiers) + " keyword is a modifier that you can use on fields. When a field declaration includes a " + Highlight ("readonly", colorStyle.KeywordModifiers) + " modifier, assignments to the fields introduced by the declaration can only occur as part of the declaration or in a constructor in the same class.";
				break;
			case SyntaxKind.RefKeyword:
				result.SignatureMarkup = Highlight ("ref", colorStyle.KeywordParameter) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("ref", colorStyle.KeywordParameter) + " method parameter keyword on a method parameter causes a method to refer to the same variable that was passed into the method.";
				break;
			case SyntaxKind.RemoveKeyword:
				result.SignatureMarkup = Highlight ("remove", colorStyle.KeywordContext) + keywordSign;
				result.AddCategory ("Form", "[modifiers] " + Highlight ("remove", colorStyle.KeywordContext) + " { accessor-body }");
				result.SummaryMarkup = "The " + Highlight ("remove", colorStyle.KeywordContext) + " keyword is used to define a custom accessor for when an event is unsubscribed from. If supplied, an add accessor must also be supplied.";
				break;
			case SyntaxKind.ReturnKeyword:
				result.SignatureMarkup = Highlight ("return", colorStyle.KeywordJump) + keywordSign;
				result.AddCategory ("Form", Highlight ("return", colorStyle.KeywordJump) + " [expression];");
				result.SummaryMarkup = "The " + Highlight ("return", colorStyle.KeywordJump) + " statement terminates execution of the method in which it appears and returns control to the calling method.";
				break;
			case SyntaxKind.SelectKeyword:
				result.SignatureMarkup = Highlight ("select", colorStyle.KeywordContext) + keywordSign;
				result.AddCategory ("Query Form", Highlight ("select", colorStyle.KeywordContext) + " return-type");
				result.SummaryMarkup = "The " + Highlight ("select", colorStyle.KeywordContext) + " clause specifies the type of value to return from the query.";
				break;
			case SyntaxKind.SealedKeyword:
				result.SignatureMarkup = Highlight ("sealed", colorStyle.KeywordModifiers) + keywordSign;
				result.SummaryMarkup = "A sealed class cannot be inherited.";
				break;
			case SyntaxKind.SetKeyword:
				result.SignatureMarkup = Highlight ("set", colorStyle.KeywordContext) + keywordSign;
				result.AddCategory ("Form", "[modifiers] " + Highlight ("set", colorStyle.KeywordContext) + " [ { accessor-body } ]");
				result.SummaryMarkup = "The " + Highlight ("set", colorStyle.KeywordContext) + " keyword is used to define an accessor method to assign to the value of the property or indexer element.";
				break;
			case SyntaxKind.SizeOfKeyword:
				result.SignatureMarkup = Highlight ("sizeof", colorStyle.KeywordOperators) + keywordSign;
				result.AddCategory ("Form", Highlight ("sizeof", colorStyle.KeywordOperators) + " (type)");
				result.SummaryMarkup = "The " + Highlight ("sizeof", colorStyle.KeywordOperators) + " operator is used to obtain the size in bytes for a value type.";
				break;
			case SyntaxKind.StackAllocKeyword:
				result.SignatureMarkup = Highlight ("stackalloc", colorStyle.KeywordOperators) + keywordSign;
				result.AddCategory ("Form", "type * ptr = " + Highlight ("stackalloc", colorStyle.KeywordOperators) + " type [ expr ];");
				result.SummaryMarkup = "Allocates a block of memory on the stack.";
				break;
			case SyntaxKind.StaticKeyword:
				result.SignatureMarkup = Highlight ("static", colorStyle.KeywordModifiers) + keywordSign;
				result.SummaryMarkup = "Use the " + Highlight ("static", colorStyle.KeywordModifiers) + " modifier to declare a static member, which belongs to the type itself rather than to a specific object.";
				break;
			case SyntaxKind.StructKeyword:
				result.SignatureMarkup = Highlight ("struct", colorStyle.KeywordDeclaration) + keywordSign;
				result.AddCategory ("Form", "[attributes] [modifiers] " + Highlight ("struct", colorStyle.KeywordDeclaration) + " identifier [:interfaces] body [;]");
				result.SummaryMarkup = "A " + Highlight ("struct", colorStyle.KeywordDeclaration) + " type is a value type that can contain constructors, constants, fields, methods, properties, indexers, operators, events, and nested types. ";
				break;
			case SyntaxKind.SwitchKeyword:
				result.SignatureMarkup = Highlight ("switch", colorStyle.KeywordSelection) + keywordSign;
				result.AddCategory ("Form", Highlight ("switch", colorStyle.KeywordSelection) + " (expression)" + Environment.NewLine +
					" {" + Environment.NewLine +
					"  " + Highlight ("case", colorStyle.KeywordSelection) + " constant-expression:" + Environment.NewLine +
					"  statement" + Environment.NewLine +
					"  jump-statement" + Environment.NewLine +
					"  [" + Highlight ("default", colorStyle.KeywordSelection) + ":" + Environment.NewLine +
					"  statement" + Environment.NewLine +
					"  jump-statement]" + Environment.NewLine +
					" }");
				result.SummaryMarkup = "The " + Highlight ("switch", colorStyle.KeywordSelection) + " statement is a control statement that handles multiple selections by passing control to one of the " + Highlight ("case", colorStyle.KeywordSelection) + " statements within its body.";
				break;
			case SyntaxKind.ThisKeyword:
				result.SignatureMarkup = Highlight ("this", colorStyle.KeywordAccessors) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("this", colorStyle.KeywordAccessors) + " keyword refers to the current instance of the class.";
				break;
			case SyntaxKind.ThrowKeyword:
				result.SignatureMarkup = Highlight ("throw", colorStyle.KeywordException) + keywordSign;
				result.AddCategory ("Form", Highlight ("throw", colorStyle.KeywordException) + " [expression];");
				result.SummaryMarkup = "The " + Highlight ("throw", colorStyle.KeywordException) + " statement is used to signal the occurrence of an anomalous situation (exception) during the program execution.";
				break;
			case SyntaxKind.TryKeyword:
				result.SignatureMarkup = Highlight ("try", colorStyle.KeywordException) + keywordSign;
				result.AddCategory ("Form", Highlight ("try", colorStyle.KeywordException) + " try-block" + Environment.NewLine +
					"  " + Highlight ("catch", colorStyle.KeywordException) + " (exception-declaration-1) catch-block-1 " + Environment.NewLine +
					"  " + Highlight ("catch", colorStyle.KeywordException) + " (exception-declaration-2) catch-block-2 " + Environment.NewLine +
					"..." + Environment.NewLine +
					Highlight ("try", colorStyle.KeywordException) + " try-block " + Highlight ("catch", colorStyle.KeywordException) + " catch-block");
				result.SummaryMarkup = "The try-catch statement consists of a " + Highlight ("try", colorStyle.KeywordException) + " block followed by one or more " + Highlight ("catch", colorStyle.KeywordException) + " clauses, which specify handlers for different exceptions.";
				break;
			case SyntaxKind.TypeOfKeyword:
				result.SignatureMarkup = Highlight ("typeof", colorStyle.KeywordOperators) + keywordSign;
				result.AddCategory ("Form", Highlight ("typeof", colorStyle.KeywordOperators) + "(type)");
				result.SummaryMarkup = "The " + Highlight ("typeof", colorStyle.KeywordOperators) + " operator is used to obtain the System.Type object for a type.";
				break;
			case SyntaxKind.UncheckedKeyword:
				result.SignatureMarkup = Highlight ("unchecked", colorStyle.KeywordOther) + keywordSign;
				result.AddCategory ("Form", Highlight ("unchecked", colorStyle.KeywordOther) + " block" + Environment.NewLine +
				Highlight ("unchecked", colorStyle.KeywordOther) + " (expression)");
				result.SummaryMarkup = "The " + Highlight ("unchecked", colorStyle.KeywordOther) + " keyword is used to control the overflow-checking context for integral-type arithmetic operations and conversions.";
				break;
			case SyntaxKind.UnsafeKeyword:
				result.SignatureMarkup = Highlight ("unsafe", colorStyle.KeywordOther) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("unsafe", colorStyle.KeywordOther) + " keyword denotes an unsafe context, which is required for any operation involving pointers.";
				break;
			case SyntaxKind.UsingKeyword:
				result.SignatureMarkup = Highlight ("using", colorStyle.KeywordNamespace) + keywordSign;
				result.AddCategory ("Form", Highlight ("using", colorStyle.KeywordNamespace) + " (expression | type identifier = initializer) statement" + Environment.NewLine +
				Highlight ("using", colorStyle.KeywordNamespace) + " [alias = ]class_or_namespace;");
				result.SummaryMarkup = "The " + Highlight ("using", colorStyle.KeywordNamespace) + " directive creates an alias for a namespace or imports types defined in other namespaces. The " + Highlight ("using", colorStyle.KeywordNamespace) + " statement defines a scope at the end of which an object will be disposed.";
				break;
			case SyntaxKind.VirtualKeyword:
				result.SignatureMarkup = Highlight ("virtual", colorStyle.KeywordModifiers) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("virtual", colorStyle.KeywordModifiers) + " keyword is used to modify a method, property, indexer, or event declaration and allow for it to be overridden in a derived class.";
				break;
			case SyntaxKind.VolatileKeyword:
				result.SignatureMarkup = Highlight ("volatile", colorStyle.KeywordModifiers) + keywordSign;
				result.AddCategory ("Form", Highlight ("volatile", colorStyle.KeywordModifiers) + " declaration");
				result.SummaryMarkup = "The " + Highlight ("volatile", colorStyle.KeywordModifiers) + " keyword indicates that a field can be modified in the program by something such as the operating system, the hardware, or a concurrently executing thread.";
				break;
			case SyntaxKind.VoidKeyword:
				result.SignatureMarkup = Highlight ("void", colorStyle.KeywordTypes) + keywordSign;
				break;
			case SyntaxKind.WhereKeyword:
				result.SignatureMarkup = Highlight ("where", colorStyle.KeywordContext) + keywordSign;
				if (node.Parent != null) {
					if (node.Parent is WhereClauseSyntax) {
						result.AddCategory ("Query Form", Highlight ("where", colorStyle.KeywordContext) + " condition");
						result.SummaryMarkup = "The " + Highlight ("where", colorStyle.KeywordContext) + " clause specifies which elements from the data source to be returned according to a given condition.";
						break;
					}
					if (node.Parent is TypeConstraintSyntax) {
						result.AddCategory ("Form", "generic-class-declaration " + Highlight ("where", colorStyle.KeywordContext) + " type-parameter : type-constraint");
						result.SummaryMarkup = "The " + Highlight ("where", colorStyle.KeywordContext) + " clause constrains which types can be used as the type parameter in a generic declaration.";
						break;
					}
				} else {
					result.AddCategory ("Form", "generic-class-declaration " + Highlight ("where", colorStyle.KeywordContext) + " type-parameter : type-constraint"
					+ Environment.NewLine + Environment.NewLine + "or" + Environment.NewLine + Environment.NewLine + "query-clauses " + Highlight ("where", colorStyle.KeywordContext) +
					" condition" + " [query-clauses]");
				}
				break;
			case SyntaxKind.YieldKeyword:
				result.SignatureMarkup = Highlight ("yield", colorStyle.KeywordContext) + keywordSign;
				result.AddCategory ("Form", Highlight ("yield", colorStyle.KeywordContext) + Highlight ("break", colorStyle.KeywordJump) + Environment.NewLine
				+ Environment.NewLine + "or" + Environment.NewLine + Environment.NewLine
				+ Highlight ("yield", colorStyle.KeywordContext) + Highlight ("return", colorStyle.KeywordJump) + " expression");
				result.SummaryMarkup = "The " + Highlight ("yield", colorStyle.KeywordContext) + " keyword is used to indicate that a method, get accessor, or operator is an iterator.";
				break;
			case SyntaxKind.WhileKeyword:
				result.SignatureMarkup = Highlight ("while", colorStyle.KeywordIteration) + keywordSign;
				result.AddCategory ("Form", Highlight ("while", colorStyle.KeywordIteration) + " (expression) statement");
				result.SummaryMarkup = "The " + Highlight ("while", colorStyle.KeywordIteration) + " statement executes a statement or a block of statements until a specified expression evaluates to false. ";
				break;
			default:
				return null;
			}
			return result;
		}

		public TooltipInformation GetConstraintTooltip (SyntaxToken keyword)
		{
			var result = new TooltipInformation ();

			var color = AlphaBlend (colorStyle.PlainText.Foreground, colorStyle.PlainText.Background, optionalAlpha);
			var colorString = MonoDevelop.Components.HelperMethods.GetColorString (color);

			var keywordSign = "<span foreground=\"" + colorString + "\">" + " (keyword)</span>";

			result.SignatureMarkup = Highlight (keyword.ToFullString (), colorStyle.KeywordTypes) + keywordSign;

			switch (keyword.Parent.Kind ()) {
			case SyntaxKind.ClassConstraint:
				result.AddCategory ("Constraint", "The type argument must be a reference type; this applies also to any class, interface, delegate, or array type.");
				break;
			case SyntaxKind.ConstructorConstraint:
				result.AddCategory ("Constraint", "The type argument must have a public parameterless constructor. When used together with other constraints, the new() constraint must be specified last.");
				break;
			case SyntaxKind.StructConstraint:
				result.AddCategory ("Constraint", "The type argument must be a value type. Any value type except Nullable can be specified. See Using Nullable Types (C# Programming Guide) for more information.");
				break;
			}

			return result;
		}

		public TooltipInformation GetTypeOfTooltip (TypeOfExpressionSyntax typeOfExpression, ITypeSymbol resolveResult)
		{
			var result = new TooltipInformation ();
			if (resolveResult == null) {
				result.SignatureMarkup = Ambience.EscapeText (typeOfExpression.Type.ToString ());
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
			var result = new StringBuilder ();
			AppendModifiers (result, evt);
			result.Append (Highlight ("event ", colorStyle.KeywordModifiers));
			result.Append (GetTypeReferenceString (evt.Type));
			if (BreakLineAfterReturnType) {
				result.AppendLine ();
			} else {
				result.Append (" ");
			}

			AppendExplicitInterfaces (result, evt.ExplicitInterfaceImplementations.Cast<ISymbol> ());
			result.Append (HighlightSemantically (FilterEntityName (evt.Name), colorStyle.UserEventDeclaration));
			return result.ToString ();
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
				result.Append (Highlight ("out ", colorStyle.KeywordParameter));
			} else if (parameter.RefKind == RefKind.Ref) {
				result.Append (Highlight ("ref ", colorStyle.KeywordParameter));
			} else if (parameter.IsParams) {
				result.Append (Highlight ("params ", colorStyle.KeywordParameter));
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
				sb.Append (Highlight ("\"" + constantValue + "\"", colorStyle.String));
				return;
			}
			if (constantValue is char) {
				sb.Append (Highlight ("'" + constantValue + "'", colorStyle.String));
				return;
			}
			if (constantValue is bool) {
				sb.Append (Highlight ((bool)constantValue ? "true" : "false", colorStyle.KeywordConstants));
				return;
			}

			if (constantValue == null) {
				if (constantType.TypeKind == TypeKind.Struct) {
					// structs can never be == null, therefore it's the default value.
					sb.Append (Highlight ("default", colorStyle.KeywordSelection) + "(" + GetTypeReferenceString (constantType) + ")");
				} else {
					sb.Append (Highlight ("null", colorStyle.KeywordConstants));
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
							sb.Append (Highlight (string.Format ("0x{0:X}", field.ConstantValue), colorStyle.Number));
						} else {
							sb.Append (GetTypeReferenceString (constantType) + "." + FilterEntityName (field.Name));
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
							sb.Append (GetTypeReferenceString (constantType) + "." + FilterEntityName (field.Name));
						}
						return;
					}
				}

				sb.Append ("(" + GetTypeReferenceString (constantType) + ")" + Highlight (constantValue.ToString (), colorStyle.Number));
				return;
			}

			sb.Append (Highlight (constantValue.ToString (), colorStyle.Number));
		}

		void AppendVariance (StringBuilder sb, VarianceKind variance)
		{
			if (variance == VarianceKind.In) {
				sb.Append (Highlight ("in ", colorStyle.KeywordParameter));
			} else if (variance == VarianceKind.Out) {
				sb.Append (Highlight ("out ", colorStyle.KeywordParameter));
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
			var result = new StringBuilder ();
			result.Append (GetTypeReferenceString (arrayType.ElementType));
			if (BreakLineAfterReturnType) {
				result.AppendLine ();
			} else {
				result.Append (" ");
			}
			result.Append (Highlight ("this", colorStyle.KeywordAccessors));
			result.Append ("[");
			for (int i = 0; i < arrayType.Rank; i++) {
				if (i > 0)
					result.Append (", ");
				var doHighightParameter = i == HighlightParameter;
				if (doHighightParameter)
					result.Append ("<u>");

				result.Append (Highlight ("int ", colorStyle.KeywordTypes));
				result.Append (arrayType.Rank == 1 ? "index" : "i" + (i + 1));
				if (doHighightParameter)
					result.Append ("</u>");
			}
			result.Append ("]");

			result.Append (" {");
			result.Append (Highlight (" get", colorStyle.KeywordProperty) + ";");
			result.Append (Highlight (" set", colorStyle.KeywordProperty) + ";");
			result.Append (" }");

			return result.ToString ();
		}


		string Highlight (string str, ChunkStyle style)
		{
			var color = (Gdk.Color)((HslColor)colorStyle.GetForeground (style));

			if (grayOut) {
				color = AlphaBlend (color, (Gdk.Color)((HslColor)colorStyle.PlainText.Background), optionalAlpha);
			}

			var colorString = MonoDevelop.Components.HelperMethods.GetColorString (color);
			return "<span foreground=\"" + colorString + "\">" + str + "</span>";
		}

		string HighlightSemantically (string str, ChunkStyle style)
		{
			if (!DefaultSourceEditorOptions.Instance.EnableSemanticHighlighting)
				return str;
			return Highlight (str, style);
		}

		public string CreateFooter (ISymbol entity)
		{
			var type = entity as ITypeSymbol;
			if (type != null) {
				var loc = type.Locations.First ();
				if (loc.IsInSource) {// TODO:
									 //					MonoDevelop.Projects.Project project;
									 //					
									 //					if (type.TryGetSourceProject (out project)) {
									 //						var relPath = FileService.AbsoluteToRelativePath (project.BaseDirectory, loc.SourceTree.FilePath);
									 //						var line = loc.SourceTree.GetLineSpan (loc.SourceSpan, true).StartLinePosition.Line;
									 //						
									 //						return (type.ContainingNamespace.IsGlobalNamespace ? "" : "<small>" + GettextCatalog.GetString ("Namespace:\t{0}", AmbienceService.EscapeText (type.ContainingNamespace.Name)) + "</small>" + Environment.NewLine) +
									 //							"<small>" + GettextCatalog.GetString ("Project:\t{0}", AmbienceService.EscapeText (type.ContainingAssembly.Name)) + "</small>" + Environment.NewLine +
									 //							"<small>" + GettextCatalog.GetString ("File:\t\t{0} (line {1})", AmbienceService.EscapeText (relPath), line) + "</small>";
									 //					}
				}
				return (type.ContainingNamespace.IsGlobalNamespace ? "" : "<small>" + GettextCatalog.GetString ("Namespace:\t{0}", Ambience.EscapeText (type.ContainingNamespace.Name)) + "</small>" + Environment.NewLine) +
					"<small>" + GettextCatalog.GetString ("Assembly:\t{0}", Ambience.EscapeText (type.ContainingAssembly.Name)) + "</small>";
			}

			if (entity.ContainingType != null) {
				var loc = entity.Locations.First ();
				if (!loc.IsInSource) {
					// TODO:
					//					MonoDevelop.Projects.Project project;
					//					if (entity.ContainingType.TryGetSourceProject (out project)) {
					//						var relPath = FileService.AbsoluteToRelativePath (project.BaseDirectory, loc.SourceTree.FilePath);
					//						var line = loc.SourceTree.GetLineSpan (loc.SourceSpan, true).StartLinePosition.Line;
					//						return "<small>" + GettextCatalog.GetString ("Project:\t{0}", AmbienceService.EscapeText (project.Name)) + "</small>" + Environment.NewLine +
					//								"<small>" + GettextCatalog.GetString ("From type:\t{0}", AmbienceService.EscapeText (entity.ContainingType.Name)) + "</small>" + Environment.NewLine +
					//								"<small>" + GettextCatalog.GetString ("File:\t\t{0} (line {1})", AmbienceService.EscapeText (relPath), line) + "</small>";
					//					}
				}
				return "<small>" + GettextCatalog.GetString ("From type:\t{0}", Ambience.EscapeText (entity.ContainingType.Name)) + "</small>" + Environment.NewLine +
					"<small>" + GettextCatalog.GetString ("Assembly:\t{0}", Ambience.EscapeText (entity.ContainingAssembly.Name)) + "</small>";
			}
			return null;
		}
	}
}
