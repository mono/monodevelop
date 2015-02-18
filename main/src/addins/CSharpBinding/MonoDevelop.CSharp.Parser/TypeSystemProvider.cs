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
using System.Collections.Generic;
using MonoDevelop.Projects;
using MonoDevelop.CSharp.Project;
using Mono.CSharp;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Ide.Editor;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MonoDevelop.Core.Text;
using System.Threading.Tasks;

namespace MonoDevelop.CSharp.Parser
{
	public class TypeSystemParser : MonoDevelop.Ide.TypeSystem.TypeSystemParser
	{
		static readonly List<Error> emptyList = new List<Error> ();
		public override System.Threading.Tasks.Task<ParsedDocument> Parse (MonoDevelop.Ide.TypeSystem.ParseOptions options, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
		{
			var fileName = options.FileName;
			var project = options.Project;
			var result = new CSharpParsedDocument (fileName);

			if (project != null) {
				
				var projectFile = project.Files.GetFile (fileName);
				if (projectFile != null && !TypeSystemParserNode.IsCompileBuildAction (projectFile.BuildAction))
					result.Flags |= ParsedDocumentFlags.NonSerializable;
			}

//			var tagComments = CommentTag.SpecialCommentTags.Select (t => t.Tag).ToArray ();
			
//			parser.CompilationUnitCallback = delegate (CompilerCompilationUnit top) {
//				foreach (var special in top.SpecialsBag.Specials) {
//					var comment = special as SpecialsBag.Comment;
//					if (comment != null) {
//						VisitComment (result, comment, tagComments);
//					} else {
//						if (storeAst) {
//							var ppd = special as SpecialsBag.PreProcessorDirective;
//							if  (ppd != null)
//								VisitPreprocessorDirective (result, ppd);
//						}
//					}
//				}
//			};
			
			var compilerArguments = GetCompilerArguments (project);
			SyntaxTree unit = null;

			if (project != null) {
				var curDoc = options.RoslynDocument;
				if (curDoc == null) {
					var projectId = TypeSystemService.Workspace.GetProjectId (project);
					var curProject = TypeSystemService.Workspace.CurrentSolution.GetProject (projectId);
					var documentId = TypeSystemService.GetDocument (project, fileName);
					curDoc = curProject.GetDocument (documentId);
				}

				try {
					var model  =  curDoc.GetSemanticModelAsync (cancellationToken).Result;
					unit = model.SyntaxTree;
					result.Ast = model;
				} catch (TaskCanceledException) {
					return Task.FromResult ((ParsedDocument)result);
				}catch (Exception e) {
					LoggingService.LogError ("Error while getting the semantic model for " + fileName, e); 
				}
			}

			if (unit == null) {
				unit = CSharpSyntaxTree.ParseText (SourceText.From (options.Content.Text), compilerArguments, fileName);
			} 

			result.Unit = unit;

			DateTime time;
			try {
				time = System.IO.File.GetLastWriteTimeUtc (fileName);
			} catch (Exception) {
				time = DateTime.UtcNow;
			}
			result.LastWriteTimeUtc = time;
			return Task.FromResult ((ParsedDocument)result);
		}


		class FoldingVisitor : CSharpSyntaxWalker
		{
			public readonly List<FoldingRegion> Foldings = new List<FoldingRegion> ();

			void AddUsings (SyntaxNode parent)
			{
				SyntaxNode firstChild = null, lastChild = null;
				foreach (var child in parent.ChildNodes ()) {
					if (child is UsingDirectiveSyntax) {
						if (firstChild == null) {
							firstChild = child;
						}
						lastChild = child;
						continue;
					}
					if (firstChild != null)
						break;
				}

				if (firstChild != null && firstChild != lastChild) {
					var first = firstChild.GetLocation ().GetLineSpan ();
					var last = lastChild.GetLocation ().GetLineSpan ();

					Foldings.Add (new FoldingRegion (new DocumentRegion (first.StartLinePosition, last.EndLinePosition), FoldType.Undefined));
				}
			}

			public override void VisitCompilationUnit (Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax node)
			{
				AddUsings (node);
				base.VisitCompilationUnit (node);
			}
//
//			static DocumentLocation CorrectEnd (AstNode token)
//			{
//				return new TextLocation (token.EndLocation.Line, token.EndLocation.Column + 1);
//			}
//
//			static bool LastToken(AstNode arg)
//			{
//				return !(arg.Role == Roles.NewLine || arg.Role == Roles.Whitespace || arg.Role == Roles.Comment);
//			}
		

			void AddFolding (SyntaxToken openBrace, SyntaxToken closeBrace)
			{
				openBrace = openBrace.GetPreviousToken (false, false, true, true);

				var first = openBrace.GetLocation ().GetLineSpan ();
				var last = closeBrace.GetLocation ().GetLineSpan ();

				if (first.EndLinePosition.Line != last.EndLinePosition.Line)
					Foldings.Add (new FoldingRegion (new DocumentRegion (first.EndLinePosition, last.EndLinePosition), FoldType.Undefined));
			}


			public override void VisitNamespaceDeclaration (Microsoft.CodeAnalysis.CSharp.Syntax.NamespaceDeclarationSyntax node)
			{
				AddUsings (node);
				AddFolding (node.OpenBraceToken, node.CloseBraceToken);
				base.VisitNamespaceDeclaration (node);
			}

			public override void VisitClassDeclaration (Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax node)
			{
				AddFolding (node.OpenBraceToken, node.CloseBraceToken);
				base.VisitClassDeclaration (node);
			}

			public override void VisitStructDeclaration (Microsoft.CodeAnalysis.CSharp.Syntax.StructDeclarationSyntax node)
			{
				AddFolding (node.OpenBraceToken, node.CloseBraceToken);
				base.VisitStructDeclaration (node);
			}

			public override void VisitInterfaceDeclaration (Microsoft.CodeAnalysis.CSharp.Syntax.InterfaceDeclarationSyntax node)
			{
				AddFolding (node.OpenBraceToken, node.CloseBraceToken);
				base.VisitInterfaceDeclaration (node);
			}

			public override void VisitEnumDeclaration (Microsoft.CodeAnalysis.CSharp.Syntax.EnumDeclarationSyntax node)
			{
				AddFolding (node.OpenBraceToken, node.CloseBraceToken);
				base.VisitEnumDeclaration (node);
			}

			public override void VisitBlock (Microsoft.CodeAnalysis.CSharp.Syntax.BlockSyntax node)
			{
				AddFolding (node.OpenBraceToken, node.CloseBraceToken);
				base.VisitBlock (node);
			}
		}


//
//		void VisitMcsUnit ()
//		{
//		}
//		
//		void VisitComment (ParsedDocument result, SpecialsBag.Comment comment, string[] tagComments)
//		{
//			var cmt = new MonoDevelop.Ide.TypeSystem.Comment (comment.Content);
//			cmt.CommentStartsLine = comment.StartsLine;
//			switch (comment.CommentType) {
//			case SpecialsBag.CommentType.Multi:
//				cmt.CommentType = MonoDevelop.Ide.TypeSystem.CommentType.Block;
//				cmt.OpenTag = "/*";
//				cmt.ClosingTag = "*/";
//				break;
//			case SpecialsBag.CommentType.Single:
//				cmt.CommentType = MonoDevelop.Ide.TypeSystem.CommentType.SingleLine;
//				cmt.OpenTag = "//";
//				break;
//			case SpecialsBag.CommentType.Documentation:
//				cmt.CommentType = MonoDevelop.Ide.TypeSystem.CommentType.Documentation;
//				cmt.IsDocumentation = true;
//				cmt.OpenTag = "///";
//				break;
//			}
//			cmt.Region = new DomRegion (comment.Line, comment.Col, comment.EndLine, comment.EndCol);
//			result.Comments.Add (cmt);
//			var trimmedContent = comment.Content.TrimStart ();
//			foreach (string tag in tagComments) {
//				if (!trimmedContent.StartsWith (tag))
//					continue;
//				result.Add (new Tag (tag, comment.Content, cmt.Region));
//			}
//		}
//		
//		Stack<SpecialsBag.PreProcessorDirective> regions = new Stack<SpecialsBag.PreProcessorDirective> ();
//		Stack<SpecialsBag.PreProcessorDirective> ifBlocks = new Stack<SpecialsBag.PreProcessorDirective> ();
//		List<SpecialsBag.PreProcessorDirective> elifBlocks = new List<SpecialsBag.PreProcessorDirective> ();
//		SpecialsBag.PreProcessorDirective elseBlock = null;
//		
//		Stack<ConditionalRegion> conditionalRegions = new Stack<ConditionalRegion> ();
//		ConditionalRegion ConditionalRegion {
//			get {
//				return conditionalRegions.Count > 0 ? conditionalRegions.Peek () : null;
//			}
//		}
//
//		void CloseConditionBlock (TextLocation loc)
//		{
//			if (ConditionalRegion == null || ConditionalRegion.ConditionBlocks.Count == 0 || !ConditionalRegion.ConditionBlocks[ConditionalRegion.ConditionBlocks.Count - 1].End.IsEmpty)
//				return;
//			ConditionalRegion.ConditionBlocks[ConditionalRegion.ConditionBlocks.Count - 1].End = loc;
//		}
//
//		void AddCurRegion (ParsedDocument result, int line, int col)
//		{
//			if (ConditionalRegion == null)
//				return;
//			ConditionalRegion.End = new TextLocation (line, col);
//			result.Add (ConditionalRegion);
//			conditionalRegions.Pop ();
//		}
//		
//		void VisitPreprocessorDirective (ParsedDocument result, SpecialsBag.PreProcessorDirective directive)
//		{
//			TextLocation loc = new TextLocation (directive.Line, directive.Col);
//			switch (directive.Cmd) {
//			case Tokenizer.PreprocessorDirective.If:
//				conditionalRegions.Push (new ConditionalRegion (directive.Arg));
//				ifBlocks.Push (directive);
//				ConditionalRegion.Start = loc;
//				break;
//			case Tokenizer.PreprocessorDirective.Elif:
//				CloseConditionBlock (new TextLocation (directive.EndLine, directive.EndCol));
//				if (ConditionalRegion != null)
//					ConditionalRegion.ConditionBlocks.Add (new ConditionBlock (directive.Arg, loc));
//				break;
//			case Tokenizer.PreprocessorDirective.Else:
//				CloseConditionBlock (new TextLocation (directive.EndLine, directive.EndCol));
//				if (ConditionalRegion != null)
//					ConditionalRegion.ElseBlock = new DomRegion (loc, TextLocation.Empty);
//				break;
//			case Tokenizer.PreprocessorDirective.Endif:
//				TextLocation endLoc = new TextLocation (directive.EndLine, directive.EndCol);
//				CloseConditionBlock (endLoc);
//				if (ConditionalRegion != null && !ConditionalRegion.ElseBlock.Begin.IsEmpty)
//					ConditionalRegion.ElseBlock = new DomRegion (ConditionalRegion.ElseBlock.Begin, endLoc);
//				AddCurRegion (result, directive.EndLine, directive.EndCol);
//				if (ifBlocks.Count > 0) {
//					var ifBlock = ifBlocks.Pop ();
//					var ifRegion = new DomRegion (ifBlock.Line, ifBlock.Col, directive.EndLine, directive.EndCol);
//					result.Add (new FoldingRegion ("#if " + ifBlock.Arg.Trim (), ifRegion, FoldType.UserRegion, false));
//					foreach (var d in elifBlocks) {
//						var elIlfRegion = new DomRegion (d.Line, d.Col, directive.EndLine, directive.EndCol);
//						result.Add (new FoldingRegion ("#elif " + ifBlock.Arg.Trim (), elIlfRegion, FoldType.UserRegion, false));
//					}
//					if (elseBlock != null) {
//						var elseBlockRegion = new DomRegion (elseBlock.Line, elseBlock.Col, elseBlock.Line, elseBlock.Col);
//						result.Add (new FoldingRegion ("#else", elseBlockRegion, FoldType.UserRegion, false));
//					}
//				}
//				elseBlock = null;
//				break;
//			case Tokenizer.PreprocessorDirective.Define:
//				//result.Add (new PreProcessorDefine (directive.Arg, loc));
//				break;
//			case Tokenizer.PreprocessorDirective.Region:
//				regions.Push (directive);
//				break;
//			case Tokenizer.PreprocessorDirective.Endregion:
//				if (regions.Count > 0) {
//					var start = regions.Pop ();
//					DomRegion dr = new DomRegion (start.Line, start.Col, directive.EndLine, directive.EndCol);
//					result.Add (new FoldingRegion (start.Arg, dr, FoldType.UserRegion, true));
//				}
//				break;
//			}
//		}

		public static CSharpParseOptions GetCompilerArguments (MonoDevelop.Projects.Project project)
		{
			var compilerArguments = new CSharpParseOptions ();
	//		compilerArguments.TabSize = 1;

			if (project == null || MonoDevelop.Ide.IdeApp.Workspace == null) {
				// compilerArguments.AllowUnsafeBlocks = true;
				return compilerArguments;
			}

			var configuration = project.GetConfiguration (MonoDevelop.Ide.IdeApp.Workspace.ActiveConfiguration) as DotNetProjectConfiguration;
			if (configuration == null)
				return compilerArguments;

			compilerArguments = compilerArguments.WithPreprocessorSymbols (configuration.GetDefineSymbols ());

			var par = configuration.CompilationParameters as CSharpCompilerParameters;
			if (par == null)
				return compilerArguments;

			 
			// compilerArguments.AllowUnsafeBlocks = par.UnsafeCode;
			compilerArguments = compilerArguments.WithLanguageVersion (ConvertLanguageVersion (par.LangVersion));
//			compilerArguments.CheckForOverflow = par.GenerateOverflowChecks;

//			compilerArguments.WarningLevel = par.WarningLevel;
//			compilerArguments.TreatWarningsAsErrors = par.TreatWarningsAsErrors;
//			if (!string.IsNullOrEmpty (par.NoWarnings)) {
//				foreach (var warning in par.NoWarnings.Split (';', ',', ' ', '\t')) {
//					int w;
//					try {
//						w = int.Parse (warning);
//					} catch (Exception) {
//						continue;
//					}
//					compilerArguments.DisabledWarnings.Add (w);
//				}
//			}
			
			return compilerArguments;
		}
		
		internal static Microsoft.CodeAnalysis.CSharp.LanguageVersion ConvertLanguageVersion (LangVersion ver)
		{
			switch (ver) {
			case LangVersion.ISO_1:
				return Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp1;
			case LangVersion.ISO_2:
				return Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp2;
			case LangVersion.Version3:
				return Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp3;
			case LangVersion.Version4:
				return Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp4;
			case LangVersion.Version5:
				return Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp5;
			case LangVersion.Version6:
				return Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp6;
			case LangVersion.Default:
				break;
			}
			return Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp6;
		}
	}
}

