// 
// ReferencesFinder.cs
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


/*
 
 This is a new approach for speeding up find all reference - it does a full text search & then it resolves the found expression.
 Unfortunately this isn't as exact as the current approach - but this should be used if the resolver is more exact & capable of
 'looking around'.
 
using System;
using System.Collections.Generic;
using Mono.TextEditor;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Core;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Ide;

namespace MonoDevelop.Refactoring.References
{
	public class MemberReference
	{
		public FilePath FileName {
			get;
			set;
		}
		
		public ITextEditorDataProvider DataProvider {
			get;
			set;
		}
		
		public DocumentLocation Location {
			get;
			set;
		}
		
		public int Offset {
			get;
			set;
		}
		
		public int Length {
			get;
			set;
		}
		
		public MemberReference (FilePath fileName, ITextEditorDataProvider dataProvider, DocumentLocation location, int offset, int length)
		{
			this.FileName = fileName;
			this.DataProvider = dataProvider;
			this.Location = location;
			this.Offset = offset;
			this.Length = length;
		}

	}
	
	public enum RefactoryScope
	{
		File,
		Project,
		Solution,
		DeclaringType
	}
	
	public static class ReferencesFinder
	{
		public static IEnumerable<MemberReference> GetReferences (IMember member)
		{
			IType type = member is IType ? (IType)member : member.DeclaringType;
			if (type == null) {
				LoggingService.LogError ("Declaring type of '" + member + "' not found.");
				yield break;
			}
			var matcher = GetReferenceMatcher (member);
			foreach (var fileInfo in GetFileNames (member)) {
				var textFile = TextFileProvider.Instance.GetEditableTextFile (fileInfo.FileName);
				if (textFile.Text == null) // file not found
					continue;
				var expressionFinder = ProjectDomService.GetExpressionFinder (fileInfo.FileName);
				
				foreach (var match in matcher.Search (textFile.Text)) {
					var data = (textFile as ITextEditorDataProvider).GetTextEditorData ();
					ExpressionResult expr = expressionFinder.FindFullExpression (data, match.Offset);
					if (expr.Expression == null) 
						continue;
					
					var resolver = ProjectDomService.GetParser (fileInfo.FileName).CreateResolver (fileInfo.Dom, data, fileInfo.FileName);
					var location = data.Document.OffsetToLocation (match.Offset);
					ResolveResult resolveResult = resolver.Resolve (expr, new DomLocation (location.Line, location.Column));
					
					// TODO: Add IsReferenceTo in the resolve results - could could be taken from FindMemberAstVisitor
					if (resolveResult != null && resolveResult.IsReferenceTo (member))
						yield return new MemberReference (fileInfo.FileName, (textFile as ITextEditorDataProvider), location, match.Position, match.Length);
				}
			}
		}
		
		public static  IReferenceMatcher GetReferenceMatcher (IMember member)
		{
			IMethod method = member as IMethod;
			if (method != null) {
				if (method.IsConstructor) {
					return new AggregatedReferenceMatcher (
						new WholeWordMatcher (member.DeclaringType.Name),
						new WholeWordMatcher ("this"),
						new WholeWordMatcher ("base")
					);
				}
				if (method.IsFinalizer)
					return new WholeWordMatcher (member.DeclaringType.Name);
			}
			
			IProperty property = member as IProperty;
			if (property != null && property.IsIndexer) 
				return new IndexBeforeReferenceMatcher ("[");
				
			return new WholeWordMatcher (member.Name);
		}
		
		
		
		
		static RefactoryScope GetScope (IMember member)
		{
			if (member.DeclaringType != null && member.DeclaringType.ClassType == ClassType.Interface)
				return GetScope (member.DeclaringType);
			
			if (member.IsPublic)
				return RefactoryScope.Solution;
			
			if (member.IsProtected || member.IsInternal || member.DeclaringType == null)
				return RefactoryScope.Project;
			return RefactoryScope.DeclaringType;
		}
	}
	
	public struct ReferenceMatch
	{
		public readonly int Offset;
		public readonly int Length;

		public ReferenceMatch (int offset, int length)
		{
			this.Offset = offset;
			this.Length = length;
		}
	}
 
	public interface IReferenceMatcher
	{
		IEnumerable<ReferenceMatch> Search (string inputText);
	}
	
	class WholeWordMatcher : IReferenceMatcher
	{
		string pattern;

		public WholeWordMatcher (string pattern)
		{
			this.pattern = pattern;
		}

		public IEnumerable<ReferenceMatch> Search (string inputText)
		{
			if (pattern.Length == 0)
				yield break;
			int pos = -1;
			while ((pos = inputText.IndexOf (pattern, pos + 1)) >= 0) {
				if (pos > 0 && char.IsLetterOrDigit (inputText, pos - 1) ||
					pos < inputText.Length - pattern.Length - 1 && char.IsLetterOrDigit (inputText, pos + pattern.Length))
					continue;
				yield return new ReferenceMatch (pos, pattern.Length);
			}
		}
	}
	
	class AggregatedReferenceMatcher : IReferenceMatcher
	{
		IEnumerable<IReferenceMatcher> referenceMatcher;
		
		public AggregatedReferenceMatcher (params IReferenceMatcher[] matcher)
		{
			this.referenceMatcher = matcher;
		}
		
		public AggregatedReferenceMatcher (IEnumerable<IReferenceMatcher> matcher)
		{
			this.referenceMatcher = matcher;
		}
		
		public IEnumerable<ReferenceMatch> Search (string inputText)
		{
			foreach (IReferenceMatcher matcher in referenceMatcher) {
				foreach (ReferenceMatch match in matcher.Search (inputText)) {
					yield return match;
				}
			}
		}
	}
	
	class IndexBeforeReferenceMatcher : IReferenceMatcher
	{
		string tag;
		
		public IndexBeforeReferenceMatcher (string tag)
		{
			this.tag = tag;
		}
		
		public IEnumerable<ReferenceMatch> Search (string inputText)
		{
			int pos = -1;
			while ((pos = inputText.IndexOf (tag, pos + 1)) >= 0) {
				yield return new ReferenceMatch (pos - 1, tag.Length);
			}
		}
	}
}*/