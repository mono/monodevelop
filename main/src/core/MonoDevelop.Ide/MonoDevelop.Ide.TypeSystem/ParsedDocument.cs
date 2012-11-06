// 
// ParsedDocument.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
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
using ICSharpCode.NRefactory.TypeSystem;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Semantics;


namespace MonoDevelop.Ide.TypeSystem
{
	[Flags]
	public enum ParsedDocumentFlags
	{
		None            = 0,
		NonSerializable = 1
	}

	public abstract class ParsedDocument
	{
		DateTime lastWriteTimeUtc = DateTime.UtcNow;
		public DateTime LastWriteTimeUtc {
			get { return lastWriteTimeUtc; }
			set { lastWriteTimeUtc = value; }
		}
		
		[NonSerialized]
		List<Comment> comments = new List<Comment> ();
		
		public virtual IUnresolvedFile ParsedFile {
			get { return null; }
			set { throw new InvalidOperationException (); }
		}

		public IList<Comment> Comments {
			get {
				return comments;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this instance is invalid and needs to be reparsed.
		/// </summary>
		public bool IsInvalid {
			get;
			set;
		}

		List<Tag> tagComments = new List<Tag> ();
		public IList<Tag> TagComments {
			get {
				return tagComments;
			}
		}
		
		List<FoldingRegion> foldings = new List<FoldingRegion> ();
		public virtual IEnumerable<FoldingRegion> Foldings {
			get {
				return foldings;
			}
		}
		
		public IEnumerable<FoldingRegion> UserRegions {
			get {
				return Foldings.Where (f => f.Type == FoldType.UserRegion);
			}
		}
		
		List<PreProcessorDefine> defines = new List<PreProcessorDefine> ();
		public IList<PreProcessorDefine> Defines {
			get {
				return defines;
			}
		}
		
		List<ConditionalRegion> conditionalRegions = new List<ConditionalRegion> ();
		public IList<ConditionalRegion> ConditionalRegions {
			get {
				return conditionalRegions;
			}
		}
		
		[NonSerialized]
		ParsedDocumentFlags flags;
		public ParsedDocumentFlags Flags {
			get {
				return flags;
			}
			set {
				flags = value;
			}
		}

		string fileName;
		public virtual string FileName {
			get {
				return fileName;
			}
			protected set {
				fileName = value;
			}
		}
		
		public virtual IList<Error> Errors {
			get {
				return new Error[0];
			}
		}
		
		public bool HasErrors {
			get {
				return Errors.Any (e => e.ErrorType == ErrorType.Error);
			}
		}
		
		/// <summary>
		/// Gets or sets the language ast used by specific language backends.
		/// </summary>
		public object Ast {
			get;
			set;
		}
		
		public T GetAst<T> () where T : class
		{
			return Ast as T;
		}
		
		public ParsedDocument ()
		{
		}
		
		public ParsedDocument (string fileName)
		{
			this.fileName = fileName;
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
			foldings.Add (region);
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
			this.foldings.AddRange (folds);
		}
		
		public void Add (IEnumerable<ConditionalRegion> conditionalRegions)
		{
			this.conditionalRegions.AddRange (conditionalRegions);
		}
		
		#region IUnresolvedFile delegation
		public virtual IUnresolvedTypeDefinition GetTopLevelTypeDefinition (TextLocation location)
		{
			return null;
		}
		
		public virtual IUnresolvedTypeDefinition GetInnermostTypeDefinition (TextLocation location)
		{
			return null;
		}

		public virtual IUnresolvedMember GetMember (TextLocation location)
		{
			return null;
		}

		public virtual IList<IUnresolvedTypeDefinition> TopLevelTypeDefinitions {
			get {
				return new List<IUnresolvedTypeDefinition> ();
			}
		}

		public virtual ITypeResolveContext GetTypeResolveContext (ICompilation compilation, TextLocation loc)
		{
			return null;
		}
		#endregion

		public Func<MonoDevelop.Ide.Gui.Document, CancellationToken, object> CreateRefactoringContext;
	}
	
	public class DefaultParsedDocument : ParsedDocument, IUnresolvedFile
	{

		public override IUnresolvedFile ParsedFile {
			get { return this; }
		}
		
		List<Error> errors = new List<Error> ();
		
		public override IList<Error> Errors {
			get {
				return errors;
			} 
		}

		public DefaultParsedDocument (string fileName) : base (fileName)
		{
			
		}
		
		#region IUnresolvedFile implementation
		public override IUnresolvedTypeDefinition GetTopLevelTypeDefinition(TextLocation location)
		{
			return TopLevelTypeDefinitions.FirstOrDefault (t => t.Region.IsInside (location));
		}
		
		public override IUnresolvedTypeDefinition GetInnermostTypeDefinition(TextLocation location)
		{
			IUnresolvedTypeDefinition parent = null;
			var type = GetTopLevelTypeDefinition(location);
			while (type != null) {
				parent = type;
				type = parent.NestedTypes.FirstOrDefault (t => t.Region.IsInside (location));
			}
			return parent;
		}
		
		public override IUnresolvedMember GetMember(TextLocation location)
		{
			var type = GetInnermostTypeDefinition(location);
			if (type == null)
				return null;
			return type.Members.FirstOrDefault (e => e.Region.IsInside(location));
		}
		
		List<IUnresolvedTypeDefinition> types = new List<IUnresolvedTypeDefinition> ();
		public override IList<IUnresolvedTypeDefinition> TopLevelTypeDefinitions {
			get {
				return types;
			}
		}
		
		List<IUnresolvedAttribute> attributes = new List<IUnresolvedAttribute> ();
		public IList<IUnresolvedAttribute> AssemblyAttributes {
			get {
				return attributes;
			}
		}
		
		public override ITypeResolveContext GetTypeResolveContext (ICompilation compilation, TextLocation loc)
		{
			return null;
		}

		public IList<IUnresolvedAttribute> ModuleAttributes {
			get {
				return new List<IUnresolvedAttribute> ();
			}
		}
		#endregion
		
		public void Add (Error error)
		{
			errors.Add (error);
		}
		
		public void Add (IEnumerable<Error> errors)
		{
			this.errors.AddRange (errors);
		}

		#region IUnresolvedFile implementation
		DateTime? IUnresolvedFile.LastWriteTime {
			get {
				return LastWriteTimeUtc;
			}
			set {
				LastWriteTimeUtc = value.HasValue ? value.Value : DateTime.UtcNow;
			}
		}
		#endregion
	}
	
	[Serializable]
	public class ParsedDocumentDecorator : ParsedDocument
	{
		IUnresolvedFile parsedFile;
		
		public override IUnresolvedFile ParsedFile {
			get { return parsedFile; }
			set { parsedFile = value; FileName = parsedFile.FileName; }
		}
		
		public override IList<Error> Errors {
			get {
				return parsedFile.Errors;
			}
		}
		
		public ParsedDocumentDecorator (IUnresolvedFile parsedFile) : base (parsedFile.FileName)
		{
			this.parsedFile = parsedFile;
		}
		
		public ParsedDocumentDecorator () : base ("")
		{
		}
		
		#region IUnresolvedFile implementation
		public override IUnresolvedTypeDefinition GetTopLevelTypeDefinition (TextLocation location)
		{
			return parsedFile.GetTopLevelTypeDefinition (location);
		}
		
		public override IUnresolvedTypeDefinition GetInnermostTypeDefinition (TextLocation location)
		{
			return parsedFile.GetInnermostTypeDefinition (location);
		}

		public override IUnresolvedMember GetMember (TextLocation location)
		{
			return parsedFile.GetMember (location);
		}

		public override IList<IUnresolvedTypeDefinition> TopLevelTypeDefinitions {
			get {
				return parsedFile.TopLevelTypeDefinitions;
			}
		}
		
		public override ITypeResolveContext GetTypeResolveContext (ICompilation compilation, TextLocation loc)
		{
			return parsedFile.GetTypeResolveContext (compilation, loc);
		}
		#endregion
	}
	
	public static class FoldingUtilities
	{
		static bool IncompleteOrSingleLine (DomRegion region)
		{
			return region.BeginLine <= 0 || region.EndLine <= region.BeginLine;
		}
		
		public static IEnumerable<FoldingRegion> ToFolds (this IList<Comment> comments)
		{
			for (int i = 0; i < comments.Count; i++) {
				Comment comment = comments [i];
				
				if (comment.CommentType == CommentType.Block) {
					int startOffset = 0;
					if (comment.Region.BeginLine == comment.Region.EndLine)
						continue;
					while (startOffset < comment.Text.Length) {
						char ch = comment.Text [startOffset];
						if (!char.IsWhiteSpace (ch) && ch != '*')
							break;
						startOffset++;
					}
					int endOffset = startOffset;
					while (endOffset < comment.Text.Length) {
						char ch = comment.Text [endOffset];
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
				int curLine = comment.Region.BeginLine - 1;
				var end = comment.Region.End;
				var commentText = new StringBuilder ();
				for (; j < comments.Count; j++) {
					Comment curComment = comments [j];
					if (curComment == null || !curComment.CommentStartsLine 
					    || curComment.CommentType != comment.CommentType 
					    || curLine + 1 != curComment.Region.BeginLine)
						break;
					commentText.Append (curComment.Text);
					end = curComment.Region.End;
					curLine = curComment.Region.BeginLine;
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
								char ch = cmtText [startOffset];
								if (!char.IsWhiteSpace (ch) && ch != '/')
									break;
								startOffset++;
							}
							int endOffset = startOffset;
							while (endOffset < maxOffset) {
								char ch = cmtText [endOffset];
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
						new DomRegion (comment.Region.Begin, end),
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
			
			if (start == 0 && length == str.Length)
				return str;

			if (str.Length == 0 || length == 0)
				return " ...";
			
			if (!(start == 0 && length <= TRUNC_LEN)) {
				if (length > TRUNC_LEN) {
					length = TRUNC_LEN;
					int wordBoundaryLen = str.LastIndexOf (' ', length) - start;
					if (wordBoundaryLen > TRUNC_LEN - 20)
						length = wordBoundaryLen;
				}
			}
			str = str.Substring (start, length);
				
			if (str [str.Length - 1] == '.')
				return str + "..";
			else if (char.IsPunctuation (str [str.Length - 1]))
				return str + " ...";
			return str + "...";
		}
		
		public static IEnumerable<FoldingRegion> FlagIfInsideMembers (this IEnumerable<FoldingRegion> folds,
			IEnumerable<IUnresolvedTypeDefinition> types, Action<FoldingRegion> flagAction)
		{
			foreach (FoldingRegion fold in folds) {
				foreach (var type in types) {
					if (fold.Region.IsInsideMember (type)) {
						flagAction (fold);
						break;
					}
				}
				yield return fold;
			}
		}
		
		static bool IsInsideMember (this DomRegion region, IUnresolvedTypeDefinition cl)
		{
			if (region.IsEmpty || cl == null || !cl.BodyRegion.IsInside (region.Begin))
				return false;
			foreach (var member in cl.Members) {
				if (member.BodyRegion.IsEmpty)
					continue;
				if (member.BodyRegion.IsInside (region.Begin) && member.BodyRegion.IsInside (region.End)) 
					return true;
			}
			foreach (var inner in cl.NestedTypes) {
				if (region.IsInsideMember (inner))
					return true;
			}
			return false;
		}
		
	}
}

