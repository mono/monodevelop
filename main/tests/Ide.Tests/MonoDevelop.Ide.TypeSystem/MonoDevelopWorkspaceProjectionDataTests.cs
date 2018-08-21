//
// MonoDevelopWorkspaceProjectionDataTests.cs
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
using System.Collections.Generic;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Projection;
using NUnit.Framework;

namespace MonoDevelop.Ide.TypeSystem
{
	[TestFixture]
	public class MonoDevelopWorkspaceProjectionDataTests
	{
		[Test]
		public void TestSimpleCreation ()
		{
			var data = new MonoDevelopWorkspace.ProjectionData ();

			var projections = new List<Projection> ();
			var initial = new MonoDevelopWorkspace.ProjectionEntry {
				File = new MonoDevelop.Projects.ProjectFile ("name"),
				Projections = projections,
			};
			data.AddProjectionEntry (initial);

			var (projectionFromMap, filePathFromMap) = data.Get ("fileName");
			Assert.IsNull (projectionFromMap);

			// Using a real document from the tests dir
			var document = TextEditorFactory.CreateNewDocument ("GitIgnore.txt");
			projections.Add (new Projection (document, Array.Empty<ProjectedSegment> ()));

			(projectionFromMap, filePathFromMap) = data.Get ("GitIgnore.txt");
			Assert.IsNotNull (projectionFromMap);
			Assert.AreEqual ((string)initial.File.FilePath, filePathFromMap);
		}

		[Test]
		public void TestClearingOldProjections ()
		{
			var data = new MonoDevelopWorkspace.ProjectionData ();

			var projections = new List<Projection> ();
			// Using a real document from the tests dir
			var document = TextEditorFactory.CreateNewDocument ("GitIgnore.txt");
			projections.Add (new Projection (document, Array.Empty<ProjectedSegment> ()));

			var initial = new MonoDevelopWorkspace.ProjectionEntry {
				File = new MonoDevelop.Projects.ProjectFile ("name"),
				Projections = projections,
			};
			data.AddProjectionEntry (initial);

			data.ClearOldProjectionList ();

			var (projectionFromMap, filePathFromMap) = data.Get ("GitIgnore.txt");
			Assert.IsNull (projectionFromMap);
		}

		[Test]
		public void TestUpdatingProjections ()
		{
			var data = new MonoDevelopWorkspace.ProjectionData ();

			var projections = new List<Projection> ();
			// Using a real document from the tests dir
			var document = TextEditorFactory.CreateNewDocument ("GitIgnore.txt");
			projections.Add (new Projection (document, Array.Empty<ProjectedSegment> ()));

			var initial = new MonoDevelopWorkspace.ProjectionEntry {
				File = new MonoDevelop.Projects.ProjectFile ("name"),
				Projections = projections,
			};
			data.AddProjectionEntry (initial);

			data.UpdateProjectionEntry (initial.File, new List<Projection> ());

			var (projectionFromMap, filePathFromMap) = data.Get ("GitIgnore.txt");
			Assert.IsNull (projectionFromMap);
		}
	}
}
