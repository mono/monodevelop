//
// TestCase.cs
//
// Author:
//       Sergey Khabibullin <sergey@khabibullin.com>
//
// Copyright (c) 2014 Sergey Khabibullin
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
//

using System;

namespace MonoDevelop.NUnit
{
	/// <summary>
	/// Data container used for test discovery/execution needs.
	/// </summary>
	public class TestCase
	{
		readonly string fullName;

		public string FullName {
			get {
				return fullName;
			}
		}

		internal TestCaseDecorator Decorator {
			get;
			set;
		}

		public TestCase (string fullName)
		{
			this.fullName = fullName;
		}
	}

	public class TestCaseResult
	{
		readonly TestCase testCase;

		public TestCase TestCase {
			get {
				return testCase;
			}
		}

		public TestCaseResultStatus Status {
			get;
			set;
		}

		public string Output {
			get;
			set;
		}

		public string Message {
			get;
			set;
		}

		public string StackTrace {
			get;
			set;
		}

		public TestCaseResult (TestCase testCase)
		{
			this.testCase = testCase;
		}
	}

	public enum TestCaseResultStatus
	{
		None,
		Success,
		Failure,
		Ignored,
		Inconclusive
	}

	/// <summary>
	/// Encapsulates internal data without affecting TestCase
	/// that is exposed as a part of external interface.
	/// </summary>
	public class TestCaseDecorator: TestCase, IComparable<TestCase>
	{
		readonly TestCase decorated;

		public TestCase DecoratedTestCase {
			get {
				return decorated;
			}
		}

		string[] nameParts;

		public string[] NameParts {
			get {
				if (nameParts == null)
					nameParts = FullName.Split ('.');
				return nameParts;
			}
		}

		public string DiscovererId { get; set; }

		public UnitTestTreeLeafNode OwnerUnitTest { get; set; }

		public TestCaseDecorator (TestCase testCase)
			: base (testCase.FullName)
		{
			decorated = testCase;
			testCase.Decorator = this;
		}

		int IComparable<TestCase>.CompareTo (TestCase other)
		{
			return FullName.CompareTo (other.FullName);
		}
	}
}

