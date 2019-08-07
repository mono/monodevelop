//
// FilePathTests.cs
//
// Author:
//       Marius Ungureanu <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace MonoDevelop.Core
{
	[TestFixture]
	public class FilePathTests
	{
		string tempDirectory;
		[TestFixtureSetUp]
		public void CreateTempDir ()
		{
			tempDirectory = FileService.CreateTempDirectory ();
		}

		[TestFixtureTearDown]
		public void CleanupTempDir ()
		{
			FileService.DeleteDirectory (tempDirectory);
		}

		[Test]
		public void CanCreateVariousPaths ()
		{
			FilePath path;
			string expected;

			expected = Path.Combine ("this", "is", "a", "path");
			path = FilePath.Build ("this", "is", "a", "path");
			Assert.AreEqual (expected, path.ToString ());

			expected = "";
			path = FilePath.Empty;
			Assert.AreEqual (expected, path.ToString ());
			Assert.IsTrue (path.IsEmpty);
			Assert.IsTrue (path.IsNullOrEmpty);

			expected = null;
			path = FilePath.Null;
			Assert.AreEqual (expected, path.ToString ());
			Assert.IsTrue (path.IsNull);
			Assert.IsTrue (path.IsNullOrEmpty);

			expected = Path.Combine ("this", "is", "a", "path");
			path = new FilePath (expected);
			Assert.AreEqual (expected, path.ToString ());

			expected = Path.Combine (expected, "and", "more");
			path = path.Combine ("and", "more");
			Assert.AreEqual (expected, path.ToString ());

			expected = "file.txt";
			path = new FilePath ("file").ChangeExtension (".txt");
			Assert.AreEqual (expected, path.ToString ());

			expected = "file.txt";
			path = new FilePath ("file.type").ChangeExtension (".txt");
			Assert.AreEqual (expected, path.ToString ());

			// TODO: Test file:// scheme
		}

		[Test]
		public void ChildIsFoundCorrectly ()
		{
			FilePath parent, child;

			// No trailing directory char.
			parent = FilePath.Build ("base");
			child = parent.Combine ("child");
			Assert.IsTrue (child.IsChildPathOf (parent));

			// No trailing directory char but not parent.
			parent = FilePath.Build ("base");
			child = FilePath.Build ("basechild");
			Assert.False (child.IsChildPathOf (parent));

			// Trailing directory char.
			parent = FilePath.Build ("base" + Path.DirectorySeparatorChar);
			child = parent.Combine ("child");
			Assert.IsTrue (child.IsChildPathOf (parent));

			// https://bugzilla.xamarin.com/show_bug.cgi?id=48212
			Assert.IsFalse (child.IsChildPathOf (child));
		}

		[TestCase("test.txt", "test")]
		[TestCase(".gitignore", "")]
		public void FileNameWithoutExtension (string fileName, string expected)
		{
			var path = FilePath.Build ("dir", fileName);

			Assert.AreEqual (expected, path.FileNameWithoutExtension);
		}


		[TestCase ("test.txt", ".txt", true)]
		[TestCase ("test.txt", ".TxT", null)]
		[TestCase ("test.txt", ".cs", false)]
		[TestCase ("test.txt", ".longer", false)]
		[TestCase ("test.txt", "", false)]
		[TestCase (".gitignore", ".gitignore", true)]
		[TestCase (".gitignore", ".git", false)]
		[TestCase (".gitignore", "", false)]
		[TestCase ("a", "", true)]
		[TestCase ("a.", "", true)]
		[TestCase ("", "", true)]
		[TestCase ("", "a", false)]
		public void HasExtensionChecks (string fileName, string assertExtension, bool? expected)
		{
			IEqualityComparer<string> comparer = FilePath.PathComparer;
			var expectedValue = expected ?? FilePath.PathComparison == StringComparison.OrdinalIgnoreCase;

			var path = FilePath.Build ("dir", fileName);

			Assert.AreEqual (expectedValue, path.HasExtension (assertExtension));

			var expectedConstraint = expectedValue ? Is.EqualTo (assertExtension) : Is.Not.EqualTo (assertExtension);
			Assert.That (path.Extension, expectedConstraint.Using (comparer));
		}

		[TestCase ("test.txt", "test.txt", true)]
		[TestCase ("test.txt", "Test.txT", null)]
		[TestCase ("test.txt", "abc.txt", false)]
		[TestCase ("test.txt", "something", false)]
		[TestCase (".gitignore", ".gitignore", true)]
		[TestCase (".gitignore", ".git", false)]
		public void HasFileNameChecks (string fileName, string assertFileName, bool? expected)
		{
			IEqualityComparer<string> comparer = FilePath.PathComparer;
			var expectedValue = expected ?? FilePath.PathComparison == StringComparison.OrdinalIgnoreCase;

			var path = FilePath.Build ("dir", fileName);

			Assert.AreEqual (expectedValue, path.HasFileName (assertFileName));

			var expectedConstraint = expectedValue ? Is.EqualTo (assertFileName) : Is.Not.EqualTo (assertFileName);
			Assert.That (path.FileName, expectedConstraint.Using (comparer));
		}

		[Test]
		public void CanGetCommonRootPath ()
		{
			FilePath[] pathsWithCommon = {
				FilePath.Build ("test", "common"),
				FilePath.Build ("test", "common", "notcommon1"),
				FilePath.Build ("test", "common", "notcommon1", "notcommon11"),
				FilePath.Build ("test", "common", "notcommon2"),
				FilePath.Build ("test", "common", "notcommon3", "notcommon31"),
			};
			Assert.AreEqual (FilePath.Build ("test", "common"), FilePath.GetCommonRootPath (pathsWithCommon));

			FilePath[] justOnePath =  {
				FilePath.Build ("test", "common"),
			};
			Assert.AreEqual (FilePath.Build ("test", "common"), FilePath.GetCommonRootPath (justOnePath));

			FilePath[] pathsNotCommon = {
				FilePath.Build ("notcommon1"),
				FilePath.Build ("notcommon2"),
				FilePath.Build ("notcommon3"),
			};
			Assert.IsTrue (FilePath.GetCommonRootPath (pathsNotCommon).IsNullOrEmpty);
		}

		[Test]
		public void CanDoIOOperations ()
		{
			var p = new FilePath (tempDirectory);
			Assert.IsTrue (p.IsDirectory);
		}

		[Test]
		public void InvalidCharactersAreCloned ()
		{
			Assert.AreNotSame (FilePath.GetInvalidFileNameChars (), FilePath.GetInvalidFileNameChars ());
			Assert.AreNotSame (FilePath.GetInvalidPathChars (), FilePath.GetInvalidPathChars ());
		}

		[Test]
		public void TestResolveLinks ()
		{
			// TODO: Check that it resolves links, but for 64bitness we just need to check it runs ok
			string pathname = "asdf.txt";
			FilePath path = new FilePath (pathname);
			path.ResolveLinks ();

			Assert.AreEqual (pathname, (string)path);
		}

		[Test]
		public void Equality ()
		{
			Assert.IsTrue (new FilePath ("") == FilePath.Empty);
			Assert.IsTrue (new FilePath () == FilePath.Null);
			Assert.IsFalse (new FilePath ("") == FilePath.Null);

			var root = Platform.IsWindows ? "c:\\" : "/";

			Assert.IsTrue (new FilePath (Path.Combine (root, "a", "b")) == new FilePath (Path.Combine (root, "a", "b")));
			Assert.IsFalse (new FilePath (Path.Combine (root, "a","c")) == new FilePath (Path.Combine (root, "a","b")));
			Assert.IsFalse (new FilePath (Path.Combine (root, "a","b")) == new FilePath (Path.Combine (root, "a","b") + Path.DirectorySeparatorChar));
			Assert.IsFalse (new FilePath (Path.Combine (root, "a","b")) == new FilePath (Path.Combine (root, "a","c", "..", "b")));

			if (Platform.IsWindows || Platform.IsMac)
				Assert.IsTrue (new FilePath (Path.Combine (root, "a","B")) == new FilePath (Path.Combine (root, "a","b")));
			else
				Assert.IsFalse (new FilePath (Path.Combine (root, "a","B")) == new FilePath (Path.Combine (root, "a","b")));
		}

		[Test]
		public void CanonicalPath ()
		{
			var root = Platform.IsWindows ? "c:\\" : "/";
			var p = new FilePath (Path.Combine (root, "a","c", "..", "b"));

			Assert.AreEqual (new FilePath (Path.Combine (root, "a","b")), p.CanonicalPath);

			// Trailing slashes are removed from canonical path
			p = new FilePath (Path.Combine (root, "a","b") + Path.DirectorySeparatorChar);
			Assert.AreEqual (new FilePath (Path.Combine (root, "a","b")), p.CanonicalPath);

			// Canonical path of null is null
			Assert.AreEqual (FilePath.Null, FilePath.Null.CanonicalPath);
		}

		[Test]
		public void TestAllFilePathCombineBuildOverloads ()
		{
			var root = FilePath.Empty;

			Assert.AreEqual (root.Combine ("a"), root.Combine (FilePath.Build ("a")));
			Assert.AreEqual (root.Combine ("a", "b"), root.Combine (FilePath.Build ("a"), FilePath.Build ("b")));
			Assert.AreEqual (root.Combine ("a", "b"), root.Combine (FilePath.Build ("a", "b")));
			Assert.AreEqual (root.Combine ("a", "b", "c"), root.Combine (FilePath.Build ("a"), FilePath.Build ("b"), FilePath.Build ("c")));
			Assert.AreEqual (root.Combine ("a", "b", "c"), root.Combine (FilePath.Build ("a", "b", "c")));
		}
	}
}

