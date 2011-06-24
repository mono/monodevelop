// CSharpAmbience.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.CodeDom;
using MonoDevelop.CSharp.Formatting;
using MonoDevelop.Ide;
using System.Collections.ObjectModel;
using MonoDevelop.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using System.IO;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp;

namespace MonoDevelop.CSharp
{
	public class CSharpAmbience : Ambience
	{
		static Dictionary<string, string> netToCSharpTypes = new Dictionary<string, string> ();
		static HashSet<string> keywords = new HashSet<string> (new [] {
			"abstract",
			"as",
			"base",
			"bool",
			"break",
			"byte",
			"case",
			"catch",
			"char",
			"checked",
			"class",
			"const",
			"continue",
			"decimal",
			"default",
			"delegate",
			"do",
			"double",
			"else",
			"enum",
			"event",
			"explicit",
			"extern",
			"false",
			"finally",
			"fixed",
			"float",
			"for",
			"foreach",
			"goto",
			"if",
			"implicit",
			"in",
			"int",
			"interface",
			"internal",
			"is",
			"lock",
			"long",
			"namespace",
			"new",
			"null",
			"object",
			"operator",
			"out",
			"override",
			"params",
			"private",
			"protected",
			"public",
			"readonly",
			"ref",
			"return",
			"sbyte",
			"sealed",
			"short",
			"sizeof",
			"stackalloc",
			"static",
			"string",
			"struct",
			"switch",
			"this",
			"throw",
			"true",
			"try",
			"typeof",
			"uint",
			"ulong",
			"unchecked",
			"unsafe",
			"ushort",
			"using",
			"virtual",
			"void",
			"volatile",
			"while",
			"partial",
			
			"where",
			"get",
			"set",
			"add",
			"remove",
			"yield",
			"select",
			"group",
			"by",
			"into",
			"from",
			"ascending",
			"descending",
			"orderby",
			"let",
			"join",
			"on",
			"equals"
		});
		
		static CSharpAmbience ()
		{
			netToCSharpTypes ["System.Void"] = "void";
			netToCSharpTypes ["System.Object"] = "object";
			netToCSharpTypes ["System.Boolean"] = "bool";
			netToCSharpTypes ["System.Byte"] = "byte";
			netToCSharpTypes ["System.SByte"] = "sbyte";
			netToCSharpTypes ["System.Char"] = "char";
			netToCSharpTypes ["System.Enum"] = "enum";
			netToCSharpTypes ["System.Int16"] = "short";
			netToCSharpTypes ["System.Int32"] = "int";
			netToCSharpTypes ["System.Int64"] = "long";
			netToCSharpTypes ["System.UInt16"] = "ushort";
			netToCSharpTypes ["System.UInt32"] = "uint";
			netToCSharpTypes ["System.UInt64"] = "ulong";
			netToCSharpTypes ["System.Single"] = "float";
			netToCSharpTypes ["System.Double"] = "double";
			netToCSharpTypes ["System.Decimal"] = "decimal";
			netToCSharpTypes ["System.String"] = "string";
			
			classTypes [ClassType.Class] = "class";
			classTypes [ClassType.Enum] = "enum";
			classTypes [ClassType.Interface] = "interface";
			classTypes [ClassType.Struct] = "struct";
			classTypes [ClassType.Delegate] = "delegate";
		}
		
		public CSharpAmbience () : base ("C#")
		{
		}
		
		static Dictionary<ClassType, string> classTypes = new Dictionary<ClassType, string> ();
		
		static string GetString (ClassType classType)
		{
			string res;
			if (classTypes.TryGetValue (classType, out res))
				return res;
			return string.Empty;
		}
		
		internal static string FilterName (string name)
		{
			if (keywords.Contains (name))
				return "@" + name;
			return name;
		}
		
		public static string NetToCSharpTypeName (string netTypeName)
		{
			if (netToCSharpTypes.ContainsKey (netTypeName)) 
				return netToCSharpTypes [netTypeName];
			return netTypeName;
		}
		
		#region implemented abstract members of MonoDevelop.TypeSystem.Ambience
		public override string GetIntrinsicTypeName (string reflectionName)
		{
			return NetToCSharpTypeName (reflectionName);
		}

		public override string SingleLineComment (string text)
		{
			return "// " + text;
		}

		public override string GetString (string nameSpace, OutputSettings settings)
		{
			var result = new StringBuilder ();
			if (settings.IncludeKeywords)
				result.Append (settings.EmitKeyword ("namespace"));
			result.Append (Format (nameSpace));
			return result.ToString ();
		}
		
		void AppendComposedType (StringBuilder sb, ComposedType compType)
		{
			AppendAstType (sb, compType.BaseType);
			if (compType.HasNullableSpecifier)
				sb.Append ("?");
			if (compType.PointerRank > 0)
				sb.Append (new string ('*', compType.PointerRank));
			foreach (ArraySpecifier spec in compType.ArraySpecifiers) {
				sb.Append ("[");
				if (spec.Dimensions > 1)
					sb.Append (new string (',', spec.Dimensions - 1));
				sb.Append ("]");
			}
		}

		public void AppendAstType (StringBuilder sb, AstType astType)
		{
			if (astType is ComposedType) {
				AppendComposedType (sb, (ComposedType)astType);
			} else if (astType is PrimitiveType) {
				sb.Append (((PrimitiveType)astType).Keyword);
			} else if (astType is SimpleType) {
				sb.Append (((SimpleType)astType).Identifier);
			} else if (astType is MemberType) {
				var mt = (MemberType)astType;
				sb.Append (mt.MemberName);
			} 
		}
		
		protected override string GetTypeReferenceString (ITypeReference reference, OutputSettings settings)
		{
			if (reference == null)
				return "";
			var csResolver = new CSharpResolver (settings.Context, System.Threading.CancellationToken.None);
			var builder = new TypeSystemAstBuilder (csResolver);
			var astType = builder.ConvertType (reference.Resolve (settings.Context));
			
			
			var sb = new StringBuilder ();
			AppendAstType (sb, astType);
			
			return sb.ToString ();
/*		if (returnType.IsNullable && returnType.GenericArguments.Count == 1)
				return Visit (returnType.GenericArguments [0], settings) + "?";
			if (returnType.Type is AnonymousType)
				return returnType.Type.AcceptVisitor (this, settings);
			StringBuilder result = new StringBuilder ();
			if (!settings.UseNETTypeNames && netToCSharpTypes.ContainsKey (returnType.FullName)) {
				result.Append (settings.EmitName (returnType, netToCSharpTypes [returnType.FullName]));
			} else {
				if (settings.UseFullName && returnType.Namespace != null) 
					result.Append (settings.EmitName (returnType, Format (NormalizeTypeName (returnType.Namespace))));
				
				foreach (ReturnTypePart part in returnType.Parts) {
					if (part.IsGenerated)
						continue;
					if (!settings.UseFullName && part != returnType.Parts.LastOrDefault ())
						continue;
					if (result.Length > 0)
						result.Append (settings.EmitName (returnType, "."));
					result.Append (settings.EmitName (returnType, Format (NormalizeTypeName (part.Name))));
					if (settings.IncludeGenerics && part.GenericArguments.Count > 0) {
						result.Append (settings.Markup ("<"));
						bool hideArrays = settings.HideArrayBrackets;
						settings.OutputFlags &= ~OutputFlags.HideArrayBrackets;
						for (int i = 0; i < part.GenericArguments.Count; i++) {
							if (i > 0)
								result.Append (settings.Markup (settings.HideGenericParameterNames ? "," : ", "));
							if (!settings.HideGenericParameterNames) 
								result.Append (GetString (part.GenericArguments [i], settings));
						}
						if (hideArrays)
							settings.OutputFlags |= OutputFlags.HideArrayBrackets;
						result.Append (settings.Markup (">"));
					}
				}
			}
			
			if (!settings.HideArrayBrackets && returnType.ArrayDimensions > 0) {
				for (int i = 0; i < returnType.ArrayDimensions; i++) {
					result.Append (settings.Markup ("["));
					int dimension = returnType.GetDimension (i);
					if (dimension > 0)
						result.Append (settings.Markup (new string (',', dimension)));
					result.Append (settings.Markup ("]"));
				}
			}
			return result.ToString ();*/
		}

		protected override string GetTypeString (ITypeDefinition type, OutputSettings settings)
		{
			if (type == null)
				return "";
			var result = new StringBuilder ();
//			if (type is AnonymousType) {
//				result.Append ("new {");
//				foreach (IProperty property in type.Properties) {
//					result.AppendLine ();
//					result.Append ("\t");
//					if (property.ReturnType != null && !string.IsNullOrEmpty (property.ReturnType.FullName)) {
//						result.Append (property.ReturnType.AcceptVisitor (this, settings));
//						result.Append (" ");
//					} else {
//						result.Append ("? ");
//					}
//					result.Append (property.Name);
//					result.Append (";");
//				}
//				result.AppendLine ();
//				result.Append ("}");
//				return result.ToString ();
//			}
			var def = type;
			AppendModifiers (result, settings, def);
			if (settings.IncludeKeywords)
				result.Append (classTypes [def.ClassType]);
			if (result.Length > 0 && !result.ToString ().EndsWith (" "))
				result.Append (settings.Markup (" "));
			
			
			if (type.IsDelegate () && settings.ReformatDelegates && settings.IncludeReturnType) {
				var invoke = type.GetDelegateInvokeMethod ();
				result.Append (GetTypeReferenceString (invoke.ReturnType, settings));
				result.Append (settings.Markup (" "));
			}
			
			
			if (settings.UseFullName && type.DeclaringType != null) {
				bool includeGenerics = settings.IncludeGenerics;
				settings.OutputFlags |= OutputFlags.IncludeGenerics;
				string typeString = GetTypeReferenceString (type.DeclaringType, settings);
				if (!includeGenerics)
					settings.OutputFlags &= ~OutputFlags.IncludeGenerics;
				result.Append (typeString);
				result.Append (settings.Markup ("."));
			}
			
			result.Append (settings.EmitName (type, type.Name));
			if (settings.IncludeGenerics && type.TypeParameterCount > 0) {
				result.Append (settings.Markup ("<"));
				for (int i = 0; i < type.TypeParameterCount; i++) {
					if (i > 0)
						result.Append (settings.Markup (settings.HideGenericParameterNames ? "," : ", "));
					if (!settings.HideGenericParameterNames) {
						result.Append (NetToCSharpTypeName (type.TypeParameters [i].FullName));
					}
				}
				result.Append (settings.Markup (">"));
			}
			
			if (type.IsDelegate () && settings.ReformatDelegates) {
//				var policy = GetPolicy (settings);
//				if (policy.BeforeMethodCallParentheses)
//					result.Append (settings.Markup (" "));
				result.Append (settings.Markup ("("));
				var invoke = type.GetDelegateInvokeMethod ();
				if (invoke != null) 
					AppendParameterList (result, settings, invoke.Parameters);
				result.Append (settings.Markup (")"));
				return result.ToString ();
			}
			
			if (settings.IncludeBaseTypes && type.BaseTypes.Any ()) {
				bool first = true;
				foreach (var baseType in type.BaseTypes) {
//				if (baseType.FullName == "System.Object" || baseType.FullName == "System.Enum")
//					continue;
					result.Append (settings.Markup (first ? " : " : ", "));
					first = false;
					result.Append (GetTypeReferenceString (baseType, settings));	
				}
				
			}
//		OutputConstraints (result, settings, type.TypeParameters);
			return result.ToString ();
		}
		
		string InternalGetMethodString (IMethod method, OutputSettings settings, string methodName, bool getReturnType)
		{
			if (method == null)
				return "";
			var result = new StringBuilder ();
			AppendModifiers (result, settings, method);
			if (!settings.CompletionListFomat && settings.IncludeReturnType && getReturnType) {
				result.Append (GetTypeReferenceString (method.ReturnType, settings));
				result.Append (settings.Markup (" "));
			}
			
			
			if (!settings.IncludeReturnType && settings.UseFullName) {
				result.Append (GetTypeReferenceString (method.DeclaringType, new OutputSettings (OutputFlags.UseFullName) { Context = settings.Context}));
				result.Append (settings.Markup ("."));
			}
			AppendExplicitInterfaces (result, method, settings);
			result.Append (methodName);
			
			if (settings.IncludeGenerics) {
				if (method.TypeParameters.Count > 0) {
					result.Append (settings.Markup ("<"));
					for (int i = 0; i < method.TypeParameters.Count; i++) {
						if (i > 0)
							result.Append (settings.Markup (settings.HideGenericParameterNames ? "," : ", "));
						if (!settings.HideGenericParameterNames) {
							result.Append (NetToCSharpTypeName (method.TypeParameters [i].Name));
						}
					}
					result.Append (settings.Markup (">"));
				}
			}
			
			if (settings.IncludeParameters) {
//			CSharpFormattingPolicy policy = GetPolicy (settings);
//			if (policy.BeforeMethodCallParentheses)
//				result.Append (settings.Markup (" "));
				
				result.Append (settings.Markup ("("));
				AppendParameterList (result, settings, method.Parameters);
				result.Append (settings.Markup (")"));
			}
			
			if (settings.CompletionListFomat && settings.IncludeReturnType && getReturnType) {
				result.Append (settings.Markup (" : "));
				result.Append (GetTypeReferenceString (method.ReturnType, settings));
			}
			
//		OutputConstraints (result, settings, method.TypeParameters);
			
			return result.ToString ();			
		}

		protected override string GetMethodString (IMethod method, OutputSettings settings)
		{
			return InternalGetMethodString (method, settings, settings.EmitName (method, Format (FilterName (method.Name))), true);
		}

		protected override string GetConstructorString (IMethod method, OutputSettings settings)
		{
			return InternalGetMethodString (method, settings, settings.EmitName (method, Format (FilterName (method.DeclaringType.Name))), false);
		}

		protected override string GetDestructorString (IMethod method, OutputSettings settings)
		{
			return InternalGetMethodString (method, settings, settings.EmitName (method, settings.Markup ("~") + Format (FilterName (method.DeclaringType.Name))), false);
		}

		protected override string GetOperatorString (IMethod method, OutputSettings settings)
		{
			return InternalGetMethodString (method, settings, settings.EmitName (method, Format (FilterName (method.Name))), true);
		}

		protected override string GetFieldString (IField field, OutputSettings settings)
		{
			if (field == null)
				return "";
			var result = new StringBuilder ();
			bool isEnum = field.DeclaringType != null && field.DeclaringType.IsEnum ();
			AppendModifiers (result, settings, field);
			
			if (!settings.CompletionListFomat && settings.IncludeReturnType && !isEnum) {
				result.Append (GetTypeReferenceString (field.ReturnType, settings));
				result.Append (settings.Markup (" "));
			}
			
			if (!settings.IncludeReturnType && settings.UseFullName) {
				result.Append (GetTypeReferenceString (field.DeclaringType, settings));
				result.Append (settings.Markup ("."));
			}
			result.Append (settings.EmitName (field, FilterName (Format (field.Name))));
			
			if (settings.CompletionListFomat && settings.IncludeReturnType && !isEnum) {
				result.Append (settings.Markup (" : "));
				result.Append (GetTypeReferenceString (field.ReturnType, settings));
			}
			return result.ToString ();
		}

		protected override string GetEventString (IEvent evt, OutputSettings settings)
		{
			if (evt == null)
				return "";
			var result = new StringBuilder ();
			AppendModifiers (result, settings, evt);
			if (settings.IncludeKeywords)
				result.Append (settings.EmitKeyword ("event"));
			if (!settings.CompletionListFomat && settings.IncludeReturnType) {
				result.Append (GetTypeReferenceString (evt.ReturnType, settings));
				result.Append (settings.Markup (" "));
			}
			
			if (!settings.IncludeReturnType && settings.UseFullName) {
				result.Append (GetTypeReferenceString (evt.DeclaringType, new OutputSettings (OutputFlags.UseFullName) { Context = settings.Context }));
				result.Append (settings.Markup ("."));
			}
			
			AppendExplicitInterfaces (result, evt, settings);
			result.Append (settings.EmitName (evt, Format (FilterName (evt.Name))));
			
			if (settings.CompletionListFomat && settings.IncludeReturnType) {
				result.Append (settings.Markup (" : "));
				result.Append (GetTypeReferenceString (evt.ReturnType, settings));
			}
			return result.ToString ();
		}

		protected override string GetPropertyString (IProperty property, OutputSettings settings)
		{
			if (property == null)
				return "";
			var result = new StringBuilder ();
			
			AppendModifiers (result, settings, property);
			
			if (!settings.CompletionListFomat && settings.IncludeReturnType) {
				result.Append (GetTypeReferenceString (property.ReturnType, settings));
				result.Append (settings.Markup (" "));
			}
			
			if (!settings.IncludeReturnType && settings.UseFullName) {
				result.Append (GetTypeReferenceString (property.DeclaringType, new OutputSettings (OutputFlags.UseFullName) { Context = settings.Context }));
				result.Append (settings.Markup ("."));
			}
			
			AppendExplicitInterfaces (result, property, settings);
			
			result.Append (settings.EmitName (property, Format (FilterName (property.Name))));
			
			if (settings.CompletionListFomat && settings.IncludeReturnType) {
				result.Append (settings.Markup (" : "));
				result.Append (GetTypeReferenceString (property.ReturnType, settings));
			}
			return result.ToString ();
		}

		protected override string GetIndexerString (IProperty property, OutputSettings settings)
		{
			if (property == null)
				return "";
			var result = new StringBuilder ();
			
			AppendModifiers (result, settings, property);
			
			if (settings.IncludeReturnType) {
				result.Append (GetTypeReferenceString (property.ReturnType, settings));
				result.Append (settings.Markup (" "));
			}
			
			if (!settings.IncludeReturnType && settings.UseFullName) {
				result.Append (GetTypeReferenceString (property.DeclaringType, new OutputSettings (OutputFlags.UseFullName) { Context = settings.Context }));
				result.Append (settings.Markup ("."));
			}
			
			AppendExplicitInterfaces (result, property, settings);
			
			result.Append (settings.EmitName (property, Format ("this")));
			
			if (settings.IncludeParameters && property.Parameters.Count > 0) {
				result.Append (settings.Markup ("["));
				AppendParameterList (result, settings, property.Parameters);
				result.Append (settings.Markup ("]"));
			}
			return result.ToString ();
		}

		protected override string GetParameterString (IParameterizedMember member, IParameter parameter, OutputSettings settings)
		{
			if (member == null)
				return "";
			StringBuilder result = new StringBuilder ();
			if (settings.IncludeParameterName) {
				if (settings.IncludeModifiers) {
					if (parameter.IsOut) {
						result.Append (settings.EmitKeyword ("out"));
					}
					if (parameter.IsRef) {
						result.Append (settings.EmitKeyword ("ref"));
					}
					if (parameter.IsParams) {
						result.Append (settings.EmitKeyword ("params"));
					}
				}
				
				if (settings.IncludeReturnType) {
					result.Append (GetTypeReferenceString (parameter.Type, settings));
					result.Append (" ");
				}
				
				if (settings.HighlightName) {
					result.Append (settings.EmitName (parameter, settings.Highlight (Format (FilterName (parameter.Name)))));
				} else {
					result.Append (settings.EmitName (parameter, Format (FilterName (parameter.Name))));
				}
			} else {
				result.Append (GetTypeReferenceString (parameter.Type, settings));
			}
			return result.ToString ();
		}

		#endregion
		
		void AppendExplicitInterfaces (StringBuilder sb, IMember member, OutputSettings settings)
		{
			foreach (var explicitInterface in member.InterfaceImplementations) {
				sb.Append (Format (explicitInterface.InterfaceType.Resolve (settings.Context).FullName));
				sb.Append (settings.Markup ("."));
			}
		}
		
		void AppendParameterList (StringBuilder result, OutputSettings settings, IEnumerable<IParameter> parameterList)
		{
			if (parameterList == null)
				return;
			bool first = true;
			foreach (var parameter in parameterList) {
				if (!first)
					result.Append (settings.Markup (", "));
				AppendParameter (settings, result, parameter);
				first = false;
			}
		}
		
		void AppendParameter (OutputSettings settings, StringBuilder result, IParameter parameter)
		{
			if (parameter.IsOut) {
				result.Append (settings.Markup ("out"));
				result.Append (settings.Markup (" "));
			} else if (parameter.IsRef) {
				result.Append (settings.Markup ("ref"));
				result.Append (settings.Markup (" "));
			} else if (parameter.IsParams) {
				result.Append (settings.Markup ("params"));
				result.Append (settings.Markup (" "));
			}
			result.Append (GetParameterString (null, parameter, settings));
		}
		
		void AppendModifiers (StringBuilder result, OutputSettings settings, IEntity entity)
		{
			if (!settings.IncludeModifiers)
				return;
			if (entity.IsStatic)
				result.Append (settings.EmitModifiers ("static"));
			if (entity.IsSealed)
				result.Append (settings.EmitModifiers ("sealed"));
			if (entity.IsAbstract)
				result.Append (settings.EmitModifiers ("abstract"));
			if (entity.IsShadowing)
				result.Append (settings.EmitModifiers ("new"));
			
			switch (entity.Accessibility) {
			case Accessibility.Internal:
				result.Append (settings.EmitModifiers ("internal"));
				break;
			case Accessibility.ProtectedAndInternal:
				result.Append (settings.EmitModifiers ("protected internal"));
				break;
			case Accessibility.ProtectedOrInternal:
				result.Append (settings.EmitModifiers ("internal protected"));
				break;
			case Accessibility.Protected:
				result.Append (settings.EmitModifiers ("protected"));
				break;
			case Accessibility.Private:
				result.Append (settings.EmitModifiers ("private"));
				break;
			case Accessibility.Public:
				result.Append (settings.EmitModifiers ("public"));
				break;
			}
		}
		
/*
		new const string nullString = "Null";

		protected override IDomVisitor<OutputSettings, string> OutputVisitor {
			get {
				return this;
			}
		}
		
		public CSharpAmbience () : base ("C#", "text/x-csharp")
		{
			
			parameterModifiers[ParameterModifiers.In]       = "";
			parameterModifiers[ParameterModifiers.Out]      = "out";
			parameterModifiers[ParameterModifiers.Ref]      = "ref";
			parameterModifiers[ParameterModifiers.Params]   = "params";
			parameterModifiers[ParameterModifiers.Optional] = "";
			
			modifiers[Modifiers.Private]              = "private";
			modifiers[Modifiers.Internal]             = "internal";
			modifiers[Modifiers.Protected]            = "protected";
			modifiers[Modifiers.Public]               = "public";
			modifiers[Modifiers.Abstract]             = "abstract";
			modifiers[Modifiers.Virtual]              = "virtual";
			modifiers[Modifiers.Sealed]               = "sealed";
			modifiers[Modifiers.Static]               = "static";
			modifiers[Modifiers.Override]             = "override";
			modifiers[Modifiers.Readonly]             = "readonly";
			modifiers[Modifiers.Const]                = "const";
			modifiers[Modifiers.Partial]              = "partial";
			modifiers[Modifiers.Extern]               = "extern";
			modifiers[Modifiers.Volatile]             = "volatile";
			modifiers[Modifiers.Unsafe]               = "unsafe";
			modifiers[Modifiers.Overloads]            = "";
			modifiers[Modifiers.WithEvents]           = "";
			modifiers[Modifiers.Default]              = "";
			modifiers[Modifiers.Fixed]                = "";
			modifiers[Modifiers.ProtectedAndInternal] = "protected internal";
			modifiers[Modifiers.ProtectedOrInternal]  = "internal protected";
		}
		
		public static string NormalizeTypeName (string typeName)
		{
			if (typeName == null)
				return null;
			int idx = typeName.IndexOf ('`');
			if (idx > 0) 
				return typeName.Substring (0, idx);
			return typeName;
		}
		
		public string Visit (IParameter parameter, OutputSettings settings)
		{
		}
		
		
		public CSharpFormattingPolicy GetPolicy (OutputSettings settings)
		{
			IEnumerable<string> types = DesktopService.GetMimeTypeInheritanceChain (MonoDevelop.CSharp.Formatting.CSharpFormatter.MimeType);
			return settings.PolicyParent != null ? settings.PolicyParent.Get<CSharpFormattingPolicy> (types) : MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<CSharpFormattingPolicy> (types);
		}
		
		public string Visit (IType type, OutputSettings settings)
		{
			
		}
		
		void OutputConstraints (StringBuilder result, OutputSettings settings, IEnumerable<ITypeParameter> typeParameters)
		{
			if (settings.IncludeConstraints && typeParameters.Any (p => p.Constraints.Any () || (p.TypeParameterModifier & TypeParameterModifier.HasDefaultConstructorConstraint) != 0)) {
				result.Append (settings.Markup (" "));
				result.Append (settings.EmitKeyword ("where"));
				int typeParameterCount = 0;
				foreach (var p in typeParameters) {
					if (!p.Constraints.Any () && (p.TypeParameterModifier & TypeParameterModifier.HasDefaultConstructorConstraint) == 0)
						continue;
					if (typeParameterCount != 0)
						result.Append (settings.Markup (", "));
					
					typeParameterCount++;
					result.Append (settings.EmitName (p, p.Name));
					result.Append (settings.Markup (" : "));
					int constraintCount = 0;
			
					if ((p.TypeParameterModifier & TypeParameterModifier.HasDefaultConstructorConstraint) != 0) {
						result.Append (settings.EmitKeyword ("new"));
						result.Append (settings.Markup ("()"));
						constraintCount++;
					}
					
					foreach (var c in p.Constraints) {
						if (constraintCount != 0)
							result.Append (settings.Markup (", "));
						constraintCount++;
						if (c.DecoratedFullName == DomReturnType.ValueType.DecoratedFullName) {
							result.Append (settings.EmitKeyword ("struct"));
							continue;
						}
						if (c.DecoratedFullName == DomReturnType.TypeReturnType.DecoratedFullName) {
							result.Append (settings.EmitKeyword ("class"));
							continue;
						}
						result.Append (this.GetString (c, settings));
					}
				}
			}
		}


		static void PrintObject (StringBuilder result, OutputSettings settings, object o)
		{
			if (o is string) {
				result.Append (settings.Markup ("\""));
				result.Append (o);
				result.Append (settings.Markup ("\""));
			} else if (o is char) {
				result.Append (settings.Markup ("'"));
				result.Append (o);
				result.Append (settings.Markup ("'"));
			} else if (o is bool) {
				result.Append (((bool)o) ? "true" : "false");
			} else if (o is CodePrimitiveExpression) {
				CodePrimitiveExpression cpe = (CodePrimitiveExpression)o;
				PrintObject (result, settings, cpe.Value);
			} else 
				result.Append (o);
		}
		
		public string Visit (IAttribute attribute, OutputSettings settings)
		{
			StringBuilder result = new StringBuilder ();
			result.Append (settings.Markup ("["));
			string attrName = GetString (attribute.AttributeType, settings);
			if (attrName.EndsWith ("Attribute"))
				attrName = attrName.Substring (0, attrName.Length - "Attribute".Length);
			result.Append (attrName);
			CSharpFormattingPolicy policy = GetPolicy (settings);
			if (policy.BeforeMethodCallParentheses)
				result.Append (settings.Markup (" "));
			result.Append (settings.Markup ("("));
			bool first = true;
			if (attribute.PositionalArguments != null) {
				foreach (object o in attribute.PositionalArguments) {
					if (!first)
						result.Append (settings.Markup (", "));
					first = false;
					PrintObject (result, settings, o);
				}
			}
			result.Append (settings.Markup (")]"));
			return result.ToString ();
		}
		
		public string Visit (LocalVariable var, OutputSettings settings)
		{
			StringBuilder result = new StringBuilder ();
			
			if (settings.IncludeReturnType) {
				result.Append (GetString (var.ReturnType, settings));
				result.Append (" ");
			}
			result.Append (settings.EmitName (var, FilterName (Format (var.Name))));
			
			return result.ToString ();
		}
*/
	}
		
}
