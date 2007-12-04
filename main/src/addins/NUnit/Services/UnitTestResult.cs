//
// UnitTestResult.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Collections;
using System.Xml.Serialization;
using System.Globalization;

namespace MonoDevelop.NUnit
{
	public class UnitTestResult
	{
		DateTime testDate;
		ResultStatus status;
		TimeSpan time;
		string message;
		string output;
		string stackTrace;
		int totalFailures;
		int totalSuccess;
		int totalIgnored;
		string cerror;
		
		public UnitTestResult ()
		{
		}
		
		public static UnitTestResult CreateFailure (Exception ex)
		{
			UnitTestResult res = new UnitTestResult ();
			res.status = ResultStatus.Failure;
			res.Message = ex.Message;
			res.stackTrace = ex.StackTrace;
			return res;
		}
		
		public static UnitTestResult CreateFailure (string message, Exception ex)
		{
			UnitTestResult res = new UnitTestResult ();
			res.status = ResultStatus.Failure;
			res.Message = message;
			res.stackTrace = ex.Message + "\n" + ex.StackTrace;
			return res;
		}
		
		public static UnitTestResult CreateIgnored (string message)
		{
			UnitTestResult res = new UnitTestResult ();
			res.status = ResultStatus.Ignored;
			res.Message = message;
			return res;
		}
		
		public static UnitTestResult CreateSuccess ()
		{
			UnitTestResult res = new UnitTestResult ();
			res.status = ResultStatus.Success;
			return res;
		}
		
		public DateTime TestDate {
			get { return testDate; }
			set { testDate = value; }
		}
		
		public ResultStatus Status {
			get { return status; }
			set { status = value; }
		}
		
		public bool IsFailure {
			get { return (status & ResultStatus.Failure) != 0; }
		}
		
		public bool IsIgnored {
			get { return (status & ResultStatus.Ignored) != 0; }
		}
		
		public bool IsSuccess {
			get { return (status & ResultStatus.Success) != 0; }
		}
		
		public int TotalFailures {
			get { return totalFailures; }
			set { totalFailures = value; }
		}
		
		public int TotalSuccess {
			get { return totalSuccess; }
			set { totalSuccess = value; }
		}
		
		public int TotalIgnored {
			get { return totalIgnored; }
			set { totalIgnored = value; }
		}
		
		public TimeSpan Time {
			get { return time; }
			set { time = value; }
		}
		
		public string Message {
			get { return message; }
			set { message = value; }
		}
		
		public string StackTrace {
			get { return stackTrace; }
			set { stackTrace = value; }
		}
		
		public string ConsoleOutput {
			get { return output; }
			set { output = value; }
		}
		
		public string ConsoleError {
			get { return cerror; }
			set { cerror = value; }
		}
	}
}

