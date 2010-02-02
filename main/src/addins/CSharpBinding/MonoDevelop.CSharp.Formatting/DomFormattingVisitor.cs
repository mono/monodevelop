// 
// DomFormattingVisitor.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.CSharp.Dom;
using System.Text;
using MonoDevelop.Projects.Dom;
using Mono.TextEditor;

namespace MonoDevelop.CSharp.Formatting
{
	public class DomFormattingVisitor :  AbtractCSharpDomVisitor<object, object>
	{
		CSharpFormattingPolicy policy;
		TextEditorData data;
		
		public DomFormattingVisitor (CSharpFormattingPolicy policy, TextEditorData data)
		{
			this.policy = policy;
			this.data = data;
		}
		
		public override object VisitTypeDeclaration (TypeDeclaration typeDeclaration, object data)
		{
			BraceStyle braceStyle;
			switch (typeDeclaration.ClassType) {
			case ClassType.Class:
				braceStyle = policy.ClassBraceStyle;
				break;
			case ClassType.Struct:
				braceStyle = policy.StructBraceStyle;
				break;
			case ClassType.Interface:
				braceStyle = policy.InterfaceBraceStyle;
				break;
			case ClassType.Enum:
				braceStyle = policy.EnumBraceStyle;
				break;
			default:
				throw new InvalidOperationException ("unsupported class type : " + typeDeclaration.ClassType);
			}
			EnforceBraceStyle (braceStyle, (CSharpTokenNode)typeDeclaration.GetChildByRole (AbstractNode.Roles.LBrace), (CSharpTokenNode)typeDeclaration.GetChildByRole (AbstractNode.Roles.RBrace));
			
			return null;
		}
		
		int GetLastNonWsChar (LineSegment line, int lastColumn)
		{
			int result = -1;
			bool inComment = false;
			for (int i = 0; i < lastColumn; i++) {
				char ch = data.Document.GetCharAt (line.Offset + i);
				if (Char.IsWhiteSpace (ch))
					continue;
				if (ch == '/' && i + 1 < line.EditableLength && data.Document.GetCharAt (line.Offset + i + 1) == '/')
					return result;
				if (ch == '/' && i + 1 < line.EditableLength && data.Document.GetCharAt (line.Offset + i + 1) == '*') {
					inComment = true;
					i++;
					continue;
				}
				if (inComment && ch == '*' && i + 1 < line.EditableLength && data.Document.GetCharAt (line.Offset + i + 1) == '/') {
					inComment = false;
					i++;
					continue;
				}
				if (!inComment)
					result = i;
			}
			return result;
		}
		
		void EnforceBraceStyle (MonoDevelop.CSharp.Formatting.BraceStyle braceStyle, CSharpTokenNode lbrace, CSharpTokenNode rbrace)
		{
			LineSegment lbraceLineSegment = data.Document.GetLine (lbrace.Location.Line);
			string indent = lbraceLineSegment.GetIndentation (data.Document);
			
			LineSegment rbraceLineSegment = data.Document.GetLine (rbrace.Location.Line);
			
			switch (braceStyle) {
			case BraceStyle.EndOfLine:
				LineSegment curLine = lbraceLineSegment;
				int curColumn = lbrace.Location.Column;
				
				int lastNonWsChar = GetLastNonWsChar (curLine, curColumn);
				if (lastNonWsChar == -1) {
					if (curColumn < lbraceLineSegment.EditableLength) {
						data.Remove (lbraceLineSegment.Offset, curColumn - 1);
					} else {
						data.Remove (lbraceLineSegment.Offset, lbraceLineSegment.Length);
					}
					do {
						curLine = data.Document.GetLineByOffset (curLine.Offset - 1);
						curColumn = curLine.EditableLength;
					} while (-1 == (lastNonWsChar = GetLastNonWsChar (curLine, curColumn)));
					data.Insert (curLine.Offset + lastNonWsChar, " {");
				}
				break;
			case BraceStyle.NextLine:
				break;
			case BraceStyle.NextLineShifted:
				break;
			case BraceStyle.NextLineShifted2:
				break;
			}
			
			int firstNonWsChar = rbrace.Location.Column - 1;
			
			for (; firstNonWsChar >= 0; firstNonWsChar--) {
				char ch = data.Document.GetCharAt (rbraceLineSegment.Offset);
				if (!Char.IsWhiteSpace (ch))
					break;
			}
			data.Remove (rbraceLineSegment.Offset + firstNonWsChar, rbrace.Location.Column - firstNonWsChar - 1);
			data.Insert (rbraceLineSegment.Offset + firstNonWsChar, firstNonWsChar != 0 ? data.EolMarker + indent : indent);
		}
	}
}

