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
using System.Text.RegularExpressions;

namespace MonoDevelop.UnitTesting
{
	[Serializable]
	public class UnitTestResult
	{
		DateTime testDate;
		ResultStatus status;
		TimeSpan time;
		string message;
		string output;
		string stackTrace;
		string cerror;
		
		public UnitTestResult ()
		{
		}
		
		public static UnitTestResult CreateFailure (Exception ex)
		{
			UnitTestResult res = new UnitTestResult ();
			res.status = ResultStatus.Failure;
			ex = ex.FlattenAggregate ();
			res.Message = ex.Message;
			res.stackTrace = ex.StackTrace;
			return res;
		}
		
		public static UnitTestResult CreateFailure (string message, Exception ex)
		{
			UnitTestResult res = new UnitTestResult ();
			res.status = ResultStatus.Failure;
			res.Message = message;
			if (ex != null) {
				ex = ex.FlattenAggregate ();
				res.stackTrace = ex.Message + "\n" + ex.StackTrace;
			}
			return res;
		}
		
		public static UnitTestResult CreateIgnored (string message)
		{
			UnitTestResult res = new UnitTestResult ();
			res.status = ResultStatus.Ignored;
			res.Message = message;
			return res;
		}
		
		public static UnitTestResult CreateInconclusive (string message)
		{
			UnitTestResult res = new UnitTestResult ();
			res.status = ResultStatus.Inconclusive;
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
			get { return ErrorsAndFailures > 0; }
		}

		public bool IsSuccess {
			get { return ErrorsAndFailures == 0 && Passed > 0; }
		}

		public bool IsInconclusive {
			get { return Passed == 0 && ErrorsAndFailures == 0 && Inconclusive > 0; }
		}

		public bool IsNotRun {
			get {
				return Passed == 0 && ErrorsAndFailures == 0 && TestsNotRun > 0;
			}
		}

		public int Passed {
			get;
			set;
		}

		public int Errors {
			get;
			set;
		}

		public int Failures {
			get;
			set;
		}

		public int ErrorsAndFailures {
			get {
				return Errors + Failures;
			}
		}

		public int TestsNotRun {
			get {
				return Ignored + NotRunnable + Skipped;
			}
		}

		public int Inconclusive {
			get;
			set;
		}

		public int NotRunnable {
			get;
			set;
		}

		public int Skipped {
			get;
			set;
		}

		public int Ignored {
			get;
			set;
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
		
		public SourceCodeLocation GetFailureLocation ()
		{
			if (string.IsNullOrEmpty (stackTrace))
				return null;
			string[] stackLines = stackTrace.Replace ("\r", "").Split ('\n');
			foreach (string line in stackLines) {
				if (line.IndexOf ("NUnit.Framework") != -1)
					continue;
				Regex r = new Regex (@".*?\(.*?\)\s\[.*?\]\s.*?\s(?<file>.*)\:(?<line>\d*)");
				Match m = r.Match (line);
				if (m.Groups ["file"] != null && m.Groups ["line"] != null && File.Exists (m.Groups ["file"].Value)) {
					int lin;
					if (int.TryParse (m.Groups ["line"].Value, out lin))
						return new SourceCodeLocation (m.Groups ["file"].Value, lin, -1);
				}
			}
			return null;
		}

		public void Add (UnitTestResult res)
		{
			Time += res.Time;
			Passed += res.Passed;
			Errors += res.Errors;
			Failures += res.Failures;
			Ignored += res.Ignored;
			Inconclusive += res.Inconclusive;
			Skipped += res.Skipped;
		}

		public override int GetHashCode ()
		{
			var unknowObject = new {
				Status,
				Passed,
				Errors,
				Failures,
				Inconclusive,
				NotRunnable,
				Skipped,
				Ignored
			};
			return unknowObject.GetHashCode ();
		}

		public override bool Equals (object obj)
		{
			var unitTestResult =  obj as UnitTestResult;
			if (unitTestResult == null)
				return false;
			return EqualsHelper (this, unitTestResult);
		}

		bool EqualsHelper (UnitTestResult firstResult, UnitTestResult secondResult)
		{
			return  firstResult.Status == secondResult.Status &&
					firstResult.Passed == secondResult.Passed &&
					firstResult.Errors == secondResult.Errors &&
					firstResult.Failures == secondResult.Failures &&
					firstResult.Inconclusive == secondResult.Inconclusive &&
					firstResult.NotRunnable == secondResult.NotRunnable &&
					firstResult.Skipped == secondResult.Skipped &&
					firstResult.Ignored == secondResult.Ignored;
		}

	}
}

