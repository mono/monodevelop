//
// From Razor Generator (http://razorgenerator.codeplex.com/)
// Licensed under the Microsoft Public License (MS-PL)
//

using System;

namespace RazorGenerator.Core
{
	public class GeneratorErrorEventArgs : EventArgs
	{
		public GeneratorErrorEventArgs(int errorCode, string errorMessage, int lineNumber, int columnNumber)
		{
			ErrorCode = errorCode;
			ErrorMessage = errorMessage;
			LineNumber = lineNumber;
			ColumnNumber = columnNumber;
		}

		public int ErrorCode { get; private set; }

		public string ErrorMessage { get; private set; }

		public int LineNumber { get; private set; }

		public int ColumnNumber { get; private set; }
	}

	public class ProgressEventArgs : EventArgs
	{
		public ProgressEventArgs(uint completed, uint total)
		{
			Completed = completed;
			Total = total;
		}

		public uint Completed { get; private set; }

		public uint Total { get; private set; }
	}
}