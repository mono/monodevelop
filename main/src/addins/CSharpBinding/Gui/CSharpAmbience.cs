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

namespace MonoDevelop.CSharpBinding
{
	public class CSharpAmbience : Ambience, IDomVisitor
	{
		const string nullString = "Null";
		static Dictionary<string, string> netToCSharpTypes = new Dictionary<string, string> ();
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

		public static string NetToCSharpTypeName (string netTypeName)
		{
			if (netToCSharpTypes.ContainsKey (netTypeName)) 
				return netToCSharpTypes[netTypeName];
			return netTypeName;
		}

		protected override IDomVisitor OutputVisitor {
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

		public override string GetString (string nameSpace, object data)
		{
			OutputSettings settings = GetSettings (data);
			StringBuilder result = new StringBuilder ();
			result.Append (settings.EmitKeyword ("namespace"));
			result.Append (Format (nameSpace));
			return result.ToString ();
		}
		
		object IDomVisitor.Visit (ICompilationUnit unit, object data)
		{
			return "TODO";
		}
		
		object IDomVisitor.Visit (IUsing u, object data)
		{
			return "TODO";
		}
		
		object IDomVisitor.Visit (IProperty property, object data)
		{
			OutputSettings settings = GetSettings (data);
			StringBuilder result = new StringBuilder ();
			result.Append (settings.EmitModifiers (base.GetString (property.Modifiers)));
			if (IncludeReturnType (data)) {
				result.Append (GetString (property.ReturnType, settings));
				result.Append (settings.Markup (" "));
			}
			AppendExplicitInterfaces(result, property);
			result.Append (Format (property.Name));
			return result.ToString ();
		}
		
		object IDomVisitor.Visit (IField field, object data)
		{
			OutputSettings settings = GetSettings (data);
			
			StringBuilder result = new StringBuilder ();
			result.Append (settings.EmitModifiers (base.GetString (field.Modifiers)));
			
			if (IncludeReturnType (data) && !field.IsLiteral) {
				result.Append (GetString (field.ReturnType, settings));
				result.Append (settings.Markup (" "));
			}
			
			result.Append (Format (field.Name));
			
			return result.ToString ();
		}
		
		object IDomVisitor.Visit (IReturnType returnType, object data)
		{
			OutputSettings settings = GetSettings (data);
			StringBuilder result = new StringBuilder ();
			if (netToCSharpTypes.ContainsKey (returnType.FullName)) {
				result.Append (netToCSharpTypes[returnType.FullName]);
			} else {
				if (UseFullName (data)) {
					result.Append (Format (NormalizeTypeName (returnType.FullName)));
				} else {
					result.Append (Format (NormalizeTypeName (returnType.Name)));
				}
			}
			if (IncludeGenerics (data)) {
				if (returnType.GenericArguments != null && returnType.GenericArguments.Count > 0) {
					result.Append (settings.Markup ("<"));
					for (int i = 0; i < returnType.GenericArguments.Count; i++) {
						if (i > 0)
							result.Append (settings.Markup (", "));
						result.Append (GetString (returnType.GenericArguments[i], settings));
					}
					result.Append (settings.Markup (">"));
				}
			}
			if (returnType.ArrayDimensions > 0) {
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
		
		void AppendExplicitInterfaces(StringBuilder sb, IMember member)
		{
			foreach (IReturnType explicitInterface in member.ExplicitInterfaces) {
				sb.Append (explicitInterface.ToInvariantString ());
				sb.Append ('.');
			}
		}
		
		object IDomVisitor.Visit (IMethod method, object data)
		{
			OutputSettings settings = GetSettings (data);
			
			StringBuilder result = new StringBuilder ();
			result.Append (settings.EmitModifiers (base.GetString (method.Modifiers)));
			
			if (IncludeReturnType (data) && !method.IsConstructor && !method.IsFinalizer) {
				result.Append (GetString (method.ReturnType, settings));
				result.Append (settings.Markup (" "));
			}
			AppendExplicitInterfaces (result, method);
			
			if (method.IsConstructor) {
				result.Append (Format (method.DeclaringType.Name));
			} else if (method.IsFinalizer) {
				result.Append (settings.Markup ("~"));
				result.Append (Format (method.DeclaringType.Name));
			} else {
				result.Append (Format (method.Name));
			}
			
			if (IncludeGenerics (data)) {
				if (method.GenericParameters.Count > 0) {
					result.Append (settings.Markup ("<"));
					
					for (int i = 0; i < method.GenericParameters.Count; i++) {
						if (i > 0)
							result.Append (settings.Markup (", "));
						result.Append (GetString (method.GenericParameters[i], settings));
					}
					result.Append (settings.Markup (">"));
				}
			}
			
			if (IncludeParameters (data)) {
				result.Append (settings.Markup ("("));
				bool first = true;
				
				if (method.Parameters != null) {
					foreach (IParameter parameter in method.Parameters) {
						if (HideExtensionsParameter (data) && method.IsExtension && parameter == method.Parameters[0])
							continue;
						if (!first)
							result.Append (settings.Markup (", "));
						if (parameter.IsOut) {
							result.Append (settings.Markup ("out "));
						} else if (parameter.IsRef) {
							result.Append (settings.Markup ("ref "));
						} else if (parameter.IsParams) {
							result.Append (settings.Markup ("params "));
						}
						result.Append (GetString (parameter, settings));
						first = false;
					}
				}
				result.Append (settings.Markup (")"));
			}
			
			return result.ToString ();
		}
		
		object IDomVisitor.Visit (IParameter parameter, object data)
		{
			OutputSettings settings = GetSettings (data);
			StringBuilder result = new StringBuilder ();
			if (IncludeParameterName (data)) {
				if (IncludeModifiers (data)) {
					if (parameter.IsOut)
						result.Append (settings.Markup ("out "));
					if (parameter.IsRef)
						result.Append (settings.Markup ("ref "));
					if (parameter.IsParams)
						result.Append (settings.Markup ("params "));
				}
				
				if (IncludeReturnType (data)) {
					result.Append (GetString (parameter.ReturnType, settings));
					result.Append (" ");
				}
				
				if (HighlightName (data)) {
					result.Append (settings.Highlight (Format (parameter.Name)));
				} else {
					result.Append (Format (parameter.Name));
				}
			} else {
				result.Append (GetString (parameter.ReturnType, settings));
			}
			return result.ToString ();
		}
		
		object IDomVisitor.Visit (IType type, object data)
		{
			OutputSettings settings = GetSettings (data);
			InstantiatedType instantiatedType = type as InstantiatedType;
			
			StringBuilder result = new StringBuilder ();
			result.Append (settings.EmitModifiers (base.GetString (type.Modifiers)));
			result.Append (settings.EmitKeyword (GetString (type.ClassType)));
			
			if (UseFullName (data)) {
				result.Append (Format (instantiatedType == null ? type.FullName : instantiatedType.UninstantiatedType.FullName));
			} else { 
				result.Append (Format (NormalizeTypeName (instantiatedType == null ? type.Name : instantiatedType.UninstantiatedType.Name)));
			}
			int parameterCount = type.TypeParameters.Count;
			if (instantiatedType != null)
				parameterCount = instantiatedType.GenericParameters.Count;
			
			if (IncludeGenerics (data) && parameterCount > 0) {
				result.Append (settings.Markup ("<"));
				for (int i = 0; i < parameterCount; i++) {
					if (i > 0)
						result.Append (settings.Markup (", "));
					if (instantiatedType != null) {
						result.Append (this.GetString (instantiatedType.GenericParameters[i], settings));
					} else {
						result.Append (NetToCSharpTypeName (type.TypeParameters[i].Name));
					}
					
				}
				result.Append (settings.Markup (">"));
			}
			
			if (IncludeBaseTypes (data) && type.BaseTypes.Any ()) {
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
		
		object IDomVisitor.Visit (IAttribute attribute, object data)
		{
			OutputSettings settings = GetSettings (data);
			StringBuilder result = new StringBuilder ();
			result.Append (settings.Markup ("["));
			result.Append (GetString (attribute.AttributeType, settings));
			result.Append (settings.Markup ("("));
			bool first = true;
			if (attribute.PositionalArguments != null) {
				foreach (object o in attribute.PositionalArguments) {
					if (!first)
						result.Append (settings.Markup (", "));
					first = false;
					if (o is string) {
						result.Append (settings.Markup ("\""));
						result.Append (o);
						result.Append (settings.Markup ("\""));
					} else if (o is char) {
						result.Append (settings.Markup ("'"));
						result.Append (o);
						result.Append (settings.Markup ("'"));
					} else
						result.Append (o);
				}
			}
			result.Append (settings.Markup (")]"));
			return result.ToString ();
		}
		
		object IDomVisitor.Visit (Namespace ns, object data)
		{
			OutputSettings settings = GetSettings (data);
			return settings.EmitKeyword ("namespace") + ns.Name;
		}
		
		object IDomVisitor.Visit (LocalVariable var, object data)
		{
			return var.Name;
		}
		
		object IDomVisitor.Visit (IEvent evt, object data)
		{
			OutputSettings settings = GetSettings (data);
			StringBuilder result = new StringBuilder ();
			result.Append (settings.EmitModifiers (base.GetString (evt.Modifiers)));
			result.Append (settings.EmitKeyword ("event"));
			if (IncludeReturnType (data)) {
				result.Append (GetString (evt.ReturnType, data));
				result.Append (settings.Markup (" "));
			}
			
			AppendExplicitInterfaces(result, evt);
			result.Append (Format (evt.Name));
			
			return result.ToString ();
		}
	}
}
