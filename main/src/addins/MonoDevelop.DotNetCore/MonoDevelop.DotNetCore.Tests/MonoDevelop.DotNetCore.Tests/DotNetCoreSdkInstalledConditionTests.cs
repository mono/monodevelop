//
// DotNetCoreSdkInstalledConditionTests.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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
using System.Linq;
using System.Xml;
using Mono.Addins;
using NUnit.Framework;

namespace MonoDevelop.DotNetCore.Tests
{
	[TestFixture]
	class DotNetCoreSdkInstalledConditionTests : DotNetCoreVersionsRestorerTestBase
	{
		[TestCase ("<Condition sdkVersion='2.*' />", "2.1.4", true)]
		[TestCase ("<Condition sdkVersion='2.*' />", "2.0.3", true)]
		[TestCase ("<Condition sdkVersion='2.*' />", "2.0.0", true)]
		[TestCase ("<Condition sdkVersion='2.*' />", "1.0.0", false)]
		[TestCase ("<Condition sdkVersion='1.*' />", "1.0.0", true)]
		[TestCase ("<Condition sdkVersion='1.*' />", "1.1.0", true)]
		[TestCase ("<Condition sdkVersion='2.0' />", "2.1.4", true)]
		[TestCase ("<Condition sdkVersion='2.0' />", "2.0.3", true)]

		// Here the sdkVersion is the logical version and not the actual version
		// .NET Core SDK 2.1.4 supports .NET Core 2.0 so this is treated as the '2.0' SDK.
		// .NET Core SDK 2.1.300 supports .NET Core 2.1 so this is treated as '2.1' SDK.
		[TestCase ("<Condition sdkVersion='2.0' />", "2.1.3", true)]
		[TestCase ("<Condition sdkVersion='2.1' />", "2.1.4", false)]
		[TestCase ("<Condition sdkVersion='2.1' />", "2.1.300", true)]
		[TestCase ("<Condition sdkVersion='2.1' />", "2.1.301", true)]
		[TestCase ("<Condition sdkVersion='2.1' />", "2.1.200", false)]
		[TestCase ("<Condition sdkVersion='2.0' />", "2.1.200", true)]
		[TestCase ("<Condition sdkVersion='2.1' />", "2.1.299", false)]
		[TestCase ("<Condition sdkVersion='2.0' />", "2.1.299", true)]
		public void DotNetCoreSdkInstalled (string conditionXml, string sdk, bool expected)
		{
			DotNetCoreSdksInstalled (new [] { sdk });

			bool result = EvaluateCondition (conditionXml);

			Assert.AreEqual (expected, result);
		}

		static bool EvaluateCondition (string conditionXml)
		{
			var node = new TestConditionNodeElement (conditionXml);

			var condition = new DotNetCoreSdkInstalledCondition ();
			return condition.Evaluate (node);
		}
	}

	class TestConditionNodeElement : NodeElement
	{
		XmlElement conditionElement;

		public TestConditionNodeElement (string xml)
		{
			var doc = new XmlDocument ();
			doc.LoadXml (xml);
			conditionElement = doc.DocumentElement;
		}

		public string NodeName => "Condition";

		public NodeAttribute [] Attributes => throw new NotImplementedException ();
		public NodeElementCollection ChildNodes => throw new NotImplementedException ();

		public string GetAttribute (string key)
		{
			return conditionElement.GetAttribute (key);
		}
	}
}
