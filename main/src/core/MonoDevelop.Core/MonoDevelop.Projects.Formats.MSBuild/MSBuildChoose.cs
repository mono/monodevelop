//
// MSBuildChooseElement.cs
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
using System.Xml;
using System.Collections.Generic;
using System.Linq;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	public class MSBuildChoose: MSBuildElement
	{
		List<MSBuildChooseOption> options = new List<MSBuildChooseOption> ();

		internal override void ReadChildElement (MSBuildXmlReader reader)
		{
			MSBuildChooseOption op = null;
			switch (reader.LocalName) {
				case "When": op = new MSBuildChooseOption (); break;
				case "Otherwise": op = new MSBuildChooseOption (true); break;
			}
			if (op != null) {
				op.ParentObject = this;
				op.Read (reader);
				options.Add (op);
			} else
				base.ReadChildElement (reader);
		}

		internal override string GetElementName ()
		{
			return "Choose";
		}

		internal override IEnumerable<MSBuildObject> GetChildren ()
		{
			return options;
		}

		internal IEnumerable<MSBuildChooseOption> GetOptions ()
		{
			return options;
		}
	}

	public class MSBuildChooseOption: MSBuildElement
	{
		List<MSBuildObject> objects = new List<MSBuildObject> ();

		public MSBuildChooseOption ()
		{
		}

		public MSBuildChooseOption (bool isOtherwise)
		{
			IsOtherwise = isOtherwise;
		}

		public bool IsOtherwise {
			get; private set;
		}

		internal override void ReadChildElement (MSBuildXmlReader reader)
		{
			MSBuildObject ob = null;
			switch (reader.LocalName) {
				case "ItemGroup": ob = new MSBuildItemGroup (); break;
				case "PropertyGroup": ob = new MSBuildPropertyGroup (); break;
				case "ImportGroup": ob = new MSBuildImportGroup (); break;
				case "Choose": ob = new MSBuildChoose (); break;
			}
			if (ob != null) {
				ob.ParentObject = this;
				ob.Read (reader);
				objects.Add (ob);
			} else
				reader.Read ();
		}

		internal override string GetElementName ()
		{
			return IsOtherwise ? "Otherwise" : "When";
		}

		internal override IEnumerable<MSBuildObject> GetChildren ()
		{
			return objects;
		}

		public IEnumerable<MSBuildObject> GetAllObjects ()
		{
			return objects;
		}
	}
}

