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

namespace MonoDevelop.CSharp
{
	public class SignatureMarkupCreator
	{/*
		public string GetTypeString (IType t, OutputSettings settings)
		{
			if (t.Kind == TypeKind.Unknown) {
				return t.Name;
			}
			
			if (t.Kind == TypeKind.TypeParameter)
				return t.FullName;
			
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
				result.Append (type.Namespace + ".");
			
			if (settings.UseFullInnerTypeName && type.DeclaringTypeDefinition != null) {
				bool includeGenerics = settings.IncludeGenerics;
				settings.OutputFlags |= OutputFlags.IncludeGenerics;
				string typeString = GetTypeReferenceString (type.DeclaringTypeDefinition, settings);
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
						if (t is ParameterizedType) {
							result.Append (GetTypeReferenceString (((ParameterizedType)t).TypeArguments [i], settings));
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
	*/}
}

