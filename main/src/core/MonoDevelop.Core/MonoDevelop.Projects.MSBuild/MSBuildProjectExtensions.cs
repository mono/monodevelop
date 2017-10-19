//
// MSBuildProjectExtensions.cs
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace MonoDevelop.Projects.MSBuild
{
	public class MSBuildProjectExtensions: MSBuildElement
	{
		public MSBuildProjectExtensions ()
		{
		}

		internal override void Read (MSBuildXmlReader reader)
		{
			if (reader.ForEvaluation)
				reader.Read ();
			else
				base.Read (reader);
		}

		internal override bool ContentRequiredForEvaluation {
			get {
				return false;
			}
		}

		internal override string GetElementName ()
		{
			return "ProjectExtensions";
		}

		internal bool IsEmpty {
			get {
				return !ChildrenOfType<MSBuildXmlElement> ().Any ();
			}
		}

		public XmlElement GetProjectExtension (string section)
		{
			var elem = ChildrenOfType<MSBuildXmlElement> ().FirstOrDefault (n => n.Name == section);
			if (elem != null) {
				var w = new StringWriter ();
				using (var tw = new XmlTextWriter (w))
					elem.Write (tw, new WriteContext ());
				var doc = new XmlDocument ();
				doc.LoadXml (w.ToString ());
				return doc.DocumentElement;
			} else
				return null;
		}

		public void SetProjectExtension (XmlElement value)
		{
			AssertCanModify ();
			var sr = new StringReader (value.OuterXml);
			var xr = new XmlTextReader (sr);
			xr.MoveToContent ();
			var cr = new MSBuildXmlReader { XmlReader = xr };
			var section = value.LocalName;

			MSBuildXmlElement elem = new MSBuildXmlElement ();
			elem.Read (cr);

			int i = FindChildIndex (n => (n is MSBuildXmlElement) && ((MSBuildXmlElement)n).Name == section);
			if (i == -1)
				AddChild (elem);
			else {
				RemoveChildAt (i);
				InsertChild (i, elem);
			}
			elem.ParentNode = this;
			elem.ResetIndent (false);
			NotifyChanged ();
		}

		public void RemoveProjectExtension (string section)
		{
			AssertCanModify ();
			int i = FindChildIndex (n => (n is MSBuildXmlElement) && ((MSBuildXmlElement)n).Name == section);
			if (i != -1) {
				RemoveChildAt (i);
				NotifyChanged ();
			}
		}
	}
}

