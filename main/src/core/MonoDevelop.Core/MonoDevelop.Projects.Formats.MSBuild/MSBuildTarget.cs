//
// MSBuildTarget.cs
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
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Text;

using MonoDevelop.Core;
using MonoDevelop.Projects.Utility;
using System.Linq;
using MonoDevelop.Projects.Text;
using System.Threading.Tasks;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	
	public class MSBuildTarget: MSBuildElement
	{
		string name;
		List<MSBuildTask> tasks = new List<MSBuildTask> ();

		static readonly string [] knownAttributes = { "Name", "Condition", "Label" };

		internal override string [] GetKnownAttributes ()
		{
			return knownAttributes;
		}

		internal override void ReadAttribute (string name, string value)
		{
			if (name == "Name")
				this.name = value;
			else
				base.ReadAttribute (name, value);
		}

		internal override string WriteAttribute (string name)
		{
			if (name == "Name")
				return this.name;
			else
				return base.WriteAttribute (name);
		}

		internal override string GetElementName ()
		{
			return "Target";
		}

		internal override void ReadChildElement (MSBuildXmlReader reader)
		{
			var task = new MSBuildTask ();
			task.ParentObject = this;
			task.Read (reader);
			tasks.Add (task);
		}

		internal MSBuildTarget ()
		{
		}

		public MSBuildTarget (string name, IEnumerable<MSBuildTask> tasks)
		{
			this.name = name;
			this.tasks = new List<MSBuildTask> (tasks);
		}

		internal override IEnumerable<MSBuildObject> GetChildren ()
		{
			return tasks;
		}

		public string Name {
			get { return name; }
		}

		public bool IsImported { get; internal set; }

		public IEnumerable<MSBuildTask> Tasks {
			get { return tasks; }
		}

		public void RemoveTask (MSBuildTask task)
		{
			if (task.ParentObject != this)
				throw new InvalidOperationException ("Task doesn't belong to the target");
			task.RemoveIndent ();
			tasks.Remove (task);
		}
	}
	
}
