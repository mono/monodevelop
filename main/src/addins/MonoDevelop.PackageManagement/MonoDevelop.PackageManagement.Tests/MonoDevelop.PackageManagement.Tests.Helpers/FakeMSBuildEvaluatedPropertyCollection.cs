//
// FakeMSBuildEvaluatedPropertyCollection.cs
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

using System;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	public class FakeMSBuildEvaluatedPropertyCollection : IMSBuildEvaluatedPropertyCollection
	{
		public IEnumerable<IMSBuildPropertyEvaluated> Properties {
			get {
				throw new NotImplementedException ();
			}
		}

		public IEnumerable<IMSBuildPropertyEvaluated> GetProperties ()
		{
			throw new NotImplementedException ();
		}

		public FilePath GetPathValue (string name, FilePath defaultValue = default (FilePath), bool relativeToProject = true, FilePath relativeToPath = default (FilePath))
		{
			throw new NotImplementedException ();
		}

		public IMSBuildPropertyEvaluated GetProperty (string name)
		{
			throw new NotImplementedException ();
		}

		public Dictionary<string, string> PropertiesDictionary = new Dictionary<string, string> ();

		public string GetValue (string name, string defaultValue = null)
		{
			string result = defaultValue;
			if (PropertiesDictionary.TryGetValue (name, out result))
				return result;

			return defaultValue;
		}

		public object GetValue (string name, Type type, object defaultValue)
		{
			throw new NotImplementedException ();
		}

		public T GetValue<T> (string name)
		{
			throw new NotImplementedException ();
		}

		public T GetValue<T> (string name, T defaultValue)
		{
			throw new NotImplementedException ();
		}

		public bool HasProperty (string name)
		{
			throw new NotImplementedException ();
		}

		public bool TryGetPathValue (string name, out FilePath value, FilePath defaultValue = default (FilePath), bool relativeToProject = true, FilePath relativeToPath = default (FilePath))
		{
			throw new NotImplementedException ();
		}
	}
}
