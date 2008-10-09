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

		public override string GetString (string nameSpace, OutputFlags flags)
		{
			StringBuilder result = new StringBuilder ();
			if (EmitKeywords (flags)) {
				if (EmitMarkup (flags))
					result.Append ("<b>");
				result.Append ("namespace ");
				if (EmitMarkup (flags))
					result.Append ("</b>");
			}
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
			OutputFlags flags = (OutputFlags)data;
			StringBuilder result = new StringBuilder ();
			if (IncludeModifiers (flags)) {
				if (EmitMarkup (flags))
					result.Append ("<b>");
				result.Append (base.GetString (property.Modifiers));
				result.Append (" ");
				if (EmitMarkup (flags))
					result.Append ("</b>");
			}
			
			if (IncludeReturnType (flags)) {
				result.Append (GetString (property.ReturnType, flags));
				result.Append (" ");
			}
			AppendExplicitInterfaces(result, property);
			if (UseFullName (flags)) {
				result.Append (Format (property.FullName));
			} else {
				result.Append (Format (property.Name));
			}
			return result.ToString ();
		}
		
		object IDomVisitor.Visit (IField field, object data)
		{
			OutputFlags flags = (OutputFlags)data;
			StringBuilder result = new StringBuilder ();
			
			if (IncludeModifiers (flags)) {
				if (EmitMarkup (flags))
					result.Append ("<b>");
				result.Append (base.GetString (field.Modifiers));
				result.Append (" ");
				if (EmitMarkup (flags))
					result.Append ("</b>");
			}
			
			if (IncludeReturnType (flags) && !field.IsLiteral) {
				result.Append (GetString (field.ReturnType, flags));
				result.Append (" ");
			}
			
			if (UseFullName (flags)) {
				result.Append (Format (field.FullName));
			} else {
				result.Append (Format (field.Name));
			}
			return result.ToString ();
		}
		
		object IDomVisitor.Visit (IReturnType returnType, object data)
		{
			OutputFlags flags = (OutputFlags)data;
			StringBuilder result = new StringBuilder ();
			if (netToCSharpTypes.ContainsKey (returnType.FullName)) {
				result.Append (netToCSharpTypes[returnType.FullName]);
			} else {
				if (UseFullName (flags)) {
					result.Append (Format (NormalizeTypeName (returnType.FullName)));
				} else {
					result.Append (Format (NormalizeTypeName (returnType.Name)));
				}
			}
			if (IncludeGenerics (flags)) {
				if (returnType.GenericArguments != null && returnType.GenericArguments.Count > 0) {
					if (EmitMarkup (flags)) {
						result.Append ("&lt;");
					} else {
						result.Append ('<');
					}
					for (int i = 0; i < returnType.GenericArguments.Count; i++) {
						if (i > 0)
							result.Append (", ");
						result.Append (GetString (returnType.GenericArguments[i], flags));
					}
					if (EmitMarkup (flags)) {
						result.Append ("&gt;");
					} else {
						result.Append ('>');
					}
				}
			}
			if (returnType.ArrayDimensions > 0) {
				for (int i = 0; i < returnType.ArrayDimensions; i++) {
					result.Append ('[');
					int dimension = returnType.GetDimension (i);
					if (dimension > 0)
						result.Append (new string (',', dimension));
					result.Append (']');
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
			OutputFlags flags = (OutputFlags)data;
			StringBuilder result = new StringBuilder ();
			
			if (IncludeModifiers (flags)) {
				if (EmitMarkup (flags))
					result.Append ("<b>");
				result.Append (base.GetString (method.Modifiers));
				result.Append (" ");
				if (EmitMarkup (flags))
					result.Append ("</b>");
			}
				
			if (IncludeReturnType (flags) && !method.IsConstructor) {
				result.Append (GetString (method.ReturnType, flags));
				result.Append (" ");
			}
			AppendExplicitInterfaces(result, method);
			
			if (method.IsConstructor) {
				result.Append (Format (method.DeclaringType.Name));
			} else {
				if (UseFullName (flags)) {
					result.Append (Format (method.FullName));
				} else {
					result.Append (Format (method.Name));
				}
			}
			if (IncludeGenerics (flags)) {
				if (method.GenericParameters.Count > 0) {
					if (EmitMarkup (flags)) {
						result.Append ("&lt;");
					} else {
						result.Append ('<');
					}
					for (int i = 0; i < method.GenericParameters.Count; i++) {
						if (i > 0)
							result.Append (", ");
						result.Append (GetString (method.GenericParameters[i], flags));
					}
					if (EmitMarkup (flags)) {
						result.Append ("&gt;");
					} else {
						result.Append ('>');
					}
				}
			}
			
			if (IncludeParameters (flags)) {
				result.Append ("(");
				bool first = true;
				
				if (method.Parameters != null) {
					foreach (IParameter parameter in method.Parameters) {
						if (HideExtensionsParameter (flags) && method.IsExtension && parameter == method.Parameters[0])
							continue;
						if (!first)
							result.Append (", ");
						if (parameter.IsOut) {
							result.Append ("out ");
						} else if (parameter.IsRef) {
							result.Append ("ref ");
						} else if (parameter.IsParams) {
							result.Append ("params ");
						}
						result.Append (GetString (parameter, flags));
						first = false;
					}
				}
				result.Append (")");
			}
			
			return result.ToString ();
		}
		
		object IDomVisitor.Visit (IParameter parameter, object data)
		{
			OutputFlags flags = (OutputFlags)data;
			StringBuilder result = new StringBuilder ();
			if (IncludeParameterName (flags)) {
				if (IncludeReturnType (flags)) {
					result.Append (GetString (parameter.ReturnType, flags));
					result.Append (" ");
				}
				if (parameter.IsOut)
					result.Append ("out ");
				if (parameter.IsRef)
					result.Append ("ref ");
				if (parameter.IsParams)
					result.Append ("params ");
				if (HighlightName (flags) && EmitMarkup (flags)) 
					result.Append ("<b>");
				result.Append (Format (parameter.Name));
				if (HighlightName (flags) && EmitMarkup (flags)) 
					result.Append ("</b>");
			} else {
				result.Append (GetString (parameter.ReturnType, flags));
			}
			return result.ToString ();
		}
		
		object IDomVisitor.Visit (IType type, object data)
		{
			OutputFlags flags = (OutputFlags)data;
			StringBuilder result = new StringBuilder ();
			if (IncludeModifiers (flags)) {
				if (EmitMarkup (flags))
					result.Append ("<b>");
				result.Append (base.GetString (type.Modifiers));
				result.Append (" ");
				if (EmitMarkup (flags))
					result.Append ("</b>");
			}
			
			if (EmitKeywords (flags)) {
				if (EmitMarkup (flags))
					result.Append ("<b>");
				result.Append (GetString (type.ClassType));
				result.Append (" ");
				if (EmitMarkup (flags))
					result.Append ("</b>");
			}
			
			if (UseFullName (flags)) {
				result.Append (Format (type.FullName));
			} else { 
				result.Append (Format (NormalizeTypeName (type.Name)));
			}
			
			if (IncludeGenerics (flags) && type.TypeParameters != null && type.TypeParameters.Count > 0) {
				if (EmitMarkup (flags)) {
					result.Append ("&lt;");
				} else {
					result.Append ('<');
				}
				for (int i = 0; i < type.TypeParameters.Count; i++) {
					if (i > 0)
						result.Append (", ");
					result.Append (NetToCSharpTypeName (type.TypeParameters[i].Name));
				}
				if (EmitMarkup (flags)) {
					result.Append ("&gt;");
				} else {
					result.Append ('>');
				}
			}
			
			if (IncludeBaseTypes (flags) && type.BaseType != null) {
				result.Append (" : ");
				result.Append (Format (NormalizeTypeName (type.BaseType.Name)));
			}
			return result.ToString ();
		}
		
		object IDomVisitor.Visit (IAttribute attribute, object data)
		{
			OutputFlags flags = (OutputFlags)data;
			StringBuilder result = new StringBuilder ();
			result.Append ('[');
			result.Append (GetString (attribute.AttributeType, flags));
			result.Append ('(');
			bool first = true;
			if (attribute.PositionalArguments != null) {
				foreach (object o in attribute.PositionalArguments) {
					if (!first)
						result.Append (", ");
					first = false;
					if (o is string) {
						result.Append ('"');
						result.Append (o);
						result.Append ('"');
					} else if (o is char) {
						result.Append ("'");
						result.Append (o);
						result.Append ("'");
					} else
						result.Append (o);
				}
			}
			result.Append (')');
			result.Append (']');
			return result.ToString ();
		}
		
		object IDomVisitor.Visit (Namespace ns, object data)
		{
			return "Namespace " + ns.Name;
		}
		object IDomVisitor.Visit (LocalVariable var, object data)
		{
			return var.Name;
		}
		object IDomVisitor.Visit (IEvent evt, object data)
		{
			OutputFlags flags = (OutputFlags)data;
			StringBuilder result = new StringBuilder ();
			if (IncludeModifiers (flags)) {
				if (EmitMarkup (flags))
					result.Append ("<b>");
				result.Append (base.GetString (evt.Modifiers));
				result.Append (" ");
				if (EmitMarkup (flags))
					result.Append ("</b>");
			}
			
			
			if (EmitKeywords (flags)) {
				if (EmitMarkup (flags))
					result.Append ("<b>");
				result.Append ("event ");
				if (EmitMarkup (flags))
					result.Append ("</b>");
			}
			
			AppendExplicitInterfaces(result, evt);
			result.Append (Format (evt.Name));
			
			return result.ToString ();
		}
	
	}
}
