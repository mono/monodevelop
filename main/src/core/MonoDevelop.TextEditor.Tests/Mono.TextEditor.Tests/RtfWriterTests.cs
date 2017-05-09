// 
// RtfWriterTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using Mono.TextEditor.Utils;
using NUnit.Framework;
using Mono.TextEditor.Highlighting;
using MonoDevelop.Core;

namespace Mono.TextEditor.Tests
{
	[Ignore("Port to new engine")]
	[TestFixture]
	class RtfWriterTests : TextEditorTestBase
	{
		[Test]
		public void TestSimpleCSharpRtf ()
		{
			if (Platform.IsWindows)
				Assert.Inconclusive ();
			var data = Create ("class Foo {}");
			//data.ColorStyle = SyntaxModeService.GetColorStyle ("Tango");
			//data.Document.SyntaxMode = SyntaxModeService.GetSyntaxMode (data.Document, "text/x-csharp");
			string generatedRtf = RtfWriter.GenerateRtf (data);
			Assert.AreEqual (
				@"{\rtf1\ansi\deff0\adeflang1025
{\fonttbl
{\f0\fnil\fprq1\fcharset128 Mono;}
}
{\colortbl ;\red51\green100\blue164;\red34\green34\blue34;}\viewkind4\uc1\pard
\f0
\fs20\cf1
\cf1 class\cf2  Foo \{\}\line
}", generatedRtf);
		}

		/// <summary>
		/// Bug 5628 - Error while pasting text/rtf
		/// </summary>
		[Test]
		public void TestBug5628 ()
		{
			if (Platform.IsWindows)
				Assert.Inconclusive ();
			var data = Create ("class Foo {}");
			//data.ColorStyle = SyntaxModeService.GetColorStyle ("Tango");
			string generatedRtf = RtfWriter.GenerateRtf (data);
			Assert.AreEqual (
				@"{\rtf1\ansi\deff0\adeflang1025
{\fonttbl
{\f0\fnil\fprq1\fcharset128 Mono;}
}
{\colortbl ;\red34\green34\blue34;}\viewkind4\uc1\pard
\f0
\fs20\cf1
\cf1 class Foo \{\}\line
}", generatedRtf);
		}

		/// <summary>
		/// Bug 7386 - Copy-paste from MonoDevelop into LibreOffice is broken
		/// </summary>
		[Test]
		public void TestBug7386 ()
		{
			if (Platform.IsWindows)
				Assert.Inconclusive ();
			var data = Create ("✔");
			//data.ColorStyle = SyntaxModeService.GetColorStyle ("Tango");
			string generatedRtf = RtfWriter.GenerateRtf (data);
			Assert.AreEqual (
				@"{\rtf1\ansi\deff0\adeflang1025
{\fonttbl
{\f0\fnil\fprq1\fcharset128 Mono;}
}
{\colortbl ;\red34\green34\blue34;}\viewkind4\uc1\pard
\f0
\fs20\cf1
\cf1 \uc1\u10004*\line
}", generatedRtf);
		}

		[Ignore("Fixme")]
		[Test]
		public void TestXml ()
		{
			if (Platform.IsWindows)
				Assert.Inconclusive ();
			var data = Create (
				@"<foo
	attr1 = ""1""
	attr2 = ""2""
/>");
			//data.ColorStyle = SyntaxModeService.GetColorStyle ("Tango");
			//data.Document.SyntaxMode = SyntaxModeService.GetSyntaxMode (data.Document, "application/xml");

			string generatedRtf = RtfWriter.GenerateRtf (data);

			Assert.AreEqual (
				@"{\rtf1\ansi\deff0\adeflang1025
{\fonttbl
{\f0\fnil\fprq1\fcharset128 Mono;}
}
{\colortbl ;\red68\green68\blue68;\red51\green100\blue164;\red245\green125\blue0;}\viewkind4\uc1\pard
\f0
\fs20\cf1
\cf1 <\cf2 foo\line
\tab\cf1 attr1 =\cf2  \cf3 ""1""\line
\cf2\tab\cf1 attr2 =\cf2  \cf3 ""2""\line
\cf1 />\line
}"
				, generatedRtf);
		}

	}
}

