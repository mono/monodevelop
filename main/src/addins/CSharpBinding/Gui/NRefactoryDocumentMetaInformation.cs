// NRefactoryDocumentMetaInformation.cs
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
using System.IO;

using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;

using ICSharpCode.NRefactory.Visitors;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Parser.CSharp;

namespace MonoDevelop.CSharpBinding
{
	public class NRefactoryDocumentMetaInformation : IDocumentMetaInformation, ISpecialVisitor
	{
		List<Tag> tagComments = new List<Tag> ();
		public IList<Tag> TagComments {
			get {
				return tagComments;
			}
		}
		
		List<MonoDevelop.Projects.Dom.Comment> comments = new List<MonoDevelop.Projects.Dom.Comment> ();
		public IList<MonoDevelop.Projects.Dom.Comment> Comments {
			get {
				return comments;
			}
		}
		
		List<FoldingRegion> foldingRegions = new List<FoldingRegion> ();
		public IList<FoldingRegion> FoldingRegion {
			get {
				return foldingRegions;
			}
		}
		
		List<PreProcessorDefine> defines = new List<PreProcessorDefine> ();
		public IList<PreProcessorDefine> Defines {
			get {
				return defines;
			}
		}
		
		List<ConditionalRegion> conditionalRegion = new List<ConditionalRegion> ();
		public IList<ConditionalRegion> ConditionalRegion {
			get {
				return conditionalRegion;
			}
		}
		
		object ISpecialVisitor.Visit(ISpecial special, object data)
		{
			return null;
		}
		
		object ISpecialVisitor.Visit(BlankLine special, object data)
		{
			return null;
		}
		
		object ISpecialVisitor.Visit(ICSharpCode.NRefactory.Comment comment, object data)
		{
			MonoDevelop.Projects.Dom.Comment newComment = new MonoDevelop.Projects.Dom.Comment ();
// TODO:
//			newComment.CommentStartsLine = comment.CommentStartsLine;
			newComment.Text = comment.CommentText;
			newComment.Region = new DomRegion (comment.StartPosition.Line, comment.StartPosition.Column, comment.EndPosition.Line, comment.EndPosition.Column);
			switch (comment.CommentType) {
				case ICSharpCode.NRefactory.CommentType.Block:
					newComment.CommentType = MonoDevelop.Projects.Dom.CommentType.MultiLine;
					break;
				case ICSharpCode.NRefactory.CommentType.Documentation:
					newComment.CommentType = MonoDevelop.Projects.Dom.CommentType.SingleLine;
					newComment.IsDocumentation = true;
					break;
				default:
					newComment.CommentType = MonoDevelop.Projects.Dom.CommentType.SingleLine;
					break;
			}
			
			comments.Add (newComment);
			return null;
		}
		
		Stack<PreprocessingDirective> regions = new Stack<PreprocessingDirective> ();
		object ISpecialVisitor.Visit(PreprocessingDirective directive, object data)
		{
			switch (directive.Cmd) {
				case "#define":
					defines.Add (new PreProcessorDefine (directive.Arg, new DomLocation (directive.StartPosition.Line, directive.StartPosition.Column)));
					break;
				case "#region":
					regions.Push (directive);
					break;
				case "#endregion":
					if (regions.Count > 0) {
						PreprocessingDirective start = regions.Pop ();
						foldingRegions.Add (new FoldingRegion (start.Arg,
						                                       new DomRegion (start.StartPosition.Line, start.StartPosition.Column, directive.EndPosition.Line, directive.EndPosition.Column),
						                                       true));
					}
					break;
			}
			return null;
		}
		
		public NRefactoryDocumentMetaInformation (SupportedLanguage lang, TextReader reader)
		{
			ILexer lexer = ParserFactory.CreateLexer (lang, reader);
			lexer.SkipCurrentBlock (Tokens.EOF);
			foreach (ISpecial special in lexer.SpecialTracker.CurrentSpecials) {
				special.AcceptVisitor (this, null);
			}
		}
	}
}
