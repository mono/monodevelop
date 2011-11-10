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
using ICSharpCode.NRefactory.CSharp;
using Mono.TextEditor;
using System.Linq;

namespace MonoDevelop.Refactoring
{
	public static class HelperMethods
	{
		/*static Dictionary<string, string> TypeTable = new Dictionary<string, string> ();
		static HelperMethods ()
		{
			TypeTable[DomReturnType.Void.FullName] = "void";
			TypeTable[DomReturnType.String.FullName] = "string";
			TypeTable[DomReturnType.Int32.FullName] = "int";
			TypeTable[DomReturnType.UInt32.FullName] = "uint";
			TypeTable[DomReturnType.Int64.FullName] = "long";
			TypeTable[DomReturnType.UInt64.FullName] = "ulong";
			TypeTable[DomReturnType.Object.FullName] = "object";
			TypeTable[DomReturnType.Float.FullName] = "float";
			TypeTable[DomReturnType.Double.FullName] = "double";
			TypeTable[DomReturnType.Byte.FullName] = "byte";
			TypeTable[DomReturnType.SByte.FullName] = "sbyte";
			TypeTable[DomReturnType.Int16.FullName] = "short";
			TypeTable[DomReturnType.UInt16.FullName] = "ushort";
			TypeTable[DomReturnType.Decimal.FullName] = "decimal";
			TypeTable[DomReturnType.Char.FullName] = "char";
			TypeTable[DomReturnType.Bool.FullName] = "bool";
		}
		
		public static bool IsIdentifierPart (this char ch)
		{
			return Char.IsLetterOrDigit (ch) || ch == '_';
		}
		
		public static DocumentLocation ToDocumentLocation (this TextLocation location, Document document)
		{
			return new DocumentLocation (location.Line, location.Column);
		}
		
		public static AstType ConvertToTypeReference (this MonoDevelop.Projects.Dom.IReturnType returnType)
		{
			string primitiveType;
			if (TypeTable.TryGetValue (returnType.DecoratedFullName, out primitiveType))
				return new PrimitiveType (primitiveType);
			
			AstType result = null;
			if (!string.IsNullOrEmpty (returnType.Namespace))
				result = new SimpleType (returnType.Namespace);
			foreach (var part in returnType.Parts) {
				if (result == null) {
					var st = new SimpleType (part.Name);
					foreach (var type in part.GenericArguments.Select (ga => ConvertToTypeReference (ga)))
						st.AddChild (type, SimpleType.Roles.TypeArgument);
					result = st;
				} else {
					var mt = new ICSharpCode.NRefactory.CSharp.MemberType () {
						Target = result,
						MemberName = part.Name
					};
					foreach (var type in part.GenericArguments.Select (ga => ConvertToTypeReference (ga)))
						mt.AddChild (type, SimpleType.Roles.TypeArgument);
					result = mt;
				}
			}
			
		
			return result;
		}
		
		public static DomReturnType ConvertToReturnType (this AstType typeRef)
		{
			if (typeRef == null)
				return null;
			DomReturnType result;
			if (typeRef is SimpleType) {
				var st = (SimpleType)typeRef;
				result = new DomReturnType (st.Identifier);
				foreach (var arg in st.TypeArguments){
					result.AddTypeParameter (ConvertToReturnType (arg));
				}
			} else if (typeRef is ICSharpCode.NRefactory.CSharp.MemberType) {
				var mt = (ICSharpCode.NRefactory.CSharp.MemberType)typeRef;
				result = ConvertToReturnType (mt.Target);
				result.Parts.Add (new ReturnTypePart (mt.MemberName));
				
				foreach (var arg in mt.TypeArguments){
					result.AddTypeParameter (ConvertToReturnType (arg));
				}
			} else if (typeRef is ComposedType) {
				var ct = (ComposedType)typeRef;
				result = ConvertToReturnType (ct.BaseType);
				result.PointerNestingLevel = ct.PointerRank;
				result.IsNullable = ct.HasNullableSpecifier;
				
				int arraySpecifiers = ct.ArraySpecifiers.Count;
				if (arraySpecifiers> 0) {
					result.ArrayDimensions = arraySpecifiers;
					int i = 0;
					foreach (var spec in ct.ArraySpecifiers) {
						result.SetDimension (i, spec.Dimensions);
						i++;
					}
				}
			} else if (typeRef is PrimitiveType) {
				var pt = (PrimitiveType)typeRef;
				result = new DomReturnType (pt.Keyword);
			} else if (typeRef.IsNull) {
				return null;

			} else { 
				throw new InvalidOperationException ("unknown AstType:" + typeRef);
			}
			
			return result;
		}
		*/
	}
}
