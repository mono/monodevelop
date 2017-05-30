//
// MSBuildPropertyCore.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Core;
using System.Globalization;

namespace MonoDevelop.Projects.MSBuild
{

	public abstract class MSBuildPropertyCore: MSBuildElement
	{
		internal override string GetElementName ()
		{
			return GetName ();
		}

		public string Name {
			get { return GetName (); }
		}

		public string Value {
			get { return GetPropertyValue (); }
		}

		public T GetValue<T> ()
		{
			return (T)GetValue (typeof(T));
		}

		public object GetValue (Type t)
		{
			var val = GetPropertyValue ();
			if (t == typeof(bool))
				return (object) val.Equals ("true", StringComparison.InvariantCultureIgnoreCase);
			if (t.IsEnum)
				return Enum.Parse (t, val, true);
			if (t.IsGenericType && t.GetGenericTypeDefinition () == typeof(Nullable<>)) {
				var at = t.GetGenericArguments () [0];
				if (string.IsNullOrEmpty (Value))
					return null;
				return Convert.ChangeType (Value, at, CultureInfo.InvariantCulture);
			}
			return Convert.ChangeType (Value, t, CultureInfo.InvariantCulture);
		}

		public FilePath GetPathValue (bool relativeToProject = true, FilePath relativeToPath = default(FilePath))
		{
			FilePath path;
			TryGetPathValue (out path, relativeToProject, relativeToPath);
			return path;
		}

		public bool TryGetPathValue (out FilePath value, bool relativeToProject = true, FilePath relativeToPath = default(FilePath))
		{
			var val = GetPropertyValue ();
			string baseDir = null;

			if (relativeToPath != null) {
				baseDir = relativeToPath;
			} else if (relativeToProject) {
				if (ParentProject == null) {
					// The path can't yet be resolved, return the raw value
					value = val;
					return true;
				}

				baseDir = ParentProject.BaseDirectory;
			}
			string path;
			var res = MSBuildProjectService.FromMSBuildPath (baseDir, val, out path);

			// Remove the trailing slash
			if (path != null && path.Length > 0 && path[path.Length - 1] == System.IO.Path.DirectorySeparatorChar && path != "." + System.IO.Path.DirectorySeparatorChar)
				path = path.TrimEnd (System.IO.Path.DirectorySeparatorChar);
			
			value = path;
			return res;
		}

		public abstract string UnevaluatedValue { get; }

		internal abstract string GetPropertyValue ();

		internal abstract string GetName ();

		public override string ToString ()
		{
			return "[" + Name + " = " + Value + "]";
		}
	}
	
}
