//
// MSBuildSdk.cs
//
// Author:
//       Mathieu Bourgeois <mathieubourgeois1338@gmail.com>
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

namespace MonoDevelop.Projects.MSBuild
{
	public class MSBuildSdk : MSBuildElement
	{
		static readonly string[] knownAttributes = { "Name", "Version" };

		internal override string[] GetKnownAttributes ()
		{
			return knownAttributes;
		}

		private string name;
		public string Name {
			get { return name; }
			set { AssertCanModify (); name = value; NotifySdkChanged (); }
		}

		private string version;
		public string Version {
			get { return version; }
			set { AssertCanModify (); version = value; NotifySdkChanged (); }
		}

		void NotifySdkChanged ()
		{
			if (ParentProject != null)
				ParentProject.NotifySdkChanged ();
		}

		internal override string GetElementName ()
		{
			return "Sdk";
		}

		internal override void ReadAttribute (string name, string value)
		{
			if (name == "Name")
				Name = value;
			else if (name == "Version")
				Version = value;
			else
				base.ReadAttribute (name, value);
		}

		internal override string WriteAttribute (string name)
		{
			if (name == "Name")
				return Name;
			else if (name == "Version")
				return Version;
			else
				return base.WriteAttribute (name);
		}
	}
}

