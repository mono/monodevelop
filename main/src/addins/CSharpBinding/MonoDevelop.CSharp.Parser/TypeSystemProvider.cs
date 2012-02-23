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
using ICSharpCode.NRefactory;

namespace MonoDevelop.CSharp.Parser
{
	public class TypeSystemParser : ITypeSystemParser
	{
		public ParsedDocument Parse (bool storeAst, string fileName, System.IO.TextReader content, MonoDevelop.Projects.Project project = null)
		{
			var parser = new ICSharpCode.NRefactory.CSharp.CSharpParser (GetCompilerArguments (project));
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
			
			var unit = parser.Parse (content, fileName);
			var pf = unit.ToTypeSystem ();
			try {
				pf.LastWriteTime = System.IO.File.GetLastWriteTime (fileName);
			} catch (Exception) {
				pf.LastWriteTime = DateTime.Now;
			}
			
			result.ParsedFile = pf;
			result.Add (GenerateFoldings (unit, result));
			if (storeAst) {
				result.Ast = unit;
			}
			return result;
		}
		
		IEnumerable<FoldingRegion> GenerateFoldings (CompilationUnit unit, ParsedDocument doc)
		{
			foreach (var fold in doc.ConditionalRegions.ToFolds ())
				yield return fold;
			
			foreach (var fold in doc.Comments.ToFolds ())
				yield return fold;
			
			var visitor = new FoldingVisitor ();
			unit.AcceptVisitor (visitor, null);
			foreach (var fold in visitor.Foldings)
				yield return fold;
		}
		
		class FoldingVisitor : DepthFirstAstVisitor<object, object>
		{
			public readonly List<FoldingRegion> Foldings = new List<FoldingRegion> ();
			
			public override object VisitNamespaceDeclaration (NamespaceDeclaration namespaceDeclaration, object data)
			{
				if (!namespaceDeclaration.RBraceToken.IsNull)
					Foldings.Add (new FoldingRegion (new DomRegion (namespaceDeclaration.LBraceToken.GetPrevNode ().EndLocation, namespaceDeclaration.RBraceToken.EndLocation), FoldType.Undefined));
				return base.VisitNamespaceDeclaration (namespaceDeclaration, data);
			}
			
			public override object VisitTypeDeclaration (TypeDeclaration typeDeclaration, object data)
			{
				if (!typeDeclaration.RBraceToken.IsNull)
					Foldings.Add (new FoldingRegion (new DomRegion (typeDeclaration.LBraceToken.GetPrevNode ().EndLocation, typeDeclaration.RBraceToken.StartLocation), FoldType.Type));
				return base.VisitTypeDeclaration (typeDeclaration, data);
			}
			
			public override object VisitMethodDeclaration (MethodDeclaration methodDeclaration, object data)
			{
				if (!methodDeclaration.Body.IsNull)
					Foldings.Add (new FoldingRegion (new DomRegion (methodDeclaration.Body.LBraceToken.GetPrevNode ().EndLocation, methodDeclaration.Body.RBraceToken.StartLocation), FoldType.Member));
				return base.VisitMethodDeclaration (methodDeclaration, data);
			}
			
			public override object VisitConstructorDeclaration (ConstructorDeclaration constructorDeclaration, object data)
			{
				if (!constructorDeclaration.Body.IsNull)
					Foldings.Add (new FoldingRegion (new DomRegion (constructorDeclaration.Body.LBraceToken.GetPrevNode ().EndLocation, constructorDeclaration.Body.RBraceToken.StartLocation), FoldType.Member));
				return base.VisitConstructorDeclaration (constructorDeclaration, data);
			}
			
			public override object VisitDestructorDeclaration (DestructorDeclaration destructorDeclaration, object data)
			{
				if (!destructorDeclaration.Body.IsNull)
					Foldings.Add (new FoldingRegion (new DomRegion (destructorDeclaration.Body.LBraceToken.GetPrevNode ().EndLocation, destructorDeclaration.Body.RBraceToken.StartLocation), FoldType.Member));
				return base.VisitDestructorDeclaration (destructorDeclaration, data);
			}
			
			public override object VisitOperatorDeclaration (OperatorDeclaration operatorDeclaration, object data)
			{
				if (!operatorDeclaration.Body.IsNull)
					Foldings.Add (new FoldingRegion (new DomRegion (operatorDeclaration.Body.LBraceToken.GetPrevNode ().EndLocation, operatorDeclaration.Body.RBraceToken.StartLocation), FoldType.Member));
				return base.VisitOperatorDeclaration (operatorDeclaration, data);
			}
			
			public override object VisitPropertyDeclaration (PropertyDeclaration propertyDeclaration, object data)
			{
				if (!propertyDeclaration.LBraceToken.IsNull)
					Foldings.Add (new FoldingRegion (new DomRegion (propertyDeclaration.LBraceToken.GetPrevNode ().EndLocation, propertyDeclaration.RBraceToken.StartLocation), FoldType.Member));
				return base.VisitPropertyDeclaration (propertyDeclaration, data);
			}
			
			public override object VisitIndexerDeclaration (IndexerDeclaration indexerDeclaration, object data)
			{
				if (!indexerDeclaration.LBraceToken.IsNull)
					Foldings.Add (new FoldingRegion (new DomRegion (indexerDeclaration.LBraceToken.GetPrevNode ().EndLocation, indexerDeclaration.RBraceToken.StartLocation), FoldType.Member));
				return base.VisitIndexerDeclaration (indexerDeclaration, data);
			}
			
			public override object VisitCustomEventDeclaration (CustomEventDeclaration eventDeclaration, object data)
			{
				if (!eventDeclaration.LBraceToken.IsNull)
					Foldings.Add (new FoldingRegion (new DomRegion (eventDeclaration.LBraceToken.GetPrevNode ().EndLocation, eventDeclaration.RBraceToken.StartLocation), FoldType.Member));
				return base.VisitCustomEventDeclaration (eventDeclaration, data);
			}
			
			public override object VisitSwitchStatement (SwitchStatement switchStatement, object data)
			{
				if (!switchStatement.RBraceToken.IsNull)
					Foldings.Add (new FoldingRegion (new DomRegion (switchStatement.LBraceToken.GetPrevNode ().EndLocation, switchStatement.RBraceToken.StartLocation), FoldType.Member));
				return base.VisitSwitchStatement (switchStatement, data);
			}
			
			public override object VisitBlockStatement (BlockStatement blockStatement, object data)
			{
				if (!(blockStatement.Parent is AttributedNode) && blockStatement.EndLocation.Line - blockStatement.StartLocation.Line > 2) {
					Foldings.Add (new FoldingRegion (new DomRegion (blockStatement.GetPrevNode ().EndLocation, blockStatement.RBraceToken.StartLocation), FoldType.Undefined));
				}
				
				return base.VisitBlockStatement (blockStatement, data);
			}
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

		void CloseConditionBlock (TextLocation loc)
		{
			if (ConditionalRegion == null || ConditionalRegion.ConditionBlocks.Count == 0 || !ConditionalRegion.ConditionBlocks[ConditionalRegion.ConditionBlocks.Count - 1].End.IsEmpty)
				return;
			ConditionalRegion.ConditionBlocks[ConditionalRegion.ConditionBlocks.Count - 1].End = loc;
		}

		void AddCurRegion (ParsedDocument result, int line, int col)
		{
			if (ConditionalRegion == null)
				return;
			ConditionalRegion.End = new TextLocation (line, col);
			result.Add (ConditionalRegion);
			conditionalRegions.Pop ();
		}
		
		void VisitPreprocessorDirective (ParsedDocument result, SpecialsBag.PreProcessorDirective directive)
		{
			TextLocation loc = new TextLocation (directive.Line, directive.Col);
			switch (directive.Cmd) {
			case Tokenizer.PreprocessorDirective.If:
				conditionalRegions.Push (new ConditionalRegion (directive.Arg));
				ifBlocks.Push (directive);
				ConditionalRegion.Start = loc;
				break;
			case Tokenizer.PreprocessorDirective.Elif:
				CloseConditionBlock (new TextLocation (directive.EndLine, directive.EndCol));
				if (ConditionalRegion != null)
					ConditionalRegion.ConditionBlocks.Add (new ConditionBlock (directive.Arg, loc));
				break;
			case Tokenizer.PreprocessorDirective.Else:
				CloseConditionBlock (new TextLocation (directive.EndLine, directive.EndCol));
				if (ConditionalRegion != null)
					ConditionalRegion.ElseBlock = new DomRegion (loc, TextLocation.Empty);
				break;
			case Tokenizer.PreprocessorDirective.Endif:
				TextLocation endLoc = new TextLocation (directive.EndLine, directive.EndCol);
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
		
		CompilerSettings GetCompilerArguments (MonoDevelop.Projects.Project project)
		{
			var compilerArguments = new CompilerSettings ();
			if (project == null || MonoDevelop.Ide.IdeApp.Workspace == null)
				return compilerArguments;

			var configuration = project.GetConfiguration (MonoDevelop.Ide.IdeApp.Workspace.ActiveConfiguration) as DotNetProjectConfiguration;
			var par = configuration != null ? configuration.CompilationParameters as CSharpCompilerParameters : null;
			
			if (par == null)
				return compilerArguments;
				
			if (!string.IsNullOrEmpty (par.DefineSymbols)) {
				foreach (var sym in par.DefineSymbols.Split (';', ',', ' ', '\t').Where (s => !string.IsNullOrWhiteSpace (s)))
					compilerArguments.AddConditionalSymbol (sym);
			}
			
			compilerArguments.Unsafe = par.UnsafeCode;
			compilerArguments.Version = ConvertLanguageVersion (par.LangVersion);
			compilerArguments.Checked = par.GenerateOverflowChecks;
			
			// TODO: Warning options need to be set in the report object.
			
/*			compilerArguments.EnhancedWarnings = par.TreatWarningsAsErrors;
			if (!string.IsNullOrEmpty (par.NoWarnings))
				compilerArguments.Add ("-nowarn:" + string.Join (",", par.NoWarnings.Split (';', ',', ' ', '\t')));
			
			compilerArguments.Add ("-warn:" + par.WarningLevel);
*/
			
			return compilerArguments;
		}
		
		static Mono.CSharp.LanguageVersion ConvertLanguageVersion (LangVersion ver)
		{
			switch (ver) {
			case LangVersion.Default:
				return Mono.CSharp.LanguageVersion.Default;
			case LangVersion.ISO_1:
				return Mono.CSharp.LanguageVersion.ISO_1;
			case LangVersion.ISO_2:
				return Mono.CSharp.LanguageVersion.ISO_2;
			}
			return Mono.CSharp.LanguageVersion.Default;
		}
	}
	
	static class FoldingUtils
	{
		public static IEnumerable<FoldingRegion> ToFolds (this IEnumerable<ConditionalRegion> conditionalRegions)
		{
			foreach (ConditionalRegion region in conditionalRegions) {
				yield return new FoldingRegion ("#if " + region.Flag, region.Region, FoldType.ConditionalDefine);
				foreach (ConditionBlock block in region.ConditionBlocks) {
					yield return new FoldingRegion ("#elif " + block.Flag, block.Region,
					                                FoldType.ConditionalDefine);
				}
				if (!region.ElseBlock.IsEmpty)
					yield return new FoldingRegion ("#else", region.ElseBlock, FoldType.ConditionalDefine);
			}
		}		
	}
}

