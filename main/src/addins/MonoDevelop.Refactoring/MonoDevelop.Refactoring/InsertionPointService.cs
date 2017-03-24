//
// InsertionPointService.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MonoDevelop.Ide.Editor;
using Microsoft.CodeAnalysis.Shared.Extensions;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.Refactoring
{
	public static class InsertionPointService
	{
		public static List<InsertionPoint> GetInsertionPoints (IReadonlyTextDocument data, MonoDevelop.Ide.TypeSystem.ParsedDocument parsedDocument, ITypeSymbol type, int part)
		{
			if (data == null)
				throw new ArgumentNullException (nameof (data));
			if (parsedDocument == null)
				throw new ArgumentNullException (nameof (parsedDocument));
			if (type == null)
				throw new ArgumentNullException (nameof (type));
			if (!type.IsDefinedInSource ())
				throw new ArgumentException ("The given type needs to be defined in source code.", nameof (type));

			// update type from parsed document, since this is always newer.
			//type = parsedDocument.GetInnermostTypeDefinition (type.GetLocation ()) ?? type;
			//var realStartLocation = data.OffsetToLocation (offset);
			var model = parsedDocument.GetAst<SemanticModel> ();
			return GetInsertionPoints (data, model, type, part);
		}

		internal static List<InsertionPoint> GetInsertionPoints (IReadonlyTextDocument data, SemanticModel model, ITypeSymbol type, int part)
		{
			List<InsertionPoint> result = new List<InsertionPoint> ();

			type = model.GetEnclosingNamedType (part, default (CancellationToken)) as ITypeSymbol ?? type;
			var sourceSpan = new TextSpan (part, 0);

			var filePath = data.FileName;
			var declaringType = type.DeclaringSyntaxReferences.FirstOrDefault (dsr => dsr.SyntaxTree.FilePath == filePath && dsr.Span.Contains (sourceSpan)) ?? type.DeclaringSyntaxReferences.FirstOrDefault ();
			if (declaringType == null)
				return result;
			return GetInsertionPoints (data, type, result, sourceSpan, declaringType);
		}

		static List<InsertionPoint> GetInsertionPoints (IReadonlyTextDocument data, ITypeSymbol type, List<InsertionPoint> result, TextSpan sourceSpan, SyntaxReference declaringType)
		{
			var openBraceToken = declaringType.GetSyntax ().ChildTokens ().FirstOrDefault (t => t.IsKind (SyntaxKind.OpenBraceToken));
			if (!openBraceToken.IsMissing) {
				var domLocation = data.OffsetToLocation (openBraceToken.SpanStart);
				result.Add (GetInsertionPosition (data, domLocation.Line, domLocation.Column));
				//			result.Add (GetInsertionPosition (data, realStartLocation.Line, realStartLocation.Column));
				result [0].LineBefore = NewLineInsertion.None;
			}
			foreach (var member in type.GetMembers ()) {
				if (member.IsImplicitlyDeclared || !member.IsDefinedInSource ())
					continue;
				//var domLocation = member.BodyRegion.End;
				foreach (var loc in member.DeclaringSyntaxReferences) {
					if (loc.SyntaxTree.FilePath != declaringType.SyntaxTree.FilePath || !declaringType.Span.Contains (sourceSpan))
						continue;
					var domLocation = data.OffsetToLocation (loc.Span.End);

					if (domLocation.Line <= 0) {
						var lineSegment = data.GetLineByOffset (loc.Span.Start);
						if (lineSegment == null)
							continue;
						domLocation = new DocumentLocation (lineSegment.LineNumber, lineSegment.Length + 1);
					}
					result.Add (GetInsertionPosition (data, domLocation.Line, domLocation.Column));
					break;
				}
			}

			result [result.Count - 1].LineAfter = NewLineInsertion.None;
			CheckStartPoint (data, result [0], result.Count == 1);
			if (result.Count > 1) {
				result.RemoveAt (result.Count - 1); 
				NewLineInsertion insertLine;
				var typeSyntaxReference = type.DeclaringSyntaxReferences.FirstOrDefault (r => r.SyntaxTree.FilePath == data.FileName && r.Span.Contains (sourceSpan));

				var lineBefore = data.GetLineByOffset (typeSyntaxReference.Span.End)?.PreviousLine;
				if (lineBefore != null && lineBefore.Length == lineBefore.GetIndentation (data).Length) {
					insertLine = NewLineInsertion.None;
				} else {
					insertLine = NewLineInsertion.Eol;
				}
				// search for line start
				var line = data.GetLineByOffset (typeSyntaxReference.Span.End);
				int col = typeSyntaxReference.Span.End - line.Offset;
				if (line != null) {
					var lineOffset = line.Offset;
					col = Math.Min (line.Length, col);
					while (lineOffset + col - 2 >= 0 && col > 1 && char.IsWhiteSpace (data.GetCharAt (lineOffset + col - 2)))
						col--;
				}
				result.Add (new InsertionPoint (new DocumentLocation (line.LineNumber, col), insertLine, NewLineInsertion.Eol));
				CheckEndPoint (data, result [result.Count - 1], result.Count == 1);
			}

//			foreach (var region in parsedDocument.UserRegions.Where (r => type.BodyRegion.IsInside (r.Region.Begin))) {
//				result.Add (new InsertionPoint (new DocumentLocation (region.Region.BeginLine + 1, 1), NewLineInsertion.Eol, NewLineInsertion.Eol));
//				result.Add (new InsertionPoint (new DocumentLocation (region.Region.EndLine, 1), NewLineInsertion.Eol, NewLineInsertion.Eol));
//				result.Add (new InsertionPoint (new DocumentLocation (region.Region.EndLine + 1, 1), NewLineInsertion.Eol, NewLineInsertion.Eol));
//			}
			result.Sort ((left, right) => left.Location.CompareTo (right.Location));
			//foreach (var res in result)
			//	Console.WriteLine (res);
			return result;
		}

		public static List<InsertionPoint> GetInsertionPoints (IReadonlyTextDocument data, MonoDevelop.Ide.TypeSystem.ParsedDocument parsedDocument, ITypeSymbol type, Location location)
		{
			return GetInsertionPoints (data, parsedDocument, type, location.SourceSpan.Start);
		}

		static void CheckEndPoint (IReadonlyTextDocument doc, InsertionPoint point, bool isStartPoint)
		{
			var line = doc.GetLine (point.Location.Line);
			if (line == null)
				return;

			if (doc.GetLineIndent (line).Length + 1 < point.Location.Column)
				point.LineBefore = NewLineInsertion.BlankLine;
			if (point.Location.Column < line.Length + 1)
				point.LineAfter = NewLineInsertion.Eol;
		}

		static void CheckStartPoint (IReadonlyTextDocument doc, InsertionPoint point, bool isEndPoint)
		{
			var line = doc.GetLine (point.Location.Line);
			if (line == null)
				return;
			if (doc.GetLineIndent (line).Length + 1 == point.Location.Column) {
				int lineNr = point.Location.Line;
				while (lineNr > 1 && doc.GetLineIndent (lineNr - 1).Length == doc.GetLine (lineNr - 1).Length) {
					lineNr--;
				}
				line = doc.GetLine (lineNr);
				point.Location = new DocumentLocation (lineNr, doc.GetLineIndent (line).Length + 1);
			}

			if (doc.GetLineIndent (line).Length + 1 < point.Location.Column)
				point.LineBefore = NewLineInsertion.Eol;
			if (point.Location.Column < line.Length + 1)
				point.LineAfter = isEndPoint ? NewLineInsertion.Eol : NewLineInsertion.BlankLine;
		}

		static InsertionPoint GetInsertionPosition (IReadonlyTextDocument doc, int line, int column)
		{
			int bodyEndOffset = doc.LocationToOffset (line, column) + 1;
			var curLine = doc.GetLine (line);
			if (curLine != null) {
				if (bodyEndOffset < curLine.Offset + curLine.Length) {
					// case1: positition is somewhere inside the start line
					return new InsertionPoint (new DocumentLocation (line, column + 1), NewLineInsertion.Eol, NewLineInsertion.BlankLine);
				}
			}

			// -> if position is at line end check next line
			var nextLine = doc.GetLine (line + 1);
			if (nextLine == null) // check for 1 line case.
				return new InsertionPoint (new DocumentLocation (line, column + 1), NewLineInsertion.BlankLine, NewLineInsertion.BlankLine);

			for (int i = nextLine.Offset; i < nextLine.EndOffset; i++) {
				char ch = doc.GetCharAt (i);
				if (!char.IsWhiteSpace (ch)) {
					// case2: next line contains non ws chars.
					return new InsertionPoint (new DocumentLocation (line + 1, 1), NewLineInsertion.Eol, NewLineInsertion.BlankLine);
				}
			}

			var nextLine2 = doc.GetLine (line + 2);
			if (nextLine2 != null) {
				for (int i = nextLine2.Offset; i < nextLine2.EndOffset; i++) {
					char ch = doc.GetCharAt (i);
					if (!char.IsWhiteSpace (ch)) {
						// case3: one blank line
						return new InsertionPoint (new DocumentLocation (line + 1, 1), NewLineInsertion.Eol, NewLineInsertion.Eol);
					}
				}
			}
			// case4: more than 1 blank line
			return new InsertionPoint (new DocumentLocation (line + 1, 1), NewLineInsertion.Eol, NewLineInsertion.None);
		}

		internal static InsertionPoint GetSuitableInsertionPoint (IReadonlyTextDocument data, IEnumerable<InsertionPoint> points, ITypeSymbol cls, Location part, SyntaxNode member)
		{
			switch (member.Kind ()) {
			case SyntaxKind.FieldDeclaration:
				return GetNewFieldPosition (data, points, cls, part);
			case SyntaxKind.MethodDeclaration:
			case SyntaxKind.ConstructorDeclaration:
			case SyntaxKind.DestructorDeclaration:
			case SyntaxKind.OperatorDeclaration:
				return GetNewMethodPosition (data, points, cls, part);
			case SyntaxKind.EventDeclaration:
				return GetNewEventPosition (data, points, cls, part);
			case SyntaxKind.PropertyDeclaration:
				return GetNewPropertyPosition (data, points, cls, part);
			}
			throw new InvalidOperationException ("Invalid member type: " + member.Kind ());
		}

		static InsertionPoint GetNewFieldPosition (IReadonlyTextDocument data, IEnumerable<InsertionPoint> points, ITypeSymbol cls, Location part)
		{
			if (!cls.GetMembers ().OfType<IFieldSymbol> ().Any ()) 
				return points.FirstOrDefault ();
			var lastField = cls.GetMembers ().OfType<IFieldSymbol> ().Last ();
			var begin = data.OffsetToLocation (lastField.Locations.First ().SourceSpan.Start);
			return points.FirstOrDefault (p => p.Location > begin);
		}

		static InsertionPoint GetNewMethodPosition (IReadonlyTextDocument data, IEnumerable<InsertionPoint> points, ITypeSymbol cls, Location part)
		{
			if (!cls.GetMembers ().OfType<IMethodSymbol> ().Any ()) 
				return GetNewPropertyPosition (data, points, cls, part);
			var lastMethod = cls.GetMembers ().OfType<IMethodSymbol> ().Last ();
			var begin = data.OffsetToLocation (lastMethod.Locations.First ().SourceSpan.Start);
			return points.FirstOrDefault (p => p.Location > begin);
		}

		static InsertionPoint GetNewPropertyPosition (IReadonlyTextDocument data, IEnumerable<InsertionPoint> points, ITypeSymbol cls, Location part)
		{
			if (!cls.GetMembers ().OfType<IPropertySymbol> ().Any ()) 
				return GetNewFieldPosition (data, points, cls, part);
			var lastProperty = cls.GetMembers ().OfType<IPropertySymbol> ().Last ();
			var begin = data.OffsetToLocation (lastProperty.Locations.First ().SourceSpan.Start);
			return points.FirstOrDefault (p => p.Location > begin);
		}

		static InsertionPoint GetNewEventPosition (IReadonlyTextDocument data, IEnumerable<InsertionPoint> points, ITypeSymbol cls, Location part)
		{
			if (!cls.GetMembers ().OfType<IEventSymbol> ().Any ()) 
				return GetNewMethodPosition (data, points, cls, part);
			var lastEvent = cls.GetMembers ().OfType<IEventSymbol> ().Last ();
			var begin = data.OffsetToLocation (lastEvent.Locations.First ().SourceSpan.Start);
			return points.FirstOrDefault (p => p.Location > begin);
		}
	}
}

