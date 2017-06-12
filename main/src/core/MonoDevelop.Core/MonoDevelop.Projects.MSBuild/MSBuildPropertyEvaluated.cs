//
// MSBuildPropertyEvaluated.cs
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

namespace MonoDevelop.Projects.MSBuild
{

	class MSBuildPropertyEvaluated: MSBuildPropertyCore, IMSBuildPropertyEvaluated, IMetadataProperty
	{
		string value;
		string evaluatedValue;
		string name;
		MSBuildProperty linkedProperty;
		LinkedPropertyFlags flags;

		internal MSBuildPropertyEvaluated (MSBuildProject project, string name, string value, string evaluatedValue)
		{
			ParentProject = project;
			this.evaluatedValue = evaluatedValue;
			this.value = value;
			this.name = name;
		}

		internal override string GetName ()
		{
			return name;
		}

		public bool IsImported { get; set; }

		public override string UnevaluatedValue {
			get {
				if (linkedProperty != null)
					return linkedProperty.UnevaluatedValue;
				return value; 
			}
		}

		internal bool IsNew {
			get { return (flags & LinkedPropertyFlags.IsNew) != 0; }
			set {
				if (value)
					flags |= LinkedPropertyFlags.IsNew;
				else
					flags &= ~LinkedPropertyFlags.IsNew; 
			}
		}

		public MSBuildProperty LinkedProperty {
			get {
				return linkedProperty;
			}
		}

		internal override string GetPropertyValue ()
		{
			if (linkedProperty != null)
				return linkedProperty.Value;
			return evaluatedValue;
		}

		public void LinkToProperty (MSBuildProperty property)
		{
			linkedProperty = property;
			if (linkedProperty != null && !linkedProperty.Modified && !IsNew)
				linkedProperty.InitEvaluatedValue (evaluatedValue);
		}

		void IMetadataProperty.SetValue (string value, bool preserveCase, bool mergeToMainGroup, MSBuildValueType valueType)
		{
			if (linkedProperty == null)
				throw new InvalidOperationException ("Evaluated property can't be modified");
			linkedProperty.SetValue (value, preserveCase, mergeToMainGroup, valueType);
		}

		void IMetadataProperty.SetValue (FilePath value, bool relativeToProject, FilePath relativeToPath, bool mergeToMainGroup)
		{
			if (linkedProperty == null)
				throw new InvalidOperationException ("Evaluated property can't be modified");
			linkedProperty.SetValue (value, relativeToProject, relativeToPath, mergeToMainGroup);
		}

		void IMetadataProperty.SetValue (object value, bool mergeToMainGroup)
		{
			if (linkedProperty == null)
				throw new InvalidOperationException ("Evaluated property can't be modified");
			linkedProperty.SetValue (value, mergeToMainGroup);
		}
	}

	[Flags]
	public enum LinkedPropertyFlags: byte
	{
		Modified = 1,
		IsNew = 2,
		HasDefaultValue = 4,
		Overwritten = 8,
		MergeToMainGroup = 16,
		Imported = 32,
		EvaluatedValueModified = 64
	}
}
