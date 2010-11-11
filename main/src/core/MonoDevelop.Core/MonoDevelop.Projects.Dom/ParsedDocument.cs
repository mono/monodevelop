// IParsedDocument.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//

using System;
using System.Collections.Generic;
using System.Text;

namespace MonoDevelop.Projects.Dom
{
	[Flags]
	public enum ParsedDocumentFlags
	{
		None            = 0,
		NonSerializable = 1
	}
	
	public class ParsedDocument
	{
		DateTime parseTime = DateTime.Now;
		
		List<Comment> comments = new List<Comment> ();
		List<FoldingRegion> folds = new List<FoldingRegion> ();
		List<Tag> tagComments = new List<Tag> ();
		List<PreProcessorDefine> defines = new List<PreProcessorDefine> ();
		List<ConditionalRegion> conditionalRegions = new List<ConditionalRegion> ();
		
		bool hasErrors = false;
		List<Error> errors = new List<Error> ();
		
		public ParsedDocument (string fileName)
		{
			this.FileName = fileName;
		}
		
		public ParsedDocumentFlags Flags {
			get; set;
		}
		
		public string FileName {
			get; private set;
		}
		
		public DateTime ParseTime {
			get {
				return parseTime;
			}
		}
		
		public IList<Tag> TagComments {
			get {
				return tagComments;
			}
		}
		
		public IList<Comment> Comments {
			get {
				return comments;
			}
		}
		
		public IEnumerable<FoldingRegion> UserRegions {
			get {
				foreach (FoldingRegion fold in AdditionalFolds)
					if (fold.Type == FoldType.UserRegion)
						yield return fold;
//				return from FoldingRegion fold in Folds
//				       where fold.Type == FoldType.UserRegion
//				       select fold;
			}
		}
		
		
		public IList<FoldingRegion> AdditionalFolds {
			get {
				return folds;
			}
		}
		
		public IList<PreProcessorDefine> Defines {
			get {
				return defines;
			}
		}
		
		public IList<ConditionalRegion> ConditionalRegions {
			get {
				return conditionalRegions;
			}
		}
		
		public IList<Error> Errors {
			get {
				return errors;
			}
		}
		
		public bool HasErrors {
			get {
				return hasErrors;
			}
		}
		
		public ICompilationUnit CompilationUnit { get; set; }
		
		public virtual IEnumerable<FoldingRegion> GenerateFolds ()
		{
			foreach (FoldingRegion fold in AdditionalFolds)
				yield return fold;
			
			foreach (FoldingRegion fold in ConditionalRegions.ToFolds ())
				yield return fold;
			
			IEnumerable<FoldingRegion> commentFolds = comments.ToFolds ();
			if (CompilationUnit != null && CompilationUnit.Types != null && CompilationUnit.Types.Count > 0) {
				commentFolds = commentFolds.FlagIfInsideMembers (CompilationUnit.Types, delegate (FoldingRegion f) {
					f.Type = FoldType.CommentInsideMember;
				});
			}
			foreach (FoldingRegion fold in commentFolds)
				yield return fold;
			
			if (CompilationUnit == null)
				yield break;
			
			FoldingRegion usingFold = CompilationUnit.Usings.ToFold ();
			if (usingFold != null)
				yield return usingFold;
			
			foreach (FoldingRegion fold in CompilationUnit.Types.ToFolds ())
				yield return fold;
		}
		
		#region Add methods
		
		public void Add (Error error)
		{
			hasErrors |= error.ErrorType == ErrorType.Error;
			errors.Add (error);
		}
		
		public void Add (Comment comment)
		{
			comments.Add (comment);
		}
		
		public void Add (Tag tagComment)
		{
			tagComments.Add (tagComment);
		}
		
		public void Add (PreProcessorDefine define)
		{
			defines.Add (define);
		}
		
		public void Add (ConditionalRegion region)
		{
			conditionalRegions.Add (region);
		}
		
		public void Add (FoldingRegion region)
		{
			folds.Add (region);
		}
		
		#endregion
		
		#region IEnumerable Add methods
		
		public void Add (IEnumerable<Error> errors)
		{
			foreach (Error error in errors) {
				hasErrors |= error.ErrorType == ErrorType.Error;
				this.errors.Add (error);
			}
		}
		
		public void Add (IEnumerable<Comment> comments)
		{
			this.comments.AddRange (comments);
		}
		
		public void Add (IEnumerable<Tag> tagComments)
		{
			this.tagComments.AddRange (tagComments);
		}
		
		public void Add (IEnumerable<PreProcessorDefine> defines)
		{
			this.defines.AddRange (defines);
		}
		
		public void Add (IEnumerable<FoldingRegion> folds)
		{
			this.folds.AddRange (folds);
		}
		
		public void Add (IEnumerable<ConditionalRegion> conditionalRegions)
		{
			this.conditionalRegions.AddRange (conditionalRegions);
		}
		
		#endregion
		
	}
	public static class FoldingUtilities
	{
		public static IEnumerable<FoldingRegion> ToFolds (this IEnumerable<ConditionalRegion> conditionalRegions)
		{
			foreach (ConditionalRegion region in conditionalRegions) {
				yield return new FoldingRegion ("#if " + region.Flag, region.Region, FoldType.ConditionalDefine);
				foreach (ConditionBlock block in region.ConditionBlocks) {
					yield return new FoldingRegion ("#elif " + block.Flag, block.Region,
					                                FoldType.ConditionalDefine);
				}
				if (!region.ElseBlock.IsEmpty)
					yield return new FoldingRegion ("#else", region.ElseBlock, FoldType.ConditionalDefine);
			}
		}
		
		public static IEnumerable<FoldingRegion> ToFolds (this IEnumerable<IType> types)
		{
			foreach (IType type in types)
				foreach (FoldingRegion fold in type.ToFolds ())
					yield return fold;
		}
		
		public static IEnumerable<FoldingRegion> ToFolds (this IType type)
		{
			if (!IncompleteOrSingleLine (type.BodyRegion))
				yield return new FoldingRegion (type.BodyRegion, FoldType.Type);
			
			foreach (IType inner in type.InnerTypes)
				foreach (FoldingRegion f in inner.ToFolds ())
					yield return f;
			
			if (type.ClassType == ClassType.Interface)
				yield break;

			foreach (IMethod method in type.Methods)
				if (!IncompleteOrSingleLine (method.BodyRegion))
					yield return new FoldingRegion (method.BodyRegion, FoldType.Member);
			
			foreach (IProperty property in type.Properties)
				if (!IncompleteOrSingleLine (property.BodyRegion))
					yield return new FoldingRegion (property.BodyRegion, FoldType.Member);
		}
		
		static bool IncompleteOrSingleLine (DomRegion region)
		{
			return region.Start.Line < 0 || region.End.Line <= region.Start.Line;
		}
		
		public static FoldingRegion ToFold (this IEnumerable<IUsing> usings)
		{
			if (usings == null)
				return null;
			var en = usings.GetEnumerator ();
			if (!en.MoveNext ())
				return null;
			IUsing first = en.Current;
			IUsing last = first;
			while (en.MoveNext ()) {
				if (en.Current.IsFromNamespace)
					break;
				last = en.Current;
			}
			
			if (first.Region.IsEmpty || last.Region.IsEmpty || first.Region.Start.Line == last.Region.End.Line)
				return null;
			return new FoldingRegion (new DomRegion (first.Region.Start, last.Region.End));
		}
		
		public static IEnumerable<FoldingRegion> ToFolds (this IList<Comment> comments)
		{
			
			
			for (int i = 0; i < comments.Count; i++) {
				Comment comment = comments[i];
				
				if (comment.CommentType == CommentType.MultiLine) {
					int startOffset = 0;
					while (startOffset < comment.Text.Length) {
						char ch = comment.Text[startOffset];
						if (!char.IsWhiteSpace (ch) && ch != '*')
							break;
						startOffset++;
					}
					int endOffset = startOffset;
					while (endOffset < comment.Text.Length) {
						char ch = comment.Text[endOffset];
						if (ch == '\r' || ch == '\n' || ch == '*')
							break;
						endOffset++;
					}
					
					string txt;
					if (endOffset > startOffset) {
						txt = "/* " + SubstrEllipsize (comment.Text, startOffset, endOffset - startOffset) + " */";
					} else {
						txt = "/* */";
					}
					yield return new FoldingRegion (txt, comment.Region, FoldType.Comment);
					continue;
				}
				
				if (!comment.CommentStartsLine)
					continue;
				int j = i;
				int curLine = comment.Region.Start.Line - 1;
				DomLocation end = comment.Region.End;
				StringBuilder commentText = new StringBuilder ();
				for (; j < comments.Count; j++) {
					Comment  curComment  = comments[j];
					if (curComment == null || !curComment.CommentStartsLine 
					    || curComment.CommentType != comment.CommentType 
					    || curLine + 1 != curComment.Region.Start.Line)
						break;
					commentText.Append(curComment.Text);
					end = curComment.Region.End;
					curLine = curComment.Region.Start.Line;
				}
				
				if (j - i > 1) {
					string txt;
					if (comment.IsDocumentation) {
						txt = "/// ..."; 
						string cmtText = commentText.ToString ();
						int idx = cmtText.IndexOf ("<summary>");
						if (idx >= 0) {
							int maxOffset = cmtText.IndexOf ("</summary>");
							while (maxOffset > 0 && cmtText[maxOffset-1] == ' ')
								maxOffset--;
							if (maxOffset < 0)
								maxOffset = cmtText.Length;
							int startOffset = idx + "<summary>".Length;
							while (startOffset < maxOffset) {
								char ch = cmtText[startOffset];
								if (!char.IsWhiteSpace (ch) && ch != '/')
									break;
								startOffset++;
							}
							int endOffset = startOffset;
							while (endOffset < maxOffset) {
								char ch = cmtText[endOffset];
								if (ch == '\r' || ch == '\n')
									break;
								endOffset++;
							}
							if (endOffset > startOffset)
								txt = "/// " + SubstrEllipsize (cmtText, startOffset, endOffset - startOffset);
						}
					} else {
						txt = "// " + SubstrEllipsize (comment.Text, 0, comment.Text.Length);
					}
					
					yield return new FoldingRegion (txt,
						new DomRegion (comment.Region.Start.Line,
							comment.Region.Start.Column, end.Line, end.Column),
						FoldType.Comment);
					i = j - 1;
				}
			}
		}
		
		static string SubstrEllipsize (string str, int start, int length)
		{
			//TODO: would be nice to ellipsize fold labels to a specific column, ideally the the formatting 
			// policy's desired width. However, we would have to know the "real" start column, i.e. respecting 
			// tab widths. Maybe that would work best by performing the ellipsis in the editor, instead of the parser.
			const int TRUNC_LEN = 60;
			
			if (str.Length == 0 || length == 0)
				return " ...";
			
			if (!(start == 0 && length <= TRUNC_LEN)) {
				if (length > TRUNC_LEN) {
					length = TRUNC_LEN;
					int wordBoundaryLen = str.LastIndexOf (' ', length) - start;
					if (wordBoundaryLen > TRUNC_LEN - 20)
						length = wordBoundaryLen;
				}
				str = str.Substring (start, length);
			}
			
			if (str[str.Length - 1] == '.')
				return str + "..";
			else if (char.IsPunctuation (str[str.Length - 1]))
				return str + " ...";
			return str + "...";
		}
		
		public static IEnumerable<FoldingRegion> FlagIfInsideMembers (this IEnumerable<FoldingRegion> folds,
			IEnumerable<IType> types, Action<FoldingRegion> flagAction)
		{
			foreach (FoldingRegion fold in folds) {
				foreach (IType type in types) {
					if (fold.Region.IsInsideMember (type)) {
						flagAction (fold);
						break;
					}
				}
				yield return fold;
			}
		}
		
		static bool IsInsideMember (this DomRegion region, IType cl)
		{
			if (region.IsEmpty || cl == null || !cl.BodyRegion.Contains (region.Start))
				return false;
			foreach (IMember member in cl.Members) {
				if (member.BodyRegion.IsEmpty)
					continue;
				if (member.BodyRegion.Contains (region.Start) && member.BodyRegion.Contains (region.End)) 
					return true;
			}
			foreach (IType inner in cl.InnerTypes) {
				if (region.IsInsideMember (inner))
					return true;
			}
			return false;
		}
	}
}
