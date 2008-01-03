//  Parser.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Andrea Paatz <andrea@icsharpcode.net>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
using System;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects;
using VBBinding.Parser.SharpDevelopTree;

using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Parser;


namespace VBBinding.Parser
{
	public class TParser : MonoDevelop.Projects.Parser.IParser
	{
	
		public TParser() : base(){
			//Console.WriteLine("Entering VB.NET parser");
		}//constructor
	
		///<summary>IParser Interface</summary> 
		string[] lexerTags;
		public string[] LexerTags {
//// Alex: get accessor needed
			get {
				return lexerTags;
			}
			set {
				lexerTags = value;
			}
		}
		
		public IExpressionFinder CreateExpressionFinder (string file)
		{
			return new ExpressionFinder ();
		}

		public bool CanParse (string fileName)
		{
			return System.IO.Path.GetExtension (fileName).ToLower () == ".vb";
		}

		void RetrieveRegions(CompilationUnit cu, SpecialTracker tracker)
		{
			for (int i = 0; i < tracker.CurrentSpecials.Count; ++i) {
				PreprocessingDirective directive = tracker.CurrentSpecials[i] as PreprocessingDirective;
				if (directive != null) {
					if (directive.Cmd.ToLower() == "#region") {
						int deep = 1; 
						for (int j = i + 1; j < tracker.CurrentSpecials.Count; ++j) {
							PreprocessingDirective nextDirective = tracker.CurrentSpecials[j] as PreprocessingDirective;
							if(nextDirective != null) {
								switch (nextDirective.Cmd.ToLower()) {
									case "#region":
										++deep;
										break;
									case "#end":
										if (nextDirective.Arg.ToLower() == "region") {
											--deep;
											if (deep == 0) {
												cu.FoldingRegions.Add(new FoldingRegion(directive.Arg.Trim('"'), new DefaultRegion(new Point (directive.StartPosition.X, directive.StartPosition.Y) , new Point (nextDirective.EndPosition.X, nextDirective.EndPosition.Y))));
												goto end;
											}
										}
										break;
								}
							}
						}
						end: ;
					}
				}
			}
		}
		
		public ICompilationUnitBase Parse(string fileName)
		{
			using (ICSharpCode.NRefactory.IParser p = ICSharpCode.NRefactory.ParserFactory.CreateParser (SupportedLanguage.VBNet, new StreamReader(fileName))) {
				return Parse (p, fileName);
			}
		}
		
		public ICompilationUnitBase Parse(string fileName, string fileContent)
		{
			using (ICSharpCode.NRefactory.IParser p = ICSharpCode.NRefactory.ParserFactory.CreateParser (SupportedLanguage.VBNet, new StringReader(fileContent))) {
				return Parse (p, fileName);
			}
		}
		
		ICompilationUnit Parse (ICSharpCode.NRefactory.IParser p, string fileName)
		{
			p.Parse();
			
			VBNetVisitor visitor = new VBNetVisitor();
			visitor.VisitCompilationUnit(p.CompilationUnit, null);
			visitor.Cu.ErrorsDuringCompile = p.Errors.Count > 0;
			visitor.Cu.Tag = p.CompilationUnit;
			RetrieveRegions(visitor.Cu, p.Lexer.SpecialTracker);
			foreach (IClass c in visitor.Cu.Classes)
				c.Region.FileName = fileName;
			AddCommentTags(visitor.Cu, p.Lexer.TagComments);
			return visitor.Cu;
		}
		
		void AddCommentTags(ICompilationUnit cu, List<TagComment> tagComments)
		{
			foreach (ICSharpCode.NRefactory.Parser.TagComment tagComment in tagComments) {
				DefaultRegion tagRegion = new DefaultRegion(tagComment.StartPosition.Y, tagComment.StartPosition.X);
				MonoDevelop.Projects.Parser.Tag tag = new MonoDevelop.Projects.Parser.Tag(tagComment.Tag, tagRegion);
				tag.CommentString = tagComment.CommentText;
				cu.TagComments.Add(tag);
			}
		}
		
		
		
		public LanguageItemCollection CtrlSpace (IParserContext parserContext, int caretLine, int caretColumn, string fileName)
		{
			return new Resolver(parserContext).CtrlSpace (caretLine, caretColumn, fileName);
		}
		
		public ResolveResult Resolve (IParserContext parserContext, string expression, int caretLineNumber, int caretColumn, string fileName, string fileContent)
		{
			return new Resolver(parserContext).Resolve (expression, caretLineNumber, caretColumn, fileName, fileContent);
		}
		
		public LanguageItemCollection IsAsResolve (IParserContext parserContext, string expression, int caretLineNumber, int caretColumn, string fileName, string fileContent)
		{
			return new Resolver (parserContext).IsAsResolve (expression, caretLineNumber, caretColumn, fileName, fileContent);
		}
		
		public ILanguageItem ResolveIdentifier (IParserContext parserContext, string id, int caretLineNumber, int caretColumn, string fileName, string fileContent)
		{
			return null;
		}
		
		///////// IParser Interface END
	}
}
