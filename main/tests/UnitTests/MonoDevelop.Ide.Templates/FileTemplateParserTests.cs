//
// FileTemplateParserTests.cs
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
using System.Collections.Generic;
using MonoDevelop.Projects;
using NUnit.Framework;

namespace MonoDevelop.Ide.Templates
{
	[TestFixture]
	public class FileTemplateParserTests
	{
		ProjectCreateParameters parameters;

		[SetUp]
		public void Init ()
		{
			parameters = new ProjectCreateParameters ();
		}

		string Parse (string text)
		{
			return FileTemplateParser.Parse (text, parameters);
		}

		void AddParameter (string name, string value)
		{
			parameters [name] = value;
		}

		[Test]
		public void NoDollarSigns ()
		{
			string text = 
				"using System;\r\n" +
				"using System.Text;\r\n";
			string expectedResult = 
				"using System;\r\n" +
				"using System.Text;\r\n";

			string result = Parse (text);

			Assert.AreEqual (expectedResult, result);
		}

		[Test]
		public void IfBlockWithConditionThatIsFalse ()
		{
			string text = 
				"$if$ ($abc$ == y)using System;\r\n" +
				"$endif$using System.Text;\r\n";
			string expectedResult = 
				"using System.Text;\r\n";

			string result = Parse (text);

			Assert.AreEqual (expectedResult, result);
		}

		[Test]
		public void IfBlockWithConditionThatIsTrue ()
		{
			string text = 
				"$if$ ($abc$ == y)using System;\r\n" +
				"$endif$using System.Text;\r\n";
			string expectedResult = 
				"using System;\r\n" +
				"using System.Text;\r\n";
			AddParameter ("abc", "y");

			string result = Parse (text);

			Assert.AreEqual (expectedResult, result);
		}

		[Test]
		public void DoubleDollarSignIsReplacedWithSingleDollarSign ()
		{
			string text = "$$";
			string expectedResult = "$";

			string result = Parse (text);

			Assert.AreEqual (expectedResult, result);
		}

		[Test]
		public void SingleDollarSignOnly ()
		{
			string text = "$";
			string expectedResult = "$";

			string result = Parse (text);

			Assert.AreEqual (expectedResult, result);
		}

		[Test]
		public void IfElseBlockWithConditionThatIsFalse ()
		{
			string text = 
				"$if$ ($abc$ == y)using System;\r\n" +
				"$else$using System.Xml;\r\n" +
				"$endif$using System.Text;\r\n";
			string expectedResult = 
				"using System.Xml;\r\n" +
				"using System.Text;\r\n";

			string result = Parse (text);

			Assert.AreEqual (expectedResult, result);
		}

		[Test]
		public void IfElseBlockWithConditionThatIsTrue ()
		{
			string text = 
				"$if$ ($abc$ == y)using System;\r\n" +
				"$else$using System.Xml;\r\n" +
				"$endif$using System.Text;\r\n";
			string expectedResult = 
				"using System;\r\n" +
				"using System.Text;\r\n";
			AddParameter ("abc", "y");

			string result = Parse (text);

			Assert.AreEqual (expectedResult, result);
		}

		[Test]
		public void IfBlockOnlyTemplateTextIsReturnedUnchanged ()
		{
			string text = 
				"$if$using System;\r\n";

			string result = Parse (text);

			Assert.AreEqual (text, result);
		}

		[Test]
		public void EmptyTemplate ()
		{
			string text = "";
			string expectedResult = "";

			string result = Parse (text);

			Assert.AreEqual (expectedResult, result);
		}

		[Test]
		public void IfElseWithNoEndIfReturnsTemplateTextUnchanged ()
		{
			string text = 
				"$if$ ($abc$ == y)using System;\r\n" +
				"$else$using System.Xml;\r\n";

			string result = Parse (text);

			Assert.AreEqual (text, result);
		}

		[Test]
		public void EmptyParameterNameUsedInConditionReturnsTemplateTextUnchanged ()
		{
			string text = 
				"$if$ (== y)using System;\r\n" +
				"$endif$using System.Text;\r\n";

			string result = Parse (text);

			Assert.AreEqual (text, result);
		}

		[Test]
		public void IfBlockWithConditionThatHasDifferentCase ()
		{
			string text = 
				"$if$ ($abc$ == true)using System;\r\n" +
				"$endif$using System.Text;\r\n";
			string expectedResult = 
				"using System;\r\n" +
				"using System.Text;\r\n";
			AddParameter ("abc", "True");

			string result = Parse (text);

			Assert.AreEqual (expectedResult, result);
		}

		[Test]
		public void IfBlockWithEscapedStringParserStringInside ()
		{
			string text = 
				"$if$ ($abc$ == true)    public string MyMethod ()\r\n" +
				"    {\r\n" +
				"        return $${Name};\r\n" +
				"    }\r\n" +
				"$endif$\r\n";
			string expectedResult = 
				"    public string MyMethod ()\r\n" +
				"    {\r\n" +
				"        return ${Name};\r\n" +
				"    }\r\n" +
				"\r\n";
			AddParameter ("abc", "True");

			string result = Parse (text);

			Assert.AreEqual (expectedResult, result);
		}

		[Test]
		public void IfElseBlockWithEscapedStringParserStringInsideElse ()
		{
			string text = 
				"$if$ ($abc$ == true)" +
				"$else$    public string MyMethod ()\r\n" +
				"    {\r\n" +
				"        return $${Name};\r\n" +
				"    }\r\n" +
				"$endif$\r\n";
			string expectedResult = 
				"    public string MyMethod ()\r\n" +
				"    {\r\n" +
				"        return ${Name};\r\n" +
				"    }\r\n" +
				"\r\n";

			string result = Parse (text);

			Assert.AreEqual (expectedResult, result);
		}

		[Test]
		public void IfBlockWithEndIfLastPartOfText ()
		{
			string text = 
				"$if$ ($abc$ == y)using System;\r\n" +
				"$endif$";
			string expectedResult = 
				"using System;\r\n";
			AddParameter ("abc", "y");

			string result = Parse (text);

			Assert.AreEqual (expectedResult, result);
		}

		[Test]
		public void IfBlockWithUnescapedStringParserStringInside ()
		{
			string text = 
				"$if$ ($abc$ == true)    public string MyMethod ()\r\n" +
				"    {\r\n" +
				"        return ${Name};\r\n" +
				"    }\r\n" +
				"$endif$\r\n";
			string expectedResult = 
				"    public string MyMethod ()\r\n" +
				"    {\r\n" +
				"        return ${Name};\r\n" +
				"    }\r\n" +
				"\r\n";
			AddParameter ("abc", "True");

			string result = Parse (text);

			Assert.AreEqual (expectedResult, result);
		}

		[Test]
		public void IfElseBlockWithUnescapedStringParserStringInsideElse ()
		{
			string text = 
				"$if$ ($abc$ == true)" +
				"$else$    public string MyMethod ()\r\n" +
				"    {\r\n" +
				"        return ${Name};\r\n" +
				"    }\r\n" +
				"$endif$\r\n";
			string expectedResult = 
				"    public string MyMethod ()\r\n" +
				"    {\r\n" +
				"        return ${Name};\r\n" +
				"    }\r\n" +
				"\r\n";

			string result = Parse (text);

			Assert.AreEqual (expectedResult, result);
		}

		[Test]
		public void IfBlockAndParameterCaseInConditionDoesNotMatchTemplateParameterCaseStillMatches ()
		{
			string text = 
				"$if$ ($abc$ == y)using System;\r\n" +
				"$endif$using System.Text;\r\n";
			string expectedResult = 
				"using System;\r\n" +
				"using System.Text;\r\n";
			AddParameter ("ABC", "Y");

			string result = Parse (text);

			Assert.AreEqual (expectedResult, result);
		}
	}
}