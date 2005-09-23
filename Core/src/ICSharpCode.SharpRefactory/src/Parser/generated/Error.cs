using System;
using System.Collections;

namespace ICSharpCode.SharpRefactory.Parser
{
	public delegate void ErrorCodeProc(int line, int col, int n);
	public delegate void ErrorMsgProc(int line, int col, string msg);

	public struct ErrorInfo
	{
		public int Column;
		public int Line;
		public string Message;

		public ErrorInfo (int line, int column, string message)
		{
			Column = column;
			Line = line;
			Message = message;
		}

		public override string ToString ()
		{
			return String.Format ("-- line {0} col {1} : {2}", Line, Column, Message);
		}
	}
	
	public class Errors
	{
		public int count = 0;	// number of errors detected
		public ErrorCodeProc SynErr;
		public ErrorCodeProc SemErr;
		public ErrorMsgProc  Error;
		ArrayList errorInfo;

		public Errors()
		{
			errorInfo = new ArrayList ();
			SynErr = new ErrorCodeProc(DefaultCodeError);  // syntactic errors
			SemErr = new ErrorCodeProc(DefaultCodeError);  // semantic errors
			Error  = new ErrorMsgProc(DefaultMsgError);    // user defined string based errors
		}

		public ErrorInfo[] ErrorInformation
		{
			get {
				return (ErrorInfo[]) errorInfo.ToArray (typeof (ErrorInfo));
			}
		}
		
		void DefaultCodeError (int line, int col, int n)
		{
			errorInfo.Add (new ErrorInfo (line, col, String.Format ("error {0}", n)));
			count++;
		}
	
		void DefaultMsgError (int line, int col, string s) {
			errorInfo.Add (new ErrorInfo (line, col, s));
			count++;
		}
	}
}

