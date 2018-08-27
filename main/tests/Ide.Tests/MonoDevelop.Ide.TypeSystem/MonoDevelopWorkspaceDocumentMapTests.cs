//
// MonoDevelopWorkspace.DocumentMapTests.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace MonoDevelop.Ide.TypeSystem
{
	[TestFixture]
	public class MonoDevelopWorkspaceDocumentMapTests
	{
		[Test]
		public void TestSimpleCreation ()
		{
			var pid = ProjectId.CreateNewId ();
			var map = new MonoDevelopWorkspace.DocumentMap (pid);

			var doc0 = map.Get ("TestName");
			Assert.IsNull (doc0);

			var doc1 = map.GetOrCreate ("TestName");
			var doc2 = map.GetOrCreate ("TestName", map);
			var doc3 = map.Get ("TestName");

			Assert.AreSame (doc1, doc2);
			Assert.AreSame (doc1, doc3);

			var docIdAdd = DocumentId.CreateNewId (pid);
			map.Add (docIdAdd, "TestName");

			var doc4 = map.Get ("TestName");
			var doc5 = map.GetOrCreate ("TestName");

			Assert.AreNotSame (doc1, doc4);
			Assert.AreSame (docIdAdd, doc4);
			Assert.AreSame (docIdAdd, doc5);

			map.Remove ("TestName");
		
			Assert.IsNull (map.Get ("TestName"));
		}

		[Test]
		public void TestMigration ()
		{
			var pid = ProjectId.CreateNewId ();
			var map1 = new MonoDevelopWorkspace.DocumentMap (pid);
			var map2 = new MonoDevelopWorkspace.DocumentMap (pid);

			var doc1 = map1.GetOrCreate ("TestName");
			var doc2 = map2.GetOrCreate ("TestName");

			Assert.AreNotSame (doc1, doc2);

			map2.Remove ("TestName");
			var doc3 = map2.GetOrCreate ("TestName", map1);

			Assert.AreSame (doc1, doc3);
		}
	}
}
