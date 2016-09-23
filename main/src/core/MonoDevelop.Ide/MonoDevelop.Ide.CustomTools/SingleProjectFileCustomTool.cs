//
// SingleProjectFileCustomTool.cs
//
// Author:
//       Greg Munn <greg@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc
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
using MonoDevelop.Projects;
using System.CodeDom.Compiler;
using MonoDevelop.Core;
using System.Threading.Tasks;

namespace MonoDevelop.Ide.CustomTools
{
	/// <summary>
	/// Abstract class for processing a file in a project for a custom tool
	/// </summary>
	public abstract class SingleProjectFileCustomTool
	{
		public abstract Task Generate (ProgressMonitor monitor, Project project, ProjectFile file, SingleFileCustomToolResult result);
	}

	/// <summary>
	/// Wraps a ISingleFileCustomTool and delegates execution to customTool's Generate method
	/// </summary>
	sealed class SingleFileCustomToolWrapper : SingleProjectFileCustomTool
	{
		ISingleFileCustomTool customTool;

		public SingleFileCustomToolWrapper (ISingleFileCustomTool customTool)
		{
			this.customTool = customTool;
		}

		public override Task Generate (ProgressMonitor monitor, Project project, ProjectFile file, SingleFileCustomToolResult result)
		{
			return this.customTool.Generate (monitor, file, result);
		}
	}
}
