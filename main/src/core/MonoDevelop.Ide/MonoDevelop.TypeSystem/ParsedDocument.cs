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
using ICSharpCode.NRefactory.CSharp;
using System.Linq;
using System.Threading;

namespace MonoDevelop.TypeSystem
{
	[Flags]
	public enum ParsedDocumentFlags
	{
		None            = 0,
		NonSerializable = 1
	}
		
	public class ParsedDocument : IParsedFile
	{
		DateTime parseTime = DateTime.Now;
		public DateTime ParseTime {
			get { return parseTime; }
		}
		
		List<Comment> comments = new List<Comment> ();
		
		List<Tag> tagComments = new List<Tag> ();
		public IList<Tag> TagComments {
			get {
				return tagComments;
			}
		}
		
		List<FoldingRegion> folds = new List<FoldingRegion> ();
		public IList<FoldingRegion> AdditionalFolds {
			get {
				return folds;
			}
		}
		
		public IEnumerable<FoldingRegion> UserRegions {
			get {
				return AdditionalFolds.Where (f => f.Type == FoldType.UserRegion);
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
		
		public ParsedDocumentFlags Flags {
			get;
			set;
		}
		
		public ParsedDocument ()
		{
		}
		
		public ParsedDocument (string fileName)
		{
			this.fileName = fileName;
		}
		
		#region IParsedFile implementation
		public virtual ITypeDefinition GetTopLevelTypeDefinition (AstLocation location)
		{
			return null;
		}

		public virtual ITypeDefinition GetTypeDefinition (AstLocation location)
		{
			return null;
		}

		public virtual IMember GetMember (AstLocation location)
		{
			return null;
		}

		public virtual IProjectContent ProjectContent {
			get {
				return null;
			}
		}
		
		string fileName;
		public virtual string FileName {
			get {
				return fileName;
			}
		}

		public virtual IList<ITypeDefinition> TopLevelTypeDefinitions {
			get {
				return new List<ITypeDefinition> ();
			}
		}

		public virtual IList<IAttribute> AssemblyAttributes {
			get {
				return new List<IAttribute> ();
			}
		}
		
		List<Error> errors = new List<Error> ();
		public virtual IList<Error> Errors {
			get {
				return errors;
			}
		}
		
		public bool HasErrors {
			get {
				return Errors.Any (e => e.ErrorType == ErrorType.Error);
			}
		}
		
		#endregion

		#region IFreezable implementation
		public virtual void Freeze ()
		{
		}

		public virtual bool IsFrozen {
			get {
				return false;
			}
		}
		#endregion
		
		public virtual IEnumerable<FoldingRegion> GenerateFolds ()
		{
			foreach (FoldingRegion fold in AdditionalFolds)
				yield return fold;
			
			foreach (FoldingRegion fold in ConditionalRegions.ToFolds ())
				yield return fold;
			
			IEnumerable<FoldingRegion> commentFolds = comments.ToFolds ();
//			if (parsedFile != null) {
//				commentFolds = commentFolds.FlagIfInsideMembers (CompilationUnit.Types, delegate (FoldingRegion f) {
//					f.Type = FoldType.CommentInsideMember;
//				});
//			}
			
//			foreach (FoldingRegion fold in commentFolds)
//				yield return fold;
			
			
//			FoldingRegion usingFold = CompilationUnit.Usings.ToFold ();
//			if (usingFold != null)
//				yield return usingFold;
			
//			foreach (var fold in this.parsedFile.TopLevelTypeDefinitions.ToFolds ())
//				yield return fold;
		}
		
		public void Add (Error error)
		{
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
		
		public void Add (IEnumerable<Error> errors)
		{
			foreach (Error error in errors) {
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
		
		#region Annotation support
		// Annotations: points either null (no annotations), to the single annotation,
		// or to an AnnotationList.
		// Once it is pointed at an AnnotationList, it will never change (this allows thread-safety support by locking the list)
		object annotations;
		
		sealed class AnnotationList : List<object>, ICloneable
		{
			// There are two uses for this custom list type:
			// 1) it's private, and thus (unlike List<object>) cannot be confused with real annotations
			// 2) It allows us to simplify the cloning logic by making the list behave the same as a clonable annotation.
			public AnnotationList (int initialCapacity) : base(initialCapacity)
			{
			}
			
			public object Clone ()
			{
				lock (this) {
					AnnotationList copy = new AnnotationList (this.Count);
					for (int i = 0; i < this.Count; i++) {
						object obj = this [i];
						ICloneable c = obj as ICloneable;
						copy.Add (c != null ? c.Clone () : obj);
					}
					return copy;
				}
			}
		}
		
		public void AddAnnotation (object annotation)
		{
			if (annotation == null)
				throw new ArgumentNullException ("annotation");
		retry: // Retry until successful
			object oldAnnotation = Interlocked.CompareExchange (ref this.annotations, annotation, null);
			if (oldAnnotation == null) {
				return; // we successfully added a single annotation
			}
			AnnotationList list = oldAnnotation as AnnotationList;
			if (list == null) {
				// we need to transform the old annotation into a list
				list = new AnnotationList (4);
				list.Add (oldAnnotation);
				list.Add (annotation);
				if (Interlocked.CompareExchange (ref this.annotations, list, oldAnnotation) != oldAnnotation) {
					// the transformation failed (some other thread wrote to this.annotations first)
					goto retry;
				}
			} else {
				// once there's a list, use simple locking
				lock (list) {
					list.Add (annotation);
				}
			}
		}
		
		public void RemoveAnnotations<T> () where T : class
		{
		retry: // Retry until successful
			object oldAnnotations = this.annotations;
			AnnotationList list = oldAnnotations as AnnotationList;
			if (list != null) {
				lock (list)
					list.RemoveAll (obj => obj is T);
			} else if (oldAnnotations is T) {
				if (Interlocked.CompareExchange (ref this.annotations, null, oldAnnotations) != oldAnnotations) {
					// Operation failed (some other thread wrote to this.annotations first)
					goto retry;
				}
			}
		}
		
		public void RemoveAnnotations (Type type)
		{
			if (type == null)
				throw new ArgumentNullException ("type");
		retry: // Retry until successful
			object oldAnnotations = this.annotations;
			AnnotationList list = oldAnnotations as AnnotationList;
			if (list != null) {
				lock (list)
					list.RemoveAll (obj => type.IsInstanceOfType (obj));
			} else if (type.IsInstanceOfType (oldAnnotations)) {
				if (Interlocked.CompareExchange (ref this.annotations, null, oldAnnotations) != oldAnnotations) {
					// Operation failed (some other thread wrote to this.annotations first)
					goto retry;
				}
			}
		}
		
		public T Annotation<T> () where T: class
		{
			object annotations = this.annotations;
			AnnotationList list = annotations as AnnotationList;
			if (list != null) {
				lock (list) {
					foreach (object obj in list) {
						T t = obj as T;
						if (t != null)
							return t;
					}
					return null;
				}
			} else {
				return annotations as T;
			}
		}
		
		public object Annotation (Type type)
		{
			if (type == null)
				throw new ArgumentNullException ("type");
			object annotations = this.annotations;
			AnnotationList list = annotations as AnnotationList;
			if (list != null) {
				lock (list) {
					foreach (object obj in list) {
						if (type.IsInstanceOfType (obj))
							return obj;
					}
				}
			} else {
				if (type.IsInstanceOfType (annotations))
					return annotations;
			}
			return null;
		}
		
		/// <summary>
		/// Gets all annotations stored on this AstNode.
		/// </summary>
		public IEnumerable<object> Annotations {
			get {
				object annotations = this.annotations;
				AnnotationList list = annotations as AnnotationList;
				if (list != null) {
					lock (list) {
						return list.ToArray ();
					}
				} else {
					if (annotations != null)
						return new object[] { annotations };
					else
						return Enumerable.Empty<object> ();
				}
			}
		}
		#endregion
	}
	
	public class DefaultParsedDocument : ParsedDocument
	{
		public DefaultParsedDocument (string fileName) : base (fileName)
		{
		}
		
		#region IParsedFile implementation
		public override ITypeDefinition GetTopLevelTypeDefinition(AstLocation location)
		{
			return FindEntity(types, location);
		}
		
		public override ITypeDefinition GetTypeDefinition(AstLocation location)
		{
			ITypeDefinition parent = null;
			ITypeDefinition type = GetTopLevelTypeDefinition(location);
			while (type != null) {
				parent = type;
				type = FindEntity(parent.InnerClasses, location);
			}
			return parent;
		}
		
		public override IMember GetMember(AstLocation location)
		{
			ITypeDefinition type = GetTypeDefinition(location);
			if (type == null)
				return null;
			return FindEntity(type.Methods, location)
				?? FindEntity(type.Fields, location)
				?? FindEntity(type.Properties, location)
				?? (IMember)FindEntity(type.Events, location);
		}
		
		static T FindEntity<T>(IList<T> list, AstLocation location) where T : class, IEntity
		{
			// This could be improved using a binary search
			foreach (T entity in list) {
				if (entity.Region.IsInside(location.Line, location.Column))
					return entity;
			}
			return null;
		}

		
		List<ITypeDefinition> types = new List<ITypeDefinition> ();
		public override IList<ITypeDefinition> TopLevelTypeDefinitions {
			get {
				return types;
			}
		}
		
		List<IAttribute> attributes = new List<IAttribute> ();
		public override IList<IAttribute> AssemblyAttributes {
			get {
				return attributes;
			}
		}
		#endregion
	}
	
	
	public class ParsedDocumentDecorator : ParsedDocument
	{
		IParsedFile parsedFile;
		
		public ParsedDocumentDecorator (IParsedFile parsedFile) : base (parsedFile.FileName)
		{
			this.parsedFile = parsedFile;
		}
	
		#region IParsedFile implementation
		public override ITypeDefinition GetTopLevelTypeDefinition (ICSharpCode.NRefactory.CSharp.AstLocation location)
		{
			return parsedFile.GetTopLevelTypeDefinition (location);
		}
		
		public override ITypeDefinition GetTypeDefinition (ICSharpCode.NRefactory.CSharp.AstLocation location)
		{
			return parsedFile.GetTypeDefinition (location);
		}

		public override IMember GetMember (ICSharpCode.NRefactory.CSharp.AstLocation location)
		{
			return parsedFile.GetMember (location);
		}

		public override IProjectContent ProjectContent {
			get {
				return parsedFile.ProjectContent;
			}
		}

		public override System.Collections.Generic.IList<ITypeDefinition> TopLevelTypeDefinitions {
			get {
				return parsedFile.TopLevelTypeDefinitions;
			}
		}

		public override System.Collections.Generic.IList<IAttribute> AssemblyAttributes {
			get {
				return parsedFile.AssemblyAttributes;
			}
		}

		public override System.Collections.Generic.IList<Error> Errors {
			get {
				return parsedFile.Errors;
			}
		}
		#endregion

		#region IFreezable implementation
		public override void Freeze ()
		{
			parsedFile.Freeze ();
		}

		public override bool IsFrozen {
			get {
				return parsedFile.IsFrozen;
			}
		}
		#endregion
	}
	
	static class FoldingUtilities
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
		
		public static IEnumerable<FoldingRegion> ToFolds (this IEnumerable<ITypeDefinition> types)
		{
			foreach (var type in types)
				foreach (FoldingRegion fold in type.ToFolds ())
					yield return fold;
		}
		
		public static IEnumerable<FoldingRegion> ToFolds (this ITypeDefinition type)
		{
			if (!IncompleteOrSingleLine (type.BodyRegion))
				yield return new FoldingRegion (type.BodyRegion, FoldType.Type);
			
			foreach (var inner in type.InnerClasses)
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
			return region.BeginLine <= 0 || region.EndLine <= region.BeginLine;
		}
		
//		public static FoldingRegion ToFold (this IEnumerable<IUsing> usings)
//		{
//			if (usings == null)
//				return null;
//			var en = usings.GetEnumerator ();
//			if (!en.MoveNext ())
//				return null;
//			IUsing first = en.Current;
//			IUsing last = first;
//			while (en.MoveNext ()) {
//				if (en.Current.IsFromNamespace)
//					break;
//				last = en.Current;
//			}
//			
//			if (first.Region.IsEmpty || last.Region.IsEmpty || first.Region.Start.Line == last.Region.End.Line)
//				return null;
//			return new FoldingRegion (new DomRegion (first.Region.Start, last.Region.End));
//		}
		
		public static IEnumerable<FoldingRegion> ToFolds (this IList<Comment> comments)
		{
			
			
			for (int i = 0; i < comments.Count; i++) {
				Comment comment = comments [i];
				
				if (comment.CommentType == CommentType.MultiLine) {
					int startOffset = 0;
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
			
			if (str [str.Length - 1] == '.')
				return str + "..";
			else if (char.IsPunctuation (str [str.Length - 1]))
				return str + " ...";
			return str + "...";
		}
		
		public static IEnumerable<FoldingRegion> FlagIfInsideMembers (this IEnumerable<FoldingRegion> folds,
			IEnumerable<ITypeDefinition> types, Action<FoldingRegion> flagAction)
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
		
		static bool IsInsideMember (this DomRegion region, ITypeDefinition cl)
		{
			if (region.IsEmpty || cl == null || !cl.BodyRegion.IsInside (region.Begin))
				return false;
			foreach (var member in cl.Members) {
				if (member.BodyRegion.IsEmpty)
					continue;
				if (member.BodyRegion.IsInside (region.Begin) && member.BodyRegion.IsInside (region.End)) 
					return true;
			}
			foreach (var inner in cl.InnerClasses) {
				if (region.IsInsideMember (inner))
					return true;
			}
			return false;
		}
	}
}

