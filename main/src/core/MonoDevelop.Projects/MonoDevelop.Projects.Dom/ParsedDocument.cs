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
		
		ICompilationUnit compilationUnit = null;
		
		List<Comment> comments = new List<Comment> ();
		List<FoldingRegion> foldingRegions = new List<FoldingRegion> ();
		List<Tag> tagComments = new List<Tag> ();
		List<PreProcessorDefine> defines = new List<PreProcessorDefine> ();
		List<ConditionalRegion> conditionalRegion = new List<ConditionalRegion> ();

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
		
		public IList<FoldingRegion> FoldingRegions {
			get {
				return foldingRegions;
			}
		}
		
		public IList<PreProcessorDefine> Defines {
			get {
				return defines;
			}
		}
		
		public IList<ConditionalRegion> ConditionalRegion {
			get {
				return conditionalRegion;
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
		
		public ICompilationUnit CompilationUnit {
			get {
				return compilationUnit;
			}
			set {
				compilationUnit = value;
			}
		}
		
		protected void GenerateFoldsFromUsings ()
		{
			if (CompilationUnit.Usings == null || CompilationUnit.Usings.Count == 0)
				return;
			IUsing first = CompilationUnit.Usings[0];
			IUsing last = first;
			for (int i = 1; i < CompilationUnit.Usings.Count; i++) {
				if (CompilationUnit.Usings[i].IsFromNamespace)
					break;
				last = CompilationUnit.Usings[i];
			}
			
			if (first.Region == null || last.Region == null || first.Region.Start.Line == last.Region.End.Line)
				return;
			foldingRegions.Add (new FoldingRegion ("...", new DomRegion (first.Region.Start, last.Region.End)));
		}
		
		protected void GenerateFoldsFromType (IType type)
		{
			if (type.BodyRegion != null && type.BodyRegion.End.Line > type.BodyRegion.Start.Line) {
				foldingRegions.Add (new FoldingRegion ("...", type.BodyRegion));
			}
			foreach (IType inner in type.InnerTypes) 
				GenerateFoldsFromType (inner);
			
			if (type.ClassType == ClassType.Interface)
				return;

			foreach (IMethod method in type.Methods) {
				if (method.BodyRegion.End.Line <= 0)
					continue;
				foldingRegions.Add (new FoldingRegion ("...", method.BodyRegion));
			}
			
			foreach (IProperty property in type.Properties) {
				if (property.BodyRegion.End.Line <= 0)
					continue;
				foldingRegions.Add (new FoldingRegion ("...", property.BodyRegion));
			}
		}
		
		public virtual void GenerateFoldInformation ()
		{
			if (CompilationUnit == null)
				return;
			GenerateFoldsFromUsings ();
			
			foreach (IType type in CompilationUnit.Types) {
				GenerateFoldsFromType (type);
			}
		}
		
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
		
		public void Add (FoldingRegion foldingRegion)
		{
			foldingRegions.Add (foldingRegion);
		}
		
	}
}
