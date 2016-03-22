//
// RemoteTestResult.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Core.Execution;

namespace MonoDevelop.UnitTesting.NUnit
{
	[MessageDataType]
	public class RunRequest: BinaryMessage<RunResponse>
	{
		[MessageDataProperty]
		public string[] NameFilter { get; set; }

		[MessageDataProperty]
		public string Path { get; set; }

		[MessageDataProperty]
		public string SuiteName { get; set; }

		[MessageDataProperty]
		public string[] SupportAssemblies { get; set; }

		[MessageDataProperty]
		public string TestRunnerType { get; set; }

		[MessageDataProperty]
		public string TestRunnerAssembly { get; set; }

		[MessageDataProperty]
		public string CrashLogFile { get; set; }
	}

	[MessageDataType]
	public class RunResponse : BinaryMessage
	{
		[MessageDataProperty]
		public RemoteTestResult Result { get; set; }
	}

	[MessageDataType]
	public class GetTestInfoRequest: BinaryMessage<GetTestInfoResponse>
	{
		[MessageDataProperty]
		public string Path { get; set; }

		[MessageDataProperty]
		public string[] SupportAssemblies { get; set; }
	}

	[MessageDataType]
	public class GetTestInfoResponse: BinaryMessage
	{
		[MessageDataProperty]
		public NunitTestInfo Result { get; set; }
	}

	[MessageDataType]
	public class TestStartedMessage: BinaryMessage
	{
		[MessageDataProperty]
		public string TestCase { get; set; }
	}

	[MessageDataType]
	public class TestFinishedMessage: BinaryMessage
	{
		[MessageDataProperty]
		public string TestCase { get; set; }

		[MessageDataProperty]
		public RemoteTestResult Result;
	}

	[MessageDataType]
	public class SuiteStartedMessage: BinaryMessage
	{
		[MessageDataProperty]
		public string Suite { get; set; }
	}

	[MessageDataType]
	public class SuiteFinishedMessage: BinaryMessage
	{
		[MessageDataProperty]
		public string Suite { get; set; }

		[MessageDataProperty]
		public RemoteTestResult Result;
	}


	[MessageDataType]
	public class RemoteTestResult
	{
		[MessageDataProperty]
		public DateTime TestDate { get; set; }

		[MessageDataProperty]
		public RemoteResultStatus Status { get; set; }

		[MessageDataProperty]
		public int Passed { get; set; }

		[MessageDataProperty]
		public int Errors { get; set; }

		[MessageDataProperty]
		public int Failures { get; set; }

		[MessageDataProperty]
		public int Inconclusive { get; set; }

		[MessageDataProperty]
		public int NotRunnable { get; set; }

		[MessageDataProperty]
		public int Skipped { get; set; }

		[MessageDataProperty]
		public int Ignored { get; set; }

		[MessageDataProperty]
		public TimeSpan Time { get; set; }

		[MessageDataProperty]
		public string Message { get; set; }

		[MessageDataProperty]
		public string StackTrace { get; set; }

		[MessageDataProperty]
		public string ConsoleOutput { get; set; }

		[MessageDataProperty]
		public string ConsoleError { get; set; }
	}

	[MessageDataType]
	[Serializable]
	public class NunitTestInfo
	{
		[MessageDataProperty]
		public string Name { get; set; }

		[MessageDataProperty]
		public string PathName { get; set; }

		[MessageDataProperty]
		public string TestId { get; set; }

		[MessageDataProperty]
		public string FixtureTypeName { get; set; }

		[MessageDataProperty]
		public string FixtureTypeNamespace { get; set; }

		[MessageDataProperty]
		public bool IsExplicit { get; set; }

		[MessageDataProperty]
		public NunitTestInfo[] Tests { get; set; }
	}

	[Flags]
	public enum RemoteResultStatus
	{
		Success = 1,
		Failure = 2,
		Ignored = 4,
		Inconclusive = 8
	}
}

