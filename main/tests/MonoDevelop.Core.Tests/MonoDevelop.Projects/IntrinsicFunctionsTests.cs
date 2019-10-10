//
// IntrinsicFunctionsTests.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2019 Microsoft Inc.
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
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Build.Evaluation;
using NUnit.Framework;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class IntrinsicFunctionsTests
	{
		[Test]
		public void TypeContainsOnlyMethods()
		{
			// MSBuildEvaluationContext.cachedIntrinsicFunctions only caches methods and code path only takes that into account
			foreach (var member in typeof(IntrinsicFunctions).GetMembers(BindingFlags.NonPublic | BindingFlags.Static)) {
				// Skip compiler generated code, such as lambda classes
				if (member.IsDefined (typeof (CompilerGeneratedAttribute)))
					continue;

				Assert.AreEqual (MemberTypes.Method, member.MemberType);
			}
		}
	}
}
