//
// BuildOutputTests.cs
//
// // Author:
//       Rodrigo Moya <rodrigo.moya@xamarin.com>
//
// Copyright (c) 2017 Microsoft Corp. (http://microsoft.com)
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
using NUnit.Framework;
using MonoDevelop.Ide.BuildOutputView;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Editor;
using System.Threading.Tasks;

namespace MonoDevelop.Ide
{
	public class BuildOutputTests : IdeTestBase
	{
		[Test]
		public void ProgressMonitor_Instantiation ()
		{
			var bo = new BuildOutput ();
			Assert.AreEqual (bo.GetProgressMonitor (), bo.GetProgressMonitor ());
		}

		[Test]
		public void CustomProject_ToDataSource ()
		{
			var bo = new BuildOutput ();
			var monitor = bo.GetProgressMonitor ();

			monitor.LogObject (new ProjectStartedProgressEvent ());
			monitor.Log.Write ("Custom project built");
			monitor.LogObject (new ProjectFinishedProgressEvent ());

			var dataSource = bo.ToTreeDataSource (true);
			var child = dataSource.GetChild (dataSource.GetChild (null, 0), 0);

			Assert.That (dataSource.GetChildrenCount (null), Is.EqualTo (1));
			Assert.That (child, Is.TypeOf (typeof (BuildOutputNode)));
			Assert.That ((child as BuildOutputNode).Message, Is.EqualTo ("Custom project built"));
		}
	}
}
