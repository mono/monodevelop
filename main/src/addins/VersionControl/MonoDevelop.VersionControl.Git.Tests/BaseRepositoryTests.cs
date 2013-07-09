//
// RepositoryTests.cs
//
// Author:
//       Therzok <teromario@yahoo.com>
//
// Copyright (c) 2013 Xamarin Inc.
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

using NUnit.Framework;
using System.IO;
using System;
using MonoDevelop.Core;

namespace MonoDevelop.VersionControl.Git.Tests
{
	[TestFixture]
	public abstract class BaseRepoUtilsTest
	{
		protected FilePath repoLocation = "";
		protected FilePath rootUrl = "";
		protected FilePath rootCheckout;

		[SetUp]
		public abstract void Setup ();

		[TearDown]
		public abstract void TearDown ();

		[Test]
		public abstract void CheckoutExists ();

		[Test]
		public abstract void FileIsAdded ();

		[Test]
		public abstract void FileIsCommitted ();

		[Test]
		public abstract void UpdateIsDone ();

		[Test]
		public abstract void LogIsProper ();

		[Test]
		public abstract void DiffIsProper ();

		[Test]
		public abstract void Reverts ();

		#region Util

		public abstract void Checkout (string path);

		public static void DeleteDirectory (string path)
		{
			string[] files = Directory.GetFiles (path);
			string[] dirs = Directory.GetDirectories (path);

			foreach (var file in files) {
				File.SetAttributes (file, FileAttributes.Normal);
				File.Delete (file);
			}

			foreach (var dir in dirs) {
				DeleteDirectory (dir);
			}

			Directory.Delete (path);
		}

		#endregion
	}
}

