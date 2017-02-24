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

		[Test]
		public void FileExtensionIsProper ()
		{
			var path = new FilePath ("asdf.txt");
			Assert.AreEqual ("asdf.txt", path.FileName);
			Assert.IsTrue (path.HasExtension (".txt"));
			Assert.AreEqual (".txt", path.Extension);
			Assert.AreEqual ("asdf", path.FileNameWithoutExtension);

			path = new FilePath (".gitignore");
			Assert.False (path.HasExtension (".gitignore"));
			Assert.AreEqual (".gitignore", path.FileName);
			Assert.AreEqual (".gitignore", path.Extension);
			Assert.AreEqual ("", path.FileNameWithoutExtension);
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

			Assert.IsTrue (new FilePath (Path.Combine (root, "a","b")) == new FilePath (Path.Combine (root, "a","b")));
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
	}
}

