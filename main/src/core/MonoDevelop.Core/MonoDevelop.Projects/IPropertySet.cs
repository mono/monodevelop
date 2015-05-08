//
// IPropertySet.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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
using System.Xml;
using MonoDevelop.Core;
using System.Xml.Linq;
using MonoDevelop.Projects.Formats.MSBuild;
using System.Collections.Generic;

namespace MonoDevelop.Projects
{
	public interface IReadOnlyPropertySet
	{
		string GetValue (string name, string defaultValue = null);
		FilePath GetPathValue (string name, FilePath defaultValue = default(FilePath), bool relativeToProject = true, FilePath relativeToPath = default(FilePath));
		bool TryGetPathValue (string name, out FilePath value, FilePath defaultValue = default(FilePath), bool relativeToProject = true, FilePath relativeToPath = default(FilePath));
		T GetValue<T> (string name);
		T GetValue<T> (string name, T defaultValue);
		object GetValue (string name, Type type, object defaultValue);
	}

	public interface IPropertySet: IReadOnlyPropertySet
	{
		bool HasProperty (string name);
		IMetadataProperty GetProperty (string name);
		IEnumerable<IMetadataProperty> GetProperties ();

		void SetValue (string name, string value, string defaultValue = null, bool preserveExistingCase = false, bool mergeToMainGroup = false, string condition = null);
		void SetValue (string name, FilePath value, FilePath defaultValue = default(FilePath), bool relativeToProject = true, FilePath relativeToPath = default(FilePath), bool mergeToMainGroup = false, string condition = null);
		void SetValue (string name, object value, object defaultValue = null, bool mergeToMainGroup = false, string condition = null);

		bool RemoveProperty (string name);
		void RemoveAllProperties ();

		void SetPropertyOrder (params string[] propertyNames);
	}
}

