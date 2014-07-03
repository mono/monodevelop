//
// JavaScriptParsedDocument.cs
//
// Author:
//       Harsimran Bath <harsimranbath@gmail.com>
//
// Copyright (c) 2014 Harsimran
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;

namespace MonoDevelop.JavaScript
{
	class JavaScriptParsedDocument : ParsedDocument
	{

		#region Variables

		string fileName;
		string content;
		IList<Error> errors;
		IList<FoldingRegion> foldings;

		#endregion

		#region Properties

		public override string FileName {
			get {
				return fileName;
			}
		}

		public IEnumerable<Jurassic.Compiler.JSAstNode> AstNodes { get; set; }

		public override IList<Error> Errors {
			get {
				return errors;
			}
		}

		public override IEnumerable<FoldingRegion> Foldings {
			get {
				return foldings;
			}
		}

		public SimpleJSAst SimpleAst {
			get;
			private set;
		}

		#endregion

		#region Constructor

		public JavaScriptParsedDocument (string fileName, System.IO.TextReader content)
			: base (fileName)
		{
			this.fileName = fileName;
			this.content = content.ReadToEnd ();

			errors = new List<Error> ();
			foldings = new List<FoldingRegion> ();

			parse ();
		}


		public JavaScriptParsedDocument (string fileName, string content)
			: base (fileName)
		{
			this.fileName = fileName;
			this.content = content;

			errors = new List<Error> ();
			foldings = new List<FoldingRegion> ();

			parse ();
		}

		#endregion

		#region Private Methods

		void parse ()
		{
			var scriptEngine = new Jurassic.ScriptEngine ();
			var scriptSource = new Jurassic.StringScriptSource (content);
			var lexar = new Jurassic.Compiler.Lexer (scriptEngine, scriptSource);
			var scope = scriptEngine.CreateGlobalScope ();
			var compilerOptions = new Jurassic.Compiler.CompilerOptions ();
			var parser = new Jurassic.Compiler.Parser (scriptEngine, lexar, scope, compilerOptions, Jurassic.Compiler.CodeContext.Global);
			SimpleAst = new SimpleJSAst ();

			Jurassic.Compiler.Statement parserResult = null;
			try {
				parserResult = parser.Parse ();
			} catch (Jurassic.JavaScriptException jsException) {
				errors.Add (new Error (ErrorType.Error, jsException.Message, jsException.LineNumber, 0));
			} catch (Exception ex) {
				errors.Add (new Error (ErrorType.Unknown, ex.Message));
			}

			if (parserResult != null) {
				errors = parser.Errors;
				AstNodes = parserResult.ChildNodes;
				setFoldings (AstNodes);
				setComments (parser.Comments);
				SimpleAst = SimpleJSAstFactory.CreateFromJavaScriptParsedDocument (AstNodes, FileName);
			}
		}

		void setFoldings (IEnumerable<Jurassic.Compiler.JSAstNode> astNodes)
		{
			if (astNodes == null)
				return;

			foreach (var node in astNodes) {
				var function = node as Jurassic.Compiler.FunctionStatement;
				if (function != null) {
					if (function.SourceSpan != null) {
						var region = DomRegionFactory.CreateDomRegion (fileName, function.SourceSpan);
						foldings.Add (new FoldingRegion (region));
					}

					setFoldings (function.BodyRoot.ChildNodes);

					continue;
				}

				var literal = node as Jurassic.Compiler.LiteralExpression;
				if (literal != null) {
					if (literal.Value != null) {
						var properties = literal.Value as Dictionary<string, object>;
						if (properties != null) {
							foreach (var value in properties.Values) {
								var objFuncExpression = value as Jurassic.Compiler.FunctionExpression;
								if (objFuncExpression != null) {
									setFoldingsForFunctionExpression (objFuncExpression);
									continue;
								}

								var objGetSetFunc = value as Jurassic.Compiler.Parser.ObjectLiteralAccessor;
								if (objGetSetFunc != null) {
									setFoldingsForFunctionExpression (objGetSetFunc.Getter);
									setFoldingsForFunctionExpression (objGetSetFunc.Setter);
								}
							}
						}
					}

					setFoldings (literal.ChildNodes);

					continue;
				}

				var blockStatement = node as Jurassic.Compiler.BlockStatement;
				if (blockStatement != null) {
					if (blockStatement.SourceSpan != null) {
						var region = DomRegionFactory.CreateDomRegion (fileName, blockStatement.SourceSpan);
						foldings.Add (new FoldingRegion (region));
					}

					setFoldings (blockStatement.Statements);

					continue;
				}

				var ifStatement = node as Jurassic.Compiler.IfStatement;
				if (ifStatement != null) {
					if (ifStatement.IfClause != null) {
						if (ifStatement.IfClause.SourceSpan != null) {
							var region = DomRegionFactory.CreateDomRegion (fileName, ifStatement.IfClause.SourceSpan);
							foldings.Add (new FoldingRegion (region));
						}
					}
					if (ifStatement.ElseClause != null) {
						if (ifStatement.ElseClause.SourceSpan != null) {
							var region = DomRegionFactory.CreateDomRegion (fileName, ifStatement.ElseClause.SourceSpan);
							foldings.Add (new FoldingRegion (region));
						}

						setFoldings (ifStatement.ElseClause.ChildNodes);
					}

					setFoldings (ifStatement.ChildNodes);

					continue;
				}

				var functionExpression = node as Jurassic.Compiler.FunctionExpression;
				if (functionExpression != null) {
					setFoldingsForFunctionExpression (functionExpression);
					continue;
				}

				setFoldings (node.ChildNodes);
			}
		}

		void setFoldingsForFunctionExpression (Jurassic.Compiler.FunctionExpression functionExpression)
		{
			if (functionExpression == null)
				return;

			if (functionExpression.SourceSpan != null) {
				if (functionExpression.SourceSpan.EndLine > functionExpression.SourceSpan.StartLine) {
					var region = DomRegionFactory.CreateDomRegion (fileName, functionExpression.SourceSpan);
					foldings.Add (new FoldingRegion (region));
				}
			}

			setFoldings (functionExpression.BodyRoot.ChildNodes);
		}

		void setComments (List<Jurassic.Compiler.Comment> comments)
		{
			foreach (var comment in comments) {
				this.Comments.Add (new Comment (comment.CommentData));

				var multilineComment = comment as Jurassic.Compiler.MultiLineComment;
				if (multilineComment != null) {
					if (multilineComment.EndLine > multilineComment.StartLine) {
						var region = DomRegionFactory.CreateDomRegion (fileName, multilineComment.StartLine, multilineComment.StartColumn, multilineComment.EndLine, multilineComment.EndColumn);
						foldings.Add (new FoldingRegion (region));
					}
				}
			}
		}

		#endregion

	}
}
