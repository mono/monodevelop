// 
// RemoveBackingStore.cs
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
	public class RemoveBackingStore : RefactoringOperation
	{
		public override string GetMenuDescription (RefactoringOptions options)
		{
			return GettextCatalog.GetString ("_Remove backing store");
		}
		
		public override bool IsValid (RefactoringOptions options)
		{
			MemberResolveResult resolveResult = options.ResolveResult as MemberResolveResult;
			if (resolveResult == null)
				return false;
			IProperty property = resolveResult.ResolvedMember as IProperty;
			if (property == null)
				return false;
			
			TextEditorData data = options.GetTextEditorData ();
			if (property.HasGet && data.Document.GetCharAt (data.Document.LocationToOffset (property.GetRegion.End.Line - 1, property.GetRegion.End.Column - 2)) == ';')
				return false;
			if (property.HasSet && data.Document.GetCharAt (data.Document.LocationToOffset (property.SetRegion.End.Line - 1, property.SetRegion.End.Column - 2)) == ';')
				return false;
			
			return true;
		}
		
		public override List<Change> PerformChanges (RefactoringOptions options, object prop)
		{
			List<Change> result = new List<Change> ();
			
			MemberResolveResult resolveResult = options.ResolveResult as MemberResolveResult;
			IProperty property = resolveResult.ResolvedMember as IProperty;
			TextEditorData data = options.GetTextEditorData ();
			INRefactoryASTProvider astProvider = options.GetASTProvider ();
			string backingStoreName = char.ToLower (property.Name[0]) + property.Name.Substring (1);
			
			List<IMember> members = options.ResolveResult.CallingType.SearchMember (backingStoreName, true);
			foreach (IMember member in members) {
				if (member.MemberType == MemberType.Field) {
					DocumentLocation location = member.Location.ToDocumentLocation (data.Document);
					int startOffset = data.Document.LocationToOffset (location);
					LineSegment line = data.Document.GetLine (location.Line);
					int endOffset = line.Offset + line.EditableLength;
					
					result.Add (new TextReplaceChange () {
						FileName = options.Document.FileName,
						Offset = startOffset,
						RemovedChars = endOffset - startOffset
					});
					break;
				}
			}
			
			if (property.HasGet) {
				int startOffset = data.Document.LocationToOffset (property.GetRegion.Start.ToDocumentLocation (data.Document));
				int endOffset = data.Document.LocationToOffset (property.GetRegion.End.ToDocumentLocation (data.Document));
				
				string text = astProvider.OutputNode (options.Dom, new PropertyGetRegion (null, null), options.GetIndent (property) + "\t").Trim ();
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
				string text = astProvider.OutputNode (options.Dom, new PropertySetRegion (null, null), options.GetIndent (property) + "\t").Trim ();
				result.Add (new TextReplaceChange () {
					FileName = options.Document.FileName,
					Offset = startOffset,
					RemovedChars = endOffset - startOffset,
					InsertedText = text
				});
			}
			
			return result;
		}
	}
}
