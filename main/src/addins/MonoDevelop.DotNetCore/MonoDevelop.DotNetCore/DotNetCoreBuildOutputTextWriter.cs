//
// DotNetCoreBuildOutputTextWriter.cs
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
using System.Text;
using MonoDevelop.Projects;

namespace MonoDevelop.DotNetCore
{
	class DotNetCoreBuildOutputTextWriter : TextWriter
	{
		TextWriter writer;
		DotNetCoreBuildOutputParser parser = new DotNetCoreBuildOutputParser ();

		public DotNetCoreBuildOutputTextWriter (TextWriter writer)
		{
			this.writer = writer;
		}

		public override Encoding Encoding {
			get { return writer.Encoding; }
		}

		public override void Flush ()
		{
			writer.Flush ();
		}

		public override void Write (string value)
		{
			parser.ProcessLine (value);
			writer.Write (value);
		}

		protected override void Dispose (bool disposing)
		{
			parser.ProcessLastLine ();
			base.Dispose (disposing);
		}

		public BuildResult GetBuildResults (DotNetProject project)
		{
			var result = new BuildResult ();
			result.SourceTarget = project;

			foreach (var error in parser.GetBuildErrors (project)) {
				result.Append (error);
			}

			return result;
		}
	}
}
