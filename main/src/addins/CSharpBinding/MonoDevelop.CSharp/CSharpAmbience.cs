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
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using System.IO;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

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
			
//			"where",
//			"get",
//			"set",
//			"add",
//			"remove",
//			"yield",
//			"select",
//			"group",
//			"by",
//			"into",
//			"from",
//			"ascending",
//			"descending",
//			"orderby",
//			"let",
//			"join",
//			"on",
//			"equals"
		});
		
//		static HashSet<string> optionalKeywords = new HashSet<string> (new [] {
//			"where",
//			"get",
//			"set",
//			"add",
//			"value",
//			"remove",
//			"yield",
//			"select",
//			"group",
//			"by",
//			"into",
//			"from",
//			"ascending",
//			"descending",
//			"orderby",
//			"let",
//			"join",
//			"on",
//			"equals"
//		});
		
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
			
			classTypes [TypeKind.Class] = "class";
			classTypes [TypeKind.Enum] = "enum";
			classTypes [TypeKind.Interface] = "interface";
			classTypes [TypeKind.Struct] = "struct";
			classTypes [TypeKind.Delegate] = "delegate";
		}
		
		public CSharpAmbience () : base ("C#")
		{
		}
		
		static Dictionary<TypeKind, string> classTypes = new Dictionary<TypeKind, string> ();

		public override MonoDevelop.Ide.CodeCompletion.TooltipInformation GetTooltip (IEntity entity)
		{
			if (entity == null)
				throw new ArgumentNullException ("entity");
			return MonoDevelop.CSharp.Completion.MemberCompletionData.CreateTooltipInformation (
				entity.Compilation,
				null,
				null,
				new CSharpFormattingPolicy (),
				entity,
				false,
				true);
		}

		static string GetString (TypeKind classType)
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
		
		#region implemented abstract members of MonoDevelop.Ide.TypeSystem.Ambience
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
		
		public void AppendType (StringBuilder sb, IType type, OutputSettings settings)
		{
			if (type.Kind == TypeKind.Unknown) {
				sb.Append (settings.IncludeMarkup ? settings.Markup (type.Name) : type.Name);
				return;
			}
			if (type.Kind == TypeKind.TypeParameter) {
				sb.Append (settings.IncludeMarkup ? settings.Markup (type.Name) : type.Name);
				return;
			}
			if (type.DeclaringType != null) {
				AppendType (sb, type.DeclaringType, settings);
				sb.Append (settings.Markup ("."));
			}
			if (type.Namespace == "System" && type.TypeParameterCount == 0) {
				switch (type.Name) {
				case "Object":
					sb.Append ("object");
					return;
				case "Boolean":
					sb.Append ("bool");
					return;
				case "Char":
					sb.Append ("char");
					return;
				case "SByte":
					sb.Append ("sbyte");
					return;
				case "Byte":
					sb.Append ("byte");
					return;
				case "Int16":
					sb.Append ("short");
					return;
				case "UInt16":
					sb.Append ("ushort");
					return;
				case "Int32":
					sb.Append ("int");
					return;
				case "UInt32":
					sb.Append ("uint");
					return;
				case "Int64":
					sb.Append ("long");
					return;
				case "UInt64":
					sb.Append ("ulong");
					return;
				case "Single":
					sb.Append ("float");
					return;
				case "Double":
					sb.Append ("double");
					return;
				case "Decimal":
					sb.Append ("decimal");
					return;
				case "String":
					sb.Append ("string");
					return;
				case "Void":
					sb.Append ("void");
					return;
				}
			}
			
			var typeWithElementType = type as TypeWithElementType;
			if (typeWithElementType != null) {
				AppendType (sb, typeWithElementType.ElementType, settings);
				
				if (typeWithElementType is PointerType) {
					sb.Append (settings.Markup ("*"));
				} 
				
				if (typeWithElementType is ArrayType) {
					sb.Append (settings.Markup ("["));
					sb.Append (settings.Markup (new string (',', ((ArrayType)type).Dimensions - 1)));
					sb.Append (settings.Markup ("]"));
				}
				return;
			}
			
			if (type.TypeArguments.Count > 0) {
				if (type.Name == "Nullable" && type.Namespace == "System" && type.TypeParameterCount == 1) {
					AppendType (sb, type.TypeArguments [0], settings);
					sb.Append (settings.Markup ("?"));
					return;
				}
				sb.Append (type.Name);
				if (type.TypeParameterCount > 0) {
					sb.Append (settings.Markup ("<"));
					for (int i = 0; i < type.TypeParameterCount; i++) {
						if (i > 0)
							sb.Append (settings.Markup (", "));
						AppendType (sb, type.TypeArguments [i], settings);
					}
					sb.Append (settings.Markup (">"));
				}
				return;
			}
			
			var typeDef = type as ITypeDefinition ?? type.GetDefinition ();
			if (typeDef != null) {
				if (settings.UseFullName) {
					sb.Append (settings.IncludeMarkup ? settings.Markup (typeDef.FullName) : typeDef.FullName);
				} else {
					sb.Append (settings.IncludeMarkup ? settings.Markup (typeDef.Name) : typeDef.Name);
				}
				
				if (typeDef.TypeParameterCount > 0) {
					sb.Append (settings.Markup ("<"));
					for (int i = 0; i < typeDef.TypeParameterCount; i++) {
						if (i > 0)
							sb.Append (settings.Markup (", "));
						AppendVariance (sb, typeDef.TypeParameters [i].Variance);
						AppendType (sb, typeDef.TypeParameters [i], settings);
					}
					sb.Append (settings.Markup (">"));
				}
			}
		}

		static void AppendVariance (StringBuilder sb, VarianceModifier variance)
		{
			if (variance  == VarianceModifier.Contravariant) {
				sb.Append ("in ");
			} else if (variance  == VarianceModifier.Covariant) {
				sb.Append ("out ");
			}
		}

		protected override string GetTypeReferenceString (IType reference, OutputSettings settings)
		{
			if (reference == null)
				return "null";
			var type = reference;
			if (type.Kind == TypeKind.Unknown) {
				return settings.IncludeMarkup ? settings.Markup (reference.Name) : reference.Name;
			}
			
			if (reference.Kind == TypeKind.TypeParameter)
				return settings.IncludeMarkup ? settings.Markup (reference.Name) : reference.FullName;
			
			var sb = new StringBuilder ();
			if (type is ITypeDefinition && ((ITypeDefinition)type).IsSynthetic && ((ITypeDefinition)type).Name == "$Anonymous$") {
				sb.Append ("new {");
				foreach (var property in ((ITypeDefinition)type).Properties) {
					sb.AppendLine ();
					sb.Append ("\t");
					sb.Append (GetTypeReferenceString (property.ReturnType, settings) ?? "?");
					sb.Append (" ");
					sb.Append (settings.IncludeMarkup ? settings.Markup (property.Name) : property.Name);
					sb.Append (";");
				}
				sb.AppendLine ();
				sb.Append ("}");
				return sb.ToString ();
			}
			
			AppendType (sb, type, settings);
			return sb.ToString ();
		}

		protected override string GetTypeString (IType t, OutputSettings settings)
		{
			if (t.Kind == TypeKind.Unknown) {
				return settings.IncludeMarkup ? settings.Markup (t.Name) : t.Name;
			}
			
			if (t.Kind == TypeKind.TypeParameter)
				return settings.IncludeMarkup ? settings.Markup (t.FullName) : t.FullName;

			var typeWithElementType = t as TypeWithElementType;
			if (typeWithElementType != null) {
				var sb = new StringBuilder ();
			
				if (typeWithElementType is PointerType) {
					sb.Append (settings.Markup ("*"));
				} 
				AppendType (sb, typeWithElementType.ElementType, settings);
				
				if (typeWithElementType is ArrayType) {
					sb.Append (settings.Markup ("["));
					sb.Append (settings.Markup (new string (',', ((ArrayType)t).Dimensions - 1)));
					sb.Append (settings.Markup ("]"));
				}
				return sb.ToString ();
			}
			
			ITypeDefinition type = t.GetDefinition ();
			if (type == null)
				return "";
			
			if (!settings.UseNETTypeNames && type.Namespace == "System" && type.TypeParameterCount == 0) {
				switch (type.Name) {
				case "Object":
					return "object";
				case "Boolean":
					return "bool";
				case "Char":
					return "char";
				case "SByte":
					return "sbyte";
				case "Byte":
					return "byte";
				case "Int16":
					return "short";
				case "UInt16":
					return "ushort";
				case "Int32":
					return "int";
				case "UInt32":
					return "uint";
				case "Int64":
					return "long";
				case "UInt64":
					return "ulong";
				case "Single":
					return "float";
				case "Double":
					return "double";
				case "Decimal":
					return "decimal";
				case "String":
					return "string";
				case "Void":
					return "void";
				}
			}
			
			// output anonymous type
			if (type.IsSynthetic && type.Name == "$Anonymous$")
				return GetTypeReferenceString (type, settings);
			
			var result = new StringBuilder ();


			var def = type;
			AppendModifiers (result, settings, def);
			if (settings.IncludeKeywords)
				result.Append (GetString (def.Kind));
			if (result.Length > 0 && !result.ToString ().EndsWith (" "))
				result.Append (settings.Markup (" "));
			
			
			if (type.Kind == TypeKind.Delegate && settings.ReformatDelegates && settings.IncludeReturnType) {
				var invoke = type.GetDelegateInvokeMethod ();
				result.Append (GetTypeReferenceString (invoke.ReturnType, settings));
				result.Append (settings.Markup (" "));
			}
			
			if (settings.UseFullName && !string.IsNullOrEmpty (type.Namespace)) 
				result.Append ((settings.IncludeMarkup ? settings.Markup (t.Namespace) : type.Namespace) + ".");
			
			if (settings.UseFullInnerTypeName && type.DeclaringTypeDefinition != null) {
				bool includeGenerics = settings.IncludeGenerics;
				settings.OutputFlags |= OutputFlags.IncludeGenerics;
				string typeString = GetTypeReferenceString (type.DeclaringTypeDefinition, settings);
				if (!includeGenerics)
					settings.OutputFlags &= ~OutputFlags.IncludeGenerics;
				result.Append (typeString);
				result.Append (settings.Markup ("."));
			}
			result.Append (settings.EmitName (type, settings.IncludeMarkup ? settings.Markup (t.Name) : type.Name));
			if (settings.IncludeGenerics && type.TypeParameterCount > 0) {
				result.Append (settings.Markup ("<"));
				for (int i = 0; i < type.TypeParameterCount; i++) {
					if (i > 0)
						result.Append (settings.Markup (settings.HideGenericParameterNames ? "," : ", "));
					if (!settings.HideGenericParameterNames) {
						if (t.TypeArguments.Count > 0) {
							result.Append (GetTypeReferenceString (t.TypeArguments [i], settings));
						} else {
							AppendVariance (result, type.TypeParameters [i].Variance);
							result.Append (NetToCSharpTypeName (type.TypeParameters [i].FullName));
						}
					}
				}
				result.Append (settings.Markup (">"));
			}
			
			if (t.Kind == TypeKind.Delegate && settings.ReformatDelegates) {
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
			
			if (settings.IncludeBaseTypes && type.DirectBaseTypes.Any ()) {
				bool first = true;
				foreach (var baseType in type.DirectBaseTypes) {
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
		
		internal static string GetOperator (string methodName)
		{
			switch (methodName) {
			case "op_Subtraction":
			case "op_UnaryNegation":
				return "-";
				
			case "op_Addition":
			case "op_UnaryPlus":
				return "+";
			case "op_Multiply":
				return "*";
			case "op_Division":
				return "/";
			case "op_Modulus":
				return "%";
			case "op_LogicalNot":
				return "!";
			case "op_OnesComplement":
				return "~";
			case "op_BitwiseAnd":
				return "&";
			case "op_BitwiseOr":
				return "|";
			case "op_ExclusiveOr":
				return "^";
			case "op_LeftShift":
				return "<<";
			case "op_RightShift":
				return ">>";
			case "op_GreaterThan":
				return ">";
			case "op_GreaterThanOrEqual":
				return ">=";
			case "op_Equality":
				return "==";
			case "op_Inequality":
				return "!=";
			case "op_LessThan":
				return "<";
			case "op_LessThanOrEqual":
				return "<=";
			case "op_Increment":
				return "++";
			case "op_Decrement":
				return "--";
				
			case "op_True":
				return "true";
			case "op_False":
				return "false";
				
			case "op_Implicit":
				return "implicit";
			case "op_Explicit":
				return "explicit";
			}
			return methodName;
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
				result.Append (GetTypeReferenceString (method.DeclaringTypeDefinition, new OutputSettings (OutputFlags.UseFullName)));
				result.Append (settings.Markup ("."));
			}
			AppendExplicitInterfaces (result, method, settings);
			if (method.EntityType == EntityType.Operator) {
				result.Append ("operator ");
				result.Append (settings.Markup (GetOperator (methodName)));
			} else {
				result.Append (methodName);
			}
			
			if (settings.IncludeGenerics) {
				if (method.TypeParameters.Count > 0) {
					result.Append (settings.Markup ("<"));
					for (int i = 0; i < method.TypeParameters.Count; i++) {
						if (i > 0)
							result.Append (settings.Markup (settings.HideGenericParameterNames ? "," : ", "));
						if (!settings.HideGenericParameterNames) {
							AppendVariance (result, method.TypeParameters [i].Variance);
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
			return InternalGetMethodString (method, settings, settings.EmitName (method, Format (FilterName (method.EntityType == EntityType.Constructor || method.EntityType == EntityType.Destructor ? method.DeclaringTypeDefinition.Name : method.Name))), true);
		}

		protected override string GetConstructorString (IMethod method, OutputSettings settings)
		{
			return InternalGetMethodString (method, settings, settings.EmitName (method, Format (FilterName (method.DeclaringTypeDefinition != null ? method.DeclaringTypeDefinition.Name : method.Name))), false);
		}

		protected override string GetDestructorString (IMethod method, OutputSettings settings)
		{
			return InternalGetMethodString (method, settings, settings.EmitName (method, settings.Markup ("~") + Format (FilterName (method.DeclaringTypeDefinition != null ? method.DeclaringTypeDefinition.Name : method.Name))), false);
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
			bool isEnum = field.DeclaringTypeDefinition != null && field.DeclaringTypeDefinition.Kind == TypeKind.Enum;
			AppendModifiers (result, settings, field);
			
			if (!settings.CompletionListFomat && settings.IncludeReturnType && !isEnum) {
				result.Append (GetTypeReferenceString (field.ReturnType, settings));
				result.Append (settings.Markup (" "));
			}
			
			if (!settings.IncludeReturnType && settings.UseFullName) {
				result.Append (GetTypeReferenceString (field.DeclaringTypeDefinition, settings));
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
				result.Append (GetTypeReferenceString (evt.DeclaringTypeDefinition, new OutputSettings (OutputFlags.UseFullName)));
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
				result.Append (GetTypeReferenceString (property.DeclaringTypeDefinition, new OutputSettings (OutputFlags.UseFullName)));
				result.Append (settings.Markup ("."));
			}
			
			AppendExplicitInterfaces (result, property, settings);
			
			if (property.EntityType == EntityType.Indexer) {
				result.Append (settings.EmitName (property, "this"));
			} else {
				result.Append (settings.EmitName (property, Format (FilterName (property.Name))));
			}
			
			if (settings.IncludeParameters && property.Parameters.Count > 0) {
				result.Append (settings.Markup ("["));
				AppendParameterList (result, settings, property.Parameters);
				result.Append (settings.Markup ("]"));
			}
						
			if (settings.CompletionListFomat && settings.IncludeReturnType) {
				result.Append (settings.Markup (" : "));
				result.Append (GetTypeReferenceString (property.ReturnType, settings));
			}
			
			if (settings.IncludeAccessor) {
				result.Append (settings.Markup (" {"));
				if (property.CanGet)
					result.Append (settings.Markup (" get;"));
				if (property.CanSet)
					result.Append (settings.Markup (" set;"));
				result.Append (settings.Markup (" }"));
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
				result.Append (GetTypeReferenceString (property.DeclaringTypeDefinition, new OutputSettings (OutputFlags.UseFullName)));
				result.Append (settings.Markup ("."));
			}
			
			AppendExplicitInterfaces (result, property, settings);
			
			result.Append (settings.EmitName (property, Format ("this")));
			
			if (settings.IncludeParameters && property.Getter.Parameters.Count > 0) {
				result.Append (settings.Markup ("["));
				AppendParameterList (result, settings, property.Getter.Parameters);
				result.Append (settings.Markup ("]"));
			}
			if (settings.IncludeAccessor) {
				result.Append (settings.Markup (" {"));
				if (property.CanGet)
					result.Append (settings.Markup (" get;"));
				if (property.CanSet)
					result.Append (settings.Markup (" set;"));
				result.Append (settings.Markup (" }"));
			}
			return result.ToString ();
		}

		protected override string GetParameterString (IParameterizedMember member, IParameter parameter, OutputSettings settings)
		{
			if (parameter == null)
				return "";
			var result = new StringBuilder ();
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
				
				result.Append (GetTypeReferenceString (parameter.Type, settings));
				result.Append (" ");

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
			if (member == null || !member.IsExplicitInterfaceImplementation)
				return;
			foreach (var implementedInterfaceMember in member.ImplementedInterfaceMembers) {
				if (settings.UseFullName) {
					sb.Append (Format (implementedInterfaceMember.DeclaringTypeDefinition.FullName));
				} else {
					sb.Append (Format (implementedInterfaceMember.DeclaringTypeDefinition.Name));
				}
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
			if (parameter == null)
				return;
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
	}
		
}
