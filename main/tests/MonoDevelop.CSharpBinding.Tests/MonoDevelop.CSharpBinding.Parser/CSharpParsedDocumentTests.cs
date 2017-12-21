//
// CSharpParsedDocumentTests.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2017 
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
using MonoDevelop.CSharp.Parser;
using MonoDevelop.Ide.TypeSystem;
using NUnit.Framework;

namespace MonoDevelop.CSharpBinding.Parser
{
	[TestFixture]
	public class CSharpParsedDocumentTests
	{
		static int finalized;

		[TestFixtureSetUp]
		public void SetUp ()
		{
			finalized = 0;
		}

		[Test]
		public void DoesNotLeakPreviousDocument ()
		{
			const int DocumentCount = 100;

			// Force only the last doc to be alive at this point.
			var doc = GetParsedDocumentStress (DocumentCount);

			GC.Collect ();
			GC.WaitForPendingFinalizers ();

			GC.KeepAlive (doc);

			// GC can  be lazy, seen this value reach both 98 and 99.
			Assert.That (finalized, Is.GreaterThanOrEqualTo (98));
		}

		class LeakTrackingCSharpParsedDocument : CSharpParsedDocument
		{
			public LeakTrackingCSharpParsedDocument (ParseOptions options) : base (options, "mock")
			{
			}

			~LeakTrackingCSharpParsedDocument ()
			{
				System.Threading.Interlocked.Increment (ref finalized);
			}
		}

		CSharpParsedDocument GetParsedDocumentStress (int count)
		{
			CSharpParsedDocument old = null, doc = null;

			for (int i = 0; i < count; ++i) {
				old = doc;

				var options = new ParseOptions {
					OldParsedDocument = old,
				};

				doc = new LeakTrackingCSharpParsedDocument (options);
			}
			return doc;
		}
	}
}
