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
		
		public static List<DocumentLocation> GetInsertionPoints (Document doc, IType type)
		{
			if (doc == null)
				throw new ArgumentNullException ("doc");
			if (type == null)
				throw new ArgumentNullException ("type");
			List<DocumentLocation> result = new List<DocumentLocation> ();
			result.Add (GetInsertionPosition (doc, type.BodyRegion.Start.Line - 1, type.BodyRegion.Start.Column));
			foreach (IMember member in type.Members) {
				DomLocation domLocation = member.BodyRegion.End;
				Console.WriteLine ("loc:" + domLocation);
				if (domLocation.Line <= 0) {
					Console.WriteLine ("location:" + member.Location);
					
					LineSegment lineSegment = doc.GetLine (member.Location.Line - 1);
					if (lineSegment == null)
						continue;
					domLocation = new DomLocation (member.Location.Line, lineSegment.EditableLength + 1);
				}
				result.Add (GetInsertionPosition (doc, domLocation.Line - 1, domLocation.Column - 1));
			}
			return result;
		}
		
		static DocumentLocation GetInsertionPosition (Document doc, int line, int column)
		{
			LineSegment nextLine = doc.GetLine (line + 1);
			int bodyEndOffset = doc.LocationToOffset (line, column) + 1;
			Console.WriteLine (bodyEndOffset);
			int endOffset = nextLine != null ? nextLine.Offset : doc.Length;
			for (int i = bodyEndOffset; i < endOffset; i++) {
				char ch = doc.GetCharAt (i);
				if (!char.IsWhiteSpace (ch))
					return doc.OffsetToLocation (i);
			}
			if (nextLine == null)
				return doc.OffsetToLocation (bodyEndOffset - 1);
			while (line + 2 < doc.LineCount && doc.GetLineIndent (line + 2).Length == doc.GetLine (line + 2).EditableLength)
				line++;
			return new DocumentLocation (line + 1, doc.GetLine (line + 1).EditableLength);
		}
	}
}
