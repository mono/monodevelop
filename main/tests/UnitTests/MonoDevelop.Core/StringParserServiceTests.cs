//
// StringParserService.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2017 2017
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
using MonoDevelop.Core.StringParsing;
using NUnit.Framework;

namespace MonoDevelop.Core
{
	[TestFixture]
	public class StringParserServiceTests
	{
		class UnencodedValueStringTagProvider : StringTagProvider<StringParserServiceTests>
		{
			public override IEnumerable<StringTagDescription> GetTags ()
			{
				yield return new StringTagDescription ("UnencodedValue", "Unencoded value that should end up being encoded");
			}

			public override object GetTagValue (StringParserServiceTests instance, string tag)
			{
				switch (tag) {
				case "UNENCODEDVALUE":
					return "Mono & .NET";
				}
				throw new InvalidOperationException ();
			}
		}

		class DateTimeStringTagProvider : StringTagProvider<StringParserServiceTests>
		{
			public static DateTime Value { get; } = new DateTime (2017, 05, 31); // The day the test was written.

			public override IEnumerable<StringTagDescription> GetTags ()
			{
				yield return new StringTagDescription ("FixedTime", "A fixed date time value to test");
			}

			public override object GetTagValue (StringParserServiceTests instance, string tag)
			{
				switch (tag) {
				case "FIXEDTIME":
					return Value;
				}
				throw new InvalidOperationException ();
			}
		}

		[Test]
		public void TestFormatDoesntExist ()
		{
			Assert.AreEqual ("${NON-EXISTENT TAG}", StringParserService.Parse ("${NON-EXISTENT TAG}"));
		}

		[Test]
		public void TestFormatFunctions ()
		{
			var model = new StringTagModel ();
			model.Add (this);

			using (new TemporaryRegistration (new UnencodedValueStringTagProvider ())) {
				// Custom tag models
				Assert.AreEqual ("Mono &amp; .NET", StringParserService.Parse ("${UNENCODEDVALUE:encode}", model));
				Assert.AreEqual ("mono & .net", StringParserService.Parse ("${UNENCODEDVALUE:lower}", model));
				Assert.AreEqual ("MONO & .NET", StringParserService.Parse ("${UNENCODEDVALUE:upper}", model));

				// Built-in string generators
				Assert.That (StringParserService.Parse ("${YEAR:F4}", model), Is.StringEnding ("0000"));
			}
		}

		[Test]
		public void TestNonStringFormatFunctions ()
		{
			var model = new StringTagModel ();
			model.Add (this);

			using (new TemporaryRegistration (new DateTimeStringTagProvider ())) {
				Assert.AreEqual (StringParserService.Parse ("${FIXEDTIME:d}", model), DateTimeStringTagProvider.Value.ToString ("d"));
			}
		}

		class TemporaryRegistration : IDisposable
		{
			readonly IStringTagProvider provider;
			public TemporaryRegistration (IStringTagProvider provider)
			{
				this.provider = provider;
				StringParserService.RegisterStringTagProvider (provider);
			}

			public void Dispose ()
			{
				StringParserService.UnregisterStringTagProvider (provider);
			}
		}
	}
}
