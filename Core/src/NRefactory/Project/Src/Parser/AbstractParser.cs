// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 915 $</version>
// </file>

using System;
using ICSharpCode.NRefactory.Parser.AST;

namespace ICSharpCode.NRefactory.Parser
{
	/// <summary>
	/// Description of AbstractParser.
	/// </summary>
	public abstract class AbstractParser : IParser
	{
		protected const  int    minErrDist   = 2;
		protected const  string errMsgFormat = "-- line {0} col {1}: {2}";  // 0=line, 1=column, 2=text
		
		protected Errors errors;
		protected ILexer lexer;
		
		protected int    errDist = minErrDist;
		
		protected CompilationUnit compilationUnit;
		
		protected bool parseMethodContents = true;
		
		public bool ParseMethodBodies {
			get {
				return parseMethodContents;
			}
			set {
				parseMethodContents = value;
			}
		}
		
		public ILexer Lexer {
			get {
				return lexer;
			}
		}
		
		public Errors Errors {
			get {
				return errors;
			}
		}
		
		public CompilationUnit CompilationUnit {
			get {
				return compilationUnit;
			}
		}
		
		public AbstractParser(ILexer lexer)
		{
			this.errors = lexer.Errors;
			this.lexer  = lexer;
			errors.SynErr = new ErrorCodeProc(SynErr);
		}
		
		public abstract void Parse();
		
		public abstract Expression ParseExpression();
		
		protected abstract void SynErr(int line, int col, int errorNumber);
			
		protected void SynErr(int n)
		{
			if (errDist >= minErrDist) {
				errors.SynErr(lexer.LookAhead.line, lexer.LookAhead.col, n);
			}
			errDist = 0;
		}
		
		protected void SemErr(string msg)
		{
			if (errDist >= minErrDist) {
				errors.Error(lexer.Token.line, lexer.Token.col, msg);
			}
			errDist = 0;
		}
		
		protected void Expect(int n)
		{
			if (lexer.LookAhead.kind == n) {
				lexer.NextToken();
			} else {
				SynErr(n);
			}
		}
		
		#region System.IDisposable interface implementation
		public void Dispose()
		{
			errors = null;
			lexer.Dispose();
			lexer = null;
		}
		#endregion
	}
}
