//
// ProjectCreateInformationTests.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Projects;
using NUnit.Framework;

namespace MonoDevelop.Ide.Templates
{
	[TestFixture]
	public class ProjectCreateInformationTests
	{
		ProjectCreateInformation projectCreateInfo;

		[SetUp]
		public void Init ()
		{
			projectCreateInfo = new ProjectCreateInformation ();
		}

		[Test]
		public void UnknownParameterUsedInCondition ()
		{
			bool result = projectCreateInfo.ShouldCreate ("UnknownCondition");

			Assert.IsFalse (result);
		}

		[Test]
		public void WhitespaceOnlyInCondition ()
		{
			bool result = projectCreateInfo.ShouldCreate ("    ");

			Assert.IsTrue (result);
		}

		[Test]
		public void NullUsedInCondition ()
		{
			bool result = projectCreateInfo.ShouldCreate (null);

			Assert.IsTrue (result);
		}

		[Test]
		public void EmptyStringUsedInCondition ()
		{
			bool result = projectCreateInfo.ShouldCreate (String.Empty);

			Assert.IsTrue (result);
		}

		[Test]
		public void KnownParameterWhichIsFalseUsedInCondition ()
		{
			projectCreateInfo.Parameters ["test"] = false.ToString ();
			bool result = projectCreateInfo.ShouldCreate ("test");

			Assert.IsFalse (result);
		}

		[Test]
		public void KnownParameterWhichIsTrueUsedInCondition ()
		{
			projectCreateInfo.Parameters ["test"] = true.ToString ();
			bool result = projectCreateInfo.ShouldCreate ("test");

			Assert.IsTrue (result);
		}

		[Test]
		public void KnownParameterWhichIsTrueUsedInConditionWithWhitespaceAtStartAndEndOfCondition ()
		{
			projectCreateInfo.Parameters ["test"] = true.ToString ();
			bool result = projectCreateInfo.ShouldCreate ("  test  ");

			Assert.IsTrue (result);
		}

		[Test]
		public void KnownParameterWhichIsFalseUsedInConditionWithNotOperator ()
		{
			projectCreateInfo.Parameters ["test"] = false.ToString ();
			bool result = projectCreateInfo.ShouldCreate ("!test");

			Assert.IsTrue (result);
		}

		[Test]
		public void KnownParameterWhichIsTrueUsedInConditionWithNotOperator ()
		{
			projectCreateInfo.Parameters ["test"] = true.ToString ();
			bool result = projectCreateInfo.ShouldCreate ("!test");

			Assert.IsFalse (result);
		}

		[Test]
		public void KnownParameterWhichIsFalseUsedInConditionWithNotOperatorWithWhitespace ()
		{
			projectCreateInfo.Parameters ["test"] = false.ToString ();
			bool result = projectCreateInfo.ShouldCreate (" ! test");

			Assert.IsTrue (result);
		}
	}
}

