//
// FileService.WatchingHandlerTests.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2019 Microsoft Inc.
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
	public class FileServiceWatcherHandlerTests
	{
		static readonly string prefix = Platform.IsWindows ? "C:\\" : "/";
		static FilePath MakePath (params string [] segments) => Path.Combine (prefix, Path.Combine (segments));

		[TestCase("a1/b/file.txt", 1, 0)]
		[TestCase("a1/c/file.txt", 0, 0)]
		[TestCase("a2/c", 0, 1)]
		[TestCase("a2/c/d/file.txt", 0, 1)]
		[TestCase("a3", 0, 0)]
		[TestCase("a4/file.txt", 0, 0)]
		public void TestRegistrationWorks(string pathSegment, int a1bCount, int a2cCount)
		{
			var handler = new FileService.RegistrationHandler ();

			using var a1b = new RegistrationCase (handler, "a1", "b");
			using var a2c = new RegistrationCase (handler, "a2", "c");

			var notifyPath = MakePath (pathSegment.Split ('/'));
			var args = new FileEventArgs (notifyPath, false);

			handler.Notify (FileService.EventDataKind.Created, args);
			Assert.AreEqual (a1bCount, a1b.Paths.Count);
			Assert.AreEqual (a2cCount, a2c.Paths.Count);
		}

		[Test]
		public void TestDeregistrationWorks ()
		{
			var handler = new FileService.RegistrationHandler ();

			using var a1 = new RegistrationCase (handler, "a");

			var a2 = new RegistrationCase (handler, "a");
			handler.Notify (FileService.EventDataKind.Created, new FileEventArgs (MakePath ("a"), false));
			a2.Dispose ();

			handler.Notify (FileService.EventDataKind.Created, new FileEventArgs (MakePath ("a"), false));

			Assert.AreEqual (2, a1.Paths.Count);
			Assert.AreEqual (1, a2.Paths.Count);
		}

		class RegistrationCase : IDisposable
		{
			public List<FilePath> Paths { get; } = new List<FilePath> ();
			IDisposable registration;

			public RegistrationCase(FileService.RegistrationHandler handler, params string[] paths)
			{
				registration = handler.WatchCreated (MakePath (paths), args => Paths.Add (args.Single ().FileName));
			}

			public void Dispose ()
			{
				registration.Dispose ();
			}
		}
	}
}
