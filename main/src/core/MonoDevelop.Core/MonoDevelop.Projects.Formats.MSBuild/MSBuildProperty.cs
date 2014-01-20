//
// MSBuildProperty.cs
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
using Microsoft.Build.BuildEngine;
using System.Xml.Linq;
using MonoDevelop.Core;
using System.Globalization;


namespace MonoDevelop.Projects.Formats.MSBuild
{
	
	public sealed class MSBuildProperty: MSBuildPropertyCore
	{
		bool preserverCase;
		string defaultValue;

		internal MSBuildProperty (MSBuildProject project, XmlElement elem): base (project, elem)
		{
		}

		public string Name {
			get { return Element.Name; }
		}

		public bool IsImported {
			get;
			set;
		}

		internal bool Overwritten { get; set; }

		internal MSBuildPropertyGroup Owner { get; set; }

		public bool MergeToMainGroup { get; set; }
		internal bool HasDefaultValue { get; set; }

		internal MergedProperty CreateMergedProperty ()
		{
			return new MergedProperty (Name, preserverCase, HasDefaultValue);
		}

		internal void SetDefaultValue (string value)
		{
			defaultValue = value;
		}

		public void SetValue (string value, bool preserveCase = false, bool mergeToMainGroup = false)
		{
			MergeToMainGroup = mergeToMainGroup;
			this.preserverCase = preserveCase;

			if (value == null)
				value = String.Empty;

			if (preserveCase) {
				var current = GetPropertyValue ();
				if (current != null) {
					if (current.Equals (value, preserveCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture))
						return;
				}
			}
			SetPropertyValue (value);
		}

		public void SetValue (FilePath value, bool relativeToProject = true, FilePath relativeToPath = default(FilePath), bool mergeToMainGroup = false)
		{
			MergeToMainGroup = mergeToMainGroup;
			this.preserverCase = false;

			string baseDir = null;
			if (relativeToPath != null) {
				baseDir = relativeToPath;
			} else if (relativeToProject) {
				baseDir = Project.BaseDirectory;
			}
			SetPropertyValue (MSBuildProjectService.ToMSBuildPath (baseDir, value, false));
		}

		public void SetValue (object value, bool mergeToMainGroup = false)
		{
			if (value is bool) {
				if (Owner != null && Owner.UppercaseBools)
					SetValue ((bool)value ? "True" : "False", preserveCase: true, mergeToMainGroup: mergeToMainGroup);
				else
					SetValue ((bool)value ? "true" : "false", preserveCase: true, mergeToMainGroup: mergeToMainGroup);
			}
			else
				SetValue (Convert.ToString (value, CultureInfo.InvariantCulture), false, mergeToMainGroup);
		}		

		void SetPropertyValue (string value)
		{
			Element.InnerText = value;
		}

		internal override string GetPropertyValue ()
		{
			return Element.InnerText;
		}

		public override string UnevaluatedValue {
			get {
				return Value;
			}
		}
	}

	class MSBuildPropertyEvaluated: MSBuildPropertyCore
	{
		string value;
		string evaluatedValue;

		internal MSBuildPropertyEvaluated (MSBuildProject project, string name, string value, string evaluatedValue): base (project, null)
		{
			this.evaluatedValue = evaluatedValue;
			this.value = value;
			Name = name;
		}

		public string Name { get; private set; }

		public bool IsImported { get; set; }

		public override string UnevaluatedValue {
			get { return value; }
		}

		internal override string GetPropertyValue ()
		{
			return evaluatedValue;
		}
	}

	public abstract class MSBuildPropertyCore: MSBuildObject, IMSBuildPropertyEvaluated
	{
		MSBuildProject project;

		internal MSBuildPropertyCore (MSBuildProject project, XmlElement elem): base (elem)
		{
			this.project = project;
		}

		public MSBuildProject Project {
			get { return project; }
		}

		public string Value {
			get { return GetPropertyValue (); }
		}

		public T GetValue<T> ()
		{
			var val = GetPropertyValue ();
			if (typeof(T) == typeof(bool))
				return (T) (object) val.Equals ("true", StringComparison.InvariantCultureIgnoreCase);
			if (typeof(T).IsEnum)
				return (T) Enum.Parse (typeof(T), val, true);
			return (T) Convert.ChangeType (Value, typeof(T), CultureInfo.InvariantCulture);
		}

		public object GetValue (Type t)
		{
			var val = GetPropertyValue ();
			if (t == typeof(bool))
				return (object) val.Equals ("true", StringComparison.InvariantCultureIgnoreCase);
			return Convert.ChangeType (Value, t, CultureInfo.InvariantCulture);
		}

		public FilePath GetPathValue (bool relativeToProject = true, FilePath relativeToPath = default(FilePath))
		{
			var val = GetPropertyValue ();
			string baseDir = null;

			if (relativeToPath != null) {
				baseDir = relativeToPath;
			} else if (relativeToProject) {
				baseDir = project.BaseDirectory;
			}
			return MSBuildProjectService.FromMSBuildPath (baseDir, val);
		}

		public bool TryGetPathValue (out FilePath value, bool relativeToProject = true, FilePath relativeToPath = default(FilePath))
		{
			var val = GetPropertyValue ();
			string baseDir = null;

			if (relativeToPath != null) {
				baseDir = relativeToPath;
			} else if (relativeToProject) {
				baseDir = project.BaseDirectory;
			}
			string path;
			var res = MSBuildProjectService.FromMSBuildPath (baseDir, val, out path);
			value = path;
			return res;
		}

		public abstract string UnevaluatedValue { get; }

		internal abstract string GetPropertyValue ();
	}

	public interface IMSBuildPropertyEvaluated
	{
		MSBuildProject Project { get; }

		string Value { get; }

		string UnevaluatedValue { get; }

		T GetValue<T> ();

		object GetValue (Type t);

		FilePath GetPathValue (bool relativeToProject = true, FilePath relativeToPath = default(FilePath));

		bool TryGetPathValue (out FilePath value, bool relativeToProject = true, FilePath relativeToPath = default(FilePath));
	}

	class MergedProperty
	{
		public readonly string Name;
		public readonly bool IsDefault;
		public readonly bool PreserveExistingCase;

		public MergedProperty (string name, bool preserveExistingCase, bool isDefault)
		{
			this.Name = name;
			IsDefault = isDefault;
			this.PreserveExistingCase = preserveExistingCase;
		}
	}
}
