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

namespace MonoDevelop.Projects.Dom
{
	public class ParsedDocument
	{
		DateTime parseTime = DateTime.Now;
		
		List<Comment> comments = new List<Comment> ();
		List<FoldingRegion> folds = new List<FoldingRegion> ();
		List<FoldingRegion> regions = new List<FoldingRegion> ();
		List<Tag> tagComments = new List<Tag> ();
		List<PreProcessorDefine> defines = new List<PreProcessorDefine> ();
		List<ConditionalRegion> conditionalRegions = new List<ConditionalRegion> ();

		bool hasErrors = false;
		List<Error> errors = new List<Error> ();
		
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
		
		public IList<FoldingRegion> UserRegions {
			get {
				return regions;
			}
		}
		
		
		public IList<FoldingRegion> Folds {
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
		
		protected void AddUserRegionsToFolds ()
		{
			folds.AddRange (regions);
		}
		
		public virtual void GenerateFoldInformation ()
		{
			AddUserRegionsToFolds ();
			Add (ConditionalRegions.ToFolds ());
			
			if (CompilationUnit == null)
				return;
			
			Fold f = CompilationUnit.Usings.ToFold ();
			if (f != null)
				Add (f);
			
			if (CompilationUnit.Types != null)
				Add (CompilationUnit.Types.ToFolds ());
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
		
		public void Add (Fold fold)
		{
			folds.Add (fold);
		}
		
		public void Add (UserRegion region)
		{
			regions.Add (region);
		}
		
		public void Add (ConditionalRegion conditionalRegion)
		{
			conditionalRegions.Add (conditionalRegion);
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
		
		public void Add (IEnumerable<Fold> folds)
		{
			foreach (FoldingRegion fold in folds)
				this.folds.Add (fold);
		}
		
		public void Add (IEnumerable<UserRegion> regions)
		{
			foreach (UserRegion region in regions)
				this.regions.Add (region);
		}
		
		public void Add (IEnumerable<ConditionalRegion> conditionalRegions)
		{
			this.conditionalRegions.AddRange (conditionalRegions);
		}
		
		#endregion
		
	}
	public static class FoldingUtilities
	{
		public static IEnumerable<Fold> ToFolds (this IEnumerable<ConditionalRegion> conditionalRegions)
		{
			foreach (ConditionalRegion region in conditionalRegions) {
				yield return  new Fold ("#if " + region.Flag, region.Region);
				foreach (ConditionBlock block in region.ConditionBlocks) {
					yield return new Fold ("#elif " + block.Flag, block.Region);
				}
				if (!region.ElseBlock.IsEmpty)
					yield return new Fold ("#else", region.ElseBlock);
			}
		}
		
		public static IEnumerable<Fold> ToFolds (this IEnumerable<IType> types)
		{
			foreach (IType type in types)
				foreach (Fold f in type.ToFolds ())
					yield return f;
		}
		
		public static IEnumerable<Fold> ToFolds (this IType type)
		{
			if (!type.BodyRegion.IsEmpty && type.BodyRegion.End.Line > type.BodyRegion.Start.Line) {
				yield return new Fold (type.BodyRegion);
			}
			foreach (IType inner in type.InnerTypes)
				foreach (Fold f in inner.ToFolds ())
					yield return f;
			
			if (type.ClassType == ClassType.Interface)
				yield break;

			foreach (IMethod method in type.Methods) {
				if (method.BodyRegion.End.Line <= 0)
					continue;
				yield return new Fold (method.BodyRegion);
			}
			
			foreach (IProperty property in type.Properties) {
				if (property.BodyRegion.End.Line <= 0)
					continue;
				yield return new Fold (property.BodyRegion);
			}
		}
		
		public static Fold ToFold (this IEnumerable<IUsing> usings)
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
			return new Fold (new DomRegion (first.Region.Start, last.Region.End));
		}
	}
}
