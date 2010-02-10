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
using MonoDevelop.Refactoring;
using System.Collections.Generic;

namespace MonoDevelop.CSharp.Formatting
{
	public class DomFormattingVisitor :  AbtractCSharpDomVisitor<object, object>
	{
		CSharpFormattingPolicy policy;
		TextEditorData data;
		List<Change> changes = new List<Change> ();
		public DomFormattingVisitor (CSharpFormattingPolicy policy, TextEditorData data)
		{
			this.policy = policy;
			this.data = data;
		}
		
		class MyTextReplaceChange : TextReplaceChange
		{
			TextEditorData data;
			protected override TextEditorData TextEditorData {
				get {
					return data;
				}
			}
			
			public MyTextReplaceChange (TextEditorData data, int offset, int count, string replaceWith)
			{
				this.data = data;
				this.FileName = data.Document.FileName;
				this.Offset = offset;
				this.RemovedChars = count;
				this.InsertedText = replaceWith;
			}
		}
		
		public override object VisitCompilationUnit (MonoDevelop.CSharp.Dom.CompilationUnit unit, object data)
		{
			base.VisitCompilationUnit (unit, data);
			RefactoringService.AcceptChanges (null, null, changes);
			return null;
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
			EnforceBraceStyle (braceStyle, 
			                   ((CSharpTokenNode)typeDeclaration.GetChildByRole (TypeDeclaration.TypeKeyword)).Location, 
			                   (CSharpTokenNode)typeDeclaration.GetChildByRole (AbstractNode.Roles.LBrace), 
			                   (CSharpTokenNode)typeDeclaration.GetChildByRole (AbstractNode.Roles.RBrace));
			
			return base.VisitTypeDeclaration (typeDeclaration, data);
		}
		
		public override object VisitPropertyDeclaration (PropertyDeclaration propertyDeclaration, object data)
		{
			EnforceBraceStyle (policy.PropertyBraceStyle, 
			                   ((FullTypeName)propertyDeclaration.GetChildByRole (PropertyDeclaration.Roles.ReturnType)).Location, 
			                   (CSharpTokenNode)propertyDeclaration.GetChildByRole (PropertyDeclaration.Roles.LBrace), 
			                   (CSharpTokenNode)propertyDeclaration.GetChildByRole (PropertyDeclaration.Roles.RBrace));
		
			return base.VisitPropertyDeclaration (propertyDeclaration, data);
		}
		
		public override object VisitIndexerDeclaration (IndexerDeclaration indexerDeclaration, object data)
		{
			EnforceBraceStyle (policy.PropertyBraceStyle, 
			                   ((FullTypeName)indexerDeclaration.GetChildByRole (IndexerDeclaration.Roles.ReturnType)).Location, 
			                   (CSharpTokenNode)indexerDeclaration.GetChildByRole (IndexerDeclaration.Roles.LBrace), 
			                   (CSharpTokenNode)indexerDeclaration.GetChildByRole (IndexerDeclaration.Roles.RBrace));
			return base.VisitIndexerDeclaration (indexerDeclaration, data);
		}


		public override object VisitFieldDeclaration (FieldDeclaration fieldDeclaration, object data)
		{
			foreach (INode node in fieldDeclaration.Children) {
				if (node is VariableInitializer && node.NextSibling != null && node.NextSibling.Role == FieldDeclaration.Roles.Comma) {
					VariableInitializer initializer = node as VariableInitializer;
					CSharpTokenNode commaToken = (CSharpTokenNode)node.NextSibling;
					int offset      = this.data.Document.LocationToOffset (initializer.NameIdentifier.Location.Line, initializer.NameIdentifier.Location.Column);
					int commaOffset = this.data.Document.LocationToOffset (commaToken.Location.Line, commaToken.Location.Column);
					ForceSpace (offset, commaOffset, policy.SpacesAfterComma);
					Console.WriteLine (initializer.Name +"/" + initializer.NameIdentifier.Location + "/" + commaToken.Location);
					
					if (node.NextSibling.NextSibling is VariableInitializer) {
						DomLocation location = ((VariableInitializer)node.NextSibling.NextSibling).NameIdentifier.Location;
						int nextOffset = this.data.Document.LocationToOffset (location.Line, location.Column);
						ForceSpace (commaOffset, nextOffset, policy.SpacesAfterComma);
					}
				}
			}
			return base.VisitFieldDeclaration (fieldDeclaration, data);
		}
		
		public override object VisitDelegateDeclaration (DelegateDeclaration delegateDeclaration, object data)
		{
			CSharpTokenNode lParen = (CSharpTokenNode)delegateDeclaration.GetChildByRole (DelegateDeclaration.Roles.LPar);
			int offset = this.data.Document.LocationToOffset (lParen.Location.Line, lParen.Location.Column);
			ForceSpaceBefore (offset, policy.BeforeDelegateDeclarationParentheses);
			return base.VisitDelegateDeclaration (delegateDeclaration, data);
		}
		
		public override object VisitMethodDeclaration (MethodDeclaration methodDeclaration, object data)
		{
			CSharpTokenNode lParen = (CSharpTokenNode)methodDeclaration.GetChildByRole (MethodDeclaration.Roles.LPar);
			int offset = this.data.Document.LocationToOffset (lParen.Location.Line, lParen.Location.Column);
			ForceSpaceBefore (offset, policy.BeforeMethodDeclarationParentheses);
			return base.VisitMethodDeclaration (methodDeclaration, data);
		}
		
		public override object VisitConstructorDeclaration (ConstructorDeclaration constructorDeclaration, object data)
		{
			CSharpTokenNode lParen = (CSharpTokenNode)constructorDeclaration.GetChildByRole (ConstructorDeclaration.Roles.LPar);
			int offset = this.data.Document.LocationToOffset (lParen.Location.Line, lParen.Location.Column);
			ForceSpaceBefore (offset, policy.BeforeConstructorDeclarationParentheses);
			
			return base.VisitConstructorDeclaration (constructorDeclaration, data);
		}
		
		public override object VisitDestructorDeclaration (DestructorDeclaration destructorDeclaration, object data)
		{
			CSharpTokenNode lParen = (CSharpTokenNode)destructorDeclaration.GetChildByRole (DestructorDeclaration.Roles.LPar);
			int offset = this.data.Document.LocationToOffset (lParen.Location.Line, lParen.Location.Column);
			ForceSpaceBefore (offset, policy.BeforeConstructorDeclarationParentheses);
			return base.VisitDestructorDeclaration (destructorDeclaration, data);
		}
		
		void ForceSpaceBefore (int offset, bool forceSpace)
		{
			bool insertedSpace = false;
			do {
				char ch = data.Document.GetCharAt (offset);
				Console.WriteLine (ch);
				if (!Char.IsWhiteSpace (ch) && (insertedSpace || !forceSpace))
					break;
				if (ch == ' ' && forceSpace) {
					if (insertedSpace) {
						changes.Add (new MyTextReplaceChange (data, offset, 1, null));
					} else {
						insertedSpace = true;
					}
				} else if (forceSpace) {
					if (!insertedSpace) {
						changes.Add (new MyTextReplaceChange (data, offset, Char.IsWhiteSpace (ch) ? 1 :  0, " "));
						insertedSpace = true;
					} else if (Char.IsWhiteSpace (ch)) {
						changes.Add (new MyTextReplaceChange (data, offset, 1, null));
					}
				}
				
				offset--;
			} while (offset >= 0);
		}

		void ForceSpace (int startOffset, int endOffset, bool spaceBefore)
		{
			int lastNonWs = SearchLastNonWsChar (startOffset, endOffset);
			changes.Add (new MyTextReplaceChange (data, lastNonWs + 1, System.Math.Max (0, endOffset - lastNonWs - 1), spaceBefore ? " " : ""));
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
		
		int SearchLastNonWsChar (int startOffset, int endOffset)
		{
			if (startOffset >= endOffset)
				return startOffset;
			int result = -1;
			bool inComment = false;
			Console.WriteLine ("----");
			for (int i = startOffset; i < endOffset; i++) {
				char ch = data.Document.GetCharAt (i);
				Console.WriteLine (ch);
				if (Char.IsWhiteSpace (ch))
					continue;
				if (ch == '/' && i + 1 < data.Document.Length && data.Document.GetCharAt (i + 1) == '/')
					return result;
				if (ch == '/' && i + 1 < data.Document.Length && data.Document.GetCharAt (i + 1) == '*') {
					inComment = true;
					i++;
					continue;
				}
				if (inComment && ch == '*' && i + 1 < data.Document.Length && data.Document.GetCharAt (i + 1) == '/') {
					inComment = false;
					i++;
					continue;
				}
				if (!inComment)
					result = i;
			}
			return result;
		}
		
		string SingleIndent {
			get {
				return "\t";
			}
		}
		void EnforceBraceStyle (MonoDevelop.CSharp.Formatting.BraceStyle braceStyle, DomLocation startLocation, CSharpTokenNode lbrace, CSharpTokenNode rbrace)
		{
			
			LineSegment lbraceLineSegment = data.Document.GetLine (lbrace.Location.Line);
			string indent = lbraceLineSegment.GetIndentation (data.Document);
			
			LineSegment rbraceLineSegment = data.Document.GetLine (rbrace.Location.Line);
			
			int lbraceOffset = data.Document.LocationToOffset (lbrace.Location.Line, lbrace.Location.Column);
			Console.WriteLine ("lbraceOffset={0}/" + data.Document.Length, lbraceOffset);
			int lastNonWsChar = SearchLastNonWsChar (data.Document.LocationToOffset (startLocation.Line, startLocation.Column), lbraceOffset);
			
			switch (braceStyle) {
			case BraceStyle.EndOfLineWithoutSpace:
				changes.Add (new MyTextReplaceChange (data, lastNonWsChar + 1, lbraceOffset - lastNonWsChar - 1, null));
				break;
			case BraceStyle.EndOfLine:
				changes.Add (new MyTextReplaceChange (data, lastNonWsChar + 1, lbraceOffset - lastNonWsChar - 1, " "));
				break;
			case BraceStyle.NextLine:
				changes.Add (new MyTextReplaceChange (data, lastNonWsChar + 1, lbraceOffset - lastNonWsChar - 1, data.EolMarker));
				break;
			case BraceStyle.NextLineShifted:
				indent += SingleIndent;
				changes.Add (new MyTextReplaceChange (data, lastNonWsChar + 1, lbraceOffset - lastNonWsChar - 1, data.EolMarker + SingleIndent));
				break;
			case BraceStyle.NextLineShifted2:
				indent += SingleIndent;
				changes.Add (new MyTextReplaceChange (data, lastNonWsChar + 1, lbraceOffset - lastNonWsChar - 1, data.EolMarker + SingleIndent));
				break;
			}
			
			int firstNonWsChar = rbrace.Location.Column - 1;
			
			for (; firstNonWsChar >= 0; firstNonWsChar--) {
				char ch = data.Document.GetCharAt (rbraceLineSegment.Offset + firstNonWsChar);
				Console.WriteLine (ch);
				if (!Char.IsWhiteSpace (ch))
					break;
			}
			
			changes.Add (new MyTextReplaceChange (data, rbraceLineSegment.Offset + firstNonWsChar + 1, rbrace.Location.Column - firstNonWsChar - 1, firstNonWsChar > 0 ? data.EolMarker + indent : indent));
		}
	}
}
