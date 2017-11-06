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
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace MonoDevelop.AssemblyBrowser
{
	static class UnresolvedIdStringProvider
	{
		public static string GetIdString (IUnresolvedEntity entity)
		{
			var sb = new StringBuilder ();
			switch (entity.SymbolKind) {
			case SymbolKind.TypeDefinition:
				sb.Append ("T:");
				AppendTypeName (sb, (IUnresolvedTypeDefinition)entity, false, 0);
				return sb.ToString ();
			case SymbolKind.Field:
				sb.Append ("F:");
				break;
			case SymbolKind.Property:
			case SymbolKind.Indexer:
				sb.Append ("P:");
				break;
			case SymbolKind.Event:
				sb.Append ("E:");
				break;
			default:
				sb.Append ("M:");
				break;
			}
			var member = (IUnresolvedMember)entity;
			if (member.DeclaringTypeDefinition != null) {
				AppendTypeName (sb, member.DeclaringTypeReference, false, 0);
				sb.Append ('.');
			}
			if (member.IsExplicitInterfaceImplementation && member.Name.IndexOf ('.') < 0 && member.ExplicitInterfaceImplementations.Count == 1) {
				AppendTypeName (sb, member.ExplicitInterfaceImplementations [0].DeclaringTypeReference, true, 0);
				sb.Append ('#');
			}
			sb.Append (member.Name.Replace ('.', '#'));
			var method = member as IUnresolvedMethod;
			if (method != null && method.TypeParameters.Count > 0) {
				sb.Append ("``");
				sb.Append (method.TypeParameters.Count);
			}

			var parameterizedMember = member as IUnresolvedParameterizedMember;
			if (parameterizedMember != null && parameterizedMember.Parameters.Count > 0) {
				var parameters = parameterizedMember.Parameters;
				int start = 0;
				if (method?.IsExtensionMethod == true)
					start = 1;
				if (start < parameters.Count) {
					sb.Append ('(');
					for (int i = start; i < parameters.Count; i++){
						if (i > start)
							sb.Append (',');
						AppendTypeName (sb, parameters[i].Type, false, GetTypeParameterCount(entity));
					}
					sb.Append (')');
				}
			}
			if (member.SymbolKind == SymbolKind.Operator && (member.Name == "op_Implicit" || member.Name == "op_Explicit")) {
				sb.Append ('~');
				AppendTypeName (sb, member.ReturnType, false, 0);
			}
			return sb.ToString ();
		}

		static int GetTypeParameterCount(IUnresolvedEntity entity)
		{
			switch (entity) {
				case IUnresolvedTypeDefinition td:
					return td.TypeParameters.Count;
				case IUnresolvedMethod method:
					return method.TypeParameters.Count;
			}
			return 0;
		}

		static void AppendTypeName (StringBuilder sb, ITypeReference type, bool explicitInterfaceImpl, int outerTypeParameterCount)
		{
			switch  (type) {
			case TypeParameterReference tp:
				sb.Append ('`');
				if (tp.OwnerType == SymbolKind.Method)
					sb.Append ('`');
				sb.Append (tp.Index);
				break;
			case ArrayTypeReference array:
				AppendTypeName (sb, array.ElementType, explicitInterfaceImpl, outerTypeParameterCount);
				sb.Append ('[');
				if (array.Dimensions > 1) {
					for (int i = 0; i < array.Dimensions; i++) {
						if (i > 0)
							sb.Append (explicitInterfaceImpl ? '@' : ',');
						if (!explicitInterfaceImpl)
							sb.Append ("0:");
					}
				}
				sb.Append (']');
				break;
			case PointerTypeReference ptrtr:
				AppendTypeName (sb, ptrtr.ElementType, explicitInterfaceImpl, outerTypeParameterCount);
				sb.Append ('*');
				break;
			case ByReferenceTypeReference brtr:
				AppendTypeName (sb, brtr.ElementType, explicitInterfaceImpl, outerTypeParameterCount);
				sb.Append ('@');
				break;
			case GetClassTypeReference gctr:
				AppendClassTypeReference (sb, gctr.FullTypeName, outerTypeParameterCount);
				break;
			case IUnresolvedTypeDefinition td:
				if (td.DeclaringTypeDefinition != null) {
					AppendTypeName (sb, td.DeclaringTypeDefinition, explicitInterfaceImpl, outerTypeParameterCount);
					sb.Append (explicitInterfaceImpl ? '#' : '.');
					sb.Append (td.Name);
					AppendTypeParameters (sb, td.TypeParameters.Count, outerTypeParameterCount + td.DeclaringTypeDefinition.TypeParameters.Count, explicitInterfaceImpl);
				} else {
					if (explicitInterfaceImpl)
						sb.Append (td.FullName.Replace ('.', '#'));
					else
						sb.Append (td.FullName);
					AppendTypeParameters (sb, td.TypeParameters.Count, outerTypeParameterCount, explicitInterfaceImpl);
				}
				break;
			case ParameterizedTypeReference ptr:
				AppendTypeName (sb, ptr.GenericType, explicitInterfaceImpl, int.MaxValue); // type parameter counrt should be omitted because it's determined by the type argument list given
				sb.Append ('{');
				for (int i = 0; i < ptr.TypeArguments.Count; i++) {
					if (i > 0)
						sb.Append(',');
					AppendTypeName (sb, ptr.TypeArguments[i], explicitInterfaceImpl, outerTypeParameterCount);
				}
				sb.Append ('}');
				break;
			}
		}

		static void AppendClassTypeReference (StringBuilder sb, FullTypeName name, int outerTypeParameterCount)
		{
			if (name.IsNested) {
				for (int i = 0; i < name.NestingLevel; i++) {
					if (i > 0)
						sb.Append ('.');
					sb.Append (name.GetNestedTypeName (i));
				}
			} else {
				if (!string.IsNullOrEmpty (name.TopLevelTypeName.Namespace)) {
					sb.Append (name.TopLevelTypeName.Namespace);
					sb.Append ('.');
				}
				sb.Append (name.TopLevelTypeName.Name);
			}
			if (name.TypeParameterCount - outerTypeParameterCount > 0) {
				sb.Append ('`');
				sb.Append (name.TypeParameterCount - outerTypeParameterCount);
			}
		}

		static void AppendTypeParameters (StringBuilder sb,  int typeParameterCount, int outerTypeParameterCount, bool explicitInterfaceImpl)
		{
			int tpc = typeParameterCount - outerTypeParameterCount;
			if (tpc > 0) {
				sb.Append ('`');
				sb.Append (tpc);
			}
		}
	}
}