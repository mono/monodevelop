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

		public override void RequestFileEdit (FilePath file)
		{
			base.RequestFileEdit (file);

			if (!IdeApp.IsInitialized)
				return;

			if (!File.Exists (file))
				return;
			
#if MAC
			// detect 'locked' files on OS X
			var attr = Foundation.NSFileManager.DefaultManager.GetAttributes (file) ;
			if (attr != null && attr.Immutable.HasValue && attr.Immutable.Value) {
				throw new UserException (GettextCatalog.GetString ("File '{0}' is locked.", file));
			}
#endif
			var atts = File.GetAttributes (file);
			if ((atts & FileAttributes.ReadOnly) == 0)
				return;
			
			string error = GettextCatalog.GetString ("File {0} is read-only", file.FileName);

			var res = MessageService.AskQuestion (error, GettextCatalog.GetString ("Would you like {0} to attempt to make the file writable and try again?", BrandingService.ApplicationName), AlertButton.MakeWriteable, AlertButton.Cancel);
			if (res == AlertButton.Cancel)
				throw new UserException (error) { AlreadyReportedToUser = true };

			File.SetAttributes (file, atts & ~FileAttributes.ReadOnly);
		}

		public override FileWriteableState GetWriteableState (FilePath file)
		{
			if (!File.Exists (file))
				return FileWriteableState.NotExistant;

#if MAC
			// detect 'locked' files on OS X
			var attr = Foundation.NSFileManager.DefaultManager.GetAttributes (file) ;
			if (attr != null && attr.Immutable.HasValue && attr.Immutable.Value) {
				return FileWriteableState.Locked;
			}
#endif

			var atts = File.GetAttributes (file);
			if ((atts & FileAttributes.ReadOnly) == FileAttributes.ReadOnly) {
				
				// try to set/unset file attributes to determine if it's possible to do so.
				try {
					File.SetAttributes (file, atts & ~FileAttributes.ReadOnly);
					File.SetAttributes (file, atts);
				} catch (Exception) {
					return FileWriteableState.Locked;
				}

				return FileWriteableState.ReadOnly;
			}
			return FileWriteableState.Writeable;
		}
	}
}