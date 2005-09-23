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
using CSharpBinding.Parser.SharpDevelopTree;
using ICSharpCode.SharpRefactory.Parser;

namespace CSharpBinding.Parser
{
	public class TParser : IParser
	{
		///<summary>IParser Interface</summary> 
		string[] lexerTags;
		public string[] LexerTags {
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
		public bool CanParse(string fileName)
		{
			return System.IO.Path.GetExtension(fileName).ToUpper() == ".CS";
		}
		public bool CanParse(Project project)
		{
			return project.ProjectType == "C#";
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
			ICSharpCode.SharpRefactory.Parser.Parser p = new ICSharpCode.SharpRefactory.Parser.Parser();
			
			Lexer lexer = new Lexer(new FileReader(fileName));
			p.Parse(lexer);
			
			CSharpVisitor visitor = new CSharpVisitor();
			visitor.Visit(p.compilationUnit, null);
			visitor.Cu.ErrorsDuringCompile = p.Errors.count > 0;
			RetrieveRegions(visitor.Cu, lexer.SpecialTracker);
			foreach (IClass c in visitor.Cu.Classes)
				c.Region.FileName = fileName;
			return visitor.Cu;
		}
		
		public ICompilationUnitBase Parse(string fileName, string fileContent)
		{
			ICSharpCode.SharpRefactory.Parser.Parser p = new ICSharpCode.SharpRefactory.Parser.Parser();
			
			Lexer lexer = new Lexer(new StringReader(fileContent));
			p.Parse(lexer);
			
			CSharpVisitor visitor = new CSharpVisitor();
			visitor.Visit(p.compilationUnit, null);
			visitor.Cu.ErrorsDuringCompile = p.Errors.count > 0;
			visitor.Cu.Tag = p.compilationUnit;
			visitor.Cu.ErrorInformation = p.Errors.ErrorInformation;
			RetrieveRegions(visitor.Cu, lexer.SpecialTracker);
			foreach (IClass c in visitor.Cu.Classes)
				c.Region.FileName = fileName;
			return visitor.Cu;
		}
		
		public ArrayList CtrlSpace(IParserContext parserContext, int caretLine, int caretColumn, string fileName)
		{
			return new Resolver (parserContext).CtrlSpace (caretLine, caretColumn, fileName);
		}

		public ArrayList IsAsResolve (IParserContext parserContext, string expression, int caretLineNumber, int caretColumn, string fileName, string fileContent)
		{
			return new Resolver (parserContext).IsAsResolve (expression, caretLineNumber, caretColumn, fileName, fileContent);
		}
		
		public ResolveResult Resolve (IParserContext parserContext, string expression, int caretLineNumber, int caretColumn, string fileName, string fileContent)
		{
			return new Resolver (parserContext).Resolve (expression, caretLineNumber, caretColumn, fileName, fileContent);
		}

		public string MonodocResolver (IParserContext parserContext, string expression, int caretLineNumber, int caretColumn, string fileName, string fileContent)
		{
			return new Resolver (parserContext).MonodocResolver (expression, caretLineNumber, caretColumn, fileName, fileContent);
		}
		///////// IParser Interface END
	}
}
