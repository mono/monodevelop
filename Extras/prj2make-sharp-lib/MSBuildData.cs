//
// MSBuildData.cs
//
// Author:
//   Ankit Jain <jankit@novell.com>
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using MonoDevelop.Projects;

using System.Xml;
using System.Collections.Generic;

namespace MonoDevelop.Prj2Make
{
	class MSBuildData
	{
		XmlDocument doc;
		XmlElement globalConfigElement;
		string guid;
		List<string> extra;

		Dictionary<DotNetProjectConfiguration, XmlElement> configElements;

		Dictionary<ProjectFile, XmlElement> projectFileElements;
		Dictionary<ProjectReference, XmlElement> projectReferenceElements;

		public MSBuildData ()
		{
		}

		public XmlDocument Document {
			get { return doc; }
			set { doc = value; }
		}

		public XmlElement GlobalConfigElement {
			get { return globalConfigElement; }
			set { globalConfigElement = value; }
		}

		/* Guid w/o enclosing {} */
		public string Guid {
			get { return guid; }
			set { guid = value; }
		}

		public Dictionary<DotNetProjectConfiguration, XmlElement> ConfigElements {
			get {
				if (configElements == null)
					configElements = new Dictionary<DotNetProjectConfiguration, XmlElement> ();
				return configElements;
			}
		}
		
		public Dictionary<ProjectFile, XmlElement> ProjectFileElements {
			get {
				if (projectFileElements == null)
					projectFileElements = new Dictionary<ProjectFile, XmlElement> ();
				return projectFileElements;
			}
		}

		public Dictionary<ProjectReference, XmlElement> ProjectReferenceElements {
			get {
				if (projectReferenceElements == null)
					projectReferenceElements = new Dictionary<ProjectReference, XmlElement> ();
				return projectReferenceElements;
			}
		}

		public List<string> Extra {
			get { return extra; }
			set { extra = value;}
		}

	}
}
