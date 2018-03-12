//
// FakeProject.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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

using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	class FakeProject : IProject
	{
		public string Name { get; set; }
		public FilePath FileName { get; set; }
		public FilePath BaseDirectory { get; set; }
		public FilePath BaseIntermediateOutputPath { get; set; }
		public ISolution ParentSolution { get; set; }
		public IDictionary ExtendedProperties { get; set; }

		List<string> flavorGuids;

		public FakeProject ()
		{
			ExtendedProperties = new Hashtable ();
			flavorGuids = new List<string> ();
		}

		public FakeProject (string fileName)
			: this ()
		{
			ChangeFileName (fileName);
		}

		public void ChangeFileName (string fileName)
		{
			FileName = new FilePath (fileName.ToNativePath ());
			BaseDirectory = FileName.ParentDirectory;
			BaseIntermediateOutputPath = BaseDirectory.Combine ("obj");
		}

		public bool IsSaved;

		public virtual Task SaveAsync ()
		{
			IsSaved = true;
			return Task.FromResult (0);
		}

		public IEnumerable<string> FlavorGuids {
			get { return flavorGuids; }
		}

		public void AddFlavorGuid (string guid)
		{
			flavorGuids.Add (guid);
		}

		public FakeMSBuildEvaluatedPropertyCollection FakeEvaluatedProperties = new FakeMSBuildEvaluatedPropertyCollection ();

		public IMSBuildEvaluatedPropertyCollection EvaluatedProperties {
			get { return FakeEvaluatedProperties; }
		}

		public bool IsReevaluated;

		public Task ReevaluateProject (ProgressMonitor monitor)
		{
			IsReevaluated = true;
			return Task.FromResult (0);
		}
	}
}

