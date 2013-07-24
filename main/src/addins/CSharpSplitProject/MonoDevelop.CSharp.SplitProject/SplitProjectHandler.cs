//
// SplitProjectHandler.cs
//
// Author:
//       Luís Reis <luiscubal@gmail.com>
//
// Copyright (c) 2013 Luís Reis
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
using MonoDevelop.Components.Commands;
using MonoDevelop.Projects;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Pads;

namespace MonoDevelop.CSharp.SplitProject
{
	public class SplitProjectHandler : CommandHandler
	{
		DotNetProject currentProject;

		protected override void Run ()
		{
			var nativeWindow = IdeApp.Workbench.RootWindow;
			Xwt.WindowFrame parentFrame = Xwt.Toolkit.CurrentEngine.WrapWindow(nativeWindow);

			using (var dialog = new SplitProjectDialog (currentProject.Files)) {
				dialog.Run (parentFrame);
			}
		}

		protected override void Update (CommandInfo info)
		{
			var solutionPad = IdeApp.Workbench.GetPad<SolutionPad> ().Content as SolutionPad;

			currentProject = solutionPad.TreeView.GetSelectedNode ().DataItem as DotNetProject;

			if (currentProject == null || currentProject.LanguageName != "C#") {
				info.Visible = false;
				return;
			}

			info.Visible = true;
		}
	}
}

