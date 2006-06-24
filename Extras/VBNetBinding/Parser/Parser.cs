// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Andrea Paatz" email="andrea@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects;
using VBBinding.Parser.SharpDevelopTree;

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
				PreProcessingDirective directive = tracker.CurrentSpecials[i] as PreProcessingDirective;
				if (directive != null) {
					if (directive.Cmd.ToLower() == "#region") {
						int deep = 1; 
						for (int j = i + 1; j < tracker.CurrentSpecials.Count; ++j) {
							PreProcessingDirective nextDirective = tracker.CurrentSpecials[j] as PreProcessingDirective;
							if(nextDirective != null) {
								switch (nextDirective.Cmd.ToLower()) {
									case "#region":
										++deep;
										break;
									case "#end":
										if (nextDirective.Arg.ToLower() == "region") {
											--deep;
											if (deep == 0) {
												cu.FoldingRegions.Add(new FoldingRegion(directive.Arg.Trim('"'), new DefaultRegion(directive.StartPosition, nextDirective.EndPosition)));
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
			ICSharpCode.NRefactory.Parser.IParser p = ICSharpCode.NRefactory.Parser.ParserFactory.CreateParser (SupportedLanguage.VBNet, new StreamReader(fileName));
			
			p.Parse();
			
			VBNetVisitor visitor = new VBNetVisitor();
			visitor.Visit(p.CompilationUnit, null);
			//visitor.Cu.FileName = fileName;
			visitor.Cu.ErrorsDuringCompile = p.Errors.count > 0;
			RetrieveRegions(visitor.Cu, p.Lexer.SpecialTracker);
			
			AddCommentTags(visitor.Cu, p.Lexer.TagComments);
			return visitor.Cu;
		}
		
		public ICompilationUnitBase Parse(string fileName, string fileContent)
		{
			ICSharpCode.NRefactory.Parser.IParser p = ICSharpCode.NRefactory.Parser.ParserFactory.CreateParser (SupportedLanguage.VBNet, new StringReader(fileContent));
			
			p.Parse();
			
			VBNetVisitor visitor = new VBNetVisitor();
			visitor.Visit(p.CompilationUnit, null);
			//visitor.Cu.FileName = fileName;
			visitor.Cu.ErrorsDuringCompile = p.Errors.count > 0;
			visitor.Cu.Tag = p.CompilationUnit;
			RetrieveRegions(visitor.Cu, p.Lexer.SpecialTracker);
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
