//
// MSBuildPropertyGroupEvaluated.cs
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
using System.Collections.Generic;
using System.Xml.Linq;
using MonoDevelop.Core;
using Microsoft.Build.BuildEngine;
using MSProject = Microsoft.Build.BuildEngine.Project;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	class MSBuildPropertyGroupEvaluated: IMSBuildPropertyGroupEvaluated
	{
		internal Dictionary<string,MSBuildPropertyEvaluated> properties = new Dictionary<string, MSBuildPropertyEvaluated> ();
		internal MSBuildProject parent;
		BuildItem sourceItem;

		internal MSBuildPropertyGroupEvaluated (MSBuildProject parent)
		{
			this.parent = parent;
		}

		internal void Sync (BuildItem item)
		{
			properties.Clear ();
			sourceItem = item;
		}

		public bool HasProperty (string name)
		{
			if (!properties.ContainsKey (name)) {
				if (sourceItem != null)
					return sourceItem.HasMetadata (name);
			}
			return false;
		}

		public IMSBuildPropertyEvaluated GetProperty (string name)
		{
			MSBuildPropertyEvaluated prop;
			if (!properties.TryGetValue (name, out prop)) {
				if (sourceItem != null) {
					if (sourceItem.HasMetadata (name)) {
						prop = new MSBuildPropertyEvaluated (parent, name, sourceItem.GetMetadata (name), sourceItem.GetEvaluatedMetadata (name));
						properties [name] = prop;
					}
				}
			}
			return prop;
		}

		public string GetValue (string name, string defaultValue = null)
		{
			var prop = GetProperty (name);
			if (prop != null)
				return prop.Value;
			else
				return defaultValue;
		}

		public FilePath GetPathValue (string name, FilePath defaultValue = default(FilePath), bool relativeToProject = true, FilePath relativeToPath = default(FilePath))
		{
			var prop = GetProperty (name);
			if (prop != null)
				return prop.GetPathValue (relativeToProject, relativeToPath);
			else
				return defaultValue;
		}

		public bool TryGetPathValue (string name, out FilePath value, FilePath defaultValue = default(FilePath), bool relativeToProject = true, FilePath relativeToPath = default(FilePath))
		{
			var prop = GetProperty (name);
			if (prop != null)
				return prop.TryGetPathValue (out value, relativeToProject, relativeToPath);
			else {
				value = defaultValue;
				return value != default(FilePath);
			}
		}

		public T GetValue<T> (string name)
		{
			var prop = GetProperty (name);
			if (prop != null)
				return prop.GetValue<T> ();
			else
				return default(T);
		}

		public T GetValue<T> (string name, T defaultValue)
		{
			var prop = GetProperty (name);
			if (prop != null)
				return prop.GetValue<T> ();
			else
				return defaultValue;
		}

		public object GetValue (string name, Type type, object defaultValue)
		{
			var prop = GetProperty (name);
			if (prop != null)
				return prop.GetValue (type);
			else
				return defaultValue;
		}
	}

	class MSBuildEvaluatedPropertyCollection: MSBuildPropertyGroupEvaluated, IMSBuildEvaluatedPropertyCollection
	{
		public MSBuildEvaluatedPropertyCollection (MSBuildProject parent): base (parent)
		{
		}

		internal void Sync (BuildPropertyGroup msgroup)
		{
			properties.Clear ();
			foreach (BuildProperty p in msgroup)
				properties [p.Name] = new MSBuildPropertyEvaluated (parent, p.Name, p.Value, p.FinalValue);
		}

		public IEnumerable<IMSBuildPropertyEvaluated> Properties {
			get { return properties.Values; }
		}
	}

	public interface IMSBuildEvaluatedPropertyCollection: IMSBuildPropertyGroupEvaluated
	{
		IEnumerable<IMSBuildPropertyEvaluated> Properties { get; }
	}
}

