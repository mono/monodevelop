//
// MSBuildItem.cs
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

using System.Xml;


namespace MonoDevelop.Projects.Formats.MSBuild
{
	public class MSBuildItem: MSBuildObject, IMSBuildItemEvaluated
	{
		MSBuildPropertyGroup metadata;
		MSBuildPropertyGroupEvaluated evaluatedMetadata;
		MSBuildProject parent;
		string evaluatedInclude;

		internal MSBuildItem (MSBuildProject parent, XmlElement elem): base (elem)
		{
			this.parent = parent;
		}
		
		public string Include {
			get { return evaluatedInclude ?? UnevaluatedInclude; }
			set { evaluatedInclude = UnevaluatedInclude = value; }
		}
		
		public string UnevaluatedInclude {
			get { return Element.GetAttribute ("Include"); }
			set { Element.SetAttribute ("Include", value); }
		}

		internal void SetEvalResult (string value)
		{
			this.evaluatedInclude = value;
		}

		public bool IsImported {
			get;
			set;
		}

		public string Name {
			get { return Element.Name; }
		}

		public IMSBuildPropertySet Metadata {
			get {
				if (metadata == null) {
					metadata = new MSBuildPropertyGroup (parent, Element);
					metadata.UppercaseBools = true;
				}
				return metadata; 
			}
		}

		public IMSBuildPropertyGroupEvaluated EvaluatedMetadata {
			get {
				if (evaluatedMetadata == null)
					evaluatedMetadata = new MSBuildPropertyGroupEvaluated (parent);
				return evaluatedMetadata; 
			}
		}

		IMSBuildPropertyGroupEvaluated IMSBuildItemEvaluated.Metadata {
			get { return EvaluatedMetadata; }
		}

		public void WriteDataObjects ()
		{
			metadata.WriteDataObjects ();
			if (!Element.HasChildNodes)
				Element.IsEmpty = true;
		}
	}

	class MSBuildItemEvaluated: MSBuildObject, IMSBuildItemEvaluated
	{
		MSBuildPropertyGroupEvaluated metadata;
		MSBuildProject parent;
		string evaluatedInclude;
		string include;

		internal MSBuildItemEvaluated (MSBuildProject parent, string name, string include, string evaluatedInclude): base (null)
		{
			this.include = include;
			this.evaluatedInclude = evaluatedInclude;
			this.parent = parent;
			Name = name;
		}

		public string Include {
			get { return evaluatedInclude; }
		}

		public string UnevaluatedInclude {
			get { return include; }
		}

		public bool IsImported {
			get;
			internal set;
		}

		public string Name { get; private set; }

		public IMSBuildPropertyGroupEvaluated Metadata {
			get {
				if (metadata == null)
					metadata = new MSBuildPropertyGroupEvaluated (parent);
				return metadata; 
			}
		}
	}


	public interface IMSBuildItemEvaluated
	{
		string Include { get; }

		string UnevaluatedInclude { get; }

		string Condition { get; }

		bool IsImported { get; }

		string Name { get; }

		IMSBuildPropertyGroupEvaluated Metadata { get; }
	}
}
