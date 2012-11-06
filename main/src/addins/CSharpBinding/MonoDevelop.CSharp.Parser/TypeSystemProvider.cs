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
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem;
using System.Collections.Generic;
using MonoDevelop.Projects;
using MonoDevelop.CSharp.Project;
using MonoDevelop.Ide.Tasks;
using Mono.CSharp;
using System.Linq;
using ICSharpCode.NRefactory;
using MonoDevelop.CSharp.Refactoring.CodeActions;

namespace MonoDevelop.CSharp.Parser
{
	public class TypeSystemParser : MonoDevelop.Ide.TypeSystem.TypeSystemParser
	{
		public override ParsedDocument Parse (bool storeAst, string fileName, System.IO.TextReader content, MonoDevelop.Projects.Project project = null)
		{
			var parser = new ICSharpCode.NRefactory.CSharp.CSharpParser (GetCompilerArguments (project));
			parser.GenerateTypeSystemMode = !storeAst;
			var result = new ParsedDocumentDecorator ();

			if (project != null) {
				var projectFile = project.Files.GetFile (fileName);
				if (projectFile != null && projectFile.BuildAction != BuildAction.Compile)
					result.Flags |= ParsedDocumentFlags.NonSerializable;
			}

			var tagComments = CommentTag.SpecialCommentTags.Select (t => t.Tag).ToArray ();
			
			parser.CompilationUnitCallback = delegate (CompilerCompilationUnit top) {
				foreach (var special in top.SpecialsBag.Specials) {
					var comment = special as SpecialsBag.Comment;
					if (comment != null) {
						VisitComment (result, comment, tagComments);
					} else {
						if (storeAst) {
							var ppd = special as SpecialsBag.PreProcessorDirective;
							if  (ppd != null)
								VisitPreprocessorDirective (result, ppd);
						}
					}
				}
			};
			
			var unit = parser.Parse (content, fileName);
			unit.Freeze ();
			var pf = unit.ToTypeSystem ();
			try {
				pf.LastWriteTime = System.IO.File.GetLastWriteTimeUtc (fileName);
			} catch (Exception) {
				pf.LastWriteTime = DateTime.UtcNow;
			}

			result.LastWriteTimeUtc = pf.LastWriteTime.Value;
			result.ParsedFile = pf;
			result.Add (GenerateFoldings (unit, result));
			result.CreateRefactoringContext = (doc, token) => new MDRefactoringContext (doc, doc.Editor.Caret.Location, token);

			if (storeAst) {
				result.Ast = unit;
			}
			return result;
		}
		
		IEnumerable<FoldingRegion> GenerateFoldings (SyntaxTree unit, ParsedDocument doc)
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

			void AddUsings (AstNode parent)
			{
				var firstChild = parent.Children.FirstOrDefault (child => child is UsingDeclaration || child is UsingAliasDeclaration);
				var node = firstChild;
				while (node != null) {
					var next = node.GetNextNode ();
					if (next is UsingDeclaration || next is UsingAliasDeclaration) {
						node = next;
					} else {
						break;
					}
				}
				if (firstChild != node) {
					Foldings.Add (new FoldingRegion (new DomRegion (firstChild.StartLocation, node.EndLocation), FoldType.Undefined));
				}
			}
			public override object VisitSyntaxTree (SyntaxTree unit, object data)
			{
				AddUsings (unit);
				return base.VisitSyntaxTree (unit, data);
			}

			public override object VisitNamespaceDeclaration (NamespaceDeclaration namespaceDeclaration, object data)
			{
				AddUsings (namespaceDeclaration);
				if (!namespaceDeclaration.RBraceToken.IsNull && namespaceDeclaration.LBraceToken.StartLocation.Line != namespaceDeclaration.RBraceToken.StartLocation.Line)
					Foldings.Add (new FoldingRegion (new DomRegion (namespaceDeclaration.LBraceToken.GetPrevNode ().EndLocation, namespaceDeclaration.RBraceToken.EndLocation), FoldType.Undefined));
				return base.VisitNamespaceDeclaration (namespaceDeclaration, data);
			}
			
			public override object VisitTypeDeclaration (TypeDeclaration typeDeclaration, object data)
			{
				if (!typeDeclaration.RBraceToken.IsNull && typeDeclaration.LBraceToken.StartLocation.Line != typeDeclaration.RBraceToken.StartLocation.Line)
					Foldings.Add (new FoldingRegion (new DomRegion (typeDeclaration.LBraceToken.GetPrevNode ().EndLocation, typeDeclaration.RBraceToken.StartLocation), FoldType.Type));
				return base.VisitTypeDeclaration (typeDeclaration, data);
			}
			
			public override object VisitMethodDeclaration (MethodDeclaration methodDeclaration, object data)
			{
				if (!methodDeclaration.Body.IsNull && methodDeclaration.Body.LBraceToken.StartLocation.Line != methodDeclaration.Body.RBraceToken.StartLocation.Line)
					Foldings.Add (new FoldingRegion (new DomRegion (methodDeclaration.Body.LBraceToken.GetPrevNode ().EndLocation, methodDeclaration.Body.RBraceToken.StartLocation), FoldType.Member));
				return base.VisitMethodDeclaration (methodDeclaration, data);
			}
			
			public override object VisitConstructorDeclaration (ConstructorDeclaration constructorDeclaration, object data)
			{
				if (!constructorDeclaration.Body.IsNull && constructorDeclaration.Body.LBraceToken.StartLocation.Line != constructorDeclaration.Body.RBraceToken.StartLocation.Line)
					Foldings.Add (new FoldingRegion (new DomRegion (constructorDeclaration.Body.LBraceToken.GetPrevNode ().EndLocation, constructorDeclaration.Body.RBraceToken.StartLocation), FoldType.Member));
				return base.VisitConstructorDeclaration (constructorDeclaration, data);
			}
			
			public override object VisitDestructorDeclaration (DestructorDeclaration destructorDeclaration, object data)
			{
				if (!destructorDeclaration.Body.IsNull && destructorDeclaration.Body.LBraceToken.StartLocation.Line != destructorDeclaration.Body.RBraceToken.StartLocation.Line)
					Foldings.Add (new FoldingRegion (new DomRegion (destructorDeclaration.Body.LBraceToken.GetPrevNode ().EndLocation, destructorDeclaration.Body.RBraceToken.StartLocation), FoldType.Member));
				return base.VisitDestructorDeclaration (destructorDeclaration, data);
			}
			
			public override object VisitOperatorDeclaration (OperatorDeclaration operatorDeclaration, object data)
			{
				if (!operatorDeclaration.Body.IsNull && operatorDeclaration.Body.LBraceToken.StartLocation.Line != operatorDeclaration.Body.RBraceToken.StartLocation.Line)
					Foldings.Add (new FoldingRegion (new DomRegion (operatorDeclaration.Body.LBraceToken.GetPrevNode ().EndLocation, operatorDeclaration.Body.RBraceToken.StartLocation), FoldType.Member));
				return base.VisitOperatorDeclaration (operatorDeclaration, data);
			}
			
			public override object VisitPropertyDeclaration (PropertyDeclaration propertyDeclaration, object data)
			{
				if (!propertyDeclaration.LBraceToken.IsNull && propertyDeclaration.LBraceToken.StartLocation.Line != propertyDeclaration.RBraceToken.StartLocation.Line)
					Foldings.Add (new FoldingRegion (new DomRegion (propertyDeclaration.LBraceToken.GetPrevNode ().EndLocation, propertyDeclaration.RBraceToken.StartLocation), FoldType.Member));
				return base.VisitPropertyDeclaration (propertyDeclaration, data);
			}
			
			public override object VisitIndexerDeclaration (IndexerDeclaration indexerDeclaration, object data)
			{
				if (!indexerDeclaration.LBraceToken.IsNull && indexerDeclaration.LBraceToken.StartLocation.Line != indexerDeclaration.RBraceToken.StartLocation.Line)
					Foldings.Add (new FoldingRegion (new DomRegion (indexerDeclaration.LBraceToken.GetPrevNode ().EndLocation, indexerDeclaration.RBraceToken.StartLocation), FoldType.Member));
				return base.VisitIndexerDeclaration (indexerDeclaration, data);
			}
			
			public override object VisitCustomEventDeclaration (CustomEventDeclaration eventDeclaration, object data)
			{
				if (!eventDeclaration.LBraceToken.IsNull && eventDeclaration.LBraceToken.StartLocation.Line != eventDeclaration.RBraceToken.StartLocation.Line)
					Foldings.Add (new FoldingRegion (new DomRegion (eventDeclaration.LBraceToken.GetPrevNode ().EndLocation, eventDeclaration.RBraceToken.StartLocation), FoldType.Member));
				return base.VisitCustomEventDeclaration (eventDeclaration, data);
			}
			
			public override object VisitSwitchStatement (SwitchStatement switchStatement, object data)
			{
				if (!switchStatement.RBraceToken.IsNull && switchStatement.LBraceToken.StartLocation.Line != switchStatement.RBraceToken.StartLocation.Line)
					Foldings.Add (new FoldingRegion (new DomRegion (switchStatement.LBraceToken.GetPrevNode ().EndLocation, switchStatement.RBraceToken.StartLocation), FoldType.Member));
				return base.VisitSwitchStatement (switchStatement, data);
			}
			
			public override object VisitBlockStatement (BlockStatement blockStatement, object data)
			{
				if (!(blockStatement.Parent is EntityDeclaration) && blockStatement.EndLocation.Line - blockStatement.StartLocation.Line > 2) {
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
			var cmt = new MonoDevelop.Ide.TypeSystem.Comment (comment.Content);
			cmt.CommentStartsLine = comment.StartsLine;
			switch (comment.CommentType) {
			case SpecialsBag.CommentType.Multi:
				cmt.CommentType = MonoDevelop.Ide.TypeSystem.CommentType.Block;
				cmt.OpenTag = "/*";
				cmt.ClosingTag = "*/";
				break;
			case SpecialsBag.CommentType.Single:
				cmt.CommentType = MonoDevelop.Ide.TypeSystem.CommentType.SingleLine;
				cmt.OpenTag = "//";
				break;
			case SpecialsBag.CommentType.Documentation:
				cmt.CommentType = MonoDevelop.Ide.TypeSystem.CommentType.Documentation;
				cmt.IsDocumentation = true;
				cmt.OpenTag = "///";
				break;
			}
			cmt.Region = new DomRegion (comment.Line, comment.Col, comment.EndLine, comment.EndCol);
			result.Comments.Add (cmt);
			var trimmedContent = comment.Content.TrimStart ();
			foreach (string tag in tagComments) {
				if (!trimmedContent.StartsWith (tag))
					continue;
				result.Add (new MonoDevelop.Ide.TypeSystem.Tag (tag, comment.Content, cmt.Region));
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
					DomRegion dr = new DomRegion (start.Line, loc.Column, directive.EndLine, directive.EndCol);
					result.Add (new FoldingRegion (start.Arg, dr, FoldType.UserRegion, true));
				}
				break;
			}
		}

		public static ICSharpCode.NRefactory.CSharp.CompilerSettings GetCompilerArguments (MonoDevelop.Projects.Project project)
		{
			var compilerArguments = new ICSharpCode.NRefactory.CSharp.CompilerSettings ();
	///		compilerArguments.TabSize = 1;

			if (project == null || MonoDevelop.Ide.IdeApp.Workspace == null) {
				compilerArguments.AllowUnsafeBlocks = true;
				return compilerArguments;
			}

			var configuration = project.GetConfiguration (MonoDevelop.Ide.IdeApp.Workspace.ActiveConfiguration) as DotNetProjectConfiguration;
			var par = configuration != null ? configuration.CompilationParameters as CSharpCompilerParameters : null;
			
			if (par == null)
				return compilerArguments;
				
			if (!string.IsNullOrEmpty (par.DefineSymbols)) {
				foreach (var sym in par.DefineSymbols.Split (';', ',', ' ', '\t').Where (s => !string.IsNullOrWhiteSpace (s)))
					compilerArguments.ConditionalSymbols.Add (sym);
			}
			
			compilerArguments.AllowUnsafeBlocks = par.UnsafeCode;
			compilerArguments.LanguageVersion = ConvertLanguageVersion (par.LangVersion);
			compilerArguments.CheckForOverflow = par.GenerateOverflowChecks;
			compilerArguments.WarningLevel = par.WarningLevel;
			compilerArguments.TreatWarningsAsErrors = par.TreatWarningsAsErrors;
			if (!string.IsNullOrEmpty (par.NoWarnings)) {
				foreach (var warning in par.NoWarnings.Split (';', ',', ' ', '\t')) {
					int w;
					try {
						w = int.Parse (warning);
					} catch (Exception) {
						continue;
					}
					compilerArguments.DisabledWarnings.Add (w);
				}
			}
			
			return compilerArguments;
		}
		
		static Version ConvertLanguageVersion (LangVersion ver)
		{
			switch (ver) {
			case LangVersion.Default:
				return new Version (5, 0, 0, 0);
			case LangVersion.ISO_1:
				return new Version (1, 0, 0, 0);
			case LangVersion.ISO_2:
				return new Version (2, 0, 0, 0);
			case LangVersion.Version3:
				return new Version (3, 0, 0, 0);
			case LangVersion.Version4:
				return new Version (4, 0, 0, 0);
			case LangVersion.Version5:
				return new Version (5, 0, 0, 0);
			}
			return new Version (5, 0, 0, 0);;
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

