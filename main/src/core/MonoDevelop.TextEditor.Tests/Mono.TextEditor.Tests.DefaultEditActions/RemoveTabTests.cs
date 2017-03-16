//
// RemoveTabTests.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using NUnit.Framework;

namespace Mono.TextEditor.Tests.Actions
{
	[TestFixture()]
	class RemoveTabTests : TextEditorTestBase
	{
		[TestCase(false)]
		[TestCase(true)]
		public void TestRemoveTab (bool reverse)
		{
			var data = InsertTabTests.Create (
@"	123456789
	123[456789
	123d456789
	123]456789
	123456789
	123456789", reverse);

			MiscActions.RemoveTab (data);
			
			InsertTabTests.Check (data, 
@"	123456789
123[456789
123d456789
123]456789
	123456789
	123456789", reverse);
		}


		[TestCase(false)]
		[TestCase(true)]
		public void TestRemoveTabCase2 (bool reverse)
		{
			var data = InsertTabTests.Create (
@"	123456789
[	123456789
	123d456789
	123]456789
	123456789
	123456789", reverse);


			MiscActions.RemoveTab (data);

			InsertTabTests.Check (data, 
@"	123456789
[123456789
123d456789
123]456789
	123456789
	123456789", reverse);
		}


		[TestCase(false)]
		[TestCase(true)]
		public void TestRemoveTabCase3 (bool reverse)
		{
			var data = InsertTabTests.Create (
@"	123456789
	123[456789
	123d456789
]	123456789
	123456789
	123456789", reverse);


			MiscActions.RemoveTab (data);

			InsertTabTests.Check (data, 
@"	123456789
123[456789
123d456789
]	123456789
	123456789
	123456789", reverse);
		}
		
		[TestCase(false)]
		[TestCase(true)]
		public void TestRemoveTabCase4 (bool reverse)
		{
			var data = InsertTabTests.Create (
@"123456789
123[456789
123d456789
123]456789
123456789
123456789", reverse);


			MiscActions.RemoveTab (data);

			InsertTabTests.Check (data, 
@"123456789
123[456789
123d456789
123]456789
123456789
123456789", reverse);
		}

		[Test]
		public void TestRemoveTabWithoutSelection ()
		{
			var data = Create (@"
	1
	$2
	3");
			MiscActions.RemoveTab (data);
			Check (data, @"
	1
$2
	3");
		}


		[Test]
		public void TestRemoveWithTabsToSpaces ()
		{
			var data = Create (@"    123d456789
        123$<-456789
        123d456789
        ->123456789
    123456789
    123456789", new TextEditorOptions () { TabsToSpaces = true } );

			MiscActions.RemoveTab (data);
			Check (data, @"    123d456789
    123$<-456789
    123d456789
    ->123456789
    123456789
    123456789");
		}
	}
}

