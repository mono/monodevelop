//
// SourceRepositoryViewModel.cs
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

using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;

namespace MonoDevelop.PackageManagement
{
	internal class SourceRepositoryViewModel
	{
		List<SourceRepository> sourceRepositories;

		public SourceRepositoryViewModel (string name)
		{
			Name = name;
			sourceRepositories = new List<SourceRepository> ();
		}

		public SourceRepositoryViewModel (SourceRepository sourceRepository)
			: this (
				new [] { sourceRepository },
				sourceRepository.PackageSource)
		{
		}

		protected SourceRepositoryViewModel (
			IEnumerable<SourceRepository> sourceRepositories,
			PackageSource packageSource)
		{
			this.sourceRepositories = sourceRepositories.ToList ();
			PackageSource = packageSource;

			if (IsAggregate) {
				Name = GettextCatalog.GetString ("All Sources");
			} else {
				Name = packageSource.Name;
			}
		}

		public string Name { get; private set; }
		public PackageSource PackageSource { get; private set; }

		public IEnumerable<SourceRepository> GetSourceRepositories ()
		{
			return sourceRepositories;
		}

		public bool IsAggregate {
			get { return sourceRepositories.Count > 1; }
		}
	}
}

