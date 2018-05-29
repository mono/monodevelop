//
// UnresolvedIdStringProvider.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2017 Microsoft Corporation
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
using System.Text;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.TypeSystem.Implementation;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using MonoDevelop.Core;

namespace MonoDevelop.AssemblyBrowser
{
	static class UnresolvedIdStringProvider
	{
		public static string GetIdString (IMemberDefinition member)
		{
			var sb = new StringBuilder ();
			switch (member) {
			case TypeDefinition type:
				sb.Append ("T:");
				AppendTypeName (sb, type, false, 0);
				return sb.ToString ();
			case FieldDefinition field:
				sb.Append ("F:");
				break;
			case PropertyDefinition property:
				sb.Append ("P:");
				break;
			case EventDefinition evt:
				sb.Append ("E:");
				break;
			default:
				sb.Append ("M:");
				break;
			}
			if (member.DeclaringType != null) {
				AppendTypeName (sb, member.DeclaringType, false, 0);
				sb.Append ('.');
			}

			/*if (member.IsExplicitInterfaceImplementation && member.Name.IndexOf ('.') < 0 && member.ExplicitInterfaceImplementations.Count == 1) {
				AppendTypeName (sb, member.ExplicitInterfaceImplementations [0].DeclaringTypeReference, true, 0);
				sb.Append ('#');
			}*/
			sb.Append (member.Name.Replace ('.', '#'));
			var method = member as MethodDefinition;
			if (method != null && method.GenericParameters.Count > 0) {
				sb.Append ("``");
				sb.Append (method.GenericParameters.Count);
			}
			var parameters = method?.Parameters ?? (member as PropertyDefinition)?.Parameters;

			if (parameters != null) {
				int start = 0;
				if (method != null && method.ExplicitThis)
					start = 1;
				if (start < parameters.Count) {
					sb.Append ('(');
					for (int i = start; i < parameters.Count; i++){
						if (i > start)
							sb.Append (',');
						AppendTypeName (sb, parameters[i].ParameterType, false, GetTypeParameterCount(member));
					}
					sb.Append (')');
				}
			}
			if (method != null && method.IsSpecialName && (method.Name == "op_Implicit" || method.Name == "op_Explicit")) {
				sb.Append ('~');
				AppendTypeName (sb, method.ReturnType, false, 0);
			}
			return sb.ToString ();
		}

		static int GetTypeParameterCount(IMemberDefinition entity)
		{
			switch (entity) {
			case TypeDefinition td:
				return td.GenericParameters.Count;
			case MethodDefinition method:
				return method.GenericParameters.Count;
			}
			return 0;
		}

		static void AppendTypeName (StringBuilder sb, TypeReference type, bool explicitInterfaceImpl, int outerTypeParameterCount)
		{
			if (type is TypeDefinition td) {
				if (td.DeclaringType != null) {
					AppendTypeName (sb, td.DeclaringType, explicitInterfaceImpl, outerTypeParameterCount);
					sb.Append (explicitInterfaceImpl ? '#' : '.');
					sb.Append (td.Name);
				} else {
					if (explicitInterfaceImpl)
						sb.Append (td.FullName.Replace ('.', '#'));
					else
						sb.Append (td.FullName);
				}
				return;
			}
			if (type.IsGenericParameter) {
				var gp = type as GenericParameter;
				sb.Append ('`');
				sb.Append (gp.Position);
				return;
			}
			if (type.IsArray) {
				AppendTypeName (sb, type.GetElementType (), explicitInterfaceImpl, outerTypeParameterCount);
				sb.Append ('[');
				var array = type.MakeArrayType ();
				if (array.Rank > 1) {
					for (int i = 0; i < array.Rank; i++) {
						if (i > 0)
							sb.Append (explicitInterfaceImpl ? '@' : ',');
						if (!explicitInterfaceImpl)
							sb.Append ("0:");
					}
				}
				sb.Append (']');
				return;
			}
			if (type.IsPointer) {
				AppendTypeName (sb, type.GetElementType (), explicitInterfaceImpl, outerTypeParameterCount);
				sb.Append ('*');
				return;
			}
			if (type.IsByReference) {
				AppendTypeName (sb, type.GetElementType (), explicitInterfaceImpl, outerTypeParameterCount);
				sb.Append ('@');
				return;
			}
			if (type.IsByReference) {
				AppendClassTypeReference (sb, type, outerTypeParameterCount);
				return;
			}
			if (type.IsGenericInstance) {
				try {
					var genericInstance = type.MakeGenericInstanceType ();
					AppendTypeName (sb, genericInstance.ElementType, explicitInterfaceImpl, int.MaxValue); // type parameter count should be omitted because it's determined by the type argument list given
					sb.Append ('{');
					for (int i = 0; i < genericInstance.GenericArguments.Count; i++) {
						if (i > 0)
							sb.Append (',');
						AppendTypeName (sb, genericInstance.GenericArguments [i], explicitInterfaceImpl, outerTypeParameterCount);
					}
					sb.Append ('}');
					return;
				} catch (Exception e) {
					LoggingService.LogError ("Exception while creating generic instance type id string " + type.FullName, e);
				}
			}

			sb.Append (type.FullName);
		}

		static void AppendClassTypeReference (StringBuilder sb, TypeReference name, int outerTypeParameterCount)
		{
			sb.Append (name.FullName);
			if (name.GenericParameters.Count - outerTypeParameterCount > 0) {
				sb.Append ('`');
				sb.Append (name.GenericParameters.Count- outerTypeParameterCount);
			}
		}
	}
}