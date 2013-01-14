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
				return Highlight (astType.GetText (formattingOptions), "keyword.type");
			}
			var text = AmbienceService.EscapeText (astType.GetText (formattingOptions));
			return highlight ? HighlightSemantically (text, "keyword.semantic.type") : text;
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
			result.Append (Highlight ("namespace ", "keyword.namespace"));
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
					result.Append (Highlight ("internal ", "keyword.modifier"));
				break;
			case Accessibility.ProtectedAndInternal:
				result.Append (Highlight ("protected internal ", "keyword.modifier"));
				break;
			case Accessibility.ProtectedOrInternal:
				result.Append (Highlight ("internal protected ", "keyword.modifier"));
				break;
			case Accessibility.Protected:
				result.Append (Highlight ("protected ", "keyword.modifier"));
				break;
			case Accessibility.Private:
// private is the default modifier - no need to show that
//				result.Append (Highlight (" private", "keyword.modifier"));
				break;
			case Accessibility.Public:
				result.Append (Highlight ("public ", "keyword.modifier"));
				break;
			}

			if (entity is IField && ((IField)entity).IsConst) {
				result.Append (Highlight ("const ", "keyword.modifier"));
			} else if (entity.IsStatic) {
				result.Append (Highlight ("static ", "keyword.modifier"));
			} else if (entity.IsSealed) {
				if (!(entity is IType && ((IType)entity).Kind == TypeKind.Delegate))
					result.Append (Highlight ("sealed ", "keyword.modifier"));
			} else if (entity.IsAbstract) {
				if (!(entity is IType && ((IType)entity).Kind == TypeKind.Interface))
					result.Append (Highlight ("abstract ", "keyword.modifier"));
			}


			if (entity.IsShadowing)
				result.Append (Highlight ("new ", "keyword.modifier"));

			var member = entity as IMember;
			if (member != null) {
				if (member.IsOverride) {
					result.Append (Highlight ("override ", "keyword.modifier"));
				} else if (member.IsVirtual) {
					result.Append (Highlight ("virtual ", "keyword.modifier"));
				}
			}
			var field = entity as IField;
			if (field != null) {
				if (field.IsVolatile)
					result.Append (Highlight ("volatile ", "keyword.modifier"));
				if (field.IsReadOnly)
					result.Append (Highlight ("readonly ", "keyword.modifier"));
			}

			var method = entity as IMethod;
			if (method != null) {
				if (method.IsAsync)
					result.Append (Highlight ("async ", "keyword.modifier"));
				if (method.IsPartial)
					result.Append (Highlight ("partial ", "keyword.modifier"));
			}
		}

		void AppendAccessibility (StringBuilder result, IMethod entity)
		{
			switch (entity.Accessibility) {
			case Accessibility.Internal:
				result.Append (Highlight ("internal", "keyword.modifier"));
				break;
			case Accessibility.ProtectedAndInternal:
				result.Append (Highlight ("protected internal", "keyword.modifier"));
				break;
			case Accessibility.ProtectedOrInternal:
				result.Append (Highlight ("internal protected", "keyword.modifier"));
				break;
			case Accessibility.Protected:
				result.Append (Highlight ("protected", "keyword.modifier"));
				break;
			case Accessibility.Private:
				result.Append (Highlight ("private", "keyword.modifier"));
				break;
			case Accessibility.Public:
				result.Append (Highlight ("public", "keyword.modifier"));
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
			var highlightedTypeName = Highlight (t.Name, "keyword.type");
			result.Append (highlightedTypeName);

			var color = AlphaBlend (colorStyle.Default.Color, colorStyle.Default.BackgroundColor, optionalAlpha);
			var colorString = Mono.TextEditor.HelperMethods.GetColorString (color);

			result.Append ("<span foreground=\"" + colorString + "\">" + " (type parameter)</span>");
			var tp = t as ITypeParameter;
			if (tp != null) {
				if (!tp.HasDefaultConstructorConstraint && !tp.HasReferenceTypeConstraint && !tp.HasValueTypeConstraint && tp.DirectBaseTypes.All (IsObjectOrValueType))
					return result.ToString ();
				result.AppendLine ();
				result.Append (Highlight (" where ", "keyword.context"));
				result.Append (highlightedTypeName);
				result.Append (" : ");
				int constraints = 0;

				if (tp.HasReferenceTypeConstraint) {
					constraints++;
					result.Append (Highlight ("class", "keyword.declaration"));
				} else if (tp.HasValueTypeConstraint) {
					constraints++;
					result.Append (Highlight ("struct", "keyword.declaration"));
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
					result.Append (Highlight ("new", "keyword.operator"));
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

		string GetTypeMarkup (IType t)
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
				result.Append (Highlight ("class ", "keyword.declaration"));
				break;
			case TypeKind.Interface:
				result.Append (Highlight ("interface ", "keyword.declaration"));
				break;
			case TypeKind.Struct:
				result.Append (Highlight ("struct ", "keyword.declaration"));
				break;
			case TypeKind.Enum:
				result.Append (Highlight ("enum ", "keyword.declaration"));
				break;
			}

			var typeName = t.Name;
			result.Append (Highlight (typeName.ToString (), "keyword.type"));
			if (t.TypeParameterCount > 0) {
				if (t is ParameterizedType) {
					AppendTypeParameters (result, ((ParameterizedType)t).TypeArguments);

				} else {
					AppendTypeParameters (result, t.GetDefinition ().TypeParameters);
				}
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

		void AppendTypeParameters (StringBuilder result, IList<ITypeParameter> typeParameters)
		{
			if (typeParameters.Count == 0)
				return;
			result.Append ("&lt;");
			for (int i = 0; i < typeParameters.Count; i++) {
				if (i > 0) {
					if (i % 5 == 0) {
						result.AppendLine (",");
						result.Append ("\t");
					}
					else {
						result.Append (", ");
					}
				}
				AppendVariance (result, typeParameters [i].Variance);
				result.Append (HighlightSemantically (CSharpAmbience.NetToCSharpTypeName (typeParameters [i].Name), "keyword.semantic.type"));
			}
			result.Append ("&gt;");
		}

		void AppendTypeParameters (StringBuilder result, IList<IType> typeParameters)
		{
			if (typeParameters.Count == 0)
				return;
			result.Append ("&lt;");
			for (int i = 0; i < typeParameters.Count; i++) {
				if (i > 0) {
					if (i % 5 == 0) {
						result.AppendLine (",");
						result.Append ("\t");
					}
					else {
						result.Append (", ");
					}
				}
				result.Append (GetTypeReferenceString (typeParameters[i], false));
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
			result.Append (Highlight ("delegate ", "keyword.declaration"));
			result.Append (GetTypeReferenceString (method.ReturnType));
			if (BreakLineAfterReturnType) {
				result.AppendLine ();
			} else {
				result.Append (" ");
			}
			
			
			result.Append (CSharpAmbience.FilterName (delegateType.Name));

			var pt = delegateType as ParameterizedType;
			if (pt != null && pt.TypeArguments.Count > 0) {
				result.Append ("&lt;");
				for (int i = 0; i < pt.TypeArguments.Count; i++) {
					if (i > 0)
						result.Append (", ");
					result.Append (HighlightSemantically (GetTypeReferenceString (pt.TypeArguments [i]), "keyword.semantic.type"));
				}
				result.Append ("&gt;");
			} else {
				var tt = delegateType as ITypeDefinition;

				if (tt != null && tt.TypeParameters.Count > 0) {
					AppendTypeParameters (result, tt.TypeParameters);
				}
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
				result.Append (Highlight ("const", "keyword.modifier"));
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

			result.Append (HighlightSemantically (CSharpAmbience.FilterName (field.Name), "keyword.semantic.field.declaration"));

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
				result.Append (HighlightSemantically (method.Name, "keyword.semantic.method.declaration"));
			}
			if (method is SpecializedMethod) {
				var sm = (SpecializedMethod)method;
				if (sm.TypeArguments.Count > 0) {
					result.Append ("&lt;");
					for (int i = 0; i < sm.TypeArguments.Count; i++) {
						if (i > 0)
							result.Append (", ");
						result.Append (HighlightSemantically (GetTypeReferenceString (sm.TypeArguments[i], false), "keyword.semantic.type"));
					}
					result.Append ("&gt;");
				}
			} else {
				AppendTypeParameters (result, method.TypeParameters);
			}

			if (formattingOptions.SpaceBeforeMethodDeclarationParentheses)
				result.Append (" ");

			result.Append ('(');
			IList<IParameter> parameters = method.Parameters;
			if (method.IsExtensionMethod) {
				parameters = new List<IParameter> (method.Parameters.Skip (1));
			}
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
				result.Append (Highlight ("this", "keyword.access"));
			} else {
				result.Append (HighlightSemantically (CSharpAmbience.FilterName (property.Name), "keyword.semantic.property.declaration"));
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
				result.Append (Highlight (" get", "keyword.property") + ";");
			}

			if (property.CanSet && IsAccessibleOrHasSourceCode(property.Setter)) {
				if (property.Setter.Accessibility != property.Accessibility) {
					result.Append (" ");
					AppendAccessibility (result, property.Setter);
				}
				result.Append (Highlight (" set", "keyword.property") + ";");
			}
			result.Append (" }");

			return result.ToString ();
		}

		
		public TooltipInformation GetExternAliasTooltip (ExternAliasDeclaration externAliasDeclaration, DotNetProject project)
		{
			var result = new TooltipInformation ();
			result.SignatureMarkup = Highlight ("extern ", "keyword.modifier") + Highlight ("alias ", "keyword.namespace") + externAliasDeclaration.Name;
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

			var color = AlphaBlend (colorStyle.Default.Color, colorStyle.Default.BackgroundColor, optionalAlpha);
			var colorString = Mono.TextEditor.HelperMethods.GetColorString (color);

			var keywordSign = "<span foreground=\"" + colorString + "\">" + " (keyword)</span>";

			switch (keyword){
			case "abstract":
				result.SignatureMarkup = Highlight ("abstract", "keyword.modifier") + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("abstract", "keyword.modifier") + " modifier can be used with classes, methods, properties, indexers, and events.";
				break;
			case "add":
				result.SignatureMarkup = Highlight ("add", "keyword.context") + keywordSign;
				//TODO
				break;
			case "ascending":
				result.SignatureMarkup = Highlight ("ascending", "keyword.context") + keywordSign;
				//TODO
				break;
			case "async":
				result.SignatureMarkup = Highlight ("async", "keyword.context") + keywordSign;
				//TODO
				break;
			case "as":
				result.SignatureMarkup = Highlight ("as", "keyword.operator") + keywordSign;
				result.AddCategory ("Form", "expression " + Highlight ("as", "keyword.operator") + " type");
				result.SummaryMarkup = "The " + Highlight ("as", "keyword.operator") + " operator is used to perform conversions between compatible types. ";
				break;
			case "await":
				result.SignatureMarkup = Highlight ("await", "keyword.context") + keywordSign;
				//TODO
				break;
			case "base":
				result.SignatureMarkup = Highlight ("base", "keyword.access") + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("base", "keyword.access") + " keyword is used to access members of the base class from within a derived class.";
				break;
			case "break":
				result.SignatureMarkup = Highlight ("break", "keyword.jump") + keywordSign;
				result.AddCategory ("Form", Highlight ("break", "keyword.jump") + ";");
				result.SummaryMarkup = "The " + Highlight ("break", "keyword.jump") + " statement terminates the closest enclosing loop or switch statement in which it appears.";
				break;
			case "case":
				result.SignatureMarkup = Highlight ("case", "keyword.selection") + keywordSign;
				result.AddCategory ("Form", Highlight ("case", "keyword.selection") + " constant-expression:" + Environment.NewLine +
				                    "  statement" + Environment.NewLine +
				                    "  jump-statement");
				result.SummaryMarkup = "";
				break;
			case "catch":
				result.SignatureMarkup = Highlight ("catch", "keyword.exceptions") + keywordSign;
				result.AddCategory ("Form", Highlight ("try", "keyword.exceptions") + " try-block" + Environment.NewLine +
				                    "  " + Highlight ("catch", "keyword.exceptions") + " (exception-declaration-1) catch-block-1" + Environment.NewLine +
				                    "  " + Highlight ("catch", "keyword.exceptions") + " (exception-declaration-2) catch-block-2" + Environment.NewLine +
				                    "  ..." + Environment.NewLine +
				                    Highlight ("try", "keyword.exceptions") + " try-block " + Highlight ("catch", "keyword.exceptions") + " catch-block");
				result.SummaryMarkup = "";
				break;
			case "checked":
				result.SignatureMarkup = Highlight ("checked", "keyword.misc") + keywordSign;
				result.AddCategory ("Form", Highlight ("checked", "keyword.misc") + " block" + Environment.NewLine +
				                    "or" + Environment.NewLine +
				                    Highlight ("checked", "keyword.misc") + " (expression)");
				result.SummaryMarkup = "The " + Highlight ("checked", "keyword.misc") + " keyword is used to control the overflow-checking context for integral-type arithmetic operations and conversions. It can be used as an operator or a statement.";
				break;
			case "class":
				result.SignatureMarkup = Highlight ("class", "keyword.declaration") + keywordSign;
				result.AddCategory ("Form", "[attributes] [modifiers] " + Highlight ("class", "keyword.declaration") + " identifier [:base-list] { class-body }[;]");
				result.SummaryMarkup = "Classes are declared using the keyword " + Highlight ("class", "keyword.declaration") + ".";
				break;
			case "const":
				result.SignatureMarkup = Highlight ("const", "keyword.modifier") + keywordSign;
				result.AddCategory ("Form", "[attributes] [modifiers] " + Highlight ("const", "keyword.modifier") + " type declarators;");
				result.SummaryMarkup = "The " + Highlight ("const", "keyword.modifier") + " keyword is used to modify a declaration of a field or local variable. It specifies that the value of the field or the local variable cannot be modified. ";
				break;
			case "continue":
				result.SignatureMarkup = Highlight ("continue", "keyword.jump") + keywordSign;
				result.AddCategory ("Form", Highlight ("continue", "keyword.jump") + ";");
				result.SummaryMarkup = "The " + Highlight ("continue", "keyword.jump") + " statement passes control to the next iteration of the enclosing iteration statement in which it appears.";
				break;
			case "default":
				result.SignatureMarkup = Highlight ("default", "keyword.selection") + keywordSign;
				result.SummaryMarkup = "";
				if (hintNode != null) {
					if (hintNode.Parent is DefaultValueExpression) {
						result.AddCategory ("Form",
						                    Highlight ("default", "keyword.selection") + " (Type)");
						break;
					} else if (hintNode.Parent is CaseLabel) {
						result.AddCategory ("Form",
						                    Highlight ("switch", "keyword.selection") + " (expression) { "+ Environment.NewLine +
						                    "  " + Highlight ("case", "keyword.selection") + " constant-expression:" + Environment.NewLine +
						                    "    statement"+ Environment.NewLine +
						                    "    jump-statement" + Environment.NewLine +
						                    "  [" + Highlight ("default", "keyword.selection") + ":" + Environment.NewLine +
						                    "    statement" + Environment.NewLine +
						                    "    jump-statement]" + Environment.NewLine +
						                    "}");
						break;
					}
				}
				result.AddCategory ("Form",
				                    Highlight ("default", "keyword.selection") + " (Type)" + Environment.NewLine + Environment.NewLine +
				                    "or" + Environment.NewLine + Environment.NewLine +
				                    Highlight ("switch", "keyword.selection") + " (expression) { "+ Environment.NewLine +
				                    "  " + Highlight ("case", "keyword.selection") + " constant-expression:" + Environment.NewLine +
				                    "    statement"+ Environment.NewLine +
				                    "    jump-statement" + Environment.NewLine +
				                    "  [" + Highlight ("default", "keyword.selection") + ":" + Environment.NewLine +
				                    "    statement" + Environment.NewLine +
				                    "    jump-statement]" + Environment.NewLine +
				                    "}");
				break;
			case "delegate":
				result.SignatureMarkup = Highlight ("delegate", "keyword.declaration") + keywordSign;
				result.AddCategory ("Form", "[attributes] [modifiers] " + Highlight ("delegate", "keyword.declaration") + " result-type identifier ([formal-parameters]);");
				result.SummaryMarkup = "A " + Highlight ("delegate", "keyword.declaration") + " declaration defines a reference type that can be used to encapsulate a method with a specific signature.";
				break;
			case "dynamic":
				result.SignatureMarkup = Highlight ("dynamic", "keyword.context") + keywordSign;
				//TODO
				break;
			case "descending":
				result.SignatureMarkup = Highlight ("descending", "keyword.context") + keywordSign;
				//TODO
				break;
			case "do":
				result.SignatureMarkup = Highlight ("do", "keyword.iteration") + keywordSign;
				result.AddCategory ("Form", Highlight ("do", "keyword.iteration") + " statement " + Highlight ("while", "keyword.iteration") + " (expression);");
				result.SummaryMarkup = "The " + Highlight ("do", "keyword.iteration") + " statement executes a statement or a block of statements repeatedly until a specified expression evaluates to false.";
				break;
			case "else":
				result.SignatureMarkup = Highlight ("else", "keyword.selection") + keywordSign;
				result.AddCategory ("Form", Highlight ("if", "keyword.selection") + " (expression)" + Environment.NewLine +
				                    "  statement1" + Environment.NewLine +
				                    "  [" + Highlight ("else", "keyword.selection") + Environment.NewLine +
				                    "  statement2]");
				result.SummaryMarkup = "";
				break;
			case "enum":
				result.SignatureMarkup = Highlight ("enum", "keyword.declaration") + keywordSign;
				result.AddCategory ("Form", "[attributes] [modifiers] " + Highlight ("enum", "keyword.declaration") + " identifier [:base-type] {enumerator-list} [;]");
				result.SummaryMarkup = "The " + Highlight ("enum", "keyword.declaration") + " keyword is used to declare an enumeration, a distinct type consisting of a set of named constants called the enumerator list.";
				break;
			case "event":
				result.SignatureMarkup = Highlight ("event", "keyword.modifier") + keywordSign;
				result.AddCategory ("Form", "[attributes] [modifiers] " + Highlight ("event", "keyword.modifier") + " type declarator;" + Environment.NewLine +
				                    "[attributes] [modifiers] " + Highlight ("event", "keyword.modifier") + " type member-name {accessor-declarations};");
				result.SummaryMarkup = "Specifies an event.";
				break;
			case "explicit":
				result.SignatureMarkup = Highlight ("explicit", "keyword.operator.declaration") + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("explicit", "keyword.operator.declaration") + " keyword is used to declare an explicit user-defined type conversion operator.";
				break;
			case "extern":
				result.SignatureMarkup = Highlight ("extern", "keyword.modifier") + keywordSign;
				result.SummaryMarkup = "Use the " + Highlight ("extern", "keyword.modifier") + " modifier in a method declaration to indicate that the method is implemented externally. A common use of the extern modifier is with the DllImport attribute.";
				break;
			case "finally":
				result.SignatureMarkup = Highlight ("finally", "keyword.exceptions") + keywordSign;
				result.AddCategory ("Form", Highlight ("try", "keyword.exceptions") + " try-block " + Highlight ("finally", "keyword.exceptions") + " finally-block");
				result.SummaryMarkup = "The " + Highlight ("finally", "keyword.exceptions") + " block is useful for cleaning up any resources allocated in the try block. Control is always passed to the finally block regardless of how the try block exits.";
				break;
			case "fixed":
				result.SignatureMarkup = Highlight ("fixed", "keyword.misc") + keywordSign;
				result.AddCategory ("Form", Highlight ("fixed", "keyword.misc") + " ( type* ptr = expr ) statement");
				result.SummaryMarkup = "Prevents relocation of a variable by the garbage collector.";
				break;
			case "for":
				result.SignatureMarkup = Highlight ("for", "keyword.iteration") + keywordSign;
				result.AddCategory ("Form", Highlight ("for", "keyword.iteration") + " ([initializers]; [expression]; [iterators]) statement");
				result.SummaryMarkup = "The " + Highlight ("for", "keyword.iteration") + " loop executes a statement or a block of statements repeatedly until a specified expression evaluates to false.";
				break;
			case "foreach":
				result.SignatureMarkup = Highlight ("foreach", "keyword.iteration") + keywordSign;
				result.AddCategory ("Form", Highlight ("foreach", "keyword.iteration") + " (type identifier " + Highlight ("in", "keyword.iteration") + " expression) statement");
				result.SummaryMarkup = "The " + Highlight ("foreach", "keyword.iteration") + " statement repeats a group of embedded statements for each element in an array or an object collection. ";
				break;
			case "from":
				result.SignatureMarkup = Highlight ("from", "keyword.context") + keywordSign;
				//TODO
				break;
			case "get":
				result.SignatureMarkup = Highlight ("get", "keyword.context") + keywordSign;
				//TODO
				break;
			case "global":
				result.SignatureMarkup = Highlight ("global", "keyword.context") + keywordSign;
				//TODO
				break;
			case "goto":
				result.SignatureMarkup = Highlight ("goto", "keyword.jump") + keywordSign;
				result.AddCategory ("Form", Highlight ("goto", "keyword.jump") + " identifier;" + Environment.NewLine +
					Highlight ("goto", "keyword.jump") + " " + Highlight ("case", "keyword.selection") + " constant-expression;" + Environment.NewLine +
				                    Highlight ("goto", "keyword.jump") + " " + Highlight ("default", "keyword.selection") + ";");
				result.SummaryMarkup = "The " + Highlight ("goto", "keyword.jump") + " statement transfers the program control directly to a labeled statement. ";
				break;
			case "group":
				result.SignatureMarkup = Highlight ("group", "keyword.context") + keywordSign;
				//TODO
				break;
			case "if":
				result.SignatureMarkup = Highlight ("if", "keyword.selection") + keywordSign;
				result.AddCategory ("Form", Highlight ("if", "keyword.selection") + " (expression)" + Environment.NewLine +
					"  statement1" + Environment.NewLine +
					"  [" + Highlight ("else", "keyword.selection") + Environment.NewLine +
				                    "  statement2]");
				result.SummaryMarkup = "The " + Highlight ("if", "keyword.selection") + " statement selects a statement for execution based on the value of a Boolean expression. ";
				break;
			case "into":
				result.SignatureMarkup = Highlight ("into", "keyword.context") + keywordSign;
				//TODO
				break;
			case "implicit":
				result.SignatureMarkup = Highlight ("implicit", "keyword.operator.declaration") + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("implicit", "keyword.operator.declaration") + " keyword is used to declare an implicit user-defined type conversion operator.";
				break;
			case "in":
				result.SignatureMarkup = Highlight ("in", "keyword.iteration") + keywordSign;
				if (hintNode != null) {
					if (hintNode.Parent is ForeachStatement) {
						result.AddCategory ("Form",
						                    Highlight ("foreach", "keyword.iteration") + " (type identifier " + Highlight ("in", "keyword.iteration") + " expression) statement");
						break;
					}
					if (hintNode.Parent is QueryFromClause) {
						result.AddCategory ("Form",
						                    Highlight ("from", "keyword.context") + " range-variable " + Highlight ("in", "keyword.iteration") + " data-source [query clauses] " + Highlight ("select", "keyword.context") + " product-expression");
						break;
					}
					if (hintNode.Parent is TypeParameterDeclaration) {
						result.AddCategory ("Form",
						                    Highlight ("interface", "keyword.declaration") + " IMyInterface&lt;" + Highlight ("in", "keyword.iteration") + " T&gt; {}");
						break;
					}
				}
				result.AddCategory ("Form", Highlight ("foreach", "keyword.iteration") + " (type identifier " + Highlight ("in", "keyword.iteration") + " expression) statement"+ Environment.NewLine + Environment.NewLine +
				                    "or" + Environment.NewLine + Environment.NewLine +
				                    Highlight ("from", "keyword.context") + " range-variable " + Highlight ("in", "keyword.iteration") + " data-source [query clauses] " + Highlight ("select", "keyword.context") + " product-expression" + Environment.NewLine + Environment.NewLine +
				                    "or" + Environment.NewLine + Environment.NewLine +
				                    Highlight ("interface", "keyword.declaration") + " IMyInterface&lt;" + Highlight ("in", "keyword.iteration") + " T&gt; {}"
				                    );
				break;
			case "interface":
				result.SignatureMarkup = Highlight ("interface", "keyword.declaration") + keywordSign;
				result.AddCategory ("Form", "[attributes] [modifiers] " + Highlight ("interface", "keyword.declaration") + " identifier [:base-list] {interface-body}[;]");
				result.SummaryMarkup = "An interface defines a contract. A class or struct that implements an interface must adhere to its contract.";
				break;
			case "internal":
				result.SignatureMarkup = Highlight ("internal", "keyword.modifier") + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("internal", "keyword.modifier") + " keyword is an access modifier for types and type members. Internal members are accessible only within files in the same assembly.";
				break;
			case "is":
				result.SignatureMarkup = Highlight ("is", "keyword.operator") + keywordSign;
				result.AddCategory ("Form", "expression " + Highlight ("is", "keyword.operator") + " type");
				result.SummaryMarkup = "The " + Highlight ("is", "keyword.operator") + " operator is used to check whether the run-time type of an object is compatible with a given type.";
				break;
			case "join":
				result.SignatureMarkup = Highlight ("join", "keyword.context") + keywordSign;
				//TODO
				break;
			case "let":
				result.SignatureMarkup = Highlight ("let", "keyword.context") + keywordSign;
				//TODO
				break;
			case "lock":
				result.SignatureMarkup = Highlight ("lock", "keyword.misc") + keywordSign;
				result.AddCategory ("Form", Highlight ("lock", "keyword.misc") + " (expression) statement_block");
				result.SummaryMarkup = "The " + Highlight ("lock", "keyword.misc") + " keyword marks a statement block as a critical section by obtaining the mutual-exclusion lock for a given object, executing a statement, and then releasing the lock. ";
				break;
			case "namespace":
				result.SignatureMarkup = Highlight ("namespace", "keyword.namespace") + keywordSign;
				result.AddCategory ("Form", Highlight ("namespace", "keyword.namespace") + " name[.name1] ...] {" + Environment.NewLine +
					"type-declarations" + Environment.NewLine +
				                    " }");
				result.SummaryMarkup = "The " + Highlight ("namespace", "keyword.namespace") + " keyword is used to declare a scope. ";
				break;
			case "new":
				result.SignatureMarkup = Highlight ("new", "keyword.operator") + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("new", "keyword.operator") + " keyword can be used as an operator or as a modifier. The operator is used to create objects on the heap and invoke constructors. The modifier is used to hide an inherited member from a base class member.";
				break;
			case "null":
				result.SignatureMarkup = Highlight ("null", "constant.language") + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("null", "constant.language") + " keyword is a literal that represents a null reference, one that does not refer to any object. " + Highlight ("null", "constant.language") + " is the default value of reference-type variables.";
				break;
			case "operator":
				result.SignatureMarkup = Highlight ("operator", "keyword.operator.declaration") + keywordSign;
				result.AddCategory ("Form", Highlight ("public static ", "keyword.modifier") + "result-type " + Highlight ("operator", "keyword.operator.declaration") + " unary-operator ( op-type operand )" + Environment.NewLine +
				                    Highlight ("public static ", "keyword.modifier") + "result-type " + Highlight ("operator", "keyword.operator.declaration") + " binary-operator (" + Environment.NewLine +
					"op-type operand," + Environment.NewLine +
					"op-type2 operand2" + Environment.NewLine +
					" )" + Environment.NewLine +
					Highlight ("public static ", "keyword.modifier") + Highlight ("implicit operator", "keyword.operator.declaration") + " conv-type-out ( conv-type-in operand )" + Environment.NewLine +
				                    Highlight ("public static ", "keyword.modifier") + Highlight ("explicit operator", "keyword.operator.declaration") + " conv-type-out ( conv-type-in operand )");
				result.SummaryMarkup = "The " + Highlight ("operator", "keyword.operator.declaration") + " keyword is used to declare an operator in a class or struct declaration.";
				break;
			case "orderby":
				result.SignatureMarkup = Highlight ("orderby", "keyword.context") + keywordSign;
				//TODO
				break;
			case "out":
				result.SignatureMarkup = Highlight ("out", "keyword.parameter") + keywordSign;
				if (hintNode != null) {
					if (hintNode.Parent is TypeParameterDeclaration) {
						result.AddCategory ("Form",
						                    Highlight ("interface", "keyword.declaration") + " IMyInterface&lt;" + Highlight ("out", "keyword.parameter") + " T&gt; {}");
						break;
					}
					if (hintNode.Parent is ParameterDeclaration) {
						result.AddCategory ("Form",
						                    Highlight ("out", "keyword.parameter") + " parameter-name");
						result.SummaryMarkup = "The " + Highlight ("out", "keyword.parameter") + " method parameter keyword on a method parameter causes a method to refer to the same variable that was passed into the method.";
						break;
					}
				}

				result.AddCategory ("Form", 
				                    Highlight ("out", "keyword.parameter") + " parameter-name" + Environment.NewLine + Environment.NewLine +
				                    "or" + Environment.NewLine + Environment.NewLine +
				                    Highlight ("interface", "keyword.declaration") + " IMyInterface&lt;" + Highlight ("out", "keyword.parameter") + " T&gt; {}"
				                    );
				break;
			case "override":
				result.SignatureMarkup = Highlight ("override", "keyword.modifier") + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("override", "keyword.modifier") + " modifier is used to override a method, a property, an indexer, or an event.";
				break;
			case "params":
				result.SignatureMarkup = Highlight ("params", "keyword.parameter") + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("params", "keyword.parameter") + " keyword lets you specify a method parameter that takes an argument where the number of arguments is variable.";
				break;
			case "partial":
				result.SignatureMarkup = Highlight ("partial", "keyword.context") + keywordSign;
				//TODO
				break;
			case "private":
				result.SignatureMarkup = Highlight ("private", "keyword.modifier") + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("private", "keyword.modifier") + " keyword is a member access modifier. Private access is the least permissive access level. Private members are accessible only within the body of the class or the struct in which they are declared.";
				break;
			case "protected":
				result.SignatureMarkup = Highlight ("protected", "keyword.modifier") + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("protected", "keyword.modifier") + " keyword is a member access modifier. A protected member is accessible from within the class in which it is declared, and from within any class derived from the class that declared this member.";
				break;
			case "public":
				result.SignatureMarkup = Highlight ("public", "keyword.modifier") + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("public", "keyword.modifier") + " keyword is an access modifier for types and type members. Public access is the most permissive access level. There are no restrictions on accessing public members.";
				break;
			case "readonly":
				result.SignatureMarkup = Highlight ("readonly", "keyword.modifier") + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("readonly", "keyword.modifier") + " keyword is a modifier that you can use on fields. When a field declaration includes a " + Highlight ("readonly", "keyword.modifier") + " modifier, assignments to the fields introduced by the declaration can only occur as part of the declaration or in a constructor in the same class.";
				break;
			case "ref":
				result.SignatureMarkup = Highlight ("ref", "keyword.parameter") + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("ref", "keyword.parameter") + " method parameter keyword on a method parameter causes a method to refer to the same variable that was passed into the method.";
				break;
			case "remove":
				result.SignatureMarkup = Highlight ("remove", "keyword.context") + keywordSign;
				//TODO
				break;
			case "return":
				result.SignatureMarkup = Highlight ("return", "keyword.jump") + keywordSign;
				result.AddCategory ("Form", Highlight ("return", "keyword.jump") + " [expression];");
				result.SummaryMarkup = "The " + Highlight ("return", "keyword.jump") + " statement terminates execution of the method in which it appears and returns control to the calling method.";
				break;
			case "select":
				result.SignatureMarkup = Highlight ("select", "keyword.context") + keywordSign;
				//TODO
				break;
			case "sealed":
				result.SignatureMarkup = Highlight ("sealed", "keyword.modifier") + keywordSign;
				result.SummaryMarkup = "A sealed class cannot be inherited.";
				break;
			case "set":
				result.SignatureMarkup = Highlight ("set", "keyword.context") + keywordSign;
				//TODO
				break;
			case "sizeof":
				result.SignatureMarkup = Highlight ("sizeof", "keyword.operator") + keywordSign;
				result.AddCategory ("Form", Highlight ("sizeof", "keyword.operator") + " (type)");
				result.SummaryMarkup = "The " + Highlight ("sizeof", "keyword.operator") + " operator is used to obtain the size in bytes for a value type.";
				break;
			case "stackalloc":
				result.SignatureMarkup = Highlight ("stackalloc", "keyword.operator") + keywordSign;
				result.AddCategory ("Form", "type * ptr = " + Highlight ("stackalloc", "keyword.operator") + " type [ expr ];");
				result.SummaryMarkup = "Allocates a block of memory on the stack.";
				break;
			case "static":
				result.SignatureMarkup = Highlight ("static", "keyword.modifier") + keywordSign;
				result.SummaryMarkup = "Use the " + Highlight ("static", "keyword.modifier") + "modifier to declare a static member, which belongs to the type itself rather than to a specific object.";
				break;
			case "struct":
				result.SignatureMarkup = Highlight ("struct", "keyword.declaration") + keywordSign;
				result.AddCategory ("Form", "[attributes] [modifiers] " + Highlight ("struct", "keyword.declaration") + " identifier [:interfaces] body [;]");
				result.SummaryMarkup = "A " + Highlight ("struct", "keyword.declaration") + " type is a value type that can contain constructors, constants, fields, methods, properties, indexers, operators, events, and nested types. ";
				break;
			case "switch":
				result.SignatureMarkup = Highlight ("switch", "keyword.selection") + keywordSign;
				result.AddCategory ("Form", Highlight ("switch", "keyword.selection") + " (expression)" + Environment.NewLine + 
					" {" + Environment.NewLine + 
				                    "  " + Highlight ("case", "keyword.selection") + " constant-expression:" + Environment.NewLine + 
					"  statement" + Environment.NewLine + 
					"  jump-statement" + Environment.NewLine + 
					"  [" + Highlight ("default", "keyword.selection") + ":" + Environment.NewLine + 
					"  statement" + Environment.NewLine + 
					"  jump-statement]" + Environment.NewLine + 
				                    " }");
				result.SummaryMarkup = "The " + Highlight ("switch", "keyword.selection") + " statement is a control statement that handles multiple selections by passing control to one of the " + Highlight ("case", "keyword.selection") + " statements within its body.";
				break;
			case "this":
				result.SignatureMarkup = Highlight ("this", "keyword.access") + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("this", "keyword.access") + " keyword refers to the current instance of the class.";
				break;
			case "throw":
				result.SignatureMarkup = Highlight ("throw", "keyword.exceptions") + keywordSign;
				result.AddCategory ("Form", Highlight ("throw", "keyword.exceptions") + " [expression];");
				result.SummaryMarkup = "The " + Highlight ("throw", "keyword.exceptions") + " statement is used to signal the occurrence of an anomalous situation (exception) during the program execution.";
				break;
			case "try":
				result.SignatureMarkup = Highlight ("try", "keyword.exceptions") + keywordSign;
				result.AddCategory ("Form", Highlight ("try", "keyword.exceptions") + " try-block" + Environment.NewLine + 
				                    "  " + Highlight ("catch", "keyword.exceptions") + " (exception-declaration-1) catch-block-1 " + Environment.NewLine + 
					"  " + Highlight ("catch", "keyword.exceptions") + " (exception-declaration-2) catch-block-2 " + Environment.NewLine + 
					"..." + Environment.NewLine + 
				                    Highlight ("try", "keyword.exceptions") + " try-block " + Highlight ("catch", "keyword.exceptions") + " catch-block");
				result.SummaryMarkup = "The try-catch statement consists of a " + Highlight ("try", "keyword.exceptions") + " block followed by one or more " + Highlight ("catch", "keyword.exceptions") + " clauses, which specify handlers for different exceptions.";
				break;
			case "typeof":
				result.SignatureMarkup = Highlight ("typeof", "keyword.operator") + keywordSign;
				result.AddCategory ("Form", Highlight ("typeof", "keyword.operator") + "(type)");
				result.SummaryMarkup = "The " + Highlight ("typeof", "keyword.operator") + " operator is used to obtain the System.Type object for a type.";
				break;
			case "unchecked":
				result.SignatureMarkup = Highlight ("unchecked", "keyword.misc") + keywordSign;
				result.AddCategory ("Form", Highlight ("unchecked", "keyword.misc") + " block" + Environment.NewLine +
				                    Highlight ("unchecked", "keyword.misc") + " (expression)");
				result.SummaryMarkup = "The "+ Highlight ("unchecked", "keyword.misc") + " keyword is used to control the overflow-checking context for integral-type arithmetic operations and conversions.";
				break;
			case "unsafe":
				result.SignatureMarkup = Highlight ("unsafe", "keyword.misc") + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("unsafe", "keyword.misc") + " keyword denotes an unsafe context, which is required for any operation involving pointers.";
				break;
			case "using":
				result.SignatureMarkup = Highlight ("using", "keyword.namespace") + keywordSign;
				result.AddCategory ("Form", Highlight ("using", "keyword.namespace") + " (expression | type identifier = initializer) statement" + Environment.NewLine +
				                    Highlight ("using", "keyword.namespace") + " [alias = ]class_or_namespace;");
				result.SummaryMarkup = "The " + Highlight ("using", "keyword.namespace") + " directive creates an alias for a namespace or imports types defined in other namespaces. The " + Highlight ("using", "keyword.namespace") + " statement defines a scope at the end of which an object will be disposed.";
				break;
			case "virtual":
				result.SignatureMarkup = Highlight ("virtual", "keyword.modifier") + keywordSign;
				result.SummaryMarkup = "The " + Highlight ("virtual", "keyword.modifier") + " keyword is used to modify a method or property declaration, in which case the method or the property is called a virtual member.";
				break;
			case "volatile":
				result.SignatureMarkup = Highlight ("volatile", "keyword.modifier") + keywordSign;
				result.AddCategory ("Form", Highlight ("volatile", "keyword.modifier") + " declaration");
				result.SummaryMarkup = "The " + Highlight ("volatile", "keyword.modifier") + " keyword indicates that a field can be modified in the program by something such as the operating system, the hardware, or a concurrently executing thread.";
				break;
			case "void":
				result.SignatureMarkup = Highlight ("void", "keyword.type") + keywordSign;
				break;
			case "where":
				result.SignatureMarkup = Highlight ("where", "keyword.context") + keywordSign;
				//TODO
				break;
			case "yield":
				result.SignatureMarkup = Highlight ("yield", "keyword.context") + keywordSign;
				//TODO
				break;
			case "while":
				result.SignatureMarkup = Highlight ("while", "keyword.iteration") + keywordSign;
				result.AddCategory ("Form", Highlight ("while", "keyword.iteration") + " (expression) statement");
				result.SummaryMarkup = "The " + Highlight ("while", "keyword.iteration") + " statement executes a statement or a block of statements until a specified expression evaluates to false. ";
				break;
			}
			return result;
		}

		string GetEventMarkup (IEvent evt)
		{
			if (evt == null)
				throw new ArgumentNullException ("evt");
			var result = new StringBuilder ();
			AppendModifiers (result, evt);
			result.Append (Highlight ("event ", "keyword.modifier"));
			result.Append (GetTypeReferenceString (evt.ReturnType));
			if (BreakLineAfterReturnType) {
				result.AppendLine ();
			} else {
				result.Append (" ");
			}

			AppendExplicitInterfaces (result, evt);
			result.Append (HighlightSemantically (CSharpAmbience.FilterName (evt.Name), "keyword.semantic.event.declaration"));
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
				result.Append (Highlight ("out ", "keyword.parameter"));
			} else if (parameter.IsRef) {
				result.Append (Highlight ("ref ", "keyword.parameter"));
			} else if (parameter.IsParams) {
				result.Append (Highlight ("params ", "keyword.parameter"));
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
					sb.Append (Highlight ("default", "keyword.selection") + "(" + GetTypeReferenceString (constantType) + ")");
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
				sb.Append (Highlight ("in ", "keyword.parameter"));
			} else if (variance  == VarianceModifier.Covariant) {
				sb.Append (Highlight ("out ", "keyword.parameter"));
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
			result.Append (Highlight ("this", "keyword.access"));
			result.Append ("[");
			for (int i = 0; i < arrayType.Dimensions; i++) {
				if (i > 0)
					result.Append (", ");
				var doHighightParameter = i == HighlightParameter;
				if (doHighightParameter)
					result.Append ("<u>");

				result.Append (Highlight ("int ", "keyword.type"));
				result.Append (arrayType.Dimensions == 1 ? "index" : "i" + (i + 1));
				if (doHighightParameter)
					result.Append ("</u>");
			}
			result.Append ("]");

			result.Append (" {");
			result.Append (Highlight (" get", "keyword.property") + ";");
			result.Append (Highlight (" set", "keyword.property") + ";");
			result.Append (" }");
			
			return result.ToString ();
		}


		string Highlight (string str, string colorScheme)
		{
			var style = colorStyle.GetChunkStyle (colorScheme);
			if (style != null) {
				var color = style.Color;
				
				if (grayOut) {
					color = AlphaBlend (color, colorStyle.Default.BackgroundColor, optionalAlpha);
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
