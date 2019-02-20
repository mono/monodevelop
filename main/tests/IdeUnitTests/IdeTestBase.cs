//
// IdeTestBase.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2017 Microsoft
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
using UnitTests;
using MonoDevelop.Ide.Gui.Documents;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Fonts;
using MonoDevelop.Ide.Gui.Shell;
using IdeUnitTests;
using MonoDevelop.Ide.Gui;
using NUnit.Framework;

namespace MonoDevelop.Ide
{
	[RequireService (typeof (DesktopService))]
	[RequireService (typeof (TypeSystemService))]
	[RequireService (typeof (FontService))]
	public class IdeTestBase: RoslynTestBase
	{
		protected override async Task InternalSetup(string rootDir)
		{
			Runtime.RegisterServiceType<IShell, MockShell> ();
			Runtime.RegisterServiceType<ProgressMonitorManager, MockProgressMonitorManager> ();

			await base.InternalSetup(rootDir);

			Xwt.Application.Initialize(Xwt.ToolkitType.Gtk);
			Gtk.Application.Init();
		}

		[TearDown]
		async Task CloseWorkspace ()
		{
			var ws = Runtime.PeekService<RootWorkspace> ();
			if (ws != null)
				await ws.Close (saveWorkspacePreferencies: false, closeProjectFiles: false, force: true);
			var dm = Runtime.PeekService<DocumentManager> ();
			if (dm != null) {
				while (dm.Documents.Count > 0)
					await dm.Documents [0].Close (true);
			}
		}

		public override void TearDown ()
		{
			IdeApp.OnExit ();
			base.TearDown ();
		}
	}
}
