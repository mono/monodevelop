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
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using System.IO;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using System.Collections.Generic;
using Mono.TextEditor.Highlighting;
using Mono.TextEditor;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Projects;
using ICSharpCode.NRefactory.Semantics;

namespace MonoDevelop.CSharp
{
	public class SignatureMarkupCreator
	{
		const double optionalAlpha = 0.7;
		readonly CSharpResolver resolver;
		readonly TypeSystemAstBuilder astBuilder;
		readonly CSharpFormattingOptions formattingOptions;
		readonly ColorScheme colorStyle;

		public bool BreakLineAfterReturnType {
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

		public SignatureMarkupCreator (CSharpResolver resolver, CSharpFormattingOptions formattingOptions)
		{
			this.colorStyle = SyntaxModeService.GetColorStyle (MonoDevelop.Ide.IdeApp.Preferences.ColorScheme);

			this.resolver = resolver;
			this.astBuilder = new TypeSystemAstBuilder (resolver) {
				ConvertUnboundTypeArguments = true,
				UseAliases = false
			};
			this.formattingOptions = formattingOptions;
		}

		public string GetTypeReferenceString (IType type, bool highlight = true)
		{
			if (type == null)
				throw new ArgumentNullException ("type");
			if (type.Kind == TypeKind.Null)
				return "?";
			AstType astType;
			try {
				astType = astBuilder.ConvertType (type);
			} catch (Exception e) {
				var compilation = GetCompilation (type);
				if (compilation == null) {

					Console.WriteLine ("type:"+type.GetType ());
					Console.WriteLine ("got exception while conversion:" + e);
					return "?";
				}
				astType = new TypeSystemAstBuilder (new CSharpResolver (compilation)).ConvertType (type);
			}

			if (astType is PrimitiveType) {
				return Highlight (astType.GetText (formattingOptions), colorStyle.KeywordTypes);
			}
			var text = AmbienceService.EscapeText (astType.GetText (formattingOptions));
			return highlight ? HighlightSemantically (text, colorStyle.UserTypes) : text;
		}

		static ICompilation GetCompilation (IType type)
		{
			var def = type.GetDefinition ();
			if (def == null) {	
				var t = type;
				while (t is TypeWithElementType) {
					t = ((TypeWithElementType)t).ElementType;
				}
				if (t != null)
					def = t.GetDefinition ();
			}
			if (def != null)
				return def.Compilation;
			return null;
		}

		public string GetMarkup (IType type)
		{
			if (type == null)
				throw new ArgumentNullException ("entity");
			return GetTypeMarkup (type);
		}

		public string GetMarkup (IEntity entity)
		{
			if (entity == null)
				throw new ArgumentNullException ("entity");
			string result;
			switch (entity.EntityType) {
			case EntityType.TypeDefinition:
				result = GetTypeMarkup ((ITypeDefinition)entity);
				break;
			case EntityType.Field:
				result = GetFieldMarkup ((IField)entity);
				break;
			case EntityType.Property:
			case EntityType.Indexer:
				result = GetPropertyMarkup ((IProperty)entity);
				break;
			case EntityType.Event:
				result = GetEventMarkup ((IEvent)entity);
				break;
			case EntityType.Method:
			case EntityType.Operator:
				result = GetMethodMarkup ((IMethod)entity);
				break;
			case EntityType.Constructor:
				result = GetConstructorMarkup ((IMethod)entity);
				break;
			case EntityType.Destructor:
				result = GetDestructorMarkup ((IMethod)entity);
				break;
			default:
				throw new ArgumentOutOfRangeException ();
			}
			string reason;
			if (entity.IsObsolete (out reason)) {
				var attr =  reason == null ? "[Obsolete]" : "[Obsolete(\"" + reason + "\")]";
				result = "<span size=\"smaller\">" + attr + "</span>" + Environment.NewLine + result;
			}
			return result;
		}

		public string GetMarkup (INamespace ns)
		{
			var result = new StringBuilder ();
			result.Append (Highlight ("namespace ", colorStyle.KeywordNamespace));
			result.Append (ns.FullName);

			return result.ToString ();
		}

		void AppendModifiers (StringBuilder result, IEntity entity)
		{
			if (entity.DeclaringType != null && entity.DeclaringType.Kind == TypeKind.Interface)
				return;

			switch (entity.Accessibility) {
			case Accessibility.Internal:
				if (entity.EntityType != EntityType.TypeDefinition)
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

			if (entity is IField && ((IField)entity).IsConst) {
				result.Append (Highlight ("const ", colorStyle.KeywordModifiers));
			} else if (entity.IsStatic) {
				result.Append (Highlight ("static ", colorStyle.KeywordModifiers));
			} else if (entity.IsSealed) {
				if (!(entity is IType && ((IType)entity).Kind == TypeKind.Delegate))
					result.Append (Highlight ("sealed ", colorStyle.KeywordModifiers));
			} else if (entity.IsAbstract) {
				if (!(entity is IType && ((IType)entity).Kind == TypeKind.Interface))
					result.Append (Highlight ("abstract ", colorStyle.KeywordModifiers));
			}


			if (entity.IsShadowing)
				result.Append (Highlight ("new ", colorStyle.KeywordModifiers));

			var member = entity as IMember;
			if (member != null) {
				if (member.IsOverride) {
					result.Append (Highlight ("override ", colorStyle.KeywordModifiers));
				} else if (member.IsVirtual) {
					result.Append (Highlight ("virtual ", colorStyle.KeywordModifiers));
				}
			}
			var field = entity as IField;
			if (field != null) {
				if (field.IsVolatile)
					result.Append (Highlight ("volatile ", colorStyle.KeywordModifiers));
				if (field.IsReadOnly)
					result.Append (Highlight ("readonly ", colorStyle.KeywordModifiers));
			}

			var method = entity as IMethod;
			if (method != null) {
				if (method.IsAsync)
					result.Append (Highlight ("async ", colorStyle.KeywordModifiers));
				if (method.IsPartial)
					result.Append (Highlight ("partial ", colorStyle.KeywordModifiers));
			}
		}

		void AppendAccessibility (StringBuilder result, IMethod entity)
		{
			switch (entity.Accessibility) {
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

		static bool IsObjectOrValueType(IType type)
		{
			var d = type.GetDefinition();
			return d != null && (d.KnownTypeCode == KnownTypeCode.Object || d.KnownTypeCode == KnownTypeCode.ValueType);
		}

		string GetTypeParameterMarkup (IType t)
		{
			if (t == null)
				throw new ArgumentNullException ("t");
			var result = new StringBuilder ();
			var highlightedTypeName = Highlight (CSharpAmbience.FilterName (t.Name), colorStyle.KeywordTypes);
			result.Append (highlightedTypeName);

			var color = AlphaBlend (colorStyle.PlainText.Foreground, colorStyle.PlainText.Background, optionalAlpha);
			var colorString = Mono.TextEditor.HelperMethods.GetColorString (color);

			result.Append ("<span foreground=\"" + colorString + "\">" + " (type parameter)</span>");
			var tp = t as ITypeParameter;
			if (tp != null) {
				if (!tp.HasDefaultConstructorConstraint && !tp.HasReferenceTypeConstraint && !tp.HasValueTypeConstraint && tp.DirectBaseTypes.All (IsObjectOrValueType))
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
				foreach (var bt in tp.DirectBaseTypes) {
					if (!IsObjectOrValueType(bt)) {
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
				if (tp.HasDefaultConstructorConstraint) {
					if (constraints > 0)
						result.Append (",");
					result.Append (Highlight ("new", colorStyle.KeywordOperators));
				}

			}
			return result.ToString ();
		}

		string GetNullableMarkup (IType t)
		{
			var result = new StringBuilder ();
			result.Append (GetTypeReferenceString (t));
			return result.ToString ();
		}

		void AppendTypeParameterList (StringBuilder result, ITypeDefinition def)
		{
			IEnumerable<ITypeParameter> parameters = def.TypeParameters;
			if (def.DeclaringTypeDefinition != null)
				parameters = parameters.Skip (def.DeclaringTypeDefinition.TypeParameterCount);
			AppendTypeParameters (result, parameters);
		}
		
		void AppendTypeArgumentList (StringBuilder result, IType def)
		{
			IEnumerable<IType> parameters = def.TypeArguments;
			if (def.DeclaringType != null)
				parameters = parameters.Skip (def.DeclaringType.TypeParameterCount);
			AppendTypeParameters (result, parameters);
		}

		string GetTypeNameWithParameters (IType t)
		{
			StringBuilder result = new StringBuilder ();
			result.Append (Highlight (CSharpAmbience.FilterName (t.Name), colorStyle.KeywordTypes));
			if (t.TypeParameterCount > 0) {
				if (t.TypeArguments.Count > 0) {
					AppendTypeArgumentList (result, t);
				} else {
					AppendTypeParameterList (result, t.GetDefinition ());
				}
			}
			return result.ToString ();
		}
		
		string GetTypeMarkup (IType t, bool includeDeclaringTypes = false)
		{
			if (t == null)
				throw new ArgumentNullException ("t");

			if (t.Kind == TypeKind.Null)
				return "Type can not be resolved.";
			if (t.Kind == TypeKind.Delegate)
				return GetDelegateMarkup (t);
			if (t.Kind == TypeKind.TypeParameter)
				return GetTypeParameterMarkup (t);
			if (NullableType.IsNullable (t))
				return GetNullableMarkup (t);
			var result = new StringBuilder ();
			if (t.GetDefinition () != null)
				AppendModifiers (result, t.GetDefinition ());

			switch (t.Kind) {
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
					curType = curType.DeclaringType;
				}
				typeNames.Reverse ();
				result.Append (string.Join (".", typeNames));
			} else {
				result.Append (GetTypeNameWithParameters (t));
			}

			if (t.Kind == TypeKind.Array)
				return result.ToString ();

			bool first = true;
			int maxLength = GetMarkupLength (result.ToString ());
			int length = maxLength;
			var sortedTypes = new List<IType> (t.DirectBaseTypes.Where (x => x.FullName != "System.Object"));
			sortedTypes.Sort ((x, y) => GetTypeReferenceString (y).Length.CompareTo (GetTypeReferenceString (x).Length));
			if (t.Kind != TypeKind.Enum) {
				foreach (var directBaseType in sortedTypes) {
					if (first) {
						result.AppendLine (" :");
						result.Append ("  ");
						length = 2;
					} else {
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
				var enumBase = t.GetDefinition ().EnumUnderlyingType;
				if (enumBase.Name != "Int32") {
					result.AppendLine (" :");
					result.Append ("  ");
					result.Append (GetTypeReferenceString (enumBase, false));
				}
			}

			return result.ToString ();
		}

		void AppendTypeParameters (StringBuilder result, IEnumerable<ITypeParameter> typeParameters)
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
					}
					else {
						result.Append (", ");
					}
				}
				AppendVariance (result, typeParameter.Variance);
				result.Append (HighlightSemantically (CSharpAmbience.NetToCSharpTypeName (typeParameter.Name), colorStyle.UserTypes));
				i++;
			}
			result.Append ("&gt;");
		}

		void AppendTypeParameters (StringBuilder result, IEnumerable<IType> typeParameters)
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
					}
					else {
						result.Append (", ");
					}
				}
				if (typeParameter is ITypeParameter)
					AppendVariance (result, ((ITypeParameter)typeParameter).Variance);
				result.Append (GetTypeReferenceString (typeParameter, false));
				i++;
			}
			result.Append ("&gt;");
		}

		public string GetDelegateInfo (IType type)
		{
			if (type == null)
				throw new ArgumentNullException ("returnType");
			var t = type.GetDefinition ();

			var result = new StringBuilder ();
			
			var method = t.GetDelegateInvokeMethod ();
			result.Append (GetTypeReferenceString (method.ReturnType));
			if (BreakLineAfterReturnType) {
				result.AppendLine ();
			} else {
				result.Append (" ");
			}
			
			
			result.Append (CSharpAmbience.FilterName (t.Name));
			
			AppendTypeParameters (result, method.TypeParameters);

			if (formattingOptions.SpaceBeforeDelegateDeclarationParentheses)
				result.Append (" ");
			
			result.Append ('(');
			AppendParameterList (result,  method.Parameters, formattingOptions.SpaceBeforeDelegateDeclarationParameterComma, formattingOptions.SpaceAfterDelegateDeclarationParameterComma, false);
			result.Append (')');
			return result.ToString ();
		}

		string GetDelegateMarkup (IType delegateType)
		{
			var result = new StringBuilder ();
			
			var method = delegateType.GetDelegateInvokeMethod ();

			if (delegateType.GetDefinition () != null)
				AppendModifiers (result, delegateType.GetDefinition ());
			result.Append (Highlight ("delegate ", colorStyle.KeywordDeclaration));
			result.Append (GetTypeReferenceString (method.ReturnType));
			if (BreakLineAfterReturnType) {
				result.AppendLine ();
			} else {
				result.Append (" ");
			}
			
			
			result.Append (CSharpAmbience.FilterName (delegateType.Name));

			if (delegateType.TypeArguments.Count > 0) {
				AppendTypeArgumentList (result, delegateType);
			} else {
				AppendTypeParameterList (result, delegateType.GetDefinition ());
			}

			if (formattingOptions.SpaceBeforeMethodDeclarationParameterComma)
				result.Append (" ");
			
			result.Append ('(');
			AppendParameterList (result,  method.Parameters, formattingOptions.SpaceBeforeDelegateDeclarationParameterComma, formattingOptions.SpaceAfterDelegateDeclarationParameterComma);
			result.Append (')');
			return result.ToString ();
		}

		public string GetLocalVariableMarkup (IVariable variable)
		{
			if (variable == null)
				throw new ArgumentNullException ("field");
			
			var result = new StringBuilder ();

			if (variable.IsConst)
				result.Append (Highlight ("const", colorStyle.KeywordModifiers));

			result.Append (GetTypeReferenceString (variable.Type));
			if (BreakLineAfterReturnType) {
				result.AppendLine ();
			} else {
				result.Append (" ");
			}
	
			result.Append (CSharpAmbience.FilterName (variable.Name));
			
			if (variable.IsConst) {
				if (formattingOptions.SpaceAroundAssignment) {
					result.Append (" = ");
				} else {
					result.Append ("=");
				}
				AppendConstant (result, variable.Type, variable.ConstantValue);
			}
			
			return result.ToString ();
		}
		

		string GetFieldMarkup (IField field)
		{
			if (field == null)
				throw new ArgumentNullException ("field");

			var result = new StringBuilder ();
			bool isEnum = field.DeclaringTypeDefinition != null && field.DeclaringTypeDefinition.Kind == TypeKind.Enum;
			if (!isEnum) {
				AppendModifiers (result, field);
				result.Append (GetTypeReferenceString (field.ReturnType));
			} else {
				result.Append (GetTypeReferenceString (field.DeclaringType));
			}
			if (BreakLineAfterReturnType) {
				result.AppendLine ();
			} else {
				result.Append (" ");
			}

			result.Append (HighlightSemantically (CSharpAmbience.FilterName (field.Name), colorStyle.UserFieldDeclaration));

			if (field.IsConst) {
				if (isEnum && !(field.DeclaringTypeDefinition.Attributes.Any (attr => attr.AttributeType.FullName == "System.FlagsAttribute"))) {
					return result.ToString ();
				}
				if (formattingOptions.SpaceAroundAssignment) {
					result.Append (" = ");
				} else {
					result.Append ("=");
				}
				AppendConstant (result, field.Type, field.ConstantValue);
			}

			return result.ToString ();
		}

		string GetMethodMarkup (IMethod method)
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

			AppendExplicitInterfaces (result, method);

			if (method.EntityType == EntityType.Operator) {
				result.Append ("operator ");
				result.Append (CSharpAmbience.GetOperator (method.Name));
			} else {
				result.Append (HighlightSemantically (CSharpAmbience.FilterName (method.Name), colorStyle.UserMethodDeclaration));
			}
			if (method.TypeArguments.Count > 0) {
				result.Append ("&lt;");
				for (int i = 0; i < method.TypeArguments.Count; i++) {
					if (i > 0)
						result.Append (", ");
					result.Append (HighlightSemantically (GetTypeReferenceString (method.TypeArguments[i], false), colorStyle.UserTypes));
				}
				result.Append ("&gt;");
			} else {
				AppendTypeParameters (result, method.TypeParameters);
			}

			if (formattingOptions.SpaceBeforeMethodDeclarationParentheses)
				result.Append (" ");

			result.Append ('(');
			IList<IParameter> parameters = method.Parameters;
			AppendParameterList (result,  parameters, formattingOptions.SpaceBeforeMethodDeclarationParameterComma, formattingOptions.SpaceAfterMethodDeclarationParameterComma);
			result.Append (')');
			return result.ToString ();
		}

		string GetConstructorMarkup (IMethod method)
		{
			if (method == null)
				throw new ArgumentNullException ("method");


			var result = new StringBuilder ();
			AppendModifiers (result, method);

			result.Append (CSharpAmbience.FilterName (method.DeclaringType.Name));

			if (formattingOptions.SpaceBeforeConstructorDeclarationParentheses)
				result.Append (" ");

			result.Append ('(');
			if (method.DeclaringType.Kind == TypeKind.Delegate) {
				result.Append (Highlight ("delegate", colorStyle.KeywordDeclaration) + " (");
				AppendParameterList (result, method.DeclaringType.GetDelegateInvokeMethod ().Parameters, formattingOptions.SpaceBeforeConstructorDeclarationParameterComma, formattingOptions.SpaceAfterConstructorDeclarationParameterComma);
				result.Append (")");
			} else {
				AppendParameterList (result, method.Parameters, formattingOptions.SpaceBeforeConstructorDeclarationParameterComma, formattingOptions.SpaceAfterConstructorDeclarationParameterComma);
			}
			result.Append (')');
			return result.ToString ();
		}

		string GetDestructorMarkup (IMethod method)
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
			result.Append (CSharpAmbience.FilterName (method.DeclaringType.Name));
			
			if (formattingOptions.SpaceBeforeConstructorDeclarationParentheses)
				result.Append (" ");
			
			result.Append ('(');
			AppendParameterList (result,  method.Parameters, formattingOptions.SpaceBeforeConstructorDeclarationParameterComma, formattingOptions.SpaceAfterConstructorDeclarationParameterComma);
			result.Append (')');
			return result.ToString ();
		}

		bool IsAccessibleOrHasSourceCode (IEntity entity)
		{
			if (!entity.Region.Begin.IsEmpty)
				return true;
			var lookup = new MemberLookup (resolver.CurrentTypeDefinition, resolver.Compilation.MainAssembly);
			return lookup.IsAccessible (entity, false);
		}

		string GetPropertyMarkup (IProperty property)
		{
			if (property == null)
				throw new ArgumentNullException ("property");
			var result = new StringBuilder ();
			AppendModifiers (result, property);
			result.Append (GetTypeReferenceString (property.ReturnType));
			if (BreakLineAfterReturnType) {
				result.AppendLine ();
			} else {
				result.Append (" ");
			}

			AppendExplicitInterfaces (result, property);
			
			if (property.EntityType == EntityType.Indexer) {
				result.Append (Highlight ("this", colorStyle.KeywordAccessors));
			} else {
				result.Append (HighlightSemantically (CSharpAmbience.FilterName (property.Name), colorStyle.UserPropertyDeclaration));
			}
			
			if (property.Parameters.Count > 0) {
				if (formattingOptions.SpaceBeforeIndexerDeclarationBracket)
					result.Append (" ");
				result.Append ("[");
				AppendParameterList (result, property.Parameters, formattingOptions.SpaceBeforeIndexerDeclarationParameterComma, formattingOptions.SpaceAfterIndexerDeclarationParameterComma);
				result.Append ("]");
			}
			
			result.Append (" {");
			if (property.CanGet && IsAccessibleOrHasSourceCode (property.Getter)) {
				if (property.Getter.Accessibility != property.Accessibility) {

					result.Append (" ");
					AppendAccessibility (result, property.Getter);
				}
				result.Append (Highlight (" get", colorStyle.KeywordProperty) + ";");
			}

			if (property.CanSet && IsAccessibleOrHasSourceCode(property.Setter)) {
				if (property.Setter.Accessibility != property.Accessibility) {
					result.Append (" ");
					AppendAccessibility (result, property.Setter);
				}
				result.Append (Highlight (" set", colorStyle.KeywordProperty) + ";");
			}
			result.Append (" }");

			return result.ToString ();
		}

		
		public TooltipInformation GetExternAliasTooltip (ExternAliasDeclaration externAliasDeclaration, DotNetProject project)
		{
			var result = new TooltipInformation ();
			result.SignatureMarkup = Highlight ("extern ", colorStyle.KeywordModifiers) + Highlight ("alias ", colorStyle.KeywordNamespace) + externAliasDeclaration.Name;
			if (project == null)
				return result;
			foreach (var r in project.References) {
				if (string.IsNullOrEmpty (r.Aliases))
					continue;
				foreach (var alias in r.Aliases.Split (',', ';')) {
					if (alias == externAliasDeclaration.Name)
						result.AddCategory (GettextCatalog.GetString ("Reference"), r.StoredReference);
				}
			}

			return result;
		}
		public TooltipInformation GetKeywordTooltip (AstNode node)
		{
			return GetKeywordTooltip (node.GetText (), node);
		}

		public TooltipInformation GetKeywordTooltip (string keyword, AstNode hintNode)
		{
			var result = new TooltipInformation ();

			var color = AlphaBlend (colorStyle.PlainText.Foreground, colorStyle.PlainText.Background, optionalAlpha);
			var colorString = Mono.TextEditor.HelperMethods.GetColorString (color);
			
			var keywordSign = "<span foreground=\"" + colorString + "\">" + " (keyword)</span>";

			switch (keyword){
			case "abstract":
				result.SignatureMarkup = Highlight ("abstract", colorStyle.KeywordModifiers) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("abstract", colorStyle.KeywordModifiers) + " modifier can be used with classes, methods, properties, indexers, and events.";
				break;
			case "add":
				result.SignatureMarkup = Highlight ("add", colorStyle.KeywordContext) + keywordSign;
				//TODO
				break;
			case "ascending":
				result.SignatureMarkup = Highlight ("ascending", colorStyle.KeywordContext) + keywordSign;
				//TODO
				break;
			case "async":
				result.SignatureMarkup = Highlight ("async", colorStyle.KeywordContext) + keywordSign;
				//TODO
				break;
			case "as":
				result.SignatureMarkup = Highlight ("as", colorStyle.KeywordOperators) + keywordSign;
				result.AddCategory ("Form", "expression " + Highlight ("as", colorStyle.KeywordOperators) + " type");
				result.SummaryMarkup = "The " + Highlight ("as", colorStyle.KeywordOperators) + " operator is used to perform conversions between compatible types. ";
				break;
			case "await":
				result.SignatureMarkup = Highlight ("await", colorStyle.KeywordContext) + keywordSign;
				//TODO
				break;
			case "base":
				result.SignatureMarkup = Highlight ("base", colorStyle.KeywordAccessors) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("base", colorStyle.KeywordAccessors) + " keyword is used to access members of the base class from within a derived class.";
				break;
			case "break":
				result.SignatureMarkup = Highlight ("break", colorStyle.KeywordJump) + keywordSign;
				result.AddCategory ("Form", Highlight ("break", colorStyle.KeywordJump) + ";");
				result.SummaryMarkup = "The " + Highlight ("break", colorStyle.KeywordJump) + " statement terminates the closest enclosing loop or switch statement in which it appears.";
				break;
			case "case":
				result.SignatureMarkup = Highlight ("case", colorStyle.KeywordSelection) + keywordSign;
				result.AddCategory ("Form", Highlight ("case", colorStyle.KeywordSelection) + " constant-expression:" + Environment.NewLine +
				                    "  statement" + Environment.NewLine +
				                    "  jump-statement");
				result.SummaryMarkup = "";
				break;
			case "catch":
				result.SignatureMarkup = Highlight ("catch", colorStyle.KeywordException) + keywordSign;
				result.AddCategory ("Form", Highlight ("try", colorStyle.KeywordException) + " try-block" + Environment.NewLine +
				                    "  " + Highlight ("catch", colorStyle.KeywordException) + " (exception-declaration-1) catch-block-1" + Environment.NewLine +
				                    "  " + Highlight ("catch", colorStyle.KeywordException) + " (exception-declaration-2) catch-block-2" + Environment.NewLine +
				                    "  ..." + Environment.NewLine +
				                    Highlight ("try", colorStyle.KeywordException) + " try-block " + Highlight ("catch", colorStyle.KeywordException) + " catch-block");
				result.SummaryMarkup = "";
				break;
			case "checked":
				result.SignatureMarkup = Highlight ("checked", colorStyle.KeywordOther) + keywordSign;
				result.AddCategory ("Form", Highlight ("checked", colorStyle.KeywordOther) + " block" + Environment.NewLine +
				                    "or" + Environment.NewLine +
				                    Highlight ("checked", colorStyle.KeywordOther) + " (expression)");
				result.SummaryMarkup = "The " + Highlight ("checked", colorStyle.KeywordOther) + " keyword is used to control the overflow-checking context for integral-type arithmetic operations and conversions. It can be used as an operator or a statement.";
				break;
			case "class":
				result.SignatureMarkup = Highlight ("class", colorStyle.KeywordDeclaration) + keywordSign;
				result.AddCategory ("Form", "[attributes] [modifiers] " + Highlight ("class", colorStyle.KeywordDeclaration) + " identifier [:base-list] { class-body }[;]");
				result.SummaryMarkup = "Classes are declared using the keyword " + Highlight ("class", colorStyle.KeywordDeclaration) + ".";
				break;
			case "const":
				result.SignatureMarkup = Highlight ("const", colorStyle.KeywordModifiers) + keywordSign;
				result.AddCategory ("Form", "[attributes] [modifiers] " + Highlight ("const", colorStyle.KeywordModifiers) + " type declarators;");
				result.SummaryMarkup = "The " + Highlight ("const", colorStyle.KeywordModifiers) + " keyword is used to modify a declaration of a field or local variable. It specifies that the value of the field or the local variable cannot be modified. ";
				break;
			case "continue":
				result.SignatureMarkup = Highlight ("continue", colorStyle.KeywordJump) + keywordSign;
				result.AddCategory ("Form", Highlight ("continue", colorStyle.KeywordJump) + ";");
				result.SummaryMarkup = "The " + Highlight ("continue", colorStyle.KeywordJump) + " statement passes control to the next iteration of the enclosing iteration statement in which it appears.";
				break;
			case "default":
				result.SignatureMarkup = Highlight ("default", colorStyle.KeywordSelection) + keywordSign;
				result.SummaryMarkup = "";
				if (hintNode != null) {
					if (hintNode.Parent is DefaultValueExpression) {
						result.AddCategory ("Form",
						                    Highlight ("default", colorStyle.KeywordSelection) + " (Type)");
						break;
					} else if (hintNode.Parent is CaseLabel) {
						result.AddCategory ("Form",
						                    Highlight ("switch", colorStyle.KeywordSelection) + " (expression) { "+ Environment.NewLine +
						                    "  " + Highlight ("case", colorStyle.KeywordSelection) + " constant-expression:" + Environment.NewLine +
						                    "    statement"+ Environment.NewLine +
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
				                    Highlight ("switch", colorStyle.KeywordSelection) + " (expression) { "+ Environment.NewLine +
				                    "  " + Highlight ("case", colorStyle.KeywordSelection) + " constant-expression:" + Environment.NewLine +
				                    "    statement"+ Environment.NewLine +
				                    "    jump-statement" + Environment.NewLine +
				                    "  [" + Highlight ("default", colorStyle.KeywordSelection) + ":" + Environment.NewLine +
				                    "    statement" + Environment.NewLine +
				                    "    jump-statement]" + Environment.NewLine +
				                    "}");
				break;
			case "delegate":
				result.SignatureMarkup = Highlight ("delegate", colorStyle.KeywordDeclaration) + keywordSign;
				result.AddCategory ("Form", "[attributes] [modifiers] " + Highlight ("delegate", colorStyle.KeywordDeclaration) + " result-type identifier ([formal-parameters]);");
				result.SummaryMarkup = "A " + Highlight ("delegate", colorStyle.KeywordDeclaration) + " declaration defines a reference type that can be used to encapsulate a method with a specific signature.";
				break;
			case "dynamic":
				result.SignatureMarkup = Highlight ("dynamic", colorStyle.KeywordContext) + keywordSign;
				//TODO
				break;
			case "descending":
				result.SignatureMarkup = Highlight ("descending", colorStyle.KeywordContext) + keywordSign;
				//TODO
				break;
			case "do":
				result.SignatureMarkup = Highlight ("do", colorStyle.KeywordIteration) + keywordSign;
				result.AddCategory ("Form", Highlight ("do", colorStyle.KeywordIteration) + " statement " + Highlight ("while", colorStyle.KeywordIteration) + " (expression);");
				result.SummaryMarkup = "The " + Highlight ("do", colorStyle.KeywordIteration) + " statement executes a statement or a block of statements repeatedly until a specified expression evaluates to false.";
				break;
			case "else":
				result.SignatureMarkup = Highlight ("else", colorStyle.KeywordSelection) + keywordSign;
				result.AddCategory ("Form", Highlight ("if", colorStyle.KeywordSelection) + " (expression)" + Environment.NewLine +
				                    "  statement1" + Environment.NewLine +
				                    "  [" + Highlight ("else", colorStyle.KeywordSelection) + Environment.NewLine +
				                    "  statement2]");
				result.SummaryMarkup = "";
				break;
			case "enum":
				result.SignatureMarkup = Highlight ("enum", colorStyle.KeywordDeclaration) + keywordSign;
				result.AddCategory ("Form", "[attributes] [modifiers] " + Highlight ("enum", colorStyle.KeywordDeclaration) + " identifier [:base-type] {enumerator-list} [;]");
				result.SummaryMarkup = "The " + Highlight ("enum", colorStyle.KeywordDeclaration) + " keyword is used to declare an enumeration, a distinct type consisting of a set of named constants called the enumerator list.";
				break;
			case "event":
				result.SignatureMarkup = Highlight ("event", colorStyle.KeywordModifiers) + keywordSign;
				result.AddCategory ("Form", "[attributes] [modifiers] " + Highlight ("event", colorStyle.KeywordModifiers) + " type declarator;" + Environment.NewLine +
				                    "[attributes] [modifiers] " + Highlight ("event", colorStyle.KeywordModifiers) + " type member-name {accessor-declarations};");
				result.SummaryMarkup = "Specifies an event.";
				break;
			case "explicit":
				result.SignatureMarkup = Highlight ("explicit", colorStyle.KeywordOperatorDeclaration) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("explicit", colorStyle.KeywordOperatorDeclaration) + " keyword is used to declare an explicit user-defined type conversion operator.";
				break;
			case "extern":
				result.SignatureMarkup = Highlight ("extern", colorStyle.KeywordModifiers) + keywordSign;
				result.SummaryMarkup = "Use the " + Highlight ("extern", colorStyle.KeywordModifiers) + " modifier in a method declaration to indicate that the method is implemented externally. A common use of the extern modifier is with the DllImport attribute.";
				break;
			case "finally":
				result.SignatureMarkup = Highlight ("finally", colorStyle.KeywordException) + keywordSign;
				result.AddCategory ("Form", Highlight ("try", colorStyle.KeywordException) + " try-block " + Highlight ("finally", colorStyle.KeywordException) + " finally-block");
				result.SummaryMarkup = "The " + Highlight ("finally", colorStyle.KeywordException) + " block is useful for cleaning up any resources allocated in the try block. Control is always passed to the finally block regardless of how the try block exits.";
				break;
			case "fixed":
				result.SignatureMarkup = Highlight ("fixed", colorStyle.KeywordOther) + keywordSign;
				result.AddCategory ("Form", Highlight ("fixed", colorStyle.KeywordOther) + " ( type* ptr = expr ) statement");
				result.SummaryMarkup = "Prevents relocation of a variable by the garbage collector.";
				break;
			case "for":
				result.SignatureMarkup = Highlight ("for", colorStyle.KeywordIteration) + keywordSign;
				result.AddCategory ("Form", Highlight ("for", colorStyle.KeywordIteration) + " ([initializers]; [expression]; [iterators]) statement");
				result.SummaryMarkup = "The " + Highlight ("for", colorStyle.KeywordIteration) + " loop executes a statement or a block of statements repeatedly until a specified expression evaluates to false.";
				break;
			case "foreach":
				result.SignatureMarkup = Highlight ("foreach", colorStyle.KeywordIteration) + keywordSign;
				result.AddCategory ("Form", Highlight ("foreach", colorStyle.KeywordIteration) + " (type identifier " + Highlight ("in", colorStyle.KeywordIteration) + " expression) statement");
				result.SummaryMarkup = "The " + Highlight ("foreach", colorStyle.KeywordIteration) + " statement repeats a group of embedded statements for each element in an array or an object collection. ";
				break;
			case "from":
				result.SignatureMarkup = Highlight ("from", colorStyle.KeywordContext) + keywordSign;
				//TODO
				break;
			case "get":
				result.SignatureMarkup = Highlight ("get", colorStyle.KeywordContext) + keywordSign;
				//TODO
				break;
			case "global":
				result.SignatureMarkup = Highlight ("global", colorStyle.KeywordContext) + keywordSign;
				//TODO
				break;
			case "goto":
				result.SignatureMarkup = Highlight ("goto", colorStyle.KeywordJump) + keywordSign;
				result.AddCategory ("Form", Highlight ("goto", colorStyle.KeywordJump) + " identifier;" + Environment.NewLine +
					Highlight ("goto", colorStyle.KeywordJump) + " " + Highlight ("case", colorStyle.KeywordSelection) + " constant-expression;" + Environment.NewLine +
				                    Highlight ("goto", colorStyle.KeywordJump) + " " + Highlight ("default", colorStyle.KeywordSelection) + ";");
				result.SummaryMarkup = "The " + Highlight ("goto", colorStyle.KeywordJump) + " statement transfers the program control directly to a labeled statement. ";
				break;
			case "group":
				result.SignatureMarkup = Highlight ("group", colorStyle.KeywordContext) + keywordSign;
				//TODO
				break;
			case "if":
				result.SignatureMarkup = Highlight ("if", colorStyle.KeywordSelection) + keywordSign;
				result.AddCategory ("Form", Highlight ("if", colorStyle.KeywordSelection) + " (expression)" + Environment.NewLine +
					"  statement1" + Environment.NewLine +
					"  [" + Highlight ("else", colorStyle.KeywordSelection) + Environment.NewLine +
				                    "  statement2]");
				result.SummaryMarkup = "The " + Highlight ("if", colorStyle.KeywordSelection) + " statement selects a statement for execution based on the value of a Boolean expression. ";
				break;
			case "into":
				result.SignatureMarkup = Highlight ("into", colorStyle.KeywordContext) + keywordSign;
				//TODO
				break;
			case "implicit":
				result.SignatureMarkup = Highlight ("implicit", colorStyle.KeywordOperatorDeclaration) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("implicit", colorStyle.KeywordOperatorDeclaration) + " keyword is used to declare an implicit user-defined type conversion operator.";
				break;
			case "in":
				result.SignatureMarkup = Highlight ("in", colorStyle.KeywordIteration) + keywordSign;
				if (hintNode != null) {
					if (hintNode.Parent is ForeachStatement) {
						result.AddCategory ("Form",
						                    Highlight ("foreach", colorStyle.KeywordIteration) + " (type identifier " + Highlight ("in", colorStyle.KeywordIteration) + " expression) statement");
						break;
					}
					if (hintNode.Parent is QueryFromClause) {
						result.AddCategory ("Form",
						                    Highlight ("from", colorStyle.KeywordContext) + " range-variable " + Highlight ("in", colorStyle.KeywordIteration) + " data-source [query clauses] " + Highlight ("select", colorStyle.KeywordContext) + " product-expression");
						break;
					}
					if (hintNode.Parent is TypeParameterDeclaration) {
						result.AddCategory ("Form",
						                    Highlight ("interface", colorStyle.KeywordDeclaration) + " IMyInterface&lt;" + Highlight ("in", colorStyle.KeywordIteration) + " T&gt; {}");
						break;
					}
				}
				result.AddCategory ("Form", Highlight ("foreach", colorStyle.KeywordIteration) + " (type identifier " + Highlight ("in", colorStyle.KeywordIteration) + " expression) statement"+ Environment.NewLine + Environment.NewLine +
				                    "or" + Environment.NewLine + Environment.NewLine +
				                    Highlight ("from", colorStyle.KeywordContext) + " range-variable " + Highlight ("in", colorStyle.KeywordIteration) + " data-source [query clauses] " + Highlight ("select", colorStyle.KeywordContext) + " product-expression" + Environment.NewLine + Environment.NewLine +
				                    "or" + Environment.NewLine + Environment.NewLine +
				                    Highlight ("interface", colorStyle.KeywordDeclaration) + " IMyInterface&lt;" + Highlight ("in", colorStyle.KeywordIteration) + " T&gt; {}"
				                    );
				break;
			case "interface":
				result.SignatureMarkup = Highlight ("interface", colorStyle.KeywordDeclaration) + keywordSign;
				result.AddCategory ("Form", "[attributes] [modifiers] " + Highlight ("interface", colorStyle.KeywordDeclaration) + " identifier [:base-list] {interface-body}[;]");
				result.SummaryMarkup = "An interface defines a contract. A class or struct that implements an interface must adhere to its contract.";
				break;
			case "internal":
				result.SignatureMarkup = Highlight ("internal", colorStyle.KeywordModifiers) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("internal", colorStyle.KeywordModifiers) + " keyword is an access modifier for types and type members. Internal members are accessible only within files in the same assembly.";
				break;
			case "is":
				result.SignatureMarkup = Highlight ("is", colorStyle.KeywordOperators) + keywordSign;
				result.AddCategory ("Form", "expression " + Highlight ("is", colorStyle.KeywordOperators) + " type");
				result.SummaryMarkup = "The " + Highlight ("is", colorStyle.KeywordOperators) + " operator is used to check whether the run-time type of an object is compatible with a given type.";
				break;
			case "join":
				result.SignatureMarkup = Highlight ("join", colorStyle.KeywordContext) + keywordSign;
				//TODO
				break;
			case "let":
				result.SignatureMarkup = Highlight ("let", colorStyle.KeywordContext) + keywordSign;
				//TODO
				break;
			case "lock":
				result.SignatureMarkup = Highlight ("lock", colorStyle.KeywordOther) + keywordSign;
				result.AddCategory ("Form", Highlight ("lock", colorStyle.KeywordOther) + " (expression) statement_block");
				result.SummaryMarkup = "The " + Highlight ("lock", colorStyle.KeywordOther) + " keyword marks a statement block as a critical section by obtaining the mutual-exclusion lock for a given object, executing a statement, and then releasing the lock. ";
				break;
			case "namespace":
				result.SignatureMarkup = Highlight ("namespace", colorStyle.KeywordNamespace) + keywordSign;
				result.AddCategory ("Form", Highlight ("namespace", colorStyle.KeywordNamespace) + " name[.name1] ...] {" + Environment.NewLine +
					"type-declarations" + Environment.NewLine +
				                    " }");
				result.SummaryMarkup = "The " + Highlight ("namespace", colorStyle.KeywordNamespace) + " keyword is used to declare a scope. ";
				break;
			case "new":
				result.SignatureMarkup = Highlight ("new", colorStyle.KeywordOperators) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("new", colorStyle.KeywordOperators) + " keyword can be used as an operator or as a modifier. The operator is used to create objects on the heap and invoke constructors. The modifier is used to hide an inherited member from a base class member.";
				break;
			case "null":
				result.SignatureMarkup = Highlight ("null", colorStyle.KeywordConstants) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("null", colorStyle.KeywordConstants) + " keyword is a literal that represents a null reference, one that does not refer to any object. " + Highlight ("null", colorStyle.KeywordConstants) + " is the default value of reference-type variables.";
				break;
			case "operator":
				result.SignatureMarkup = Highlight ("operator", colorStyle.KeywordOperatorDeclaration) + keywordSign;
				result.AddCategory ("Form", Highlight ("public static ", colorStyle.KeywordModifiers) + "result-type " + Highlight ("operator", colorStyle.KeywordOperatorDeclaration) + " unary-operator ( op-type operand )" + Environment.NewLine +
				                    Highlight ("public static ", colorStyle.KeywordModifiers) + "result-type " + Highlight ("operator", colorStyle.KeywordOperatorDeclaration) + " binary-operator (" + Environment.NewLine +
					"op-type operand," + Environment.NewLine +
					"op-type2 operand2" + Environment.NewLine +
					" )" + Environment.NewLine +
					Highlight ("public static ", colorStyle.KeywordModifiers) + Highlight ("implicit operator", colorStyle.KeywordOperatorDeclaration) + " conv-type-out ( conv-type-in operand )" + Environment.NewLine +
				                    Highlight ("public static ", colorStyle.KeywordModifiers) + Highlight ("explicit operator", colorStyle.KeywordOperatorDeclaration) + " conv-type-out ( conv-type-in operand )");
				result.SummaryMarkup = "The " + Highlight ("operator", colorStyle.KeywordOperatorDeclaration) + " keyword is used to declare an operator in a class or struct declaration.";
				break;
			case "orderby":
				result.SignatureMarkup = Highlight ("orderby", colorStyle.KeywordContext) + keywordSign;
				//TODO
				break;
			case "out":
				result.SignatureMarkup = Highlight ("out", colorStyle.KeywordParameter) + keywordSign;
				if (hintNode != null) {
					if (hintNode.Parent is TypeParameterDeclaration) {
						result.AddCategory ("Form",
						                    Highlight ("interface", colorStyle.KeywordDeclaration) + " IMyInterface&lt;" + Highlight ("out", colorStyle.KeywordParameter) + " T&gt; {}");
						break;
					}
					if (hintNode.Parent is ParameterDeclaration) {
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
			case "override":
				result.SignatureMarkup = Highlight ("override", colorStyle.KeywordModifiers) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("override", colorStyle.KeywordModifiers) + " modifier is used to override a method, a property, an indexer, or an event.";
				break;
			case "params":
				result.SignatureMarkup = Highlight ("params", colorStyle.KeywordParameter) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("params", colorStyle.KeywordParameter) + " keyword lets you specify a method parameter that takes an argument where the number of arguments is variable.";
				break;
			case "partial":
				result.SignatureMarkup = Highlight ("partial", colorStyle.KeywordContext) + keywordSign;
				//TODO
				break;
			case "private":
				result.SignatureMarkup = Highlight ("private", colorStyle.KeywordModifiers) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("private", colorStyle.KeywordModifiers) + " keyword is a member access modifier. Private access is the least permissive access level. Private members are accessible only within the body of the class or the struct in which they are declared.";
				break;
			case "protected":
				result.SignatureMarkup = Highlight ("protected", colorStyle.KeywordModifiers) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("protected", colorStyle.KeywordModifiers) + " keyword is a member access modifier. A protected member is accessible from within the class in which it is declared, and from within any class derived from the class that declared this member.";
				break;
			case "public":
				result.SignatureMarkup = Highlight ("public", colorStyle.KeywordModifiers) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("public", colorStyle.KeywordModifiers) + " keyword is an access modifier for types and type members. Public access is the most permissive access level. There are no restrictions on accessing public members.";
				break;
			case "readonly":
				result.SignatureMarkup = Highlight ("readonly", colorStyle.KeywordModifiers) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("readonly", colorStyle.KeywordModifiers) + " keyword is a modifier that you can use on fields. When a field declaration includes a " + Highlight ("readonly", colorStyle.KeywordModifiers) + " modifier, assignments to the fields introduced by the declaration can only occur as part of the declaration or in a constructor in the same class.";
				break;
			case "ref":
				result.SignatureMarkup = Highlight ("ref", colorStyle.KeywordParameter) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("ref", colorStyle.KeywordParameter) + " method parameter keyword on a method parameter causes a method to refer to the same variable that was passed into the method.";
				break;
			case "remove":
				result.SignatureMarkup = Highlight ("remove", colorStyle.KeywordContext) + keywordSign;
				//TODO
				break;
			case "return":
				result.SignatureMarkup = Highlight ("return", colorStyle.KeywordJump) + keywordSign;
				result.AddCategory ("Form", Highlight ("return", colorStyle.KeywordJump) + " [expression];");
				result.SummaryMarkup = "The " + Highlight ("return", colorStyle.KeywordJump) + " statement terminates execution of the method in which it appears and returns control to the calling method.";
				break;
			case "select":
				result.SignatureMarkup = Highlight ("select", colorStyle.KeywordContext) + keywordSign;
				//TODO
				break;
			case "sealed":
				result.SignatureMarkup = Highlight ("sealed", colorStyle.KeywordModifiers) + keywordSign;
				result.SummaryMarkup = "A sealed class cannot be inherited.";
				break;
			case "set":
				result.SignatureMarkup = Highlight ("set", colorStyle.KeywordContext) + keywordSign;
				//TODO
				break;
			case "sizeof":
				result.SignatureMarkup = Highlight ("sizeof", colorStyle.KeywordOperators) + keywordSign;
				result.AddCategory ("Form", Highlight ("sizeof", colorStyle.KeywordOperators) + " (type)");
				result.SummaryMarkup = "The " + Highlight ("sizeof", colorStyle.KeywordOperators) + " operator is used to obtain the size in bytes for a value type.";
				break;
			case "stackalloc":
				result.SignatureMarkup = Highlight ("stackalloc", colorStyle.KeywordOperators) + keywordSign;
				result.AddCategory ("Form", "type * ptr = " + Highlight ("stackalloc", colorStyle.KeywordOperators) + " type [ expr ];");
				result.SummaryMarkup = "Allocates a block of memory on the stack.";
				break;
			case "static":
				result.SignatureMarkup = Highlight ("static", colorStyle.KeywordModifiers) + keywordSign;
				result.SummaryMarkup = "Use the " + Highlight ("static", colorStyle.KeywordModifiers) + " modifier to declare a static member, which belongs to the type itself rather than to a specific object.";
				break;
			case "struct":
				result.SignatureMarkup = Highlight ("struct", colorStyle.KeywordDeclaration) + keywordSign;
				result.AddCategory ("Form", "[attributes] [modifiers] " + Highlight ("struct", colorStyle.KeywordDeclaration) + " identifier [:interfaces] body [;]");
				result.SummaryMarkup = "A " + Highlight ("struct", colorStyle.KeywordDeclaration) + " type is a value type that can contain constructors, constants, fields, methods, properties, indexers, operators, events, and nested types. ";
				break;
			case "switch":
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
			case "this":
				result.SignatureMarkup = Highlight ("this", colorStyle.KeywordAccessors) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("this", colorStyle.KeywordAccessors) + " keyword refers to the current instance of the class.";
				break;
			case "throw":
				result.SignatureMarkup = Highlight ("throw", colorStyle.KeywordException) + keywordSign;
				result.AddCategory ("Form", Highlight ("throw", colorStyle.KeywordException) + " [expression];");
				result.SummaryMarkup = "The " + Highlight ("throw", colorStyle.KeywordException) + " statement is used to signal the occurrence of an anomalous situation (exception) during the program execution.";
				break;
			case "try":
				result.SignatureMarkup = Highlight ("try", colorStyle.KeywordException) + keywordSign;
				result.AddCategory ("Form", Highlight ("try", colorStyle.KeywordException) + " try-block" + Environment.NewLine + 
				                    "  " + Highlight ("catch", colorStyle.KeywordException) + " (exception-declaration-1) catch-block-1 " + Environment.NewLine + 
					"  " + Highlight ("catch", colorStyle.KeywordException) + " (exception-declaration-2) catch-block-2 " + Environment.NewLine + 
					"..." + Environment.NewLine + 
				                    Highlight ("try", colorStyle.KeywordException) + " try-block " + Highlight ("catch", colorStyle.KeywordException) + " catch-block");
				result.SummaryMarkup = "The try-catch statement consists of a " + Highlight ("try", colorStyle.KeywordException) + " block followed by one or more " + Highlight ("catch", colorStyle.KeywordException) + " clauses, which specify handlers for different exceptions.";
				break;
			case "typeof":
				result.SignatureMarkup = Highlight ("typeof", colorStyle.KeywordOperators) + keywordSign;
				result.AddCategory ("Form", Highlight ("typeof", colorStyle.KeywordOperators) + "(type)");
				result.SummaryMarkup = "The " + Highlight ("typeof", colorStyle.KeywordOperators) + " operator is used to obtain the System.Type object for a type.";
				break;
			case "unchecked":
				result.SignatureMarkup = Highlight ("unchecked", colorStyle.KeywordOther) + keywordSign;
				result.AddCategory ("Form", Highlight ("unchecked", colorStyle.KeywordOther) + " block" + Environment.NewLine +
				                    Highlight ("unchecked", colorStyle.KeywordOther) + " (expression)");
				result.SummaryMarkup = "The "+ Highlight ("unchecked", colorStyle.KeywordOther) + " keyword is used to control the overflow-checking context for integral-type arithmetic operations and conversions.";
				break;
			case "unsafe":
				result.SignatureMarkup = Highlight ("unsafe", colorStyle.KeywordOther) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("unsafe", colorStyle.KeywordOther) + " keyword denotes an unsafe context, which is required for any operation involving pointers.";
				break;
			case "using":
				result.SignatureMarkup = Highlight ("using", colorStyle.KeywordNamespace) + keywordSign;
				result.AddCategory ("Form", Highlight ("using", colorStyle.KeywordNamespace) + " (expression | type identifier = initializer) statement" + Environment.NewLine +
				                    Highlight ("using", colorStyle.KeywordNamespace) + " [alias = ]class_or_namespace;");
				result.SummaryMarkup = "The " + Highlight ("using", colorStyle.KeywordNamespace) + " directive creates an alias for a namespace or imports types defined in other namespaces. The " + Highlight ("using", colorStyle.KeywordNamespace) + " statement defines a scope at the end of which an object will be disposed.";
				break;
			case "virtual":
				result.SignatureMarkup = Highlight ("virtual", colorStyle.KeywordModifiers) + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("virtual", colorStyle.KeywordModifiers) + " keyword is used to modify a method or property declaration, in which case the method or the property is called a virtual member.";
				break;
			case "volatile":
				result.SignatureMarkup = Highlight ("volatile", colorStyle.KeywordModifiers) + keywordSign;
				result.AddCategory ("Form", Highlight ("volatile", colorStyle.KeywordModifiers) + " declaration");
				result.SummaryMarkup = "The " + Highlight ("volatile", colorStyle.KeywordModifiers) + " keyword indicates that a field can be modified in the program by something such as the operating system, the hardware, or a concurrently executing thread.";
				break;
			case "void":
				result.SignatureMarkup = Highlight ("void", colorStyle.KeywordTypes) + keywordSign;
				break;
			case "where":
				result.SignatureMarkup = Highlight ("where", colorStyle.KeywordContext) + keywordSign;
				//TODO
				break;
			case "yield":
				result.SignatureMarkup = Highlight ("yield", colorStyle.KeywordContext) + keywordSign;
				//TODO
				break;
			case "while":
				result.SignatureMarkup = Highlight ("while", colorStyle.KeywordIteration) + keywordSign;
				result.AddCategory ("Form", Highlight ("while", colorStyle.KeywordIteration) + " (expression) statement");
				result.SummaryMarkup = "The " + Highlight ("while", colorStyle.KeywordIteration) + " statement executes a statement or a block of statements until a specified expression evaluates to false. ";
				break;
			}
			return result;
		}

		public TooltipInformation GetConstraintTooltip (string keyword)
		{
			var result = new TooltipInformation ();

			var color = AlphaBlend (colorStyle.PlainText.Foreground, colorStyle.PlainText.Background, optionalAlpha);
			var colorString = Mono.TextEditor.HelperMethods.GetColorString (color);
			
			var keywordSign = "<span foreground=\"" + colorString + "\">" + " (keyword)</span>";

			result.SignatureMarkup = Highlight (keyword, colorStyle.KeywordTypes) + keywordSign;

			switch (keyword) {
			case "class":
				result.AddCategory ("Constraint", "The type argument must be a reference type; this applies also to any class, interface, delegate, or array type.");
				break;
			case "new":
				result.AddCategory ("Constraint", "The type argument must have a public parameterless constructor. When used together with other constraints, the new() constraint must be specified last.");
				break;
			case "struct":
				result.AddCategory ("Constraint", "The type argument must be a value type. Any value type except Nullable can be specified. See Using Nullable Types (C# Programming Guide) for more information.");
				break;
			}

			return result;
		}

		public TooltipInformation GetTypeOfTooltip (TypeOfExpression typeOfExpression, TypeOfResolveResult resolveResult)
		{
			var result = new TooltipInformation ();
			if (resolveResult == null) {
				result.SignatureMarkup = AmbienceService.EscapeText (typeOfExpression.Type.ToString ());
			} else {
				result.SignatureMarkup = GetTypeMarkup (resolveResult.ReferencedType, true);
			}
			return result;
		}

		public TooltipInformation GetAliasedNamespaceTooltip (AliasNamespaceResolveResult resolveResult)
		{
			var result = new TooltipInformation ();
			result.SignatureMarkup = GetMarkup (resolveResult.Namespace);
			result.AddCategory (GettextCatalog.GetString ("Alias information"), GettextCatalog.GetString ("Resolved using alias '{0}'", resolveResult.Alias));
			return result;
		}
		
		public TooltipInformation GetAliasedTypeTooltip (AliasTypeResolveResult resolveResult)
		{
			var result = new TooltipInformation ();
			result.SignatureMarkup = GetTypeMarkup (resolveResult.Type, true);
			result.AddCategory (GettextCatalog.GetString ("Alias information"), GettextCatalog.GetString ("Resolved using alias '{0}'", resolveResult.Alias));
			return result;
		}
		
		string GetEventMarkup (IEvent evt)
		{
			if (evt == null)
				throw new ArgumentNullException ("evt");
			var result = new StringBuilder ();
			AppendModifiers (result, evt);
			result.Append (Highlight ("event ", colorStyle.KeywordModifiers));
			result.Append (GetTypeReferenceString (evt.ReturnType));
			if (BreakLineAfterReturnType) {
				result.AppendLine ();
			} else {
				result.Append (" ");
			}

			AppendExplicitInterfaces (result, evt);
			result.Append (HighlightSemantically (CSharpAmbience.FilterName (evt.Name), colorStyle.UserEventDeclaration));
			return result.ToString ();
		}

		bool grayOut;
		bool GrayOut {
			get {
				return grayOut;
			}
			set {
				grayOut = value;
			}
		}

		void AppendParameterList (StringBuilder result, IList<IParameter> parameterList, bool spaceBefore, bool spaceAfter, bool newLine = true)
		{
			if (parameterList == null || parameterList.Count == 0)
				return;
			if (newLine)
				result.AppendLine ();
			for (int i = 0; i < parameterList.Count; i++) {
				var parameter = parameterList [i];
				if (newLine)
					result.Append (new string (' ', 2));
				var doHighightParameter = i == HighlightParameter || HighlightParameter >= i && i == parameterList.Count - 1 && parameter.IsParams;
				if (doHighightParameter)
					result.Append("<u>");
				/*				if (parameter.IsOptional) {
					GrayOut = true;
					var color = AlphaBlend (colorStyle.Default.Color, colorStyle.Default.BackgroundColor, optionalAlpha);
					var colorString = Mono.TextEditor.HelperMethods.GetColorString (color);
					result.Append ("<span foreground=\"" + colorString + "\">");
				}*/
				AppendParameter (result, parameter);
				if (parameter.IsOptional) {
					if (formattingOptions.SpaceAroundAssignment) {
						result.Append (" = ");
					} else {
						result.Append ("=");
					}
					AppendConstant (result, parameter.Type, parameter.ConstantValue);
//					GrayOut = false;
//					result.Append ("</span>");
				}
				if (doHighightParameter)
					result.Append("</u>");
				if (i + 1 < parameterList.Count) {
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
		
		void AppendParameter (StringBuilder result, IParameter parameter)
		{
			if (parameter == null)
				return;
			if (parameter.IsOut) {
				result.Append (Highlight ("out ", colorStyle.KeywordParameter));
			} else if (parameter.IsRef) {
				result.Append (Highlight ("ref ", colorStyle.KeywordParameter));
			} else if (parameter.IsParams) {
				result.Append (Highlight ("params ", colorStyle.KeywordParameter));
			}
			result.Append (GetTypeReferenceString (parameter.Type));
			result.Append (" ");
			result.Append (CSharpAmbience.FilterName (parameter.Name));
		}

		void AppendExplicitInterfaces (StringBuilder sb, IMember member)
		{
			if (member == null || !member.IsExplicitInterfaceImplementation)
				return;
			foreach (var implementedInterfaceMember in member.ImplementedInterfaceMembers) {
				sb.Append (GetTypeReferenceString (implementedInterfaceMember.DeclaringTypeDefinition));
				sb.Append (".");
			}
		}

		void AppendConstant (StringBuilder sb, IType constantType, object constantValue)
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
				if (constantType.Kind == TypeKind.Struct) {
					// structs can never be == null, therefore it's the default value.
					sb.Append (Highlight ("default", colorStyle.KeywordSelection) + "(" + GetTypeReferenceString (constantType) + ")");
				} else {
					sb.Append (Highlight ("null", colorStyle.KeywordConstants));
				}
				return;
			}

			while (NullableType.IsNullable (constantType)) 
				constantType = NullableType.GetUnderlyingType (constantType);
			if (constantType.Kind == TypeKind.Enum) {
				foreach (var field in constantType.GetFields ()) {
					if (field.ConstantValue == constantValue){
						sb.Append (GetTypeReferenceString (constantType) + "." + CSharpAmbience.FilterName (field.Name));
						return;
					}
				}
				sb.Append ("(" + GetTypeReferenceString (constantType) + ")" + Highlight (constantValue.ToString (), colorStyle.Number));
				return;
			}

			sb.Append (Highlight (constantValue.ToString (), colorStyle.Number));
		}

		void AppendVariance (StringBuilder sb, VarianceModifier variance)
		{
			if (variance  == VarianceModifier.Contravariant) {
				sb.Append (Highlight ("in ", colorStyle.KeywordParameter));
			} else if (variance  == VarianceModifier.Covariant) {
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
			return AlphaBlend ((Gdk.Color) ((HslColor)color), (Gdk.Color) ((HslColor)color2), alpha);
		}

		public string GetArrayIndexerMarkup (ArrayType arrayType)
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
			for (int i = 0; i < arrayType.Dimensions; i++) {
				if (i > 0)
					result.Append (", ");
				var doHighightParameter = i == HighlightParameter;
				if (doHighightParameter)
					result.Append ("<u>");

				result.Append (Highlight ("int ", colorStyle.KeywordTypes));
				result.Append (arrayType.Dimensions == 1 ? "index" : "i" + (i + 1));
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
			var color = (Gdk.Color) ((HslColor)colorStyle.GetForeground (style));

			if (grayOut) {
				color = AlphaBlend (color, (Gdk.Color) ((HslColor)colorStyle.PlainText.Background), optionalAlpha);
			}

			var colorString = Mono.TextEditor.HelperMethods.GetColorString (color);
			return "<span foreground=\"" + colorString + "\">" + str + "</span>";
		}

		string HighlightSemantically (string str, ChunkStyle style)
		{
			if (!MonoDevelop.SourceEditor.DefaultSourceEditorOptions.Instance.EnableSemanticHighlighting)
				return str;
			return Highlight (str, style);
		}
	}
}
