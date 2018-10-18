//
// MyClass.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Mono.Addins;
using MonoDevelop.Core;
using Xwt;

namespace MonoDevelop.ExtensionTools
{
	public class Application: IApplication
	{
		public Task<int> Run (string[] arguments)
		{
			Xwt.Application.Initialize (ToolkitType.Gtk);
#if MAC
			var dir = Path.GetDirectoryName (typeof (Application).Assembly.Location);
			if (ObjCRuntime.Dlfcn.dlopen (Path.Combine (dir, "libxammac.dylib"), 0) == IntPtr.Zero) {
				LoggingService.LogFatalError ("Unable to load libxammac");
				return Task.FromResult (1);
			}
#endif
			LoggingService.LogInfo ("Initialized toolkit");

			Xwt.Toolkit.NativeEngine.Invoke (() => {
				using (CreateWindow ()) {
					LoggingService.LogInfo ("Showing main window");
					Xwt.Application.Run ();
				}
			});

			return Task.FromResult (0);
		}

		static Window CreateWindow ()
		{
			var window = new Window {
				Content = CreateWindowContent (),
				Width = 800,
				Height = 800,
			};

			window.Closed += (_, __) => Xwt.Application.Exit ();
			window.Show ();

			return window;
		}

		static Widget CreateWindowContent ()
		{
			var nb = new Notebook ();

			nb.Add (new AddinListWidget (), "List");
			nb.Add (new AddinDependencyTreeWidget (), "Dependency Tree");
			nb.Add (new ExtensionPointsWidget (), "Extension Points");

			return nb;
		}
	}
}
