// Lock.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using MonoDevelop.Core;
using System.IO;

namespace MonoDevelop.VersionControl
{
	public class LockCommand
	{
		public static bool Lock (Repository vc, string path, bool test)
		{
			if (vc.CanLock (path)) {
				if (test) return true;
				new LockWorker (vc, path).Start();
				return true;
			}
			return false;
		}

		private class LockWorker : Task 
		{
			Repository vc;
			string path;
						
			public LockWorker (Repository vc, string path) {
				this.vc = vc;
				this.path = path;
			}
			
			protected override string GetDescription() {
				return GettextCatalog.GetString ("Locking {0}...", path);
			}
			
			protected override void Run ()
			{
				vc.Lock (GetProgressMonitor (), path);
				Gtk.Application.Invoke (delegate {
					VersionControlService.NotifyFileStatusChanged (vc, path, Directory.Exists (path));
				});
			}
		}
	}
}
