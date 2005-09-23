// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Andrea Paatz" email="andrea@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Drawing;
using System.Collections;
using MonoDevelop.Services;
using MonoDevelop.Internal.Parser;
using MonoDevelop.Internal.Project;
using VBBinding.Parser.SharpDevelopTree;
using ICSharpCode.SharpRefactory.Parser.VB;


namespace VBBinding.Parser
{
	public class TParser : IParser
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
		public IExpressionFinder ExpressionFinder {
			get {
				return new ExpressionFinder();
			}
		}

		public bool CanParse (string fileName)
		{
			return System.IO.Path.GetExtension (fileName).ToLower () == ".vb";
		}

		public bool CanParse (Project project)
		{
			return project.ProjectType == "VBNET";
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
												cu.FoldingRegions.Add(new FoldingRegion(directive.Arg.Trim('"'), new DefaultRegion(directive.Start, nextDirective.End)));
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
			ICSharpCode.SharpRefactory.Parser.VB.Parser p = new ICSharpCode.SharpRefactory.Parser.VB.Parser();
			
			Lexer lexer = new Lexer(new FileReader(fileName));
			lexer.SpecialCommentTags = lexerTags;
			p.Parse(lexer);
			
			VBNetVisitor visitor = new VBNetVisitor();
			visitor.Visit(p.compilationUnit, null);
			//visitor.Cu.FileName = fileName;
			visitor.Cu.ErrorsDuringCompile = p.Errors.count > 0;
			RetrieveRegions(visitor.Cu, lexer.SpecialTracker);
			
			AddCommentTags(visitor.Cu, lexer.TagComments);
			return visitor.Cu;
		}
		
		public ICompilationUnitBase Parse(string fileName, string fileContent)
		{
			ICSharpCode.SharpRefactory.Parser.VB.Parser p = new ICSharpCode.SharpRefactory.Parser.VB.Parser();
			
			Lexer lexer = new Lexer(new StringReader(fileContent));
			lexer.SpecialCommentTags = lexerTags;
			p.Parse(lexer);
			
			VBNetVisitor visitor = new VBNetVisitor();
			visitor.Visit(p.compilationUnit, null);
			//visitor.Cu.FileName = fileName;
			visitor.Cu.ErrorsDuringCompile = p.Errors.count > 0;
			visitor.Cu.Tag = p.compilationUnit;
			RetrieveRegions(visitor.Cu, lexer.SpecialTracker);
			AddCommentTags(visitor.Cu, lexer.TagComments);
			return visitor.Cu;
		}
		
		void AddCommentTags(ICompilationUnit cu, ArrayList tagComments)
		{
			foreach (ICSharpCode.SharpRefactory.Parser.VB.TagComment tagComment in tagComments) {
				DefaultRegion tagRegion = new DefaultRegion(tagComment.StartPosition.Y, tagComment.StartPosition.X);
				MonoDevelop.Internal.Parser.Tag tag = new MonoDevelop.Internal.Parser.Tag(tagComment.Tag, tagRegion);
				tag.CommentString = tagComment.CommentText;
				cu.TagComments.Add(tag);
			}
		}
		
		
		
		public ArrayList CtrlSpace (IParserContext parserContext, int caretLine, int caretColumn, string fileName)
		{
			return new Resolver(parserContext).CtrlSpace (caretLine, caretColumn, fileName);
		}
		
		public ResolveResult Resolve (IParserContext parserContext, string expression, int caretLineNumber, int caretColumn, string fileName, string fileContent)
		{
			return new Resolver(parserContext).Resolve (expression, caretLineNumber, caretColumn, fileName, fileContent);
		}
		
		public ArrayList IsAsResolve (IParserContext parserContext, string expression, int caretLineNumber, int caretColumn, string fileName, string fileContent)
		{
			return new Resolver (parserContext).IsAsResolve (expression, caretLineNumber, caretColumn, fileName, fileContent);
		}
		
		public string MonodocResolver (IParserContext parserContext, string expression, int caretLineNumber, int caretColumn, string fileName, string fileContent)
		{
			return new Resolver (parserContext).MonodocResolver (expression, caretLineNumber, caretColumn, fileName, fileContent);
		}
		

		
		///////// IParser Interface END
	}
}
