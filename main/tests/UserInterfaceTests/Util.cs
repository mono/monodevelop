//
// Util.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core;

namespace UserInterfaceTests
{
	public static class Util
	{
		static FilePath rootDir;
		static int projectId = 1;

		static string testRunIdentifier = DateTime.Now.ToString ("dddd-MMMM-dd-yyyy-HH-mm-ss");

		public static string TestRunId {
			get {
				return testRunIdentifier;
			}
		}

		public static FilePath TestsRootDir {
			get {
				if (rootDir.IsNull) {
					rootDir = Path.GetDirectoryName (typeof(Util).Assembly.Location);
					rootDir = rootDir.ParentDirectory.ParentDirectory.Combine ("tests");
				}
				return rootDir;
			}
		}

		public static FilePath TmpDir {
			get { return TestsRootDir.Combine ("tmp"); }
		}

		public static void ClearTmpDir ()
		{
			if (Directory.Exists (TmpDir))
				Directory.Delete (TmpDir, true);
			projectId = 1;
		}

		public static string ToValidPath (string path)
		{
			if (Path.DirectorySeparatorChar == '/')
				return path;
			return path.Replace ('/', Path.DirectorySeparatorChar);
		}

		public static FilePath GetSampleProject (FilePath solution)
		{
			solution = GetSampleProjectPath (solution);

			FilePath srcDir = solution.ParentDirectory;

			FilePath tmpDir = CreateTmpDir (srcDir.FileName);
			CopyDir (srcDir, tmpDir);
			return tmpDir.Combine (solution.FileName);
		}

		public static FilePath GetSampleProjectPath (string solution)
		{
			solution = ToValidPath (solution);
			return TestsRootDir.Combine ("test-projects").Combine (solution);
		}

		public static FilePath CreateTmpDir (string hint)
		{
			string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), hint);

			if (!Directory.Exists (tempDirectory))
				Directory.CreateDirectory (tempDirectory);
			return tempDirectory;
		}

		static void CopyDir (string src, string dst)
		{
			if (Path.GetFileName (src) == ".svn")
				return;

			if (!Directory.Exists (dst))
				Directory.CreateDirectory (dst);

			foreach (string file in Directory.GetFiles (src))
				File.Copy (file, Path.Combine (dst, Path.GetFileName (file)));

			foreach (string dir in Directory.GetDirectories (src))
				CopyDir (dir, Path.Combine (dst, Path.GetFileName (dir)));
		}
	}
}
