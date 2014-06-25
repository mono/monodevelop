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
using System.Collections.Generic;
using System.Text;

namespace MonoDevelop.NUnit
{
	/// <summary>
	/// Data used to identify a test during discovery and execution.
	/// </summary>
	[Serializable]
	public class TestCase
	{
		readonly string name;

		public string Name {
			get {
				return name;
			}
		}

		public string Source {
			get;
			set;
		}

		public TestCase (string name)
		{
			if (String.IsNullOrEmpty (name))
				throw new ArgumentException ("Name cannot be null or emptry", "name");

			this.name = name;
		}
	}

	[Serializable]
	public sealed class TestCaseResult
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
	/// that is exposed as a part of the interface.
	/// </summary>
	public class TestCaseWrapper: IComparable<TestCaseWrapper>
	{
		readonly TestCase wrapped;

		public TestCase TestCase {
			get {
				return wrapped;
			}
		}

		string[] nameParts;

		public string[] NameParts {
			get {
				if (nameParts == null)
					nameParts = Split(wrapped.Name);

				return nameParts;
			}
		}

		public TestCaseWrapper (TestCase testCase)
		{
			wrapped = testCase;
		}

		int IComparable<TestCaseWrapper>.CompareTo (TestCaseWrapper other)
		{
			return wrapped.Name.CompareTo (other.wrapped.Name);
		}

		string[] Split (string name)
		{
			// split string using the dot character as delimiter
			// except the dots escaped with a backslash
			var parts = new List<string> ();
			var builder = new StringBuilder ();
			bool escape = false;
			foreach (char c in name) {
				switch (c) {
				case '\\':
					if (escape) {
						builder.Append (c);
						escape = false;
					} else {
						escape = true;
					}
					break;
				case '.':
					if (escape) {
						builder.Append (c);
						escape = false;
					} else {
						parts.Add (builder.ToString());
						builder.Clear ();
					}
					break;
				default:
					builder.Append (c);
					break;
				}
			}
			parts.Add (builder.ToString());
			return parts.ToArray ();
		}
	}
}

