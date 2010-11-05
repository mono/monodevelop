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
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;
using System.CodeDom;
using MonoDevelop.CSharp.Formatting;
using MonoDevelop.Ide;

namespace MonoDevelop.CSharp.Dom
{
	public class CSharpAmbience : Ambience, IDomVisitor<OutputSettings, string>
	{
		const string nullString = "Null";
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
			netToCSharpTypes["System.Void"]    = "void";
			netToCSharpTypes["System.Object"]  = "object";
			netToCSharpTypes["System.Boolean"] = "bool";
			netToCSharpTypes["System.Byte"]    = "byte";
			netToCSharpTypes["System.SByte"]   = "sbyte";
			netToCSharpTypes["System.Char"]    = "char";
			netToCSharpTypes["System.Enum"]    = "enum";
			netToCSharpTypes["System.Int16"]   = "short";
			netToCSharpTypes["System.Int32"]   = "int";
			netToCSharpTypes["System.Int64"]   = "long";
			netToCSharpTypes["System.UInt16"]  = "ushort";
			netToCSharpTypes["System.UInt32"]  = "uint";
			netToCSharpTypes["System.UInt64"]  = "ulong";
			netToCSharpTypes["System.Single"]  = "float";
			netToCSharpTypes["System.Double"]  = "double";
			netToCSharpTypes["System.Decimal"] = "decimal";
			netToCSharpTypes["System.String"]  = "string";
		}
		
		public override string ConvertTypeName (string typeName)
		{
			return NetToCSharpTypeName (typeName);
		}

		public static string NetToCSharpTypeName (string netTypeName)
		{
			if (netToCSharpTypes.ContainsKey (netTypeName)) 
				return netToCSharpTypes[netTypeName];
			return netTypeName;
		}

		protected override IDomVisitor<OutputSettings, string> OutputVisitor {
			get {
				return this;
			}
		}
		
		public CSharpAmbience () : base ("C#", "text/x-csharp")
		{
			classTypes[ClassType.Class]     = "class";
			classTypes[ClassType.Enum]      = "enum";
			classTypes[ClassType.Interface] = "interface";
			classTypes[ClassType.Struct]    = "struct";
			classTypes[ClassType.Delegate]  = "delegate";
			
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
			int idx = typeName.IndexOf ('`');
			if (idx > 0) 
				return typeName.Substring (0, idx);
			return typeName;
		}
		
		public override bool IsValidFor (string fileName)
		{
			if (fileName == null)
				return false;
			return fileName.EndsWith (".cs");
		}
		
		public override string SingleLineComment (string text)
		{
			return "// " + text;
		}

		public override string GetString (string nameSpace, OutputSettings settings)
		{
			StringBuilder result = new StringBuilder ();
			result.Append (settings.EmitKeyword ("namespace"));
			result.Append (Format (nameSpace));
			return result.ToString ();
		}
		
		public string Visit (ICompilationUnit unit, OutputSettings settings)
		{
			return "TODO";
		}
		
		public string Visit (IUsing u, OutputSettings settings)
		{
			return "TODO";
		}
		
		internal static string FilterName (string name)
		{
			if (keywords.Contains (name))
				return "@" + name;
			return name;
		}
		
		public string Visit (IProperty property, OutputSettings settings)
		{
			StringBuilder result = new StringBuilder ();
			result.Append (settings.EmitModifiers (base.GetString (property.Modifiers)));
			if (settings.IncludeReturnType) {
				result.Append (GetString (property.ReturnType, settings));
				result.Append (settings.Markup (" "));
			}
			if (!settings.IncludeReturnType && settings.UseFullName) {
				result.Append (GetString (property.DeclaringType, OutputFlags.UseFullName));
				result.Append (settings.Markup ("."));
			}
			AppendExplicitInterfaces(result, property, settings);
			result.Append (settings.EmitName (property, Format (FilterName (property.Name))));
			if (settings.IncludeParameters && property.Parameters.Count > 0) {
				result.Append (settings.Markup ("["));
				AppendParameterList (result, settings, property.Parameters);
				result.Append (settings.Markup ("]"));
			}
			return result.ToString ();
		}
		
		void AppendParameterList (StringBuilder result, OutputSettings settings, IEnumerable<IParameter> parameterList)
		{
			if (parameterList == null)
				return;
			bool first = true;
			foreach (IParameter parameter in parameterList) {
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
			result.Append (GetString (parameter, settings));
		}
		
		public string Visit (IField field, OutputSettings settings)
		{
			StringBuilder result = new StringBuilder ();
			result.Append (settings.EmitModifiers (base.GetString (field.Modifiers)));
			bool isEnum = field.DeclaringType != null && field.DeclaringType.ClassType == ClassType.Enum;
			if (settings.IncludeReturnType && !isEnum) {
				result.Append (GetString (field.ReturnType, settings));
				result.Append (settings.Markup (" "));
			}
			
			if (!settings.IncludeReturnType && settings.UseFullName) {
				result.Append (GetString (field.DeclaringType, OutputFlags.UseFullName));
				result.Append (settings.Markup ("."));
			}
			result.Append (settings.EmitName (field, FilterName (Format (field.Name))));
			
			return result.ToString ();
		}
		
		public string Visit (IReturnType returnType, OutputSettings settings)
		{
			if (returnType.IsNullable && returnType.GenericArguments.Count == 1)
				return Visit (returnType.GenericArguments[0], settings) + "?";
			if (returnType.Type is AnonymousType)
				return returnType.Type.AcceptVisitor (this, settings);
			StringBuilder result = new StringBuilder ();
			if (!settings.UseNETTypeNames && netToCSharpTypes.ContainsKey (returnType.FullName)) {
				result.Append (settings.EmitName (returnType, netToCSharpTypes[returnType.FullName]));
			} else {
				if (settings.UseFullName) 
					result.Append (settings.EmitName (returnType, Format (NormalizeTypeName (returnType.Namespace))));
				
				foreach (ReturnTypePart part in returnType.Parts) {
					if (part.IsGenerated)
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
								result.Append (GetString (part.GenericArguments[i], settings));
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
			return result.ToString ();
		}
		
		void AppendExplicitInterfaces(StringBuilder sb, IMember member, OutputSettings settings)
		{
			foreach (IReturnType explicitInterface in member.ExplicitInterfaces) {
				sb.Append (Visit (explicitInterface, settings));
				sb.Append ('.');
			}
		}
		
		public string Visit (IMethod method, OutputSettings settings)
		{
			StringBuilder result = new StringBuilder ();
			result.Append (settings.EmitModifiers (base.GetString (method.Modifiers)));
			
			if (settings.IncludeReturnType && !method.IsConstructor && !method.IsFinalizer) {
				result.Append (GetString (method.ReturnType, settings));
				result.Append (settings.Markup (" "));
			}
			if (!settings.IncludeReturnType && settings.UseFullName) {
				result.Append (GetString (method.DeclaringType, OutputFlags.UseFullName));
				result.Append (settings.Markup ("."));
			}
			AppendExplicitInterfaces (result, method, settings);
			if (method.IsConstructor) {
				result.Append (settings.EmitName (method, Format (FilterName (method.DeclaringType.Name))));
			} else if (method.IsFinalizer) {
				result.Append (settings.EmitName (method, settings.Markup ("~") + Format (FilterName (method.DeclaringType.Name))));
			} else {
				result.Append (settings.EmitName (method, Format (FilterName (method.Name))));
			}
			//this is only ever used if GeneralizeGenerics is true
			DomMethod.GenericMethodInstanceResolver resolver = null;
			if (settings.GeneralizeGenerics) {
				resolver = new DomMethod.GenericMethodInstanceResolver ();
			}
			
			if (settings.IncludeGenerics) {
				if (method.TypeParameters.Count > 0) {
					result.Append (settings.Markup ("<"));
					
					InstantiatedMethod instantiatedMethod = method as InstantiatedMethod;
					
					for (int i = 0; i < method.TypeParameters.Count; i++) {
						if (i > 0)
							result.Append (settings.Markup (settings.HideGenericParameterNames ? "," : ", "));
						if (!settings.HideGenericParameterNames) {
							if (instantiatedMethod != null) {
								result.Append (this.GetString (instantiatedMethod.GenericParameters[i], settings));
							} else {
								if (settings.GeneralizeGenerics) {
									string generalizedName = "$M" + i;
									result.Append (generalizedName);
									var t = new DomReturnType ();
									t.Name = generalizedName;
									resolver.Add (method.DeclaringType.SourceProjectDom, new DomReturnType (method.TypeParameters[i].Name), t);
								} else {
									result.Append (NetToCSharpTypeName (method.TypeParameters[i].Name));
								}
							}
						}
					}
					result.Append (settings.Markup (">"));
				}
			}
			
			if (settings.IncludeParameters) {
				CSharpFormattingPolicy policy = GetPolicy (settings);
				if (policy.BeforeMethodCallParentheses)
					result.Append (settings.Markup (" "));
				
				result.Append (settings.Markup ("("));
				bool first = true;
				if (method.Parameters != null) {
					foreach (IParameter parameter in method.Parameters) {
						if (settings.HideExtensionsParameter && method.IsExtension && first)
							continue;
						if (method.IsExtension && first)
							result.Append (settings.Markup ("this "));
						if (!first)
							result.Append (settings.Markup (", "));
						if (settings.GeneralizeGenerics) {
							AppendParameter (settings, result, (IParameter)resolver.Visit (parameter, method));
						} else {
							AppendParameter (settings, result, parameter);
						}
						first = false;
					}
				}
				result.Append (settings.Markup (")"));
			}
			
			return result.ToString ();
		}
		
		public string Visit (IParameter parameter, OutputSettings settings)
		{
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
					result.Append (GetString (parameter.ReturnType, settings));
					result.Append (" ");
				}
				
				if (settings.HighlightName) {
					result.Append (settings.EmitName (parameter, settings.Highlight (Format (FilterName (parameter.Name)))));
				} else {
					result.Append (settings.EmitName (parameter, Format (FilterName (parameter.Name))));
				}
			} else {
				result.Append (GetString (parameter.ReturnType, settings));
			}
			return result.ToString ();
		}
		
		
		public CSharpFormattingPolicy GetPolicy (OutputSettings settings)
		{
			IEnumerable<string> types = DesktopService.GetMimeTypeInheritanceChain (MonoDevelop.CSharp.Formatting.CSharpFormatter.MimeType);
			return settings.PolicyParent != null ? settings.PolicyParent.Get<CSharpFormattingPolicy> (types) : MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<CSharpFormattingPolicy> (types);
		}
		
		public string Visit (IType type, OutputSettings settings)
		{
			StringBuilder result = new StringBuilder ();
			if (type is AnonymousType) {
				result.Append ("new {");
				foreach (IProperty property in type.Properties) {
					result.AppendLine ();
					result.Append ("\t");
					if (property.ReturnType != null && !string.IsNullOrEmpty (property.ReturnType.FullName)) {
						result.Append (property.ReturnType.AcceptVisitor (this, settings));
						result.Append (" ");
					} else {
						result.Append ("? ");
					}
					result.Append (property.Name);
					result.Append (";");
				}
				result.AppendLine ();
				result.Append ("}");
				return result.ToString ();
			}
			
			
			InstantiatedType instantiatedType = type as InstantiatedType;
			string modStr = base.GetString (type.ClassType == ClassType.Enum ? (type.Modifiers & ~Modifiers.Sealed) :  type.Modifiers);
			string modifiers = !String.IsNullOrEmpty (modStr) ? settings.EmitModifiers (modStr) : "";
			string keyword = settings.EmitKeyword (GetString (type.ClassType));
			
			string name = null;
			if (instantiatedType == null && type.Name.EndsWith("[]")) {
				List<IMember> member = type.SearchMember ("Item", true);
				if (member != null && member.Count >0)
					name = Visit (member[0].ReturnType, settings);
				if (settings.IncludeGenerics)
					name += "[]";
			} 
			if (name == null) {
				if (settings.UseFullName && type.DeclaringType == null) {
					name = Format (instantiatedType == null ? type.FullName : instantiatedType.UninstantiatedType.FullName);
				} else {
					IType realType = instantiatedType == null ? type : instantiatedType.UninstantiatedType;
					name = Format (NormalizeTypeName ((settings.UseFullInnerTypeName && realType.DeclaringType != null) ? GetString (realType.DeclaringType, OutputFlags.UseFullInnerTypeName) + "." + realType.Name : realType.Name));
				}
			}
			int parameterCount = type.TypeParameters.Count;
			if (instantiatedType != null) 
				parameterCount = instantiatedType.UninstantiatedType.TypeParameters.Count;
			
			result.Append (modifiers);
			result.Append (keyword);
			if (result.Length > 0 && !result.ToString ().EndsWith (" "))
				result.Append (settings.Markup (" "));
			
			
			if (type.ClassType == ClassType.Delegate && settings.ReformatDelegates && settings.IncludeReturnType) {
				IMethod invoke = type.SearchMember ("Invoke", true).FirstOrDefault () as IMethod;
				if (invoke != null) {
					result.Append (this.GetString (invoke.ReturnType, settings));
					result.Append (settings.Markup (" "));
				}
			}
			
			if (settings.UseFullName && type.DeclaringType != null) {
				bool includeGenerics = settings.IncludeGenerics;
				settings.OutputFlags |= OutputFlags.IncludeGenerics;
				string typeString = GetString (type.DeclaringType, settings);
				if (!includeGenerics)
					settings.OutputFlags &= ~OutputFlags.IncludeGenerics;
				result.Append (typeString);
				result.Append (settings.Markup ("."));
			}
			
			result.Append (settings.EmitName (type, FilterName (name)));
			if (settings.IncludeGenerics && parameterCount > 0) {
				result.Append (settings.Markup ("<"));
				for (int i = 0; i < parameterCount; i++) {
					if (i > 0)
						result.Append (settings.Markup (settings.HideGenericParameterNames ? "," : ", "));
					if (!settings.HideGenericParameterNames) {
						if (instantiatedType != null) {
							if (i < instantiatedType.GenericParameters.Count) {
								result.Append (this.GetString (instantiatedType.GenericParameters[i], settings));
							} else {
								result.Append (instantiatedType.UninstantiatedType.TypeParameters[i].Name);
							}
						} else {
							result.Append (NetToCSharpTypeName (type.TypeParameters[i].Name));
						}
					}
				}
				result.Append (settings.Markup (">"));
			}
			
			
			if (type.ClassType == ClassType.Delegate && settings.ReformatDelegates) {
				CSharpFormattingPolicy policy = GetPolicy (settings);
				if (policy.BeforeMethodCallParentheses)
					result.Append (settings.Markup (" "));
				result.Append (settings.Markup ("("));
				IMethod invoke = type.SearchMember ("Invoke", true).FirstOrDefault () as IMethod;
				if (invoke != null) 
					AppendParameterList (result, settings, invoke.Parameters);
				result.Append (settings.Markup (")"));
				return result.ToString ();
			}
			
			if (settings.IncludeBaseTypes && type.BaseTypes.Any ()) {
				bool first = true;
				foreach (IReturnType baseType in type.BaseTypes) {
					if (baseType.FullName == "System.Object" || baseType.FullName == "System.Enum")
						continue;
					result.Append (settings.Markup (first ? " : " : ", "));
					first = false;
					result.Append (this.GetString (baseType, settings));	
				}
				
			}
			return result.ToString ();
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
		
		public string Visit (Namespace ns, OutputSettings settings)
		{
			return settings.EmitKeyword ("namespace") + settings.EmitName (ns, FilterName (ns.Name));
		}
		
		public string Visit (LocalVariable var, OutputSettings settings)
		{
			return settings.EmitName (var, FilterName (var.Name));
		}
		
		public string Visit (IEvent evt, OutputSettings settings)
		{
			StringBuilder result = new StringBuilder ();
			result.Append (settings.EmitModifiers (base.GetString (evt.Modifiers)));
			result.Append (settings.EmitKeyword ("event"));
			if (settings.IncludeReturnType) {
				result.Append (GetString (evt.ReturnType, settings));
				result.Append (settings.Markup (" "));
			}
			
			if (!settings.IncludeReturnType && settings.UseFullName) {
				result.Append (GetString (evt.DeclaringType, OutputFlags.UseFullName));
				result.Append (settings.Markup ("."));
			}

			AppendExplicitInterfaces(result, evt, settings);
			result.Append (settings.EmitName (evt, Format (FilterName (evt.Name))));
			
			return result.ToString ();
		}
	}
}
