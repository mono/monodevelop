// 
// TypeSystemParser.cs
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
using MonoDevelop.TypeSystem;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem;
using System.Collections.Generic;
using MonoDevelop.Projects;
using MonoDevelop.CSharp.Project;
using MonoDevelop.Ide.Tasks;
using Mono.CSharp;
using System.Linq;

namespace MonoDevelop.CSharp.Parser
{
	public class TypeSystemParser : ITypeSystemParser
	{
		public ParsedDocument Parse (IProjectContent projectContent, bool storeAst, string fileName, System.IO.TextReader content)
		{
			var parser = new ICSharpCode.NRefactory.CSharp.CSharpParser (GetCompilerArguments (projectContent));
			parser.GenerateTypeSystemMode = !storeAst;
			var result = new ParsedDocumentDecorator ();
			
			var tagComments = CommentTag.SpecialCommentTags.Select (t => t.Tag).ToArray ();
			
			parser.CompilationUnitCallback = delegate (CompilerCompilationUnit top) {
				foreach (var special in top.SpecialsBag.Specials) {
					var comment = special as SpecialsBag.Comment;
					if (comment != null) {
						VisitComment (result, comment, tagComments);
					} else {
						if (storeAst)
							VisitPreprocessorDirective (result, special as SpecialsBag.PreProcessorDirective);
					}
				}
			};
			
			var unit = parser.Parse (content);
			var visitor = new TypeSystemConvertVisitor (projectContent, fileName);
			unit.AcceptVisitor (visitor, null);
			result.ParsedFile = visitor.ParsedFile;
			if (storeAst) {
				result.AddAnnotation (unit);
				result.AddAnnotation (visitor.ParsedFile);
			}
			return result;
		}
		
		void VisitMcsUnit ()
		{
		}
		
		void VisitComment (ParsedDocument result, SpecialsBag.Comment comment, string[] tagComments)
		{
			var cmt = new MonoDevelop.TypeSystem.Comment (comment.Content);
			cmt.CommentStartsLine = comment.StartsLine;
			switch (comment.CommentType) {
			case SpecialsBag.CommentType.Multi:
				cmt.CommentType = CommentType.MultiLine;
				cmt.OpenTag = "/*";
				cmt.ClosingTag = "*/";
				break;
			case SpecialsBag.CommentType.Single:
				cmt.CommentType = CommentType.SingleLine;
				cmt.OpenTag = "//";
				break;
			case SpecialsBag.CommentType.Documentation:
				cmt.CommentType = CommentType.SingleLine;
				cmt.IsDocumentation = true;
				cmt.OpenTag = "///";
				break;
			}
			cmt.Region = new DomRegion (comment.Line, comment.Col, comment.EndLine, comment.EndCol);
			result.Comments.Add (cmt);
			foreach (string tag in tagComments) {
				int idx = comment.Content.IndexOf (tag);
				if (idx < 0)
					continue;
				result.Add (new MonoDevelop.TypeSystem.Tag (tag, comment.Content, cmt.Region));
			}
		}
		
		Stack<SpecialsBag.PreProcessorDirective> regions = new Stack<SpecialsBag.PreProcessorDirective> ();
		Stack<SpecialsBag.PreProcessorDirective> ifBlocks = new Stack<SpecialsBag.PreProcessorDirective> ();
		List<SpecialsBag.PreProcessorDirective> elifBlocks = new List<SpecialsBag.PreProcessorDirective> ();
		SpecialsBag.PreProcessorDirective elseBlock = null;
		
		Stack<ConditionalRegion> conditionalRegions = new Stack<ConditionalRegion> ();
		ConditionalRegion ConditionalRegion {
			get {
				return conditionalRegions.Count > 0 ? conditionalRegions.Peek () : null;
			}
		}

		void CloseConditionBlock (AstLocation loc)
		{
			if (ConditionalRegion == null || ConditionalRegion.ConditionBlocks.Count == 0 || !ConditionalRegion.ConditionBlocks[ConditionalRegion.ConditionBlocks.Count - 1].End.IsEmpty)
				return;
			ConditionalRegion.ConditionBlocks[ConditionalRegion.ConditionBlocks.Count - 1].End = loc;
		}

		void AddCurRegion (ParsedDocument result, int line, int col)
		{
			if (ConditionalRegion == null)
				return;
			ConditionalRegion.End = new AstLocation (line, col);
			result.Add (ConditionalRegion);
			conditionalRegions.Pop ();
		}
		
		void VisitPreprocessorDirective (ParsedDocument result, SpecialsBag.PreProcessorDirective directive)
		{
			AstLocation loc = new AstLocation (directive.Line, directive.Col);
			switch (directive.Cmd) {
			case Tokenizer.PreprocessorDirective.If:
				conditionalRegions.Push (new ConditionalRegion (directive.Arg));
				ifBlocks.Push (directive);
				ConditionalRegion.Start = loc;
				break;
			case Tokenizer.PreprocessorDirective.Elif:
				CloseConditionBlock (new AstLocation (directive.EndLine, directive.EndCol));
				if (ConditionalRegion != null)
					ConditionalRegion.ConditionBlocks.Add (new ConditionBlock (directive.Arg, loc));
				break;
			case Tokenizer.PreprocessorDirective.Else:
				CloseConditionBlock (new AstLocation (directive.EndLine, directive.EndCol));
				if (ConditionalRegion != null)
					ConditionalRegion.ElseBlock = new DomRegion (loc, AstLocation.Empty);
				break;
			case Tokenizer.PreprocessorDirective.Endif:
				AstLocation endLoc = new AstLocation (directive.EndLine, directive.EndCol);
				CloseConditionBlock (endLoc);
				if (ConditionalRegion != null && !ConditionalRegion.ElseBlock.Begin.IsEmpty)
					ConditionalRegion.ElseBlock = new DomRegion (ConditionalRegion.ElseBlock.Begin, endLoc);
				AddCurRegion (result, directive.EndLine, directive.EndCol);
				if (ifBlocks.Count > 0) {
					var ifBlock = ifBlocks.Pop ();
					var ifRegion = new DomRegion (ifBlock.Line, ifBlock.Col, directive.EndLine, directive.EndCol);
					result.Add (new FoldingRegion ("#if " + ifBlock.Arg.Trim (), ifRegion, FoldType.UserRegion, false));
					foreach (var d in elifBlocks) {
						var elIlfRegion = new DomRegion (d.Line, d.Col, directive.EndLine, directive.EndCol);
						result.Add (new FoldingRegion ("#elif " + ifBlock.Arg.Trim (), elIlfRegion, FoldType.UserRegion, false));
					}
					if (elseBlock != null) {
						var elseBlockRegion = new DomRegion (elseBlock.Line, elseBlock.Col, elseBlock.Line, elseBlock.Col);
						result.Add (new FoldingRegion ("#else", elseBlockRegion, FoldType.UserRegion, false));
					}
				}
				elseBlock = null;
				break;
			case Tokenizer.PreprocessorDirective.Define:
				result.Add (new PreProcessorDefine (directive.Arg, loc));
				break;
			case Tokenizer.PreprocessorDirective.Region:
				regions.Push (directive);
				break;
			case Tokenizer.PreprocessorDirective.Endregion:
				if (regions.Count > 0) {
					var start = regions.Pop ();
					DomRegion dr = new DomRegion (start.Line, start.Col, directive.EndLine, directive.EndCol);
					result.Add (new FoldingRegion (start.Arg, dr, FoldType.UserRegion, true));
				}
				break;
			}
		}
		
		string[] GetCompilerArguments (IProjectContent projectContent)
		{
			var compilerArguments = new List<string> ();
			var project = projectContent.GetProject ();
			if (project == null || MonoDevelop.Ide.IdeApp.Workspace == null)
				return compilerArguments.ToArray ();
			
			var configuration = project.GetConfiguration (MonoDevelop.Ide.IdeApp.Workspace.ActiveConfiguration) as DotNetProjectConfiguration;
			var par = configuration != null ? configuration.CompilationParameters as CSharpCompilerParameters : null;
			
			if (par == null)
				return compilerArguments.ToArray ();
				
			if (!string.IsNullOrEmpty (par.DefineSymbols)) {
				compilerArguments.Add ("-define:" + string.Join (";", par.DefineSymbols.Split (';', ',', ' ', '\t')));
			}
			if (par.UnsafeCode)
				compilerArguments.Add ("-unsafe");
			if (par.TreatWarningsAsErrors)
				compilerArguments.Add ("-warnaserror");
			if (!string.IsNullOrEmpty (par.NoWarnings))
				compilerArguments.Add ("-nowarn:" + string.Join (",", par.NoWarnings.Split (';', ',', ' ', '\t')));
			compilerArguments.Add ("-warn:" + par.WarningLevel);
			compilerArguments.Add ("-langversion:" + GetLangString (par.LangVersion));
			if (par.GenerateOverflowChecks)
				compilerArguments.Add ("-checked");
			
			return compilerArguments.ToArray ();
		}
		
		static string GetLangString (LangVersion ver)
		{
			switch (ver) {
			case LangVersion.Default:
				return "Default";
			case LangVersion.ISO_1:
				return "ISO-1";
			case LangVersion.ISO_2:
				return "ISO-2";
			}
			return "Default";
		}
	}
}

