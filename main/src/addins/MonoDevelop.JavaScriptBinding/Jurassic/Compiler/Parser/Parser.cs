using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Core;
using MonoDevelop.JavaScript.Factories;

namespace Jurassic.Compiler
{
	/// <summary>
	/// Converts a series of tokens into an abstract syntax tree.
	/// </summary>
	public sealed class Parser
	{
		ScriptEngine engine;
		Lexer lexer;
		SourceCodePosition positionBeforeWhitespace, positionAfterWhitespace;
		Token nextToken;
		bool consumedLineTerminator;
		ParserExpressionState expressionState;
		Scope initialScope;
		Scope currentScope;
		MethodOptimizationHints methodOptimizationHints;
		List<string> labelsForCurrentStatement = new List<string> ();
		Token endToken;
		CompilerOptions options;
		CodeContext context;
		bool strictMode;



		//     INITIALIZATION
		//_________________________________________________________________________________________

		/// <summary>
		/// Creates a Parser instance with the given lexer supplying the tokens.
		/// </summary>
		/// <param name="engine"> The associated script engine. </param>
		/// <param name="lexer"> The lexical analyser that provides the tokens. </param>
		/// <param name="initialScope"> The initial variable scope. </param>
		/// <param name="options"> Options that influence the compiler. </param>
		/// <param name="context"> The context of the code (global, function or eval). </param>
		public Parser (ScriptEngine engine, Lexer lexer, Scope initialScope, CompilerOptions options, CodeContext context)
		{
			if (engine == null)
				throw new ArgumentNullException ("engine");
			if (lexer == null)
				throw new ArgumentNullException ("lexer");
			if (initialScope == null)
				throw new ArgumentNullException ("initialScope");
			this.engine = engine;
			this.lexer = lexer;
			this.lexer.ParserExpressionState = ParserExpressionState.Literal;
			currentScope = this.initialScope = initialScope;
			methodOptimizationHints = new MethodOptimizationHints ();
			this.options = options;
			this.context = context;
			StrictMode = options.ForceStrictMode;
			Consume ();
		}

		/// <summary>
		/// Creates a parser that can read the body of a function.
		/// </summary>
		/// <param name="parser"> The parser for the parent context. </param>
		/// <param name="scope"> The function scope. </param>
		/// <returns> A new parser. </returns>
		static Parser CreateFunctionBodyParser (Parser parser, Scope scope)
		{
			var result = (Parser)parser.MemberwiseClone ();
			result.currentScope = result.initialScope = scope;
			result.methodOptimizationHints = new MethodOptimizationHints ();
			result.context = CodeContext.Function;
			result.endToken = PunctuatorToken.RightBrace;
			result.DirectivePrologueProcessedCallback = null;
			return result;
		}



		//     PROPERTIES
		//_________________________________________________________________________________________

		/// <summary>
		/// Gets the line number of the next token.
		/// </summary>
		public int LineNumber {
			get { return positionAfterWhitespace.Line; }
		}

		/// <summary>
		/// Gets the position just after the last character of the previously consumed token.
		/// </summary>
		public SourceCodePosition PositionBeforeWhitespace {
			get { return positionBeforeWhitespace; }
		}

		internal List<Comment> Comments {
			get { return lexer.Comments; }
		}

		/// <summary>
		/// Gets the position of the first character of the next token.
		/// </summary>
		public SourceCodePosition PositionAfterWhitespace {
			get { return positionAfterWhitespace; }
		}

		public List<Error> Errors { get; set; }

		/// <summary>
		/// Gets the path or URL of the source file.  Can be <c>null</c>.
		/// </summary>
		public string SourcePath {
			get { return lexer.Source.Path; }
		}

		/// <summary>
		/// Gets or sets the scope that variables are declared in.
		/// </summary>
		public Scope Scope {
			get { return currentScope; }
			set { currentScope = value; }
		}

		/// <summary>
		/// Gets or sets the scope that function declarations are declared in.
		/// </summary>
		public Scope InitialScope {
			get { return initialScope; }
			set { initialScope = value; }
		}

		/// <summary>
		/// Gets or sets a value that indicates whether the parser is operating in strict mode.
		/// </summary>
		public bool StrictMode {
			get { return strictMode; }
			set {
				strictMode = value;
				lexer.StrictMode = value;
			}
		}

		/// <summary>
		/// Gets or sets a callback that is called after the directive prologue has been processed.
		/// This callback will be called even if no directive prologue exists.
		/// </summary>
		public Action<Parser> DirectivePrologueProcessedCallback {
			get;
			set;
		}

		/// <summary>
		/// Gets optimization information about the code that was parsed (Parse() must be called
		/// first).
		/// </summary>
		public MethodOptimizationHints MethodOptimizationHints {
			get { return methodOptimizationHints; }
		}



		//     VARIABLES
		//_________________________________________________________________________________________

		/// <summary>
		/// Throws an exception if the variable name is invalid.
		/// </summary>
		/// <param name="name"> The name of the variable to check. </param>
		void ValidateVariableName (string name)
		{
			// In strict mode, the variable name cannot be "eval" or "arguments".
			if (StrictMode && (name == "eval" || name == "arguments"))
				throw new JavaScriptException (engine, "SyntaxError", string.Format ("The variable name cannot be '{0}' in strict mode.", name), LineNumber, SourcePath);

			// Record each occurance of a variable name.
			methodOptimizationHints.EncounteredVariable (name);
		}



		//     TOKEN HELPERS
		//_________________________________________________________________________________________

		/// <summary>
		/// Discards the current token and reads the next one.
		/// </summary>
		/// <param name="expressionState"> Indicates whether the next token can be a literal or an
		/// operator. </param>
		void Consume (ParserExpressionState expressionState = ParserExpressionState.Literal)
		{
			this.expressionState = expressionState;
			lexer.ParserExpressionState = expressionState;
			consumedLineTerminator = false;
			positionBeforeWhitespace = new SourceCodePosition (lexer.LineNumber, lexer.ColumnNumber);
			positionAfterWhitespace = positionBeforeWhitespace;
			while (true) {
				nextToken = lexer.NextToken ();
				if (!(nextToken is WhiteSpaceToken))
					break;
				if (((WhiteSpaceToken)nextToken).LineTerminatorCount > 0)
					consumedLineTerminator = true;
				positionAfterWhitespace = new SourceCodePosition (lexer.LineNumber, lexer.ColumnNumber);
			}
		}

		/// <summary>
		/// Indicates that the next token is identical to the given one.  Throws an exception if
		/// this is not the case.  Consumes the token.
		/// </summary>
		/// <param name="token"> The expected token. </param>
		void Expect (Token token)
		{
			if (nextToken == token)
				Consume ();
			else
				throw new JavaScriptException (engine, "SyntaxError", string.Format ("Expected '{0}' but found {1}", token.Text, Token.ToText (nextToken)), LineNumber, SourcePath);
		}

		/// <summary>
		/// Indicates that the next token should be an identifier.  Throws an exception if this is
		/// not the case.  Consumes the token.
		/// </summary>
		/// <returns> The identifier name. </returns>
		string ExpectIdentifier ()
		{
			var token = nextToken;
			if (token is IdentifierToken) {
				Consume ();
				return ((IdentifierToken)token).Name;
			} else {
				throw new JavaScriptException (engine, "SyntaxError", string.Format ("Expected identifier but found {0}", Token.ToText (nextToken)), LineNumber, SourcePath);
			}
		}

		/// <summary>
		/// Returns a value that indicates whether the current position is a valid position to end
		/// a statement.
		/// </summary>
		/// <returns> <c>true</c> if the current position is a valid position to end a statement;
		/// <c>false</c> otherwise. </returns>
		bool AtValidEndOfStatement ()
		{
			// A statement can be terminator in four ways: by a semi-colon (;), by a right brace (}),
			// by the end of a line or by the end of the program.
			return nextToken == PunctuatorToken.Semicolon ||
			nextToken == PunctuatorToken.RightBrace ||
			consumedLineTerminator ||
			nextToken == null;
		}

		/// <summary>
		/// Indicates that the next token should end the current statement.  This implies that the
		/// next token is a semicolon, right brace or a line terminator.
		/// </summary>
		void ExpectEndOfStatement ()
		{
			if (nextToken == PunctuatorToken.Semicolon)
				Consume ();
			else {
				// Automatic semi-colon insertion.
				// If an illegal token is found then a semicolon is automatically inserted before
				// the offending token if one or more of the following conditions is true: 
				// 1. The offending token is separated from the previous token by at least one LineTerminator.
				// 2. The offending token is '}'.
				if (consumedLineTerminator || nextToken == PunctuatorToken.RightBrace)
					return;

				// If the end of the input stream of tokens is encountered and the parser is unable
				// to parse the input token stream as a single complete ECMAScript Program, then a
				// semicolon is automatically inserted at the end of the input stream.
				if (nextToken == null)
					return;

				// Otherwise, throw an error.
				throw new JavaScriptException (engine, "SyntaxError", string.Format ("Expected ';' but found {0}", Token.ToText (nextToken)), LineNumber, SourcePath);
			}
		}



		//     PARSE METHODS
		//_________________________________________________________________________________________

		/// <summary>
		/// Parses javascript source code.
		/// </summary>
		/// <returns> An expression that can be executed to run the program represented by the
		/// source code. </returns>
		public Statement Parse ()
		{
			// Read the directive prologue.
			Errors = new List<Error> ();
			var result = new BlockStatement (new string[0]);
			while (true) {
				// Check if we should stop parsing.
				if (nextToken == endToken)
					break;

				// A directive must start with a string literal token.  Record it now so that the
				// escape sequence and line continuation information is not lost.
				var directiveToken = nextToken as StringLiteralToken;
				if (directiveToken == null)
					break;

				// Directives cannot have escape sequences or line continuations.
				if (directiveToken.EscapeSequenceCount != 0 || directiveToken.LineContinuationCount != 0)
					break;

				// If the statement starts with a string literal, it must be an expression.
				var beforeInitialToken = PositionAfterWhitespace;
				var expression = ParseExpression (PunctuatorToken.Semicolon);

				// The statement must be added to the AST so that eval("'test'") works.
				var initialStatement = new ExpressionStatement (labelsForCurrentStatement, expression);
				initialStatement.SourceSpan = new SourceCodeSpan (beforeInitialToken, PositionBeforeWhitespace);
				result.Statements.Add (initialStatement);

				// In order for the expression to be part of the directive prologue, it must
				// consist solely of a string literal.
				if (!(expression is LiteralExpression))
					break;

				// Strict mode directive.
				if (directiveToken.Value == "use strict")
					StrictMode = true;

				// Read the end of the statement.  This must happen last so that the lexer has a
				// chance to act on the strict mode flag.
				ExpectEndOfStatement ();
			}

			// Call the directive prologue callback.
			if (DirectivePrologueProcessedCallback != null)
				DirectivePrologueProcessedCallback (this);

			// Read zero or more regular statements.
			while (true) {
				// Check if we should stop parsing.
				if (nextToken == endToken)
					break;

				try {
					// Parse a single statement.
					result.Statements.Add (ParseStatement ());
				} catch (JavaScriptException jsException) {
					Errors.Add (new Error (ErrorType.Error, jsException.Message, jsException.LineNumber, this.lexer.ColumnNumber));
					Consume ();
				} catch (Exception ex) {
					Errors.Add (new Error (ErrorType.Unknown, ex.Message));
					Consume ();
				}
			}

			return result;
		}

		/// <summary>
		/// Parses any statement other than a function declaration.
		/// </summary>
		/// <returns> An expression that represents the statement. </returns>
		Statement ParseStatement ()
		{
			// This is a new statement so clear any labels.
			labelsForCurrentStatement.Clear ();

			// Parse the statement.
			Statement statement = ParseStatementNoNewContext ();

			return statement;
		}

		/// <summary>
		/// Parses any statement other than a function declaration, without beginning a new
		/// statement context.
		/// </summary>
		/// <returns> An expression that represents the statement. </returns>
		Statement ParseStatementNoNewContext ()
		{
			if (nextToken == PunctuatorToken.LeftBrace)
				return ParseBlock ();
			if (nextToken == KeywordToken.Var)
				return ParseVar ();
			if (nextToken == PunctuatorToken.Semicolon)
				return ParseEmpty ();
			if (nextToken == KeywordToken.If)
				return ParseIf ();
			if (nextToken == KeywordToken.Do)
				return ParseDo ();
			if (nextToken == KeywordToken.While)
				return ParseWhile ();
			if (nextToken == KeywordToken.For)
				return ParseFor ();
			if (nextToken == KeywordToken.Continue)
				return ParseContinue ();
			if (nextToken == KeywordToken.Break)
				return ParseBreak ();
			if (nextToken == KeywordToken.Return)
				return ParseReturn ();
			if (nextToken == KeywordToken.With)
				return ParseWith ();
			if (nextToken == KeywordToken.Switch)
				return ParseSwitch ();
			if (nextToken == KeywordToken.Throw)
				return ParseThrow ();
			if (nextToken == KeywordToken.Try)
				return ParseTry ();
			if (nextToken == KeywordToken.Debugger)
				return ParseDebugger ();
			if (nextToken == KeywordToken.Function)
				return ParseFunctionDeclaration ();
			if (nextToken == null)
				throw new JavaScriptException (engine, "SyntaxError", "Unexpected end of input", LineNumber, SourcePath);

			// The statement is either a label or an expression.
			return ParseLabelOrExpressionStatement ();
		}

		/// <summary>
		/// Parses a block of statements.
		/// </summary>
		/// <returns> A BlockStatement containing the statements. </returns>
		/// <remarks> The value of a block statement is the value of the last statement in the block,
		/// or undefined if there are no statements in the block. </remarks>
		BlockStatement ParseBlock ()
		{
			SourceCodePosition start = PositionBeforeWhitespace;

			// Consume the start brace ({).
			Expect (PunctuatorToken.LeftBrace);

			// Read zero or more statements.
			var result = new BlockStatement (labelsForCurrentStatement);
			while (true) {
				// Check for the end brace (}).
				if (nextToken == PunctuatorToken.RightBrace)
					break;

				// Parse a single statement.
				result.Statements.Add (ParseStatement ());
			}

			// Consume the end brace.
			Expect (PunctuatorToken.RightBrace);

			result.SourceSpan = new SourceCodeSpan (start, PositionBeforeWhitespace);

			return result;
		}

		/// <summary>
		/// Parses a var statement.
		/// </summary>
		/// <returns> A var statement. </returns>
		VarStatement ParseVar ()
		{
			var result = new VarStatement (labelsForCurrentStatement, currentScope);

			// Read past the var token.
			Expect (KeywordToken.Var);

			// Keep track of the start of the statement so that source debugging works correctly.
			var start = PositionAfterWhitespace;

			// There can be multiple declarations.
			while (true) {
				var declaration = new VariableDeclaration ();

				// The next token must be a variable name.
				declaration.VariableName = ExpectIdentifier ();
				ValidateVariableName (declaration.VariableName);

				// Add the variable to the current function's list of local variables.
				currentScope.DeclareVariable (declaration.VariableName,
					context == CodeContext.Function ? null : new LiteralExpression (Undefined.Value),
					writable: true, deletable: context == CodeContext.Eval);

				// The next token is either an equals sign (=), a semi-colon or a comma.
				if (nextToken == PunctuatorToken.Assignment) {
					// Read past the equals token (=).
					Expect (PunctuatorToken.Assignment);

					// Read the setter expression.
					declaration.InitExpression = ParseExpression (PunctuatorToken.Semicolon, PunctuatorToken.Comma);
				}

				// Record the portion of the source document that will be highlighted when debugging.
				declaration.SourceSpan = new SourceCodeSpan (start, PositionBeforeWhitespace);

				// Add the declaration to the result.
				result.Declarations.Add (declaration);

				// Check if we are at the end of the statement.
				if (AtValidEndOfStatement () && nextToken != PunctuatorToken.Comma)
					break;

				// Read past the comma token.
				Expect (PunctuatorToken.Comma);

				// Keep track of the start of the statement so that source debugging works correctly.
				start = PositionAfterWhitespace;
			}

			// Consume the end of the statement.
			ExpectEndOfStatement ();

			return result;
		}

		/// <summary>
		/// Parses an empty statement.
		/// </summary>
		/// <returns> An empty statement. </returns>
		Statement ParseEmpty ()
		{
			var result = new EmptyStatement (labelsForCurrentStatement);

			// Keep track of the start of the statement so that source debugging works correctly.
			var start = PositionAfterWhitespace;

			// Read past the semicolon.
			Expect (PunctuatorToken.Semicolon);

			// Record the portion of the source document that will be highlighted when debugging.
			result.SourceSpan = new SourceCodeSpan (start, PositionBeforeWhitespace);

			return result;
		}

		/// <summary>
		/// Parses an if statement.
		/// </summary>
		/// <returns> An expression representing the if statement. </returns>
		IfStatement ParseIf ()
		{
			var result = new IfStatement (labelsForCurrentStatement);

			// Consume the if keyword.
			Expect (KeywordToken.If);

			// Read the left parenthesis.
			Expect (PunctuatorToken.LeftParenthesis);

			// Keep track of the start of the statement so that source debugging works correctly.
			var start = PositionAfterWhitespace;

			// Parse the condition.
			result.Condition = ParseExpression (PunctuatorToken.RightParenthesis);

			// Record the portion of the source document that will be highlighted when debugging.
			result.SourceSpan = new SourceCodeSpan (start, PositionBeforeWhitespace);

			// Read the right parenthesis.
			Expect (PunctuatorToken.RightParenthesis);

			// Read the statements that will be executed when the condition is true.
			result.IfClause = ParseStatement ();

			// Optionally, read the else statement.
			if (nextToken == KeywordToken.Else) {
				// Consume the else keyword.
				Consume ();

				// Read the statements that will be executed when the condition is false.
				result.ElseClause = ParseStatement ();
			}

			return result;
		}

		/// <summary>
		/// Parses a do statement.
		/// </summary>
		/// <returns> An expression representing the do statement. </returns>
		DoWhileStatement ParseDo ()
		{
			var result = new DoWhileStatement (labelsForCurrentStatement);

			// Consume the do keyword.
			Expect (KeywordToken.Do);

			// Read the statements that will be executed in the loop body.
			result.Body = ParseStatement ();

			// Read the while keyword.
			Expect (KeywordToken.While);

			// Read the left parenthesis.
			Expect (PunctuatorToken.LeftParenthesis);

			// Keep track of the start of the statement so that source debugging works correctly.
			var start = PositionAfterWhitespace;

			// Parse the condition.
			start = PositionAfterWhitespace;
			result.ConditionStatement = new ExpressionStatement (ParseExpression (PunctuatorToken.RightParenthesis));
			result.ConditionStatement.SourceSpan = new SourceCodeSpan (start, PositionBeforeWhitespace);

			// Record the portion of the source document that will be highlighted when debugging.
			result.SourceSpan = new SourceCodeSpan (start, PositionBeforeWhitespace);

			// Read the right parenthesis.
			Expect (PunctuatorToken.RightParenthesis);

			// Consume the end of the statement.
			ExpectEndOfStatement ();

			return result;
		}

		/// <summary>
		/// Parses a while statement.
		/// </summary>
		/// <returns> A while statement. </returns>
		WhileStatement ParseWhile ()
		{
			var result = new WhileStatement (labelsForCurrentStatement);

			// Consume the while keyword.
			Expect (KeywordToken.While);

			// Read the left parenthesis.
			Expect (PunctuatorToken.LeftParenthesis);

			// Parse the condition.
			var start = PositionAfterWhitespace;
			result.ConditionStatement = new ExpressionStatement (ParseExpression (PunctuatorToken.RightParenthesis));
			result.ConditionStatement.SourceSpan = new SourceCodeSpan (start, PositionBeforeWhitespace);

			// Read the right parenthesis.
			Expect (PunctuatorToken.RightParenthesis);

			// Read the statements that will be executed in the loop body.
			result.Body = ParseStatement ();

			return result;
		}

		/// <summary>
		/// Parses a for statement or a for-in statement.
		/// </summary>
		/// <returns> A for statement or a for-in statement. </returns>
		Statement ParseFor ()
		{
			// Consume the for keyword.
			Expect (KeywordToken.For);

			// Read the left parenthesis.
			Expect (PunctuatorToken.LeftParenthesis);

			// The initialization statement.
			Statement initializationStatement = null;

			// The for-in expression needs a variable to assign to.  Is null for a regular for statement.
			IReferenceExpression forInReference = null;

			// Keep track of the start of the statement so that source debugging works correctly.
			var start = PositionAfterWhitespace;

			if (nextToken == KeywordToken.Var) {
				// Read past the var token.
				Expect (KeywordToken.Var);

				// There can be multiple initializers (but not for for-in statements).
				var varStatement = new VarStatement (labelsForCurrentStatement, currentScope);
				initializationStatement = varStatement;

				// Only a simple variable name is allowed for for-in statements.
				bool cannotBeForIn = false;

				while (true) {
					var declaration = new VariableDeclaration ();

					// The next token must be a variable name.
					declaration.VariableName = ExpectIdentifier ();
					ValidateVariableName (declaration.VariableName);

					// Add the variable to the current function's list of local variables.
					currentScope.DeclareVariable (declaration.VariableName,
						context == CodeContext.Function ? null : new LiteralExpression (Undefined.Value),
						writable: true, deletable: context == CodeContext.Eval);

					// The next token is either an equals sign (=), a semi-colon, a comma, or the "in" keyword.
					if (nextToken == PunctuatorToken.Assignment) {
						// Read past the equals token (=).
						Expect (PunctuatorToken.Assignment);

						// Read the setter expression.
						declaration.InitExpression = ParseExpression (PunctuatorToken.Semicolon, PunctuatorToken.Comma);

						// Record the portion of the source document that will be highlighted when debugging.
						declaration.SourceSpan = new SourceCodeSpan (start, PositionBeforeWhitespace);

						// This is a regular for statement.
						cannotBeForIn = true;
					}

					// Add the declaration to the initialization statement.
					varStatement.Declarations.Add (declaration);

					if (nextToken == PunctuatorToken.Semicolon) {
						// This is a regular for statement.
						break;
					} else if (nextToken == KeywordToken.In && !cannotBeForIn) {
						// This is a for-in statement.
						forInReference = new NameExpression (currentScope, declaration.VariableName);
						break;
					} else if (nextToken != PunctuatorToken.Comma)
						throw new JavaScriptException (engine, "SyntaxError", string.Format ("Unexpected token {0}", Token.ToText (nextToken)), LineNumber, SourcePath);

					// Read past the comma token.
					Expect (PunctuatorToken.Comma);

					// Keep track of the start of the statement so that source debugging works correctly.
					start = PositionAfterWhitespace;

					// Multiple initializers are not allowed in for-in statements.
					cannotBeForIn = true;
				}

				// Record the portion of the source document that will be highlighted when debugging.
				varStatement.SourceSpan = new SourceCodeSpan (start, PositionBeforeWhitespace);
			} else {
				// Not a var initializer - can be a simple variable name then "in" or any expression ending with a semi-colon.
				// The expression can be empty.
				if (nextToken != PunctuatorToken.Semicolon) {
					// Parse an expression.
					var initializationExpression = ParseExpression (PunctuatorToken.Semicolon, KeywordToken.In);

					// Record debug info for the expression.
					initializationStatement = new ExpressionStatement (initializationExpression);
					initializationStatement.SourceSpan = new SourceCodeSpan (start, PositionBeforeWhitespace);

					if (nextToken == KeywordToken.In) {
						// This is a for-in statement.
						if (!(initializationExpression is IReferenceExpression))
							throw new JavaScriptException (engine, "ReferenceError", "Invalid left-hand side in for-in", LineNumber, SourcePath);
						forInReference = (IReferenceExpression)initializationExpression;
					}
				}
			}

			if (forInReference != null) {
				// for (x in y)
				// for (var x in y)
				var result = new ForInStatement (labelsForCurrentStatement);
				result.Variable = forInReference;
				result.VariableSourceSpan = initializationStatement.SourceSpan;

				// Consume the "in".
				Expect (KeywordToken.In);

				// Parse the right-hand-side expression.
				start = PositionAfterWhitespace;
				result.TargetObject = ParseExpression (PunctuatorToken.RightParenthesis);
				result.TargetObjectSourceSpan = new SourceCodeSpan (start, PositionBeforeWhitespace);

				// Read the right parenthesis.
				Expect (PunctuatorToken.RightParenthesis);

				// Read the statements that will be executed in the loop body.
				result.Body = ParseStatement ();

				return result;
			} else {
				var result = new ForStatement (labelsForCurrentStatement);

				// Set the initialization statement.
				if (initializationStatement != null)
					result.InitStatement = initializationStatement;

				// Read the semicolon.
				Expect (PunctuatorToken.Semicolon);

				// Parse the optional condition expression.
				// Note: if the condition is omitted then it is considered to always be true.
				if (nextToken != PunctuatorToken.Semicolon) {
					start = PositionAfterWhitespace;
					result.ConditionStatement = new ExpressionStatement (ParseExpression (PunctuatorToken.Semicolon));
					result.ConditionStatement.SourceSpan = new SourceCodeSpan (start, PositionBeforeWhitespace);
				}

				// Read the semicolon.
				// Note: automatic semicolon insertion never inserts a semicolon in the header of a
				// for statement.
				Expect (PunctuatorToken.Semicolon);

				// Parse the optional increment expression.
				if (nextToken != PunctuatorToken.RightParenthesis) {
					start = PositionAfterWhitespace;
					result.IncrementStatement = new ExpressionStatement (ParseExpression (PunctuatorToken.RightParenthesis));
					result.IncrementStatement.SourceSpan = new SourceCodeSpan (start, PositionBeforeWhitespace);
				}

				// Read the right parenthesis.
				Expect (PunctuatorToken.RightParenthesis);

				// Read the statements that will be executed in the loop body.
				result.Body = ParseStatement ();

				return result;
			}
		}

		/// <summary>
		/// Parses a continue statement.
		/// </summary>
		/// <returns> A continue statement. </returns>
		ContinueStatement ParseContinue ()
		{
			var result = new ContinueStatement (labelsForCurrentStatement);

			// Keep track of the start of the statement so that source debugging works correctly.
			var start = PositionAfterWhitespace;

			// Consume the continue keyword.
			Expect (KeywordToken.Continue);

			// The continue statement can have an optional label to jump to.
			if (!AtValidEndOfStatement ()) {
				// continue [label]

				// Read the label name.
				result.Label = ExpectIdentifier ();
			}

			// Consume the semi-colon, if there was one.
			ExpectEndOfStatement ();

			// Record the portion of the source document that will be highlighted when debugging.
			result.SourceSpan = new SourceCodeSpan (start, PositionBeforeWhitespace);

			return result;
		}

		/// <summary>
		/// Parses a break statement.
		/// </summary>
		/// <returns> A break statement. </returns>
		BreakStatement ParseBreak ()
		{
			var result = new BreakStatement (labelsForCurrentStatement);

			// Keep track of the start of the statement so that source debugging works correctly.
			var start = PositionAfterWhitespace;

			// Consume the break keyword.
			Expect (KeywordToken.Break);

			// The break statement can have an optional label to jump to.
			if (!AtValidEndOfStatement ()) {
				// break [label]

				// Read the label name.
				result.Label = ExpectIdentifier ();
			}

			// Consume the semi-colon, if there was one.
			ExpectEndOfStatement ();

			// Record the portion of the source document that will be highlighted when debugging.
			result.SourceSpan = new SourceCodeSpan (start, PositionBeforeWhitespace);

			return result;
		}

		/// <summary>
		/// Parses a return statement.
		/// </summary>
		/// <returns> A return statement. </returns>
		ReturnStatement ParseReturn ()
		{
			if (context != CodeContext.Function)
				throw new JavaScriptException (engine, "SyntaxError", "Return statements are only allowed inside functions", LineNumber, SourcePath);

			var result = new ReturnStatement (labelsForCurrentStatement);

			// Keep track of the start of the statement so that source debugging works correctly.
			var start = PositionAfterWhitespace;

			// Consume the return keyword.
			Expect (KeywordToken.Return);

			if (!AtValidEndOfStatement ()) {
				// Parse the return value expression.
				result.Value = ParseExpression (PunctuatorToken.Semicolon);
			}

			// Consume the end of the statement.
			ExpectEndOfStatement ();

			// Record the portion of the source document that will be highlighted when debugging.
			result.SourceSpan = new SourceCodeSpan (start, PositionBeforeWhitespace);

			return result;
		}

		/// <summary>
		/// Parses a with statement.
		/// </summary>
		/// <returns> An expression representing the with statement. </returns>
		WithStatement ParseWith ()
		{
			// This statement is not allowed in strict mode.
			if (StrictMode)
				throw new JavaScriptException (engine, "SyntaxError", "The with statement is not supported in strict mode", LineNumber, SourcePath);

			var result = new WithStatement (labelsForCurrentStatement);

			// Read past the "with" token.
			Expect (KeywordToken.With);

			// Read a left parenthesis token "(".
			Expect (PunctuatorToken.LeftParenthesis);

			// Keep track of the start of the statement so that source debugging works correctly.
			var start = PositionAfterWhitespace;

			// Read an object reference.
			var objectEnvironment = ParseExpression (PunctuatorToken.RightParenthesis);

			// Record the portion of the source document that will be highlighted when debugging.
			result.SourceSpan = new SourceCodeSpan (start, PositionBeforeWhitespace);

			// Read a right parenthesis token ")".
			Expect (PunctuatorToken.RightParenthesis);

			// Create a new scope and assign variables within the with statement to the scope.
			result.Scope = ObjectScope.CreateWithScope (currentScope, objectEnvironment);
			currentScope = result.Scope;

			// Read the body of the with statement.
			result.Body = ParseStatement ();

			// Revert the scope.
			currentScope = currentScope.ParentScope;

			return result;
		}

		/// <summary>
		/// Parses a switch statement.
		/// </summary>
		/// <returns> A switch statement. </returns>
		SwitchStatement ParseSwitch ()
		{
			var result = new SwitchStatement (labelsForCurrentStatement);

			// Consume the switch keyword.
			Expect (KeywordToken.Switch);

			// Read the left parenthesis.
			Expect (PunctuatorToken.LeftParenthesis);

			// Keep track of the start of the statement so that source debugging works correctly.
			var start = PositionAfterWhitespace;

			// Parse the switch expression.
			result.Value = ParseExpression (PunctuatorToken.RightParenthesis);

			// Record the portion of the source document that will be highlighted when debugging.
			result.SourceSpan = new SourceCodeSpan (start, PositionBeforeWhitespace);

			// Read the right parenthesis.
			Expect (PunctuatorToken.RightParenthesis);

			// Consume the start brace ({).
			Expect (PunctuatorToken.LeftBrace);

			SwitchCase defaultClause = null;
			while (true) {
				if (nextToken == KeywordToken.Case) {
					var caseClause = new SwitchCase ();

					// Read the case keyword.
					Expect (KeywordToken.Case);

					// Parse the case expression.
					caseClause.Value = ParseExpression (PunctuatorToken.Colon);

					// Consume the colon.
					Expect (PunctuatorToken.Colon);

					// Zero or more statements can be added to the case statement.
					while (nextToken != KeywordToken.Case && nextToken != KeywordToken.Default && nextToken != PunctuatorToken.RightBrace)
						caseClause.BodyStatements.Add (ParseStatement ());

					// Add the case clause to the switch statement.
					result.CaseClauses.Add (caseClause);
				} else if (nextToken == KeywordToken.Default) {
					// Make sure this is the only default clause.
					if (defaultClause != null)
						throw new JavaScriptException (engine, "SyntaxError", "Only one default clause is allowed.", LineNumber, SourcePath);

					defaultClause = new SwitchCase ();

					// Read the case keyword.
					Expect (KeywordToken.Default);

					// Consume the colon.
					Expect (PunctuatorToken.Colon);

					// Zero or more statements can be added to the case statement.
					while (nextToken != KeywordToken.Case && nextToken != KeywordToken.Default && nextToken != PunctuatorToken.RightBrace)
						defaultClause.BodyStatements.Add (ParseStatement ());

					// Add the default clause to the switch statement.
					result.CaseClauses.Add (defaultClause);
				} else if (nextToken == PunctuatorToken.RightBrace) {
					break;
				} else {
					// Statements cannot be added directly after the switch.
					throw new JavaScriptException (engine, "SyntaxError", "Expected 'case' or 'default'.", LineNumber, SourcePath);
				}
			}

			// Consume the end brace.
			Consume ();

			return result;
		}

		/// <summary>
		/// Parses a throw statement.
		/// </summary>
		/// <returns> A throw statement. </returns>
		ThrowStatement ParseThrow ()
		{
			var result = new ThrowStatement (labelsForCurrentStatement);

			// Keep track of the start of the statement so that source debugging works correctly.
			var start = PositionAfterWhitespace;

			// Consume the throw keyword.
			Expect (KeywordToken.Throw);

			// A line terminator is not allowed here.
			if (consumedLineTerminator)
				throw new JavaScriptException (engine, "SyntaxError", "Illegal newline after throw", LineNumber, SourcePath);

			// Parse the expression to throw.
			result.Value = ParseExpression (PunctuatorToken.Semicolon);

			// Consume the end of the statement.
			ExpectEndOfStatement ();

			// Record the portion of the source document that will be highlighted when debugging.
			result.SourceSpan = new SourceCodeSpan (start, PositionBeforeWhitespace);

			return result;
		}

		/// <summary>
		/// Parses a try statement.
		/// </summary>
		/// <returns> A try-catch-finally statement. </returns>
		TryCatchFinallyStatement ParseTry ()
		{
			var result = new TryCatchFinallyStatement (labelsForCurrentStatement);

			// Consume the try keyword.
			Expect (KeywordToken.Try);

			// Parse the try block.
			result.TryBlock = ParseBlock ();

			// The next token is either 'catch' or 'finally'.
			if (nextToken == KeywordToken.Catch) {
				// Consume the catch token.
				Expect (KeywordToken.Catch);

				// Read the left parenthesis.
				Expect (PunctuatorToken.LeftParenthesis);

				// Read the name of the variable to assign the exception to.
				result.CatchVariableName = ExpectIdentifier ();
				ValidateVariableName (result.CatchVariableName);

				// Read the right parenthesis.
				Expect (PunctuatorToken.RightParenthesis);

				// Create a new scope for the catch variable.
				currentScope = result.CatchScope = DeclarativeScope.CreateCatchScope (currentScope, result.CatchVariableName);

				// Parse the statements inside the catch block.
				result.CatchBlock = ParseBlock ();

				// Revert the scope.
				currentScope = currentScope.ParentScope;
			}

			if (nextToken == KeywordToken.Finally) {
				// Consume the finally token.
				Expect (KeywordToken.Finally);

				// Read the finally statements.
				result.FinallyBlock = ParseBlock ();
			}

			// There must be a catch or finally block.
			if (result.CatchBlock == null && result.FinallyBlock == null)
				throw new JavaScriptException (engine, "SyntaxError", "Missing catch or finally after try", LineNumber, SourcePath);

			return result;
		}

		/// <summary>
		/// Parses a debugger statement.
		/// </summary>
		/// <returns> A debugger statement. </returns>
		DebuggerStatement ParseDebugger ()
		{
			var result = new DebuggerStatement (labelsForCurrentStatement);

			// Keep track of the start of the statement so that source debugging works correctly.
			var start = PositionAfterWhitespace;

			// Consume the debugger keyword.
			Expect (KeywordToken.Debugger);

			// Consume the end of the statement.
			ExpectEndOfStatement ();

			// Record the portion of the source document that will be highlighted when debugging.
			result.SourceSpan = new SourceCodeSpan (start, PositionBeforeWhitespace);

			return result;
		}

		/// <summary>
		/// Parses a function declaration.
		/// </summary>
		/// <returns> A statement representing the function. </returns>
		Statement ParseFunctionDeclaration ()
		{
			var start = PositionAfterWhitespace;

			// Parse the function declaration.
			var expression = ParseFunction (FunctionType.Declaration, initialScope);

			// Add the function to the top-level scope.
			initialScope.DeclareVariable (expression.FunctionName, expression, writable: true, deletable: context == CodeContext.Eval);

			// HB : Commented this and instead return FunctionStatement

			// Function declarations do nothing at the point of declaration - everything happens
			// at the top of the function/global code.
			// return new EmptyStatement(this.labelsForCurrentStatement);
			var sourceSpan = new SourceCodeSpan (start, PositionBeforeWhitespace);
			return new FunctionStatement (expression.FunctionName, expression.ArgumentNames, expression.BodyRoot, sourceSpan, labelsForCurrentStatement);
		}

		enum FunctionType
		{
			Declaration,
			Expression,
			Getter,
			Setter,
		}

		/// <summary>
		/// Parses a function declaration or a function expression.
		/// </summary>
		/// <param name="functionType"> The type of function to parse. </param>
		/// <param name="parentScope"> The parent scope for the function. </param>
		/// <returns> A function expression. </returns>
		FunctionExpression ParseFunction (FunctionType functionType, Scope parentScope, NameExpression nameExpression = null)
		{
			if (functionType != FunctionType.Getter && functionType != FunctionType.Setter) {
				// Consume the function keyword.
				Expect (KeywordToken.Function);
			}

			// Read the function name.
			var functionName = string.Empty;
			if (functionType == FunctionType.Declaration) {
				functionName = ExpectIdentifier ();
			} else if (functionType == FunctionType.Expression) {
				// The function name is optional for function expressions.
				if (nextToken is IdentifierToken)
					functionName = ExpectIdentifier ();
				else {
					if (nameExpression != null)
						functionName = nameExpression.Name;
				}
			} else if (functionType == FunctionType.Getter || functionType == FunctionType.Setter) {
				// Getters and setters can have any name that is allowed of a property.
				bool wasIdentifier;
				functionName = ReadPropertyName (out wasIdentifier);
			} else
				throw new ArgumentOutOfRangeException ("functionType");
			ValidateVariableName (functionName);

			// Read the left parenthesis.
			Expect (PunctuatorToken.LeftParenthesis);

			// Read zero or more argument names.
			var argumentNames = new List<string> ();

			// Read the first argument name.
			if (nextToken != PunctuatorToken.RightParenthesis) {
				var argumentName = ExpectIdentifier ();
				ValidateVariableName (argumentName);
				argumentNames.Add (argumentName);
			}

			while (true) {
				if (nextToken == PunctuatorToken.Comma) {
					// Consume the comma.
					Consume ();

					// Read and validate the argument name.
					var argumentName = ExpectIdentifier ();
					ValidateVariableName (argumentName);
					argumentNames.Add (argumentName);
				} else if (nextToken == PunctuatorToken.RightParenthesis)
					break;
				else
					throw new JavaScriptException (engine, "SyntaxError", "Expected ',' or ')'", LineNumber, SourcePath);
			}

			// Getters must have zero arguments.
			if (functionType == FunctionType.Getter && argumentNames.Count != 0)
				throw new JavaScriptException (engine, "SyntaxError", "Getters cannot have arguments", LineNumber, SourcePath);

			// Setters must have one argument.
			if (functionType == FunctionType.Setter && argumentNames.Count != 1)
				throw new JavaScriptException (engine, "SyntaxError", "Setters must have a single argument", LineNumber, SourcePath);

			// Read the right parenthesis.
			Expect (PunctuatorToken.RightParenthesis);

			// Record the start of the function body.
			var startPosition = PositionBeforeWhitespace;

			// Since the parser reads one token in advance, start capturing the function body here.
			var bodyTextBuilder = new StringBuilder ();
			var originalBodyTextBuilder = lexer.InputCaptureStringBuilder;
			lexer.InputCaptureStringBuilder = bodyTextBuilder;

			// Read the start brace.
			Expect (PunctuatorToken.LeftBrace);

			// This context has a nested function.
			methodOptimizationHints.HasNestedFunction = true;

			// Create a new scope and assign variables within the function body to the scope.
			bool includeNameInScope = functionType != FunctionType.Getter && functionType != FunctionType.Setter;
			var scope = DeclarativeScope.CreateFunctionScope (parentScope, includeNameInScope ? functionName : string.Empty, argumentNames);

			// Read the function body.
			var functionParser = CreateFunctionBodyParser (this, scope);
			var body = functionParser.Parse ();

			// Transfer state back from the function parser.
			nextToken = functionParser.nextToken;
			lexer.StrictMode = StrictMode;
			lexer.InputCaptureStringBuilder = originalBodyTextBuilder;
			if (originalBodyTextBuilder != null)
				originalBodyTextBuilder.Append (bodyTextBuilder);

			SourceCodePosition endPosition;
			if (functionType == FunctionType.Expression) {
				// The end token '}' will be consumed by the parent function.
				if (nextToken != PunctuatorToken.RightBrace)
					throw new JavaScriptException (engine, "SyntaxError", "Expected '}'", LineNumber, SourcePath);

				// Record the end of the function body.
				endPosition = new SourceCodePosition (PositionAfterWhitespace.Line, PositionAfterWhitespace.Column + 1);
			} else {
				// Consume the '}'.
				Expect (PunctuatorToken.RightBrace);

				// Record the end of the function body.
				endPosition = new SourceCodePosition (PositionAfterWhitespace.Line, PositionAfterWhitespace.Column + 1);
			}

			// Create a new function expression.
			var options = this.options.Clone ();
			options.ForceStrictMode = functionParser.StrictMode;
			var context = new FunctionMethodGenerator (engine, functionName,
				              includeNameInScope, argumentNames,
				              bodyTextBuilder.ToString (0, bodyTextBuilder.Length - 1), body,
				              SourcePath, options);
			context.MethodOptimizationHints = functionParser.methodOptimizationHints;

			var sourceSpan = new SourceCodeSpan (startPosition, endPosition);
			return new FunctionExpression (context, sourceSpan);
		}

		/// <summary>
		/// Parses a statement consisting of an expression or starting with a label.  These two
		/// cases are disambiguated here.
		/// </summary>
		/// <returns> A statement. </returns>
		Statement ParseLabelOrExpressionStatement ()
		{
			// Keep track of the start of the statement so that source debugging works correctly.
			var start = PositionAfterWhitespace;

			// Parse the statement as though it was an expression - but stop if there is an unexpected colon.
			var expression = ParseExpression (PunctuatorToken.Semicolon, PunctuatorToken.Colon);

			if (nextToken == PunctuatorToken.Colon && expression is NameExpression) {
				// The expression is actually a label.

				// Extract the label name.
				var labelName = ((NameExpression)expression).Name;
				labelsForCurrentStatement.Add (labelName);

				// Read past the colon.
				Expect (PunctuatorToken.Colon);

				// Read the rest of the statement.
				return ParseStatementNoNewContext ();
			} else {

				// Consume the end of the statement.
				ExpectEndOfStatement ();

				// Create a new expression statement.
				var result = new ExpressionStatement (labelsForCurrentStatement, expression);

				// Record the portion of the source document that will be highlighted when debugging.
				result.SourceSpan = new SourceCodeSpan (start, PositionBeforeWhitespace);

				return result;
			}
		}

		//     EXPRESSION PARSER
		//_________________________________________________________________________________________

		/// <summary>
		/// Represents a key by which to look up an operator.
		/// </summary>
		struct OperatorKey
		{
			public Token Token;
			public bool PostfixOrInfix;

			public override int GetHashCode ()
			{
				return Token.GetHashCode () ^ PostfixOrInfix.GetHashCode ();
			}
		}

		/// <summary>
		/// Gets or sets a mapping from token -> operator.  There can be at most two operators per
		/// token (the prefix version and the infix/postfix version).
		/// </summary>
		static Dictionary<OperatorKey, Operator> operatorLookup;

		/// <summary>
		/// Initializes the token -> operator mapping dictionary.
		/// </summary>
		/// <returns> The token -> operator mapping dictionary. </returns>
		static Dictionary<OperatorKey, Operator> InitializeOperatorLookup ()
		{
			var result = new Dictionary<OperatorKey, Operator> (55);
			foreach (var @operator in Operator.AllOperators) {
				result.Add (new OperatorKey () {
					Token = @operator.Token,
					PostfixOrInfix = @operator.HasLHSOperand
				}, @operator);
				if (@operator.SecondaryToken != null) {
					// Note: the secondary token for the grouping operator and function call operator ')' is a duplicate.
					result [new OperatorKey () {
						Token = @operator.SecondaryToken,
						PostfixOrInfix = @operator.HasRHSOperand
					}] = @operator;
					if (@operator.InnerOperandIsOptional)
						result [new OperatorKey () {
							Token = @operator.SecondaryToken,
							PostfixOrInfix = false
						}] = @operator;
				}
			}
			return result;
		}

		/// <summary>
		/// Finds a operator given a token and an indication whether the prefix or infix/postfix
		/// version is desired.
		/// </summary>
		/// <param name="token"> The token to search for. </param>
		/// <param name="postfixOrInfix"> <c>true</c> if the infix/postfix version of the operator
		/// is desired; <c>false</c> otherwise. </param>
		/// <returns> An Operator instance, or <c>null</c> if the operator could not be found. </returns>
		static Operator OperatorFromToken (Token token, bool postfixOrInfix)
		{
			Operator result;
			if (operatorLookup == null) {
				// Initialize the operator lookup table.
				var temp = InitializeOperatorLookup ();
				Thread.MemoryBarrier ();
				operatorLookup = temp;
			}
			if (!operatorLookup.TryGetValue (new OperatorKey () {
				Token = token,
				PostfixOrInfix = postfixOrInfix
			}, out result))
				return null;
			return result;
		}

		/// <summary>
		/// Parses a javascript expression.
		/// </summary>
		/// <param name="endToken"> A token that indicates the end of the expression. </param>
		/// <returns> An expression tree that represents the expression. </returns>
		Expression ParseExpression (params Token[] endTokens)
		{
			// The root of the expression tree.
			Expression root = null;

			// The active operator, i.e. the one last encountered.
			OperatorExpression unboundOperator = null;

			// Will be used by functionexpression, which has variable defined before
			NameExpression nameExpression = null;

			while (nextToken != null) {
				if (nextToken is LiteralToken ||
				    nextToken is IdentifierToken ||
				    nextToken == KeywordToken.Function ||
				    nextToken == KeywordToken.This ||
				    nextToken == PunctuatorToken.LeftBrace ||
				    (nextToken == PunctuatorToken.LeftBracket && expressionState == ParserExpressionState.Literal) ||
				    (nextToken is KeywordToken && unboundOperator != null && unboundOperator.OperatorType == OperatorType.MemberAccess && expressionState == ParserExpressionState.Literal)) {
					// If a literal was found where an operator was expected, insert a semi-colon
					// automatically (if this would fix the error and a line terminator was
					// encountered) or throw an error.
					if (expressionState != ParserExpressionState.Literal) {
						// Check for automatic semi-colon insertion.
						if (Array.IndexOf (endTokens, PunctuatorToken.Semicolon) >= 0 && consumedLineTerminator)
							break;
						throw new JavaScriptException (engine, "SyntaxError", string.Format ("Expected operator but found {0}", Token.ToText (nextToken)), LineNumber, SourcePath);
					}

					// New in ECMAScript 5 is the ability to use keywords as property names.
					if ((nextToken is KeywordToken || (nextToken is LiteralToken && ((LiteralToken)nextToken).IsKeyword)) &&
					    unboundOperator != null &&
					    unboundOperator.OperatorType == OperatorType.MemberAccess &&
					    expressionState == ParserExpressionState.Literal) {
						nextToken = new IdentifierToken (nextToken.Text);
					}

					Expression terminal;
					if (nextToken is LiteralToken)
						// If the token is a literal, convert it to a literal expression.
						terminal = new LiteralExpression (((LiteralToken)nextToken).Value);
					else if (nextToken is IdentifierToken) {
						// If the token is an identifier, convert it to a NameExpression.
						var identifierName = ((IdentifierToken)nextToken).Name;
						terminal = new NameExpression (currentScope, identifierName);
						nameExpression = terminal as NameExpression;

						// Record each occurance of a variable name.
						if (unboundOperator == null || unboundOperator.OperatorType != OperatorType.MemberAccess)
							methodOptimizationHints.EncounteredVariable (identifierName);
					} else if (nextToken == KeywordToken.This) {
						// Convert "this" to an expression.
						terminal = new ThisExpression ();

						// Add method optimization info.
						methodOptimizationHints.HasThis = true;
					} else if (nextToken == PunctuatorToken.LeftBracket)
						// Array literal.
						terminal = ParseArrayLiteral ();
					else if (nextToken == PunctuatorToken.LeftBrace)
						// Object literal.
						terminal = ParseObjectLiteral ();
					else if (nextToken == KeywordToken.Function)
						terminal = ParseFunctionExpression (nameExpression);
					else
						throw new InvalidOperationException ("Unsupported literal type.");

					// Push the literal to the most recent unbound operator, or, if there is none, to
					// the root of the tree.
					if (root == null) {
						// This is the first term in an expression.
						root = terminal;
					} else {
						Debug.Assert (unboundOperator != null && unboundOperator.AcceptingOperands);
						unboundOperator.Push (terminal);
					}
				} else if (nextToken is PunctuatorToken || nextToken is KeywordToken) {
					// The token is an operator (o1).
					Operator newOperator = OperatorFromToken (nextToken, postfixOrInfix: expressionState == ParserExpressionState.Operator);

					// Make sure the token is actually an operator and not just a random keyword.
					if (newOperator == null) {
						// Check if the token is an end token, for example a semi-colon.
						if (Array.IndexOf (endTokens, nextToken) >= 0)
							break;
						// Check for automatic semi-colon insertion.
						if (Array.IndexOf (endTokens, PunctuatorToken.Semicolon) >= 0 && (consumedLineTerminator || nextToken == PunctuatorToken.RightBrace))
							break;
						throw new JavaScriptException (engine, "SyntaxError", string.Format ("Unexpected token {0} in expression.", Token.ToText (nextToken)), LineNumber, SourcePath);
					}

					// Post-fix increment and decrement cannot have a line terminator in between
					// the operator and the operand.
					if (consumedLineTerminator && (newOperator == Operator.PostIncrement || newOperator == Operator.PostDecrement))
						break;

					// There are four possibilities:
					// 1. The token is the second of a two-part operator (for example, the ':' in a
					//    conditional operator.  In this case, we need to go up the tree until we find
					//    an instance of the operator and make that the active unbound operator.
					if (nextToken == newOperator.SecondaryToken) {
						// Traverse down the tree looking for the parent operator that corresponds to
						// this token.
						OperatorExpression parentExpression = null;
						var node = root as OperatorExpression;
						while (node != null) {
							if (node.Operator.Token == newOperator.Token && !node.SecondTokenEncountered)
								parentExpression = node;
							if (node == unboundOperator)
								break;
							node = node.RightBranch;
						}

						// If the operator was not found, then this is a mismatched token, unless
						// it is the end token.  For example, if an unbalanced right parenthesis is
						// found in an if statement then it is merely the end of the test expression.
						if (parentExpression == null) {
							// Check if the token is an end token, for example a right parenthesis.
							if (Array.IndexOf (endTokens, nextToken) >= 0)
								break;
							// Check for automatic semi-colon insertion.
							if (Array.IndexOf (endTokens, PunctuatorToken.Semicolon) >= 0 && consumedLineTerminator)
								break;
							throw new JavaScriptException (engine, "SyntaxError", "Mismatched closing token in expression.", LineNumber, SourcePath);
						}

						// Mark that we have seen the closing token.
						unboundOperator = parentExpression;
						unboundOperator.SecondTokenEncountered = true;
					} else {
						// Check if the token is an end token, for example the comma in a variable
						// declaration.
						if (Array.IndexOf (endTokens, nextToken) >= 0) {
							// But make sure the token isn't inside an operator.
							// For example, in the expression "var x = f(a, b)" the comma does not
							// indicate the start of a new variable clause because it is inside the
							// function call operator.
							bool insideOperator = false;
							var node = root as OperatorExpression;
							while (node != null) {
								if (node.Operator.SecondaryToken != null && !node.SecondTokenEncountered)
									insideOperator = true;
								if (node == unboundOperator)
									break;
								node = node.RightBranch;
							}
							if (!insideOperator)
								break;
						}

						// All the other situations involve the creation of a new operator.
						var newExpression = OperatorExpression.FromOperator (newOperator);

						// 2. The new operator is a prefix operator.  The new operator becomes an operand
						//    of the previous operator.
						if (!newOperator.HasLHSOperand) {
							if (root == null)
								// "!"
								root = newExpression;
							else if (unboundOperator != null && unboundOperator.AcceptingOperands) {
								// "5 + !"
								unboundOperator.Push (newExpression);
							} else {
								// "5 !" or "5 + 5 !"
								// Check for automatic semi-colon insertion.
								if (Array.IndexOf (endTokens, PunctuatorToken.Semicolon) >= 0 && consumedLineTerminator)
									break;
								throw new JavaScriptException (engine, "SyntaxError", "Invalid use of prefix operator.", LineNumber, SourcePath);
							}
						} else {
							// Search up the tree for an operator that has a lower precedence.
							// Because we don't store the parent link, we have to traverse down the
							// tree and take the last one we find instead.
							OperatorExpression lowPrecedenceOperator = null;
							if (unboundOperator == null ||
							    (newOperator.Associativity == OperatorAssociativity.LeftToRight && unboundOperator.Precedence < newOperator.Precedence) ||
							    (newOperator.Associativity == OperatorAssociativity.RightToLeft && unboundOperator.Precedence <= newOperator.Precedence)) {
								// Performance optimization: look at the previous operator first.
								lowPrecedenceOperator = unboundOperator;
							} else {
								// Search for a lower precedence operator by traversing the tree.
								var node = root as OperatorExpression;
								while (node != null && node != unboundOperator) {
									if ((newOperator.Associativity == OperatorAssociativity.LeftToRight && node.Precedence < newOperator.Precedence) ||
									    (newOperator.Associativity == OperatorAssociativity.RightToLeft && node.Precedence <= newOperator.Precedence))
										lowPrecedenceOperator = node;
									node = node.RightBranch;
								}
							}

							if (lowPrecedenceOperator == null) {
								// 3. The new operator has a lower precedence (or if the associativity is left to
								//    right, a lower or equal precedence) than all the parent operators.  The new
								//    operator goes to the root of the tree and the previous operator becomes the
								//    first operand for the new operator.
								if (root != null)
									newExpression.Push (root);
								root = newExpression;
							} else {
								// 4. Otherwise, the new operator can steal the last operand from the previous
								//    operator and then put itself in the place of that last operand.
								if (lowPrecedenceOperator.OperandCount == 0) {
									// "! ++"
									// Check for automatic semi-colon insertion.
									if (Array.IndexOf (endTokens, PunctuatorToken.Semicolon) >= 0 && consumedLineTerminator)
										break;
									throw new JavaScriptException (engine, "SyntaxError", "Invalid use of prefix operator.", LineNumber, SourcePath);
								}
								newExpression.Push (lowPrecedenceOperator.Pop ());
								lowPrecedenceOperator.Push (newExpression);
							}
						}

						unboundOperator = newExpression;
					}
				} else {
					throw new JavaScriptException (engine, "SyntaxError", string.Format ("Unexpected token {0} in expression", Token.ToText (nextToken)), LineNumber, SourcePath);
				}

				// Read the next token.
				Consume (root != null && (unboundOperator == null || !unboundOperator.AcceptingOperands) ? ParserExpressionState.Operator : ParserExpressionState.Literal);
			}

			// Empty expressions are invalid.
			if (root == null)
				throw new JavaScriptException (engine, "SyntaxError", string.Format ("Expected an expression but found {0} instead", Token.ToText (nextToken)), LineNumber, SourcePath);

			// Check the AST is valid.
			CheckASTValidity (root);

			// A literal is the next valid expression token.
			expressionState = ParserExpressionState.Literal;
			lexer.ParserExpressionState = expressionState;

			// Resolve all the unbound operators into real operators.
			return root;
		}

		/// <summary>
		/// Checks the given AST is valid.
		/// </summary>
		/// <param name="root"> The root of the AST. </param>
		void CheckASTValidity (Expression root)
		{
			// Push the root expression onto a stack.
			Stack<Expression> stack = new Stack<Expression> ();
			stack.Push (root);

			while (stack.Count > 0) {
				// Pop the next expression from the stack.
				var expression = stack.Pop () as OperatorExpression;

				// Only operator expressions are checked for validity.
				if (expression == null)
					continue;

				// Check the operator expression has the right number of operands.
				if (!expression.Operator.IsValidNumberOfOperands (expression.OperandCount))
					throw new JavaScriptException (engine, "SyntaxError", "Wrong number of operands", LineNumber, SourcePath);

				// Check the operator expression is closed.
				if (expression.Operator.SecondaryToken != null && !expression.SecondTokenEncountered)
					throw new JavaScriptException (engine, "SyntaxError", string.Format ("Missing closing token '{0}'", expression.Operator.SecondaryToken.Text), LineNumber, SourcePath);

				// Check the child nodes.
				for (int i = 0; i < expression.OperandCount; i++)
					stack.Push (expression.GetRawOperand (i));
			}
		}

		/// <summary>
		/// Parses an array literal (e.g. "[1, 2]").
		/// </summary>
		/// <returns> A literal expression that represents the array literal. </returns>
		LiteralExpression ParseArrayLiteral ()
		{
			// Read past the initial '[' token.
			Debug.Assert (nextToken == PunctuatorToken.LeftBracket);
			Consume ();

			var items = new List<Expression> ();
			while (true) {
				// If the next token is ']', then the array literal is complete.
				if (nextToken == PunctuatorToken.RightBracket)
					break;

				// If the next token is ',', then the array element is undefined.
				if (nextToken == PunctuatorToken.Comma)
					items.Add (null);
				else
					// Otherwise, read the next item in the array.
					items.Add (ParseExpression (PunctuatorToken.Comma, PunctuatorToken.RightBracket));

				// Read past the comma.
				Debug.Assert (nextToken == PunctuatorToken.Comma || nextToken == PunctuatorToken.RightBracket);
				if (nextToken == PunctuatorToken.Comma)
					Consume ();
			}

			// The end token ']' will be consumed by the parent function.
			Debug.Assert (nextToken == PunctuatorToken.RightBracket);

			return new LiteralExpression (items);
		}

		/// <summary>
		/// Used to store the getter and setter for an object literal property.
		/// </summary>
		public class ObjectLiteralAccessor
		{
			public FunctionExpression Getter;
			public FunctionExpression Setter;
		}

		/// <summary>
		/// Parses an object literal (e.g. "{a: 5}").
		/// </summary>
		/// <returns> A literal expression that represents the object literal. </returns>
		LiteralExpression ParseObjectLiteral ()
		{
			// Read past the initial '{' token.
			Debug.Assert (nextToken == PunctuatorToken.LeftBrace);
			Consume ();

			var properties = new Dictionary<string, object> ();
			while (true) {
				// If the next token is '}', then the object literal is complete.
				if (nextToken == PunctuatorToken.RightBrace)
					break;

				// Read the next property name.
				bool mightBeGetOrSet;
				string propertyName = ReadPropertyName (out mightBeGetOrSet);

				// Check if this is a getter or setter.
				Expression propertyValue;
				if (nextToken != PunctuatorToken.Colon && mightBeGetOrSet && (propertyName == "get" || propertyName == "set")) {
					// Parse the function name and body.
					var function = ParseFunction (propertyName == "get" ? FunctionType.Getter : FunctionType.Setter, currentScope);

					// Get the function name.
					var getOrSet = propertyName;
					propertyName = function.FunctionName;

					if (getOrSet == "get") {
						// This is a getter property.
						object existingValue;
						if (!properties.TryGetValue (propertyName, out existingValue))
							// The property has not been seen before.
							properties.Add (propertyName, new ObjectLiteralAccessor () { Getter = function });
						else {
							// Add to the existing property.
							var existingAccessor = existingValue as ObjectLiteralAccessor;
							if (existingAccessor == null)
								throw new JavaScriptException (engine, "SyntaxError", string.Format ("The property '{0}' cannot have both a data property and a getter", propertyName), LineNumber, SourcePath);
							if (existingAccessor.Getter != null)
								throw new JavaScriptException (engine, "SyntaxError", string.Format ("The property '{0}' cannot have multiple getters", propertyName), LineNumber, SourcePath);
							existingAccessor.Getter = function;
						}
					} else {
						// This is a setter property.
						object existingValue;
						if (!properties.TryGetValue (propertyName, out existingValue))
							// The property has not been seen before.
							properties.Add (propertyName, new ObjectLiteralAccessor () { Setter = function });
						else {
							// Add to the existing property.
							var existingAccessor = existingValue as ObjectLiteralAccessor;
							if (existingAccessor == null)
								throw new JavaScriptException (engine, "SyntaxError", string.Format ("The property '{0}' cannot have both a data property and a setter", propertyName), LineNumber, SourcePath);
							if (existingAccessor.Setter != null)
								throw new JavaScriptException (engine, "SyntaxError", string.Format ("The property '{0}' cannot have multiple setters", propertyName), LineNumber, SourcePath);
							existingAccessor.Setter = function;
						}
					}
				} else {
					// This is a regular property.

					// Read the colon.
					Expect (PunctuatorToken.Colon);

					// Now read the property value.
					propertyValue = ParseExpression (PunctuatorToken.Comma, PunctuatorToken.RightBrace);

					// In strict mode, properties cannot be added twice.
					object existingValue;
					if (properties.TryGetValue (propertyName, out existingValue)) {
						if (existingValue is ObjectLiteralAccessor)
							throw new JavaScriptException (engine, "SyntaxError", string.Format ("The property '{0}' cannot have both a data property and a getter/setter", propertyName), LineNumber, SourcePath);
						if (StrictMode)
							throw new JavaScriptException (engine, "SyntaxError", string.Format ("The property '{0}' already has a value", propertyName), LineNumber, SourcePath);
					}

					// Add the property setter to the list.
					properties [propertyName] = propertyValue;
				}

				// Read past the comma.
				Debug.Assert (nextToken == PunctuatorToken.Comma || nextToken == PunctuatorToken.RightBrace);
				if (nextToken == PunctuatorToken.Comma)
					Consume ();
			}

			// The end token '}' will be consumed by the parent function.
			Debug.Assert (nextToken == PunctuatorToken.RightBrace);

			return new LiteralExpression (properties);
		}

		/// <summary>
		/// Reads a property name, used in object literals.
		/// </summary>
		/// <param name="wasIdentifier"> Receives <c>true</c> if the property name was identifier;
		/// <c>false</c> otherwise. </param>
		/// <returns> The property name that was read. </returns>
		string ReadPropertyName (out bool wasIdentifier)
		{
			string propertyName;
			if (nextToken is LiteralToken) {
				// The property name can be a string or a number or (in ES5) a keyword.
				if (((LiteralToken)nextToken).IsKeyword) {
					// false, true or null.
					propertyName = nextToken.Text;
				} else {
					object literalValue = ((LiteralToken)nextToken).Value;
					if (!(literalValue is string || literalValue is double || literalValue is int))
						throw new JavaScriptException (engine, "SyntaxError", string.Format ("Expected property name but found {0}", Token.ToText (nextToken)), LineNumber, SourcePath);
					propertyName = ((LiteralToken)nextToken).Value.ToString ();
				}
				wasIdentifier = false;
			} else if (nextToken is IdentifierToken) {
				// An identifier is also okay.
				propertyName = ((IdentifierToken)nextToken).Name;
				wasIdentifier = true;
			} else if (nextToken is KeywordToken) {
				// In ES5 a keyword is also okay.
				propertyName = ((KeywordToken)nextToken).Name;
				wasIdentifier = false;
			} else
				throw new JavaScriptException (engine, "SyntaxError", string.Format ("Expected property name but found {0}", Token.ToText (nextToken)), LineNumber, SourcePath);

			// Consume the token.
			Consume ();

			// Return the property name.
			return propertyName;
		}

		/// <summary>
		/// Parses a function expression.
		/// </summary>
		/// <returns> A function expression. </returns>
		FunctionExpression ParseFunctionExpression (NameExpression nameExpression = null)
		{
			return ParseFunction (FunctionType.Expression, currentScope, nameExpression);
		}
	}

}