//
// DotNetCoreOperationConsole.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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

using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects;

namespace MonoDevelop.DotNetCore
{
	class DotNetCoreOperationConsole : OperationConsole
	{
		ProgressMonitor monitor;
		StringReader reader = new StringReader ("");
		DotNetCoreBuildOutputTextWriter buildOutputWriter;

		public DotNetCoreOperationConsole (ProgressMonitor monitor)
			: base (monitor.CancellationToken)
		{
			this.monitor = monitor;
			buildOutputWriter = new DotNetCoreBuildOutputTextWriter (monitor.Log);
		}

		public override TextReader In {
			get { return reader; }
		}

		public override TextWriter Out {
			get { return buildOutputWriter; }
		}

		public override TextWriter Error {
			get { return monitor.Log; }
		}

		public override TextWriter Log {
			get { return monitor.Log; }
		}

		public override void Dispose ()
		{
			buildOutputWriter.Dispose ();
		}

		public BuildResult GetBuildResult (DotNetProject project)
		{
			var result = new BuildResult ();
			result.Append (buildOutputWriter.GetBuildResults (project));
			result.SourceTarget = project;
			return result;
		}
	}
}
