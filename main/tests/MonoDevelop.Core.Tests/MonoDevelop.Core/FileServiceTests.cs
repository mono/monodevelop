//
// FileServiceTests.cs
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
using NUnit.Framework;

namespace MonoDevelop.Core
{
	[TestFixture]
	public class FileServiceTests
	{
		[Test]
		public void InvalidFileCharsTests ()
		{
			Assert.True (FileService.IsValidFileName ("file"), "File without extension");
			Assert.True (FileService.IsValidFileName ("text.txt"), "File with extension");
			Assert.True (FileService.IsValidFileName (".gitignore"), "Dot file");

			Assert.False (FileService.IsValidFileName (""), "Empty string");
			Assert.False (FileService.IsValidFileName ("  "), "Whitespace string");

			// Test strings containing an invalid character.
			foreach (var c in FilePath.GetInvalidFileNameChars ())
				Assert.False (FileService.IsValidFileName (c.ToString ()),
					string.Format ("String with {0} (charcode: {1})", Char.IsControl (c) ? "<Control Char>" : c.ToString (), Convert.ToInt32 (c)));
		}

		[Test]
		public void InvalidPathCharsTests ()
		{
			Assert.True (FileService.IsValidPath ("./relative_file"), "Relative path string");
			Assert.True (FileService.IsValidPath ("/path/to/file"), "Absolute unix path string");
			Assert.True (FileService.IsValidPath ("Drive:\\some\\path\\here"), "Absolute windows path string");

			Assert.False (FileService.IsValidPath (""), "Empty string");
			Assert.False (FileService.IsValidPath ("  "), "Whitespace string");

			// Test strings containing an invalid character.
			foreach (var c in FilePath.GetInvalidPathChars ())
				Assert.False (FileService.IsValidPath (c.ToString ()),
					string.Format ("String with {0} (charcode: {1})", Char.IsControl (c) ? "<Control Char>" : c.ToString (), Convert.ToInt32 (c)));
		}

		[Test]
		public void NormalizeRelativePathTests ()
		{
			var sep = Path.DirectorySeparatorChar;

			Assert.AreEqual (string.Empty, FileService.NormalizeRelativePath (string.Empty));
			Assert.AreEqual (Path.Combine ("..", "bin"), FileService.NormalizeRelativePath (Path.Combine ("..", "bin")));
			Assert.AreEqual ("bin", FileService.NormalizeRelativePath (Path.Combine ("." + sep, "bin" + sep)));
			Assert.AreEqual ("bin", FileService.NormalizeRelativePath (Path.Combine ("." + sep, "." + sep, "bin" + sep + sep)));
			Assert.AreEqual ("FilePath.cs", FileService.NormalizeRelativePath ($".{sep}FilePath.cs"));
			Assert.AreEqual ("FilePath.cs", FileService.NormalizeRelativePath ($".{sep}{sep}FilePath.cs"));
		}
	}
}

