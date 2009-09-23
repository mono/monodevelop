// 
// HelperMethods.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using ICSharpCode.NRefactory.Ast;
using MonoDevelop.Projects.Dom;
using Mono.TextEditor;

namespace MonoDevelop.Refactoring
{
	public static class HelperMethods
	{
		public static bool IsIdentifierPart (this char ch)
		{
			return Char.IsLetterOrDigit (ch) || ch == '_';
		}
		
		public static DocumentLocation ToDocumentLocation (this DomLocation location, Document document)
		{
			return new DocumentLocation (location.Line - 1, location.Column - 1);
		}
		
		public static TypeReference ConvertToTypeReference (this MonoDevelop.Projects.Dom.IReturnType returnType)
		{
			List<TypeReference> genericTypes = new List<TypeReference> ();
			foreach (MonoDevelop.Projects.Dom.IReturnType genericType in returnType.GenericArguments) {
				genericTypes.Add (ConvertToTypeReference (genericType));
			}
			TypeReference result = new TypeReference (returnType.FullName, genericTypes);
			result.IsKeyword = true;
			result.PointerNestingLevel = returnType.PointerNestingLevel;
			if (returnType.ArrayDimensions > 0) {
				int[] rankSpecfier = new int[returnType.ArrayDimensions];
				for (int i = 0; i < returnType.ArrayDimensions; i++) {
					rankSpecfier[i] = returnType.GetDimension (i);
				}
				result.RankSpecifier = rankSpecfier;
			}
			return result;
		}
		
		public static DomReturnType ConvertToReturnType (this TypeReference typeRef)
		{
			if (typeRef == null)
				return null;
			DomReturnType result;
			if (typeRef is InnerClassTypeReference) {
				InnerClassTypeReference innerTypeRef = (InnerClassTypeReference)typeRef;
				result = innerTypeRef.BaseType.ConvertToReturnType ();
				result.Parts.Add (new ReturnTypePart (typeRef.Type));
			} else {
				result = new DomReturnType (typeRef.Type);
			}
			foreach (TypeReference genericArgument in typeRef.GenericTypes) {
				result.AddTypeParameter (ConvertToReturnType (genericArgument));
			}
			result.PointerNestingLevel = typeRef.PointerNestingLevel;
			if (typeRef.IsArrayType) {
				result.ArrayDimensions = typeRef.RankSpecifier.Length;
				for (int i = 0; i < typeRef.RankSpecifier.Length; i++) {
					result.SetDimension (i, typeRef.RankSpecifier[i]);
				}
			}
			return result;
		}
	}
	
}
