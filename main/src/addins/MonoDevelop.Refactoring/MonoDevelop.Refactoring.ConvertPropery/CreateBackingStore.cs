// 
// CreateBackingStore.cs
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
using MonoDevelop.Core;
using Mono.TextEditor;
using Mono.TextEditor.Highlighting;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Dom;

namespace MonoDevelop.Refactoring.ConvertPropery
{
	public class CreateBackingStore : RefactoringOperation
	{
		public override string GetMenuDescription (RefactoringOptions options)
		{
			return GettextCatalog.GetString ("_Create backing store");
		}
		
		public override bool IsValid (RefactoringOptions options)
		{
			MemberResolveResult resolveResult = options.ResolveResult as MemberResolveResult;
			if (resolveResult == null)
				return false;
			IProperty property = resolveResult.ResolvedMember as IProperty;
			if (property == null || resolveResult.CallingMember == null || resolveResult.CallingMember.FullName != property.FullName || !property.HasGet || property.DeclaringType == null)
				return false;
			TextEditorData data = options.GetTextEditorData ();
			if (property.HasGet && data.Document.GetCharAt (data.Document.LocationToOffset (property.GetRegion.End.Line - 1, property.GetRegion.End.Column - 2)) != ';')
				return false;
			if (property.HasSet && data.Document.GetCharAt (data.Document.LocationToOffset (property.SetRegion.End.Line - 1, property.SetRegion.End.Column - 2)) != ';')
				return false;
			return true;
		}
		
		public override void Run (RefactoringOptions options)
		{
			base.Run (options);
			
			TextEditorData data = options.GetTextEditorData ();
				
			Mono.TextEditor.TextEditor editor = MonoDevelop.Refactoring.Rename.RenameRefactoring.GetEditor (options.Document.ActiveView.Control);
				
			List<TextLink> links = new List<TextLink> ();
			TextLink link = new TextLink ("name");
			int referenceCount = 1;
			MemberResolveResult resolveResult = options.ResolveResult as MemberResolveResult;
			IProperty property = resolveResult.ResolvedMember as IProperty;
			if (property.HasGet)
				referenceCount++;
			if (property.HasSet)
				referenceCount++;
			for (int i = refactoringStartOffset; i < data.Document.Length - backingStoreName.Length; i++) {
				if (data.Document.GetTextAt (i, backingStoreName.Length) == backingStoreName) {
					link.AddLink (new Segment (i - refactoringStartOffset, backingStoreName.Length));
					if (link.Count == referenceCount)
						break;
				}
			}
			
			links.Add (link);
			TextLinkEditMode tle = new TextLinkEditMode (editor, refactoringStartOffset, links);
			tle.SetCaretPosition = false;
			if (tle.ShouldStartTextLinkMode) {
				tle.OldMode = data.CurrentMode;
				tle.StartMode ();
				data.CurrentMode = tle;
			}
		}
		
		string backingStoreName;
		int refactoringStartOffset;
		public override List<Change> PerformChanges (RefactoringOptions options, object prop)
		{
			List<Change> result = new List<Change> ();
			
			MemberResolveResult resolveResult = options.ResolveResult as MemberResolveResult;
			IProperty property = resolveResult.ResolvedMember as IProperty;
			TextEditorData data = options.GetTextEditorData ();
			INRefactoryASTProvider astProvider = options.GetASTProvider ();
			backingStoreName = GetBackingStoreName (property);
			
			FieldDeclaration backingStore = new FieldDeclaration (null);
			backingStore.TypeReference = property.ReturnType.ConvertToTypeReference ();
			backingStore.Fields.Add (new VariableDeclaration (backingStoreName));
			DocumentLocation location = property.Location.ToDocumentLocation (data.Document);
			location.Column = 0;
			refactoringStartOffset = data.Document.LocationToOffset (location);
			
			result.Add (new TextReplaceChange () {
				FileName = options.Document.FileName,
				Offset = refactoringStartOffset,
				InsertedText = astProvider.OutputNode (options.Dom, backingStore, options.GetIndent (property))
			});
			
			if (property.HasGet) {
				int startOffset = data.Document.LocationToOffset (property.GetRegion.Start.ToDocumentLocation (data.Document));
				int endOffset = data.Document.LocationToOffset (property.GetRegion.End.ToDocumentLocation (data.Document));
				
				BlockStatement getBlock = new BlockStatement ();
				getBlock.AddChild (new ReturnStatement (new IdentifierExpression (backingStoreName)));
				string text = astProvider.OutputNode (options.Dom, new PropertyGetRegion (getBlock, null), options.GetIndent (property) + "\t").Trim ();
				result.Add (new TextReplaceChange () {
					FileName = options.Document.FileName,
					Offset = startOffset,
					RemovedChars = endOffset - startOffset,
					InsertedText = text
				});
			}
			
			if (property.HasSet) {
				int startOffset = data.Document.LocationToOffset (property.SetRegion.Start.ToDocumentLocation (data.Document));
				int endOffset = data.Document.LocationToOffset (property.SetRegion.End.ToDocumentLocation (data.Document));
				BlockStatement setBlock = new BlockStatement ();
				setBlock.AddChild (new ExpressionStatement (new AssignmentExpression (new IdentifierExpression (backingStoreName), AssignmentOperatorType.Assign, new IdentifierExpression ("value"))));
				string text = astProvider.OutputNode (options.Dom, new PropertySetRegion (setBlock, null), options.GetIndent (property) + "\t").Trim ();
				result.Add (new TextReplaceChange () {
					FileName = options.Document.FileName,
					Offset = startOffset,
					RemovedChars = endOffset - startOffset,
					InsertedText = text
				});
			}
			
			return result;
		}

		static string GetBackingStoreName (MonoDevelop.Projects.Dom.IProperty property)
		{
			string baseName = char.ToLower (property.Name[0]) + property.Name.Substring (1);
			int    number   = -1;
			IType type = property.DeclaringType;
			if (type != null) {
				List<IMember> members;
				do {
					number++;
					members = type.SearchMember (GenNumberedName (baseName, number), true);
				} while (members != null && members.Count > 0);
			}
			
			return GenNumberedName (baseName, number);
		}

		static string GenNumberedName (string baseName, int number)
		{
//			if (number == 1)
//				return "_" + baseName;
//			if (number == 2)
//				return "m_" + baseName;
			return baseName + (number > 0 ? number.ToString () : "");
		}


	}
}
