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
				return Highlight (astType.GetText (formattingOptions), "Keyword(Type)");
			}
			var text = AmbienceService.EscapeText (astType.GetText (formattingOptions));
			return highlight ? HighlightSemantically (text, "User Types") : text;
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
			result.Append (Highlight ("namespace ", "Keyword(Namespace)"));
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
					result.Append (Highlight ("internal ", "Keyword(Modifiers)"));
				break;
			case Accessibility.ProtectedAndInternal:
				result.Append (Highlight ("protected internal ", "Keyword(Modifiers)"));
				break;
			case Accessibility.ProtectedOrInternal:
				result.Append (Highlight ("internal protected ", "Keyword(Modifiers)"));
				break;
			case Accessibility.Protected:
				result.Append (Highlight ("protected ", "Keyword(Modifiers)"));
				break;
			case Accessibility.Private:
// private is the default modifier - no need to show that
//				result.Append (Highlight (" private", "Keyword(Modifiers)"));
				break;
			case Accessibility.Public:
				result.Append (Highlight ("public ", "Keyword(Modifiers)"));
				break;
			}

			if (entity is IField && ((IField)entity).IsConst) {
				result.Append (Highlight ("const ", "Keyword(Modifiers)"));
			} else if (entity.IsStatic) {
				result.Append (Highlight ("static ", "Keyword(Modifiers)"));
			} else if (entity.IsSealed) {
				if (!(entity is IType && ((IType)entity).Kind == TypeKind.Delegate))
					result.Append (Highlight ("sealed ", "Keyword(Modifiers)"));
			} else if (entity.IsAbstract) {
				if (!(entity is IType && ((IType)entity).Kind == TypeKind.Interface))
					result.Append (Highlight ("abstract ", "Keyword(Modifiers)"));
			}


			if (entity.IsShadowing)
				result.Append (Highlight ("new ", "Keyword(Modifiers)"));

			var member = entity as IMember;
			if (member != null) {
				if (member.IsOverride) {
					result.Append (Highlight ("override ", "Keyword(Modifiers)"));
				} else if (member.IsVirtual) {
					result.Append (Highlight ("virtual ", "Keyword(Modifiers)"));
				}
			}
			var field = entity as IField;
			if (field != null) {
				if (field.IsVolatile)
					result.Append (Highlight ("volatile ", "Keyword(Modifiers)"));
				if (field.IsReadOnly)
					result.Append (Highlight ("readonly ", "Keyword(Modifiers)"));
			}

			var method = entity as IMethod;
			if (method != null) {
				if (method.IsAsync)
					result.Append (Highlight ("async ", "Keyword(Modifiers)"));
				if (method.IsPartial)
					result.Append (Highlight ("partial ", "Keyword(Modifiers)"));
			}
		}

		void AppendAccessibility (StringBuilder result, IMethod entity)
		{
			switch (entity.Accessibility) {
			case Accessibility.Internal:
				result.Append (Highlight ("internal", "Keyword(Modifiers)"));
				break;
			case Accessibility.ProtectedAndInternal:
				result.Append (Highlight ("protected internal", "Keyword(Modifiers)"));
				break;
			case Accessibility.ProtectedOrInternal:
				result.Append (Highlight ("internal protected", "Keyword(Modifiers)"));
				break;
			case Accessibility.Protected:
				result.Append (Highlight ("protected", "Keyword(Modifiers)"));
				break;
			case Accessibility.Private:
				result.Append (Highlight ("private", "Keyword(Modifiers)"));
				break;
			case Accessibility.Public:
				result.Append (Highlight ("public", "Keyword(Modifiers)"));
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
			var highlightedTypeName = Highlight (t.Name, "Keyword(Type)");
			result.Append (highlightedTypeName);

			var color = AlphaBlend (colorStyle.PlainText.CairoColor, colorStyle.PlainText.CairoBackgroundColor, optionalAlpha);
			var colorString = Mono.TextEditor.HelperMethods.GetColorString (color);

			result.Append ("<span foreground=\"" + colorString + "\">" + " (type parameter)</span>");
			var tp = t as ITypeParameter;
			if (tp != null) {
				if (!tp.HasDefaultConstructorConstraint && !tp.HasReferenceTypeConstraint && !tp.HasValueTypeConstraint && tp.DirectBaseTypes.All (IsObjectOrValueType))
					return result.ToString ();
				result.AppendLine ();
				result.Append (Highlight (" where ", "Keyword(Context)"));
				result.Append (highlightedTypeName);
				result.Append (" : ");
				int constraints = 0;

				if (tp.HasReferenceTypeConstraint) {
					constraints++;
					result.Append (Highlight ("class", "Keyword(Declaration)"));
				} else if (tp.HasValueTypeConstraint) {
					constraints++;
					result.Append (Highlight ("struct", "Keyword(Declaration)"));
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
					result.Append (Highlight ("new", "Keyword(Operator)"));
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
			result.Append (Highlight (t.Name, "Keyword(Type)"));
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
				result.Append (Highlight ("class ", "Keyword(Declaration)"));
				break;
			case TypeKind.Interface:
				result.Append (Highlight ("interface ", "Keyword(Declaration)"));
				break;
			case TypeKind.Struct:
				result.Append (Highlight ("struct ", "Keyword(Declaration)"));
				break;
			case TypeKind.Enum:
				result.Append (Highlight ("enum ", "Keyword(Declaration)"));
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
				result.Append (HighlightSemantically (CSharpAmbience.NetToCSharpTypeName (typeParameter.Name), "User Types"));
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
			result.Append (Highlight ("delegate ", "Keyword(Declaration)"));
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

			if (variable.IsConst) {
				result.Append (Highlight ("const", "Keyword(Modifiers)"));
			}

			result.Append (GetTypeReferenceString (variable.Type));
			if (BreakLineAfterReturnType) {
				result.AppendLine ();
			} else {
				result.Append (" ");
			}
	
			result.Append (variable.Name);
			
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

			result.Append (HighlightSemantically (CSharpAmbience.FilterName (field.Name), "User Field Declaration"));

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
				result.Append (HighlightSemantically (method.Name, "User Method Declaration"));
			}
			if (method.TypeArguments.Count > 0) {
				result.Append ("&lt;");
				for (int i = 0; i < method.TypeArguments.Count; i++) {
					if (i > 0)
						result.Append (", ");
					result.Append (HighlightSemantically (GetTypeReferenceString (method.TypeArguments[i], false), "User Types"));
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

			result.Append (method.DeclaringType.Name);

			if (formattingOptions.SpaceBeforeConstructorDeclarationParentheses)
				result.Append (" ");

			result.Append ('(');
			AppendParameterList (result,  method.Parameters, formattingOptions.SpaceBeforeConstructorDeclarationParameterComma, formattingOptions.SpaceAfterConstructorDeclarationParameterComma);
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
			result.Append (method.DeclaringType.Name);
			
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
				result.Append (Highlight ("this", "Keyword(Access)"));
			} else {
				result.Append (HighlightSemantically (CSharpAmbience.FilterName (property.Name), "User Property Declaration"));
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
				result.Append (Highlight (" get", "Keyword(Property)") + ";");
			}

			if (property.CanSet && IsAccessibleOrHasSourceCode(property.Setter)) {
				if (property.Setter.Accessibility != property.Accessibility) {
					result.Append (" ");
					AppendAccessibility (result, property.Setter);
				}
				result.Append (Highlight (" set", "Keyword(Property)") + ";");
			}
			result.Append (" }");

			return result.ToString ();
		}

		
		public TooltipInformation GetExternAliasTooltip (ExternAliasDeclaration externAliasDeclaration, DotNetProject project)
		{
			var result = new TooltipInformation ();
			result.SignatureMarkup = Highlight ("extern ", "Keyword(Modifiers)") + Highlight ("alias ", "Keyword(Namespace)") + externAliasDeclaration.Name;
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

			var color = AlphaBlend (colorStyle.PlainText.CairoColor, colorStyle.PlainText.CairoBackgroundColor, optionalAlpha);
			var colorString = Mono.TextEditor.HelperMethods.GetColorString (color);
			
			var keywordSign = "<span foreground=\"" + colorString + "\">" + " (keyword)</span>";

			switch (keyword){
			case "abstract":
				result.SignatureMarkup = Highlight ("abstract", "Keyword(Modifiers)") + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("abstract", "Keyword(Modifiers)") + " modifier can be used with classes, methods, properties, indexers, and events.";
				break;
			case "add":
				result.SignatureMarkup = Highlight ("add", "Keyword(Context)") + keywordSign;
				//TODO
				break;
			case "ascending":
				result.SignatureMarkup = Highlight ("ascending", "Keyword(Context)") + keywordSign;
				//TODO
				break;
			case "async":
				result.SignatureMarkup = Highlight ("async", "Keyword(Context)") + keywordSign;
				//TODO
				break;
			case "as":
				result.SignatureMarkup = Highlight ("as", "Keyword(Operator)") + keywordSign;
				result.AddCategory ("Form", "expression " + Highlight ("as", "Keyword(Operator)") + " type");
				result.SummaryMarkup = "The " + Highlight ("as", "Keyword(Operator)") + " operator is used to perform conversions between compatible types. ";
				break;
			case "await":
				result.SignatureMarkup = Highlight ("await", "Keyword(Context)") + keywordSign;
				//TODO
				break;
			case "base":
				result.SignatureMarkup = Highlight ("base", "Keyword(Access)") + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("base", "Keyword(Access)") + " keyword is used to access members of the base class from within a derived class.";
				break;
			case "break":
				result.SignatureMarkup = Highlight ("break", "Keyword(Jump)") + keywordSign;
				result.AddCategory ("Form", Highlight ("break", "Keyword(Jump)") + ";");
				result.SummaryMarkup = "The " + Highlight ("break", "Keyword(Jump)") + " statement terminates the closest enclosing loop or switch statement in which it appears.";
				break;
			case "case":
				result.SignatureMarkup = Highlight ("case", "Keyword(Selection)") + keywordSign;
				result.AddCategory ("Form", Highlight ("case", "Keyword(Selection)") + " constant-expression:" + Environment.NewLine +
				                    "  statement" + Environment.NewLine +
				                    "  jump-statement");
				result.SummaryMarkup = "";
				break;
			case "catch":
				result.SignatureMarkup = Highlight ("catch", "Keyword(Exception)") + keywordSign;
				result.AddCategory ("Form", Highlight ("try", "Keyword(Exception)") + " try-block" + Environment.NewLine +
				                    "  " + Highlight ("catch", "Keyword(Exception)") + " (exception-declaration-1) catch-block-1" + Environment.NewLine +
				                    "  " + Highlight ("catch", "Keyword(Exception)") + " (exception-declaration-2) catch-block-2" + Environment.NewLine +
				                    "  ..." + Environment.NewLine +
				                    Highlight ("try", "Keyword(Exception)") + " try-block " + Highlight ("catch", "Keyword(Exception)") + " catch-block");
				result.SummaryMarkup = "";
				break;
			case "checked":
				result.SignatureMarkup = Highlight ("checked", "Keyword(Other)") + keywordSign;
				result.AddCategory ("Form", Highlight ("checked", "Keyword(Other)") + " block" + Environment.NewLine +
				                    "or" + Environment.NewLine +
				                    Highlight ("checked", "Keyword(Other)") + " (expression)");
				result.SummaryMarkup = "The " + Highlight ("checked", "Keyword(Other)") + " keyword is used to control the overflow-checking context for integral-type arithmetic operations and conversions. It can be used as an operator or a statement.";
				break;
			case "class":
				result.SignatureMarkup = Highlight ("class", "Keyword(Declaration)") + keywordSign;
				result.AddCategory ("Form", "[attributes] [modifiers] " + Highlight ("class", "Keyword(Declaration)") + " identifier [:base-list] { class-body }[;]");
				result.SummaryMarkup = "Classes are declared using the keyword " + Highlight ("class", "Keyword(Declaration)") + ".";
				break;
			case "const":
				result.SignatureMarkup = Highlight ("const", "Keyword(Modifiers)") + keywordSign;
				result.AddCategory ("Form", "[attributes] [modifiers] " + Highlight ("const", "Keyword(Modifiers)") + " type declarators;");
				result.SummaryMarkup = "The " + Highlight ("const", "Keyword(Modifiers)") + " keyword is used to modify a declaration of a field or local variable. It specifies that the value of the field or the local variable cannot be modified. ";
				break;
			case "continue":
				result.SignatureMarkup = Highlight ("continue", "Keyword(Jump)") + keywordSign;
				result.AddCategory ("Form", Highlight ("continue", "Keyword(Jump)") + ";");
				result.SummaryMarkup = "The " + Highlight ("continue", "Keyword(Jump)") + " statement passes control to the next iteration of the enclosing iteration statement in which it appears.";
				break;
			case "default":
				result.SignatureMarkup = Highlight ("default", "Keyword(Selection)") + keywordSign;
				result.SummaryMarkup = "";
				if (hintNode != null) {
					if (hintNode.Parent is DefaultValueExpression) {
						result.AddCategory ("Form",
						                    Highlight ("default", "Keyword(Selection)") + " (Type)");
						break;
					} else if (hintNode.Parent is CaseLabel) {
						result.AddCategory ("Form",
						                    Highlight ("switch", "Keyword(Selection)") + " (expression) { "+ Environment.NewLine +
						                    "  " + Highlight ("case", "Keyword(Selection)") + " constant-expression:" + Environment.NewLine +
						                    "    statement"+ Environment.NewLine +
						                    "    jump-statement" + Environment.NewLine +
						                    "  [" + Highlight ("default", "Keyword(Selection)") + ":" + Environment.NewLine +
						                    "    statement" + Environment.NewLine +
						                    "    jump-statement]" + Environment.NewLine +
						                    "}");
						break;
					}
				}
				result.AddCategory ("Form",
				                    Highlight ("default", "Keyword(Selection)") + " (Type)" + Environment.NewLine + Environment.NewLine +
				                    "or" + Environment.NewLine + Environment.NewLine +
				                    Highlight ("switch", "Keyword(Selection)") + " (expression) { "+ Environment.NewLine +
				                    "  " + Highlight ("case", "Keyword(Selection)") + " constant-expression:" + Environment.NewLine +
				                    "    statement"+ Environment.NewLine +
				                    "    jump-statement" + Environment.NewLine +
				                    "  [" + Highlight ("default", "Keyword(Selection)") + ":" + Environment.NewLine +
				                    "    statement" + Environment.NewLine +
				                    "    jump-statement]" + Environment.NewLine +
				                    "}");
				break;
			case "delegate":
				result.SignatureMarkup = Highlight ("delegate", "Keyword(Declaration)") + keywordSign;
				result.AddCategory ("Form", "[attributes] [modifiers] " + Highlight ("delegate", "Keyword(Declaration)") + " result-type identifier ([formal-parameters]);");
				result.SummaryMarkup = "A " + Highlight ("delegate", "Keyword(Declaration)") + " declaration defines a reference type that can be used to encapsulate a method with a specific signature.";
				break;
			case "dynamic":
				result.SignatureMarkup = Highlight ("dynamic", "Keyword(Context)") + keywordSign;
				//TODO
				break;
			case "descending":
				result.SignatureMarkup = Highlight ("descending", "Keyword(Context)") + keywordSign;
				//TODO
				break;
			case "do":
				result.SignatureMarkup = Highlight ("do", "Keyword(Iteration)") + keywordSign;
				result.AddCategory ("Form", Highlight ("do", "Keyword(Iteration)") + " statement " + Highlight ("while", "Keyword(Iteration)") + " (expression);");
				result.SummaryMarkup = "The " + Highlight ("do", "Keyword(Iteration)") + " statement executes a statement or a block of statements repeatedly until a specified expression evaluates to false.";
				break;
			case "else":
				result.SignatureMarkup = Highlight ("else", "Keyword(Selection)") + keywordSign;
				result.AddCategory ("Form", Highlight ("if", "Keyword(Selection)") + " (expression)" + Environment.NewLine +
				                    "  statement1" + Environment.NewLine +
				                    "  [" + Highlight ("else", "Keyword(Selection)") + Environment.NewLine +
				                    "  statement2]");
				result.SummaryMarkup = "";
				break;
			case "enum":
				result.SignatureMarkup = Highlight ("enum", "Keyword(Declaration)") + keywordSign;
				result.AddCategory ("Form", "[attributes] [modifiers] " + Highlight ("enum", "Keyword(Declaration)") + " identifier [:base-type] {enumerator-list} [;]");
				result.SummaryMarkup = "The " + Highlight ("enum", "Keyword(Declaration)") + " keyword is used to declare an enumeration, a distinct type consisting of a set of named constants called the enumerator list.";
				break;
			case "event":
				result.SignatureMarkup = Highlight ("event", "Keyword(Modifiers)") + keywordSign;
				result.AddCategory ("Form", "[attributes] [modifiers] " + Highlight ("event", "Keyword(Modifiers)") + " type declarator;" + Environment.NewLine +
				                    "[attributes] [modifiers] " + Highlight ("event", "Keyword(Modifiers)") + " type member-name {accessor-declarations};");
				result.SummaryMarkup = "Specifies an event.";
				break;
			case "explicit":
				result.SignatureMarkup = Highlight ("explicit", "Keyword(Operator Declaration)") + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("explicit", "Keyword(Operator Declaration)") + " keyword is used to declare an explicit user-defined type conversion operator.";
				break;
			case "extern":
				result.SignatureMarkup = Highlight ("extern", "Keyword(Modifiers)") + keywordSign;
				result.SummaryMarkup = "Use the " + Highlight ("extern", "Keyword(Modifiers)") + " modifier in a method declaration to indicate that the method is implemented externally. A common use of the extern modifier is with the DllImport attribute.";
				break;
			case "finally":
				result.SignatureMarkup = Highlight ("finally", "Keyword(Exception)") + keywordSign;
				result.AddCategory ("Form", Highlight ("try", "Keyword(Exception)") + " try-block " + Highlight ("finally", "Keyword(Exception)") + " finally-block");
				result.SummaryMarkup = "The " + Highlight ("finally", "Keyword(Exception)") + " block is useful for cleaning up any resources allocated in the try block. Control is always passed to the finally block regardless of how the try block exits.";
				break;
			case "fixed":
				result.SignatureMarkup = Highlight ("fixed", "Keyword(Other)") + keywordSign;
				result.AddCategory ("Form", Highlight ("fixed", "Keyword(Other)") + " ( type* ptr = expr ) statement");
				result.SummaryMarkup = "Prevents relocation of a variable by the garbage collector.";
				break;
			case "for":
				result.SignatureMarkup = Highlight ("for", "Keyword(Iteration)") + keywordSign;
				result.AddCategory ("Form", Highlight ("for", "Keyword(Iteration)") + " ([initializers]; [expression]; [iterators]) statement");
				result.SummaryMarkup = "The " + Highlight ("for", "Keyword(Iteration)") + " loop executes a statement or a block of statements repeatedly until a specified expression evaluates to false.";
				break;
			case "foreach":
				result.SignatureMarkup = Highlight ("foreach", "Keyword(Iteration)") + keywordSign;
				result.AddCategory ("Form", Highlight ("foreach", "Keyword(Iteration)") + " (type identifier " + Highlight ("in", "Keyword(Iteration)") + " expression) statement");
				result.SummaryMarkup = "The " + Highlight ("foreach", "Keyword(Iteration)") + " statement repeats a group of embedded statements for each element in an array or an object collection. ";
				break;
			case "from":
				result.SignatureMarkup = Highlight ("from", "Keyword(Context)") + keywordSign;
				//TODO
				break;
			case "get":
				result.SignatureMarkup = Highlight ("get", "Keyword(Context)") + keywordSign;
				//TODO
				break;
			case "global":
				result.SignatureMarkup = Highlight ("global", "Keyword(Context)") + keywordSign;
				//TODO
				break;
			case "goto":
				result.SignatureMarkup = Highlight ("goto", "Keyword(Jump)") + keywordSign;
				result.AddCategory ("Form", Highlight ("goto", "Keyword(Jump)") + " identifier;" + Environment.NewLine +
					Highlight ("goto", "Keyword(Jump)") + " " + Highlight ("case", "Keyword(Selection)") + " constant-expression;" + Environment.NewLine +
				                    Highlight ("goto", "Keyword(Jump)") + " " + Highlight ("default", "Keyword(Selection)") + ";");
				result.SummaryMarkup = "The " + Highlight ("goto", "Keyword(Jump)") + " statement transfers the program control directly to a labeled statement. ";
				break;
			case "group":
				result.SignatureMarkup = Highlight ("group", "Keyword(Context)") + keywordSign;
				//TODO
				break;
			case "if":
				result.SignatureMarkup = Highlight ("if", "Keyword(Selection)") + keywordSign;
				result.AddCategory ("Form", Highlight ("if", "Keyword(Selection)") + " (expression)" + Environment.NewLine +
					"  statement1" + Environment.NewLine +
					"  [" + Highlight ("else", "Keyword(Selection)") + Environment.NewLine +
				                    "  statement2]");
				result.SummaryMarkup = "The " + Highlight ("if", "Keyword(Selection)") + " statement selects a statement for execution based on the value of a Boolean expression. ";
				break;
			case "into":
				result.SignatureMarkup = Highlight ("into", "Keyword(Context)") + keywordSign;
				//TODO
				break;
			case "implicit":
				result.SignatureMarkup = Highlight ("implicit", "Keyword(Operator Declaration)") + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("implicit", "Keyword(Operator Declaration)") + " keyword is used to declare an implicit user-defined type conversion operator.";
				break;
			case "in":
				result.SignatureMarkup = Highlight ("in", "Keyword(Iteration)") + keywordSign;
				if (hintNode != null) {
					if (hintNode.Parent is ForeachStatement) {
						result.AddCategory ("Form",
						                    Highlight ("foreach", "Keyword(Iteration)") + " (type identifier " + Highlight ("in", "Keyword(Iteration)") + " expression) statement");
						break;
					}
					if (hintNode.Parent is QueryFromClause) {
						result.AddCategory ("Form",
						                    Highlight ("from", "Keyword(Context)") + " range-variable " + Highlight ("in", "Keyword(Iteration)") + " data-source [query clauses] " + Highlight ("select", "Keyword(Context)") + " product-expression");
						break;
					}
					if (hintNode.Parent is TypeParameterDeclaration) {
						result.AddCategory ("Form",
						                    Highlight ("interface", "Keyword(Declaration)") + " IMyInterface&lt;" + Highlight ("in", "Keyword(Iteration)") + " T&gt; {}");
						break;
					}
				}
				result.AddCategory ("Form", Highlight ("foreach", "Keyword(Iteration)") + " (type identifier " + Highlight ("in", "Keyword(Iteration)") + " expression) statement"+ Environment.NewLine + Environment.NewLine +
				                    "or" + Environment.NewLine + Environment.NewLine +
				                    Highlight ("from", "Keyword(Context)") + " range-variable " + Highlight ("in", "Keyword(Iteration)") + " data-source [query clauses] " + Highlight ("select", "Keyword(Context)") + " product-expression" + Environment.NewLine + Environment.NewLine +
				                    "or" + Environment.NewLine + Environment.NewLine +
				                    Highlight ("interface", "Keyword(Declaration)") + " IMyInterface&lt;" + Highlight ("in", "Keyword(Iteration)") + " T&gt; {}"
				                    );
				break;
			case "interface":
				result.SignatureMarkup = Highlight ("interface", "Keyword(Declaration)") + keywordSign;
				result.AddCategory ("Form", "[attributes] [modifiers] " + Highlight ("interface", "Keyword(Declaration)") + " identifier [:base-list] {interface-body}[;]");
				result.SummaryMarkup = "An interface defines a contract. A class or struct that implements an interface must adhere to its contract.";
				break;
			case "internal":
				result.SignatureMarkup = Highlight ("internal", "Keyword(Modifiers)") + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("internal", "Keyword(Modifiers)") + " keyword is an access modifier for types and type members. Internal members are accessible only within files in the same assembly.";
				break;
			case "is":
				result.SignatureMarkup = Highlight ("is", "Keyword(Operator)") + keywordSign;
				result.AddCategory ("Form", "expression " + Highlight ("is", "Keyword(Operator)") + " type");
				result.SummaryMarkup = "The " + Highlight ("is", "Keyword(Operator)") + " operator is used to check whether the run-time type of an object is compatible with a given type.";
				break;
			case "join":
				result.SignatureMarkup = Highlight ("join", "Keyword(Context)") + keywordSign;
				//TODO
				break;
			case "let":
				result.SignatureMarkup = Highlight ("let", "Keyword(Context)") + keywordSign;
				//TODO
				break;
			case "lock":
				result.SignatureMarkup = Highlight ("lock", "Keyword(Other)") + keywordSign;
				result.AddCategory ("Form", Highlight ("lock", "Keyword(Other)") + " (expression) statement_block");
				result.SummaryMarkup = "The " + Highlight ("lock", "Keyword(Other)") + " keyword marks a statement block as a critical section by obtaining the mutual-exclusion lock for a given object, executing a statement, and then releasing the lock. ";
				break;
			case "namespace":
				result.SignatureMarkup = Highlight ("namespace", "Keyword(Namespace)") + keywordSign;
				result.AddCategory ("Form", Highlight ("namespace", "Keyword(Namespace)") + " name[.name1] ...] {" + Environment.NewLine +
					"type-declarations" + Environment.NewLine +
				                    " }");
				result.SummaryMarkup = "The " + Highlight ("namespace", "Keyword(Namespace)") + " keyword is used to declare a scope. ";
				break;
			case "new":
				result.SignatureMarkup = Highlight ("new", "Keyword(Operator)") + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("new", "Keyword(Operator)") + " keyword can be used as an operator or as a modifier. The operator is used to create objects on the heap and invoke constructors. The modifier is used to hide an inherited member from a base class member.";
				break;
			case "null":
				result.SignatureMarkup = Highlight ("null", "constant.language") + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("null", "constant.language") + " keyword is a literal that represents a null reference, one that does not refer to any object. " + Highlight ("null", "constant.language") + " is the default value of reference-type variables.";
				break;
			case "operator":
				result.SignatureMarkup = Highlight ("operator", "Keyword(Operator Declaration)") + keywordSign;
				result.AddCategory ("Form", Highlight ("public static ", "Keyword(Modifiers)") + "result-type " + Highlight ("operator", "Keyword(Operator Declaration)") + " unary-operator ( op-type operand )" + Environment.NewLine +
				                    Highlight ("public static ", "Keyword(Modifiers)") + "result-type " + Highlight ("operator", "Keyword(Operator Declaration)") + " binary-operator (" + Environment.NewLine +
					"op-type operand," + Environment.NewLine +
					"op-type2 operand2" + Environment.NewLine +
					" )" + Environment.NewLine +
					Highlight ("public static ", "Keyword(Modifiers)") + Highlight ("implicit operator", "Keyword(Operator Declaration)") + " conv-type-out ( conv-type-in operand )" + Environment.NewLine +
				                    Highlight ("public static ", "Keyword(Modifiers)") + Highlight ("explicit operator", "Keyword(Operator Declaration)") + " conv-type-out ( conv-type-in operand )");
				result.SummaryMarkup = "The " + Highlight ("operator", "Keyword(Operator Declaration)") + " keyword is used to declare an operator in a class or struct declaration.";
				break;
			case "orderby":
				result.SignatureMarkup = Highlight ("orderby", "Keyword(Context)") + keywordSign;
				//TODO
				break;
			case "out":
				result.SignatureMarkup = Highlight ("out", "Keyword(Parameter)") + keywordSign;
				if (hintNode != null) {
					if (hintNode.Parent is TypeParameterDeclaration) {
						result.AddCategory ("Form",
						                    Highlight ("interface", "Keyword(Declaration)") + " IMyInterface&lt;" + Highlight ("out", "Keyword(Parameter)") + " T&gt; {}");
						break;
					}
					if (hintNode.Parent is ParameterDeclaration) {
						result.AddCategory ("Form",
						                    Highlight ("out", "Keyword(Parameter)") + " parameter-name");
						result.SummaryMarkup = "The " + Highlight ("out", "Keyword(Parameter)") + " method parameter keyword on a method parameter causes a method to refer to the same variable that was passed into the method.";
						break;
					}
				}

				result.AddCategory ("Form", 
				                    Highlight ("out", "Keyword(Parameter)") + " parameter-name" + Environment.NewLine + Environment.NewLine +
				                    "or" + Environment.NewLine + Environment.NewLine +
				                    Highlight ("interface", "Keyword(Declaration)") + " IMyInterface&lt;" + Highlight ("out", "Keyword(Parameter)") + " T&gt; {}"
				                    );
				break;
			case "override":
				result.SignatureMarkup = Highlight ("override", "Keyword(Modifiers)") + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("override", "Keyword(Modifiers)") + " modifier is used to override a method, a property, an indexer, or an event.";
				break;
			case "params":
				result.SignatureMarkup = Highlight ("params", "Keyword(Parameter)") + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("params", "Keyword(Parameter)") + " keyword lets you specify a method parameter that takes an argument where the number of arguments is variable.";
				break;
			case "partial":
				result.SignatureMarkup = Highlight ("partial", "Keyword(Context)") + keywordSign;
				//TODO
				break;
			case "private":
				result.SignatureMarkup = Highlight ("private", "Keyword(Modifiers)") + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("private", "Keyword(Modifiers)") + " keyword is a member access modifier. Private access is the least permissive access level. Private members are accessible only within the body of the class or the struct in which they are declared.";
				break;
			case "protected":
				result.SignatureMarkup = Highlight ("protected", "Keyword(Modifiers)") + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("protected", "Keyword(Modifiers)") + " keyword is a member access modifier. A protected member is accessible from within the class in which it is declared, and from within any class derived from the class that declared this member.";
				break;
			case "public":
				result.SignatureMarkup = Highlight ("public", "Keyword(Modifiers)") + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("public", "Keyword(Modifiers)") + " keyword is an access modifier for types and type members. Public access is the most permissive access level. There are no restrictions on accessing public members.";
				break;
			case "readonly":
				result.SignatureMarkup = Highlight ("readonly", "Keyword(Modifiers)") + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("readonly", "Keyword(Modifiers)") + " keyword is a modifier that you can use on fields. When a field declaration includes a " + Highlight ("readonly", "Keyword(Modifiers)") + " modifier, assignments to the fields introduced by the declaration can only occur as part of the declaration or in a constructor in the same class.";
				break;
			case "ref":
				result.SignatureMarkup = Highlight ("ref", "Keyword(Parameter)") + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("ref", "Keyword(Parameter)") + " method parameter keyword on a method parameter causes a method to refer to the same variable that was passed into the method.";
				break;
			case "remove":
				result.SignatureMarkup = Highlight ("remove", "Keyword(Context)") + keywordSign;
				//TODO
				break;
			case "return":
				result.SignatureMarkup = Highlight ("return", "Keyword(Jump)") + keywordSign;
				result.AddCategory ("Form", Highlight ("return", "Keyword(Jump)") + " [expression];");
				result.SummaryMarkup = "The " + Highlight ("return", "Keyword(Jump)") + " statement terminates execution of the method in which it appears and returns control to the calling method.";
				break;
			case "select":
				result.SignatureMarkup = Highlight ("select", "Keyword(Context)") + keywordSign;
				//TODO
				break;
			case "sealed":
				result.SignatureMarkup = Highlight ("sealed", "Keyword(Modifiers)") + keywordSign;
				result.SummaryMarkup = "A sealed class cannot be inherited.";
				break;
			case "set":
				result.SignatureMarkup = Highlight ("set", "Keyword(Context)") + keywordSign;
				//TODO
				break;
			case "sizeof":
				result.SignatureMarkup = Highlight ("sizeof", "Keyword(Operator)") + keywordSign;
				result.AddCategory ("Form", Highlight ("sizeof", "Keyword(Operator)") + " (type)");
				result.SummaryMarkup = "The " + Highlight ("sizeof", "Keyword(Operator)") + " operator is used to obtain the size in bytes for a value type.";
				break;
			case "stackalloc":
				result.SignatureMarkup = Highlight ("stackalloc", "Keyword(Operator)") + keywordSign;
				result.AddCategory ("Form", "type * ptr = " + Highlight ("stackalloc", "Keyword(Operator)") + " type [ expr ];");
				result.SummaryMarkup = "Allocates a block of memory on the stack.";
				break;
			case "static":
				result.SignatureMarkup = Highlight ("static", "Keyword(Modifiers)") + keywordSign;
				result.SummaryMarkup = "Use the " + Highlight ("static", "Keyword(Modifiers)") + " modifier to declare a static member, which belongs to the type itself rather than to a specific object.";
				break;
			case "struct":
				result.SignatureMarkup = Highlight ("struct", "Keyword(Declaration)") + keywordSign;
				result.AddCategory ("Form", "[attributes] [modifiers] " + Highlight ("struct", "Keyword(Declaration)") + " identifier [:interfaces] body [;]");
				result.SummaryMarkup = "A " + Highlight ("struct", "Keyword(Declaration)") + " type is a value type that can contain constructors, constants, fields, methods, properties, indexers, operators, events, and nested types. ";
				break;
			case "switch":
				result.SignatureMarkup = Highlight ("switch", "Keyword(Selection)") + keywordSign;
				result.AddCategory ("Form", Highlight ("switch", "Keyword(Selection)") + " (expression)" + Environment.NewLine + 
					" {" + Environment.NewLine + 
				                    "  " + Highlight ("case", "Keyword(Selection)") + " constant-expression:" + Environment.NewLine + 
					"  statement" + Environment.NewLine + 
					"  jump-statement" + Environment.NewLine + 
					"  [" + Highlight ("default", "Keyword(Selection)") + ":" + Environment.NewLine + 
					"  statement" + Environment.NewLine + 
					"  jump-statement]" + Environment.NewLine + 
				                    " }");
				result.SummaryMarkup = "The " + Highlight ("switch", "Keyword(Selection)") + " statement is a control statement that handles multiple selections by passing control to one of the " + Highlight ("case", "Keyword(Selection)") + " statements within its body.";
				break;
			case "this":
				result.SignatureMarkup = Highlight ("this", "Keyword(Access)") + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("this", "Keyword(Access)") + " keyword refers to the current instance of the class.";
				break;
			case "throw":
				result.SignatureMarkup = Highlight ("throw", "Keyword(Exception)") + keywordSign;
				result.AddCategory ("Form", Highlight ("throw", "Keyword(Exception)") + " [expression];");
				result.SummaryMarkup = "The " + Highlight ("throw", "Keyword(Exception)") + " statement is used to signal the occurrence of an anomalous situation (exception) during the program execution.";
				break;
			case "try":
				result.SignatureMarkup = Highlight ("try", "Keyword(Exception)") + keywordSign;
				result.AddCategory ("Form", Highlight ("try", "Keyword(Exception)") + " try-block" + Environment.NewLine + 
				                    "  " + Highlight ("catch", "Keyword(Exception)") + " (exception-declaration-1) catch-block-1 " + Environment.NewLine + 
					"  " + Highlight ("catch", "Keyword(Exception)") + " (exception-declaration-2) catch-block-2 " + Environment.NewLine + 
					"..." + Environment.NewLine + 
				                    Highlight ("try", "Keyword(Exception)") + " try-block " + Highlight ("catch", "Keyword(Exception)") + " catch-block");
				result.SummaryMarkup = "The try-catch statement consists of a " + Highlight ("try", "Keyword(Exception)") + " block followed by one or more " + Highlight ("catch", "Keyword(Exception)") + " clauses, which specify handlers for different exceptions.";
				break;
			case "typeof":
				result.SignatureMarkup = Highlight ("typeof", "Keyword(Operator)") + keywordSign;
				result.AddCategory ("Form", Highlight ("typeof", "Keyword(Operator)") + "(type)");
				result.SummaryMarkup = "The " + Highlight ("typeof", "Keyword(Operator)") + " operator is used to obtain the System.Type object for a type.";
				break;
			case "unchecked":
				result.SignatureMarkup = Highlight ("unchecked", "Keyword(Other)") + keywordSign;
				result.AddCategory ("Form", Highlight ("unchecked", "Keyword(Other)") + " block" + Environment.NewLine +
				                    Highlight ("unchecked", "Keyword(Other)") + " (expression)");
				result.SummaryMarkup = "The "+ Highlight ("unchecked", "Keyword(Other)") + " keyword is used to control the overflow-checking context for integral-type arithmetic operations and conversions.";
				break;
			case "unsafe":
				result.SignatureMarkup = Highlight ("unsafe", "Keyword(Other)") + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("unsafe", "Keyword(Other)") + " keyword denotes an unsafe context, which is required for any operation involving pointers.";
				break;
			case "using":
				result.SignatureMarkup = Highlight ("using", "Keyword(Namespace)") + keywordSign;
				result.AddCategory ("Form", Highlight ("using", "Keyword(Namespace)") + " (expression | type identifier = initializer) statement" + Environment.NewLine +
				                    Highlight ("using", "Keyword(Namespace)") + " [alias = ]class_or_namespace;");
				result.SummaryMarkup = "The " + Highlight ("using", "Keyword(Namespace)") + " directive creates an alias for a namespace or imports types defined in other namespaces. The " + Highlight ("using", "Keyword(Namespace)") + " statement defines a scope at the end of which an object will be disposed.";
				break;
			case "virtual":
				result.SignatureMarkup = Highlight ("virtual", "Keyword(Modifiers)") + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("virtual", "Keyword(Modifiers)") + " keyword is used to modify a method or property declaration, in which case the method or the property is called a virtual member.";
				break;
			case "volatile":
				result.SignatureMarkup = Highlight ("volatile", "Keyword(Modifiers)") + keywordSign;
				result.AddCategory ("Form", Highlight ("volatile", "Keyword(Modifiers)") + " declaration");
				result.SummaryMarkup = "The " + Highlight ("volatile", "Keyword(Modifiers)") + " keyword indicates that a field can be modified in the program by something such as the operating system, the hardware, or a concurrently executing thread.";
				break;
			case "void":
				result.SignatureMarkup = Highlight ("void", "Keyword(Type)") + keywordSign;
				break;
			case "where":
				result.SignatureMarkup = Highlight ("where", "Keyword(Context)") + keywordSign;
				//TODO
				break;
			case "yield":
				result.SignatureMarkup = Highlight ("yield", "Keyword(Context)") + keywordSign;
				//TODO
				break;
			case "while":
				result.SignatureMarkup = Highlight ("while", "Keyword(Iteration)") + keywordSign;
				result.AddCategory ("Form", Highlight ("while", "Keyword(Iteration)") + " (expression) statement");
				result.SummaryMarkup = "The " + Highlight ("while", "Keyword(Iteration)") + " statement executes a statement or a block of statements until a specified expression evaluates to false. ";
				break;
			}
			return result;
		}

		public TooltipInformation GetConstraintTooltip (string keyword)
		{
			var result = new TooltipInformation ();

			var color = AlphaBlend (colorStyle.PlainText.CairoColor, colorStyle.PlainText.CairoBackgroundColor, optionalAlpha);
			var colorString = Mono.TextEditor.HelperMethods.GetColorString (color);
			
			var keywordSign = "<span foreground=\"" + colorString + "\">" + " (keyword)</span>";

			result.SignatureMarkup = Highlight (keyword, "Keyword(Type)") + keywordSign;

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
			result.Append (Highlight ("event ", "Keyword(Modifiers)"));
			result.Append (GetTypeReferenceString (evt.ReturnType));
			if (BreakLineAfterReturnType) {
				result.AppendLine ();
			} else {
				result.Append (" ");
			}

			AppendExplicitInterfaces (result, evt);
			result.Append (HighlightSemantically (CSharpAmbience.FilterName (evt.Name), "User Event Declaration"));
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
				result.Append (Highlight ("out ", "Keyword(Parameter)"));
			} else if (parameter.IsRef) {
				result.Append (Highlight ("ref ", "Keyword(Parameter)"));
			} else if (parameter.IsParams) {
				result.Append (Highlight ("params ", "Keyword(Parameter)"));
			}
			result.Append (GetTypeReferenceString (parameter.Type));
			result.Append (" ");
			result.Append (parameter.Name);
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
				sb.Append (Highlight ("\"" + constantValue + "\"", "string.double"));
				return;
			}
			if (constantValue is char) {
				sb.Append (Highlight ("'" + constantValue + "'", "string.single"));
				return;
			}
			if (constantValue is bool) {
				sb.Append (Highlight ((bool)constantValue ? "true" : "false", "constant.language"));
				return;
			}

			if (constantValue == null) {
				if (constantType.Kind == TypeKind.Struct) {
					// structs can never be == null, therefore it's the default value.
					sb.Append (Highlight ("default", "Keyword(Selection)") + "(" + GetTypeReferenceString (constantType) + ")");
				} else {
					sb.Append (Highlight ("null", "constant.language"));
				}
				return;
			}

			while (NullableType.IsNullable (constantType)) 
				constantType = NullableType.GetUnderlyingType (constantType);
			if (constantType.Kind == TypeKind.Enum) {
				foreach (var field in constantType.GetFields ()) {
					if (field.ConstantValue == constantValue){
						sb.Append (GetTypeReferenceString (constantType) + "." + field.Name);
						return;
					}
				}
				sb.Append ("(" + GetTypeReferenceString (constantType) + ")" + Highlight (constantValue.ToString (), "constant.digit"));
				return;
			}

			sb.Append (Highlight (constantValue.ToString (), "constant.digit"));
		}

		void AppendVariance (StringBuilder sb, VarianceModifier variance)
		{
			if (variance  == VarianceModifier.Contravariant) {
				sb.Append (Highlight ("in ", "Keyword(Parameter)"));
			} else if (variance  == VarianceModifier.Covariant) {
				sb.Append (Highlight ("out ", "Keyword(Parameter)"));
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
			result.Append (Highlight ("this", "Keyword(Access)"));
			result.Append ("[");
			for (int i = 0; i < arrayType.Dimensions; i++) {
				if (i > 0)
					result.Append (", ");
				var doHighightParameter = i == HighlightParameter;
				if (doHighightParameter)
					result.Append ("<u>");

				result.Append (Highlight ("int ", "Keyword(Type)"));
				result.Append (arrayType.Dimensions == 1 ? "index" : "i" + (i + 1));
				if (doHighightParameter)
					result.Append ("</u>");
			}
			result.Append ("]");

			result.Append (" {");
			result.Append (Highlight (" get", "Keyword(Property)") + ";");
			result.Append (Highlight (" set", "Keyword(Property)") + ";");
			result.Append (" }");
			
			return result.ToString ();
		}


		string Highlight (string str, string colorScheme)
		{
			var style = colorStyle.GetChunkStyle (colorScheme);
			if (style != null) {
				var color = (Gdk.Color) ((HslColor)style.CairoColor);
				
				if (grayOut) {
					color = AlphaBlend (color, (Gdk.Color) ((HslColor)colorStyle.PlainText.CairoBackgroundColor), optionalAlpha);
				}
				
				var colorString = Mono.TextEditor.HelperMethods.GetColorString (color);
				return "<span foreground=\"" + colorString + "\">" + str + "</span>";
			}
			return str;
		}

		string HighlightSemantically (string str, string colorScheme)
		{
			if (!MonoDevelop.SourceEditor.DefaultSourceEditorOptions.Instance.EnableSemanticHighlighting)
				return str;
			return Highlight (str, colorScheme);
		}
	}
}
