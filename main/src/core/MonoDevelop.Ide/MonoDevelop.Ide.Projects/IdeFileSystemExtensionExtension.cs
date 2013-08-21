//
// IdeProjectServiceExtension.cs
//
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
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
using System;
using System.Linq;
using MonoDevelop.Projects;
using MonoDevelop.Core.FileSystem;
using System.Collections.Generic;
using MonoDevelop.Core;
using System.IO;

namespace MonoDevelop.Ide.Projects
{
	class IdeFileSystemExtensionExtension: FileSystemExtension
	{
		public override bool CanHandlePath (FilePath path, bool isDirectory)
		{
			return !isDirectory;
		}

		public override void RequestFileEdit (IEnumerable<FilePath> files)
		{
			base.RequestFileEdit (files);

			if (!IdeApp.IsInitialized)
				return;

			List<FilePath> readOnlyFiles = new List<FilePath> ();
			foreach (var f in files) {
				if (File.Exists (f) && File.GetAttributes (f).HasFlag (FileAttributes.ReadOnly))
					readOnlyFiles.Add (f);
			}
			string error;
			if (readOnlyFiles.Count == 1)
				error = GettextCatalog.GetString ("File {0} is read-only", readOnlyFiles [0].FileName);
			else if (readOnlyFiles.Count > 1) {
				var f1 = string.Join (", ", readOnlyFiles.Take (readOnlyFiles.Count - 1).ToArray ());
				var f2 = readOnlyFiles [readOnlyFiles.Count - 1];
				error = GettextCatalog.GetString ("Files {0} and {1} are read-only", f1, f2);
			} else
				return;

			var btn = new AlertButton (readOnlyFiles.Count == 1 ? GettextCatalog.GetString ("Make Writtable") : GettextCatalog.GetString ("Make Writtable"));
			var res = MessageService.AskQuestion (error, GettextCatalog.GetString ("Would you like MonoDevelop to attempt to make the file writable and try again?"), btn, AlertButton.Cancel);
			if (res == AlertButton.Cancel)
				throw new UserException (error) { AlreadyReportedToUser = true };

			foreach (var f in readOnlyFiles) {
				var atts = File.GetAttributes (f);
				File.SetAttributes (f, atts & ~FileAttributes.ReadOnly);
			}
		}
	}
}

