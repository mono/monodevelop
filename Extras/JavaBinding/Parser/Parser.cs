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
using JavaBinding.Parser.SharpDevelopTree;
using JRefactory.Parser;

namespace JavaBinding.Parser
{
	public class TParser : IParser
	{
		///<summary>IParser Interface</summary> 
		string[] lexerTags;
		public string[] LexerTags {
			set {
				lexerTags = value;
			}
		}
		public IExpressionFinder ExpressionFinder {
			get {
				return new ExpressionFinder();
			}
		}
		
		void RetrieveRegions(CompilationUnit cu, SpecialTracker tracker)
		{
			for (int i = 0; i < tracker.CurrentSpecials.Count; ++i) {
				PreProcessingDirective directive = tracker.CurrentSpecials[i] as PreProcessingDirective;
				if (directive != null) {
					if (directive.Cmd == "#region") {
						int deep = 1; 
						for (int j = i + 1; j < tracker.CurrentSpecials.Count; ++j) {
							PreProcessingDirective nextDirective = tracker.CurrentSpecials[j] as PreProcessingDirective;
							if (nextDirective != null) {
								switch (nextDirective.Cmd) {
									case "#region":
										++deep;
										break;
									case "#endregion":
										--deep;
										if (deep == 0) {
											cu.FoldingRegions.Add(new FoldingRegion(directive.Arg.Trim(), new DefaultRegion(directive.Start, new Point(nextDirective.End.X - 2, nextDirective.End.Y))));
											goto end;
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
			Console.WriteLine ("*****");
			JRefactory.Parser.Parser p = new JRefactory.Parser.Parser();
			
			Lexer lexer = new Lexer(new FileReader(fileName));
			p.Parse(lexer);
			
			JavaVisitor visitor = new JavaVisitor ();
			visitor.Visit(p.compilationUnit, null);
			visitor.Cu.ErrorsDuringCompile = p.Errors.count > 0;
			RetrieveRegions(visitor.Cu, lexer.SpecialTracker);
			return visitor.Cu;
		}
		
		public ICompilationUnitBase Parse(string fileName, string fileContent)
		{
			JRefactory.Parser.Parser p = new JRefactory.Parser.Parser();
			
			Lexer lexer = new Lexer(new StringReader(fileContent));
			p.Parse(lexer);
			
			JavaVisitor visitor = new JavaVisitor ();
			visitor.Visit(p.compilationUnit, null);
			visitor.Cu.ErrorsDuringCompile = p.Errors.count > 0;
			visitor.Cu.Tag = p.compilationUnit;
			RetrieveRegions(visitor.Cu, lexer.SpecialTracker);
			return visitor.Cu;
		}
		
		public ArrayList CtrlSpace(IParserContext parserService, int caretLine, int caretColumn, string fileName)
		{
			return new Resolver().CtrlSpace(parserService, caretLine, caretColumn, fileName);
		}
		
		public ResolveResult Resolve(IParserContext parserService, string expression, int caretLineNumber, int caretColumn, string fileName, string fileContent)
		{
			return new Resolver().Resolve(parserService, expression, caretLineNumber, caretColumn, fileName, fileContent);
		}


		public bool HandlesFileExtension(string fileExtension){
			if(fileExtension == null) return false;
			return (fileExtension.ToLower() == ".java");
		}

		
		///////// IParser Interface END
	}
}
