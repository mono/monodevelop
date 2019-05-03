//
// ICSharpCodeStringFormatter.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corporation. All rights reserved.
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
using ICSharpCode.Decompiler.TypeSystem;
using MonoDevelop.Core;
using System.Text;

namespace MonoDevelop.AssemblyBrowser
{
	static class ICSharpCodeStringFormatter
	{
		public static string GetDisplayString (this IMethod method)
		{
			if (method == null)
				throw new ArgumentNullException (nameof (method));

			var sb = StringBuilderCache.Allocate ();
			sb.Append (method.Name);
			if (method.TypeParameters.Count > 0) {
				sb.Append ('<');
				for (int i = 0; i < method.TypeParameters.Count; i++) {
					if (i > 0)
						sb.Append (", ");
					sb.Append (method.TypeParameters [i].Name);
				}
				sb.Append ('>');
			}

			sb.Append ('(');
			for (int i = 0; i < method.Parameters.Count; i++) {
				if (i > 0)
					sb.Append (", ");
				sb.Append (method.Parameters [i].Type.Name);
			}
			if (method.IsConstructor) {
				sb.Append (')');
			} else {
				sb.Append (") : ");
				AppendReturnType (sb, method.ReturnType);
			}
			return StringBuilderCache.ReturnAndFree (sb);
		}

		public static string GetDisplayString (this IProperty property)
		{
			var sb = StringBuilderCache.Allocate ();
			sb.Append (property.Name);
			var parameters = property.Parameters;
			if (parameters.Count != 0) {
				sb.Append ('(');
				for (int i = 0; i < parameters.Count; i++) {
					if (i > 0)
						sb.Append (", ");
					sb.Append (parameters [i].Type.Name);
				}
				sb.Append (") : ");
			} else {
				sb.Append (" : ");
			}
			AppendReturnType (sb, property.ReturnType);

			return StringBuilderCache.ReturnAndFree (sb);
		}

		public static string GetDisplayString (this ITypeDefinition type)
		{
			var sb = StringBuilderCache.Allocate ();
			AppendTypeDisplayString(sb, type);
			return StringBuilderCache.ReturnAndFree (sb);
		}

		static void AppendTypeDisplayString(StringBuilder sb, ITypeDefinition type)
		{
			if (type.DeclaringTypeDefinition != null) {
				AppendTypeDisplayString(sb, type.DeclaringTypeDefinition);
				sb.Append ('.');
			}
			sb.Append (type.FullTypeName.Name);
			if (type.TypeParameterCount > 0) {
				sb.Append ('<');
				for (int i = 0; i < type.TypeParameterCount; i++) {
					if (i > 0)
						sb.Append (", ");
					sb.Append (type.TypeParameters [i].Name);
				}
				sb.Append ('>');
			}
		}

		public static string GetDisplayString (this IField field)
		{
			var sb = StringBuilderCache.Allocate ();
			sb.Append (field.Name);
			sb.Append (" : ");
			AppendReturnType (sb, field.ReturnType);
			return StringBuilderCache.ReturnAndFree (sb);
		}

		public static string GetDisplayString (this IEvent evt)
		{
			var sb = StringBuilderCache.Allocate ();
			sb.Append (evt.Name);
			sb.Append (" : ");
			AppendReturnType (sb, evt.ReturnType);
			return StringBuilderCache.ReturnAndFree (sb);
		}

		static void AppendReturnType (StringBuilder sb, IType returnType)
		{
			sb.Append (returnType.Name);
			if (returnType.TypeArguments.Count > 0) {
				sb.Append ('<');
				for (int i = 0; i < returnType.TypeArguments.Count; i++) {
					if (i > 0)
						sb.Append (", ");
					AppendReturnType (sb, returnType.TypeArguments [i]);
				}
				sb.Append ('>');
			}
		}
	}
}
