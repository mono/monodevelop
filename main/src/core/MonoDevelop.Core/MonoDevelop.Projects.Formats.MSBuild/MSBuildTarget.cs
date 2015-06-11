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
		string afterTargets;
		string inputs;
		string outputs;
		string beforeTargets;
		string dependsOnTargets;
		string returns;
		string keepDuplicateOutputs;

		static readonly string [] knownAttributes = { "Name", "Condition", "Label", "AfterTargets", "Inputs", "Outputs", "BeforeTargets", "DependsOnTargets", "Returns", "KeepDuplicateOutputs" };

		internal override string [] GetKnownAttributes ()
		{
			return knownAttributes;
		}

		string AfterTargets {
			get {
				return this.afterTargets;
			}
			set {
				this.afterTargets = value;
				NotifyChanged ();
			}
		}

		string Inputs {
			get {
				return this.inputs;
			}
			set {
				this.inputs = value;
				NotifyChanged ();
			}
		}

		string Outputs {
			get {
				return this.outputs;
			}
			set {
				this.outputs = value;
				NotifyChanged ();
			}
		}

		string BeforeTargets {
			get {
				return this.beforeTargets;
			}
			set {
				this.beforeTargets = value;
				NotifyChanged ();
			}
		}

		string DependsOnTargets
		{
			get {
				return this.dependsOnTargets;
			}
			set {
				this.dependsOnTargets = value;
				NotifyChanged ();
			}
		}

		string Returns {
			get {
				return this.returns;
			}
			set {
				this.returns = value;
				NotifyChanged ();
			}
		}

		string KeepDuplicateOutputs {
			get {
				return this.keepDuplicateOutputs;
			}
			set {
				this.keepDuplicateOutputs = value;
				NotifyChanged ();
			}
		}

		internal override void ReadAttribute (string name, string value)
		{
			switch (name) {
				case "Name": this.name = value; break;
				case "AfterTargets": AfterTargets = value; break;
				case "Inputs": Inputs = value; break;
				case "Outputs": Outputs = value; break;
				case "BeforeTargets": BeforeTargets = value; break;
				case "DependsOnTargets": DependsOnTargets = value; break;
				case "Returns": Returns = value; break;
				case "KeepDuplicateOutputs": KeepDuplicateOutputs = value; break;
				default: base.ReadAttribute (name, value); break;
			}
		}

		internal override string WriteAttribute (string name)
		{
			switch (name) {
				case "Name": return this.name;
				case "AfterTargets": return AfterTargets;
				case "Inputs": return Inputs;
				case "Outputs": return Outputs;
				case "BeforeTargets": return BeforeTargets;
				case "DependsOnTargets": return DependsOnTargets;
				case "Returns": return Returns;
				case "KeepDuplicateOutputs": return KeepDuplicateOutputs;
				default: return base.WriteAttribute (name);
			}
		}

		internal override string GetElementName ()
		{
			return "Target";
		}

		internal override void ReadChildElement (MSBuildXmlReader reader)
		{
			MSBuildObject ob = null;
			switch (reader.LocalName) {
				case "ItemGroup": ob = new MSBuildItemGroup (); break;
				case "PropertyGroup": ob = new MSBuildPropertyGroup (); break;
			}
			if (ob != null) {
				ob.ParentNode = this;
				ob.Read (reader);
				ChildNodes.Add (ob);
				return;
			}

			var task = new MSBuildTask ();
			task.ParentNode = this;
			task.Read (reader);
			ChildNodes.Add (task);
		}

		internal override void Write (XmlWriter writer, WriteContext context)
		{
			base.Write (writer, context);
		}

		internal MSBuildTarget ()
		{
		}

		public MSBuildTarget (string name, IEnumerable<MSBuildTask> tasks)
		{
			this.name = name;
			ChildNodes.AddRange (tasks);
		}

		public string Name {
			get { return name; }
		}

		public bool IsImported { get; internal set; }

		public IEnumerable<MSBuildTask> Tasks {
			get { return ChildNodes.OfType<MSBuildTask> (); }
		}

		public void RemoveTask (MSBuildTask task)
		{
			if (task.ParentObject != this)
				throw new InvalidOperationException ("Task doesn't belong to the target");
			task.RemoveIndent ();
			ChildNodes.Remove (task);
		}
	}
	
}
