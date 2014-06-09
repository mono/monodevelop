//
// MSBuildElements.cs
//
// Author:
//       Michael Hutchinson <mhutch@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc.
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
using System.Linq;

namespace MonoDevelop.Xml.MSBuild
{
	class MSBuildElement
	{
		static readonly string[] emptyArray = new string[0];

		string[] children, attributes;

		public IEnumerable<string> Children { get { return children; } }
		public IEnumerable<string> Attributes { get { return attributes; } }
		public string Name { get; private set; }
		public string ChildType { get; private set; }
		public bool IsSpecial  { get; private set; }

		public MSBuildElement (
			string name, bool isSpecial = false, string[] children = null,
			string[] attributes = null, string childType = null)
		{
			this.Name = name;
			this.children = children ?? emptyArray;
			this.attributes = attributes ?? emptyArray;
			this.ChildType = childType;
			this.IsSpecial = isSpecial;
		}

		public bool HasChild (string name)
		{
			return children != null && children.Contains (name, StringComparer.Ordinal);
		}

		public static MSBuildElement Get (string name, MSBuildElement parent = null)
		{
			//if not in parent's known children, and parent has special children, then it's a special child
			if (parent != null && parent.ChildType != null && !parent.HasChild (name))
				name = parent.ChildType;
			MSBuildElement result;
			builtin.TryGetValue (name, out result);
			return result;
		}

		static readonly Dictionary<string,MSBuildElement> builtin = new Dictionary<string, MSBuildElement> {
			{
				"Choose", new MSBuildElement ("Choose",
					children: new[] { "Otherwise", "When" }
				)
			},
			{
				"Import", new MSBuildElement ("Import",
					attributes: new[] { "Condition", "Project" }
				)
			},
			{
				"ImportGroup", new MSBuildElement ("ImportGroup",
					children: new[] { "Import" },
					attributes: new[] { "Condition" }
				)
			},
			{
				"Item", new MSBuildElement ("Item",
					childType: "ItemMetadata",
					attributes: new[] { "Condition", "Exclude", "Include", "Remove" },
					isSpecial: true
				)
			},
			{
				"ItemDefinitionGroup", new MSBuildElement ("ItemDefinitionGroup",
					childType: "Item",
					attributes: new[] { "Condition" }
				)
			},
			{
				"ItemGroup", new MSBuildElement ("ItemGroup",
					childType: "Item",
					attributes: new[] { "Condition" }
				)
			},
			{
				"ItemMetadata", new MSBuildElement ("ItemMetadata",
					attributes: new[] { "Condition" },
					isSpecial: true
				)
			},
			{
				"OnError", new MSBuildElement ("OnError",
					attributes: new[] { "Condition", "ExecuteTargets" }
				)
			},
			{
				"Otherwise", new MSBuildElement ("Otherwise",
					children: new[] { "Choose", "ItemGroup", "PropertyGroup" }
				)
			},
			{
				"Output", new MSBuildElement ("Output",
					attributes: new[] { "Condition", "ItemName", "PropertyName", "TaskParameter" }
				)
			},
			{
				"Parameter", new MSBuildElement ("Parameter",
					attributes: new[] { "Output", "ParameterType", "Required" },
					isSpecial: true
				)
			},
			{
				"ParameterGroup", new MSBuildElement ("ParameterGroup",
					childType: "Parameter"
				)
			},
			{
				"Project", new MSBuildElement ("Project",
					children: new[] {
						"Choose", "Import", "ItemGroup", "ProjectExtensions", "PropertyGroup", "Target", "UsingTask"
					},
					attributes: new[] {
						"DefaultTargets", "InitialTargets", "ToolsVersion", "TreatAsLocalProperty", "xmlns"
					}
				)
			},
			{
				"ProjectExtensions", new MSBuildElement ("ProjectExtensions",
					childType: "Data"
				)
			},
			{
				"Property", new MSBuildElement ("Property",
					attributes: new[] { "Condition" },
					isSpecial: true
				)
			},
			{
				"PropertyGroup", new MSBuildElement ("PropertyGroup",
					childType: "Property",
					attributes: new[] { "Condition" }
				)
			},
			{
				"Target", new MSBuildElement ("Target",
					childType: "Task",
					children: new[] { "OnError", "ItemGroup", "PropertyGroup" },
					attributes: new[] {
						"AfterTargets", "BeforeTargets", "Condition", "DependsOnTargets", "Inputs",
						"KeepDuplicateOutputs", "Name", "Outputs", "Returns"
					}
				)
			},
			{
				"Task", new MSBuildElement ("Task",
					children: new[] { "Output" },
					attributes: new[] { "Condition", "ContinueOnError", "Parameter" },
					isSpecial: true
				)
			},
			{
				"TaskBody", new MSBuildElement ("TaskBody",
					childType: "Data",
					attributes: new[] { "Evaluate" }
				)
			},
			{
				"UsingTask", new MSBuildElement ("UsingTask",
					children: new[] { "ParameterGroup", "TaskBody" },
					attributes: new[] { "AssemblyFile", "AssemblyName", "Condition", "TaskFactory", "TaskName" }
				)
			},
			{
				"When", new MSBuildElement ("When",
					children: new[] { "Choose", "ItemGroup", "PropertyGroup" },
					attributes: new[] { "Condition" }
				)
			},
		};
	}
}