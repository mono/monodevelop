// MdsTests.cs
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

using System.IO;
using NUnit.Framework;
using UnitTests;
using System.Threading.Tasks;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class MdsTests: TestBase
	{
		[Test]
		public async Task TestSaveWorkspace ()
		{
			// Saving a workspace must save all solutions and projects it contains
			
			string dir = Util.CreateTmpDir ("TestSaveWorkspace");
			Workspace ws = new Workspace ();
			ws.FileName = Path.Combine (dir, "workspace");
			
			Solution sol = new Solution ();
			sol.FileName = Path.Combine (dir, "thesolution");
			ws.Items.Add (sol);
			
			DotNetProject p = Services.ProjectService.CreateDotNetProject ("C#");
			p.FileName = Path.Combine (dir, "theproject");
			sol.RootFolder.Items.Add (p);
			
			await ws.SaveAsync (Util.GetMonitor ());
			
			Assert.IsTrue (File.Exists (ws.FileName));
			Assert.IsTrue (File.Exists (sol.FileName));
			Assert.IsTrue (File.Exists (p.FileName));

			ws.Dispose ();
		}
	}
}
