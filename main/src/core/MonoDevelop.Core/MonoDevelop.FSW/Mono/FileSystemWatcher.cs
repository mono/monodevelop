//
// FileSystemWatcher.cs
//
// Author:
//       ludovic <ludovic.henry@xamarin.com>
//
// Copyright (c) 2017 ludovic
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

namespace MonoDevelop.FSW.Mono
{
	internal class FileSystemWatcher : System.IO.FileSystemWatcher
	{
		public FileSystemWatcher ()
			: base ()
		{
		}

		public FileSystemWatcher (string path)
			: base (path)
		{
		}

		public FileSystemWatcher (string path, string filter)
			: base (path, filter)
		{
		}

		protected internal new void OnChanged (System.IO.FileSystemEventArgs e)
		{
			base.OnChanged (e);
		}

		protected internal new void OnCreated (System.IO.FileSystemEventArgs e)
		{
			base.OnCreated (e);
		}

		protected internal new void OnDeleted (System.IO.FileSystemEventArgs e)
		{
			base.OnDeleted (e);
		}

		protected internal new void OnError (System.IO.ErrorEventArgs e)
		{
			base.OnError (e);
		}

		protected internal new void OnRenamed (System.IO.RenamedEventArgs e)
		{
			base.OnRenamed (e);
		}

		protected internal new void Dispose (bool disposing)
		{
			base.Dispose (disposing);
		}
	}
}
