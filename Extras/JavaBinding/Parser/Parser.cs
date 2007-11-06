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
using System.Collections;
using MonoDevelop.Core;
using MonoDevelop.Projects.Parser;
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
		
		public LanguageItemCollection CtrlSpace(IParserContext parserService, int caretLine, int caretColumn, string fileName)
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
