//
// MSBuildTextEditorExtension.cs
//
// Authors:
//   Michael Hutchinson <mhutch@xamarin.com>
//
// Copyright:
//   (C) 2014 Xamarin Inc. (http://www.xamarin.com)
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
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Xml.StateEngine;
using MonoDevelop.XmlEditor.Completion;
using MonoDevelop.Core;

namespace MonoDevelop.XmlEditor
{
	class MSBuildTextEditorExtension : MonoDevelop.XmlEditor.Gui.BaseXmlEditorExtension
	{
		public static readonly string MSBuildMimeType = "application/x-msbuild";

		protected override void GetElementCompletions (CompletionDataList list)
		{
			AddMiscBeginTags (list);

			var path = GetCurrentPath ();

			if (path.Count == 0) {
				list.Add (new XmlCompletionData ("Project", XmlCompletionData.DataType.XmlElement));
			} else {
				var el = GetMSBuildElement (path);
				if (el != null && el.Children != null)
					foreach (var c in el.Children)
						list.Add (new XmlCompletionData (c, XmlCompletionData.DataType.XmlElement));
			}
		}

		protected override CompletionDataList GetAttributeCompletions (IAttributedXObject attributedOb,
			Dictionary<string, string> existingAtts)
		{
			var el = GetMSBuildElement (GetCurrentPath ());

			if (el != null && el.Attributes != null) {
				var list = new CompletionDataList ();
				foreach (var a in el.Attributes)
					if (!existingAtts.ContainsKey (a))
						list.Add (new XmlCompletionData (a, XmlCompletionData.DataType.XmlElement));
				return list;
			}

			return null;
		}

		MSBuildElement GetMSBuildElement (IList<XObject> path)
		{
			//need to look up element by walking how the path, since at each level, if the parent has special children,
			//then that gives us information to identify the type of its children
			MSBuildElement el = null;
			for (int i = 0; i < path.Count; i++) {
				//if children of parent is known to be arbitrary data, give up on completion
				if (el != null && el.SpecialChildName == "Data")
					return null;
				//code completion is forgiving, all we care about best guess resolve for deepest child
				var xel = path [i] as XElement;
				el = xel != null && xel.Name.Prefix != null ? ResolveBuiltinElement (xel.Name.Name, el) : null;
			}

			return el;
		}

		bool inferenceQueued = false;
		MSBuildResolveInfo inferredCompletionData;

		void QueueInference ()
		{
			XmlParsedDocument doc = this.CU as XmlParsedDocument;
			if (doc == null || doc.XDocument == null || inferenceQueued)
				return;
			if (inferredCompletionData != null) {
				if ((doc.LastWriteTimeUtc - inferredCompletionData.TimeStampUtc).TotalSeconds < 5)
					return;
			}
			inferenceQueued = true;
			System.Threading.ThreadPool.QueueUserWorkItem (delegate {
				try {
					var newData = new MSBuildResolveInfo ();
					newData.Populate (doc.XDocument);
					newData.TimeStampUtc = DateTime.UtcNow;
					if (doc.Errors.Count > 0)
						newData.Merge (inferredCompletionData);
					inferredCompletionData = newData;
					inferenceQueued = false;
				} catch (Exception ex) {
					LoggingService.LogInternalError ("Unhandled error in XML inference", ex);
				}
			});
		}

		protected override void OnParsedDocumentUpdated ()
		{
			QueueInference ();
			base.OnParsedDocumentUpdated ();
		}

		//expected to be immutable except via Populate and Merge
		class MSBuildResolveInfo
		{
			public DateTime TimeStampUtc;
			public readonly Dictionary<string,HashSet<string>> Items = new Dictionary<string, HashSet<string>> ();
			public readonly Dictionary<string,HashSet<string>> Tasks = new Dictionary<string, HashSet<string>> ();
			public readonly HashSet<string> Properties = new HashSet<string> ();
			public readonly HashSet<string> Imports = new HashSet<string> ();

			public void Merge (MSBuildResolveInfo other)
			{
				foreach (var otherItem in other.Items) {
					HashSet<string> item;
					if (Items.TryGetValue (otherItem.Key, out item)) {
						foreach (var att in otherItem.Value)
							item.Add (att);
					} else {
						Items [otherItem.Key] = otherItem.Value;
					}
				}
				foreach (var otherTask in other.Tasks) {
					HashSet<string> task;
					if (Tasks.TryGetValue (otherTask.Key, out task)) {
						foreach (var att in otherTask.Value)
							task.Add (att);
					} else {
						Tasks [otherTask.Key] = otherTask.Value;
					}
				}
				foreach (var prop in other.Properties) {
					Properties.Add (prop);
				}
				foreach (var imp in other.Imports) {
					Imports.Add (imp);
				}
			}

			public void Populate (XDocument doc)
			{
				var project = doc.Nodes.OfType<XElement> ().FirstOrDefault (x => x.Name == xnProject);
				if (project == null)
					return;
				var pel = BuiltinElements ["Project"];
				foreach (var el in project.Nodes.OfType<XElement> ())
					Populate (el, pel);
			}

			void Populate (XElement el, MSBuildElement parent)
			{
				if (el.Name.Prefix != null)
					return;
				var name = el.Name.Name;
				var bi = ResolveBuiltinElement (name, parent);
				if (bi == null || !bi.IsSpecial) {
					foreach (var child in el.Nodes.OfType<XElement> ())
						Populate (child, bi);
					return;
				}
				switch (parent.SpecialChildName) {
				case "Item":
					HashSet<string> item;
					if (!Items.TryGetValue (name, out item))
						Items [name] = item = new HashSet<string> ();
					foreach (var metadata in el.Nodes.OfType<XElement> ())
						if (!metadata.Name.HasPrefix)
							item.Add (metadata.Name.Name);
					break;
				case "Property":
					Properties.Add (name);
					break;
				case "Import":
					var import = el.Attributes [xnProject];
					if (import != null && string.IsNullOrEmpty (import.Value))
						Imports.Add (import.Value);
					break;
				case "Task":
					HashSet<string> task;
					if (!Tasks.TryGetValue (name, out task))
						Tasks [name] = task = new HashSet<string> ();
					foreach (var att in el.Attributes)
						if (!att.Name.HasPrefix)
							task.Add (att.Name.Name);
					break;
				case "Parameter":
					//TODO: Parameter
					break;
				}
			}
		}

		static readonly XName xnProject = new XName ("Project");

		class MSBuildElement
		{
			public string[] Children;
			public string [] Attributes;
			public string SpecialChildName;
			public bool IsSpecial;

			public bool HasChild (string name)
			{
				return Children != null && Children.Contains (name, StringComparer.Ordinal);
			}
		}

		static MSBuildElement ResolveBuiltinElement (string name, MSBuildElement parent)
		{
			//if not in parent's known children, and parent has special children, then it's a special child
			if (parent != null && parent.SpecialChildName != null && !parent.HasChild (name))
				name = parent.SpecialChildName;
			MSBuildElement result;
			BuiltinElements.TryGetValue (name, out result);
			return result;
		}

		static readonly Dictionary<string,MSBuildElement> BuiltinElements = new Dictionary<string, MSBuildElement> {
			{
				"Choose", new MSBuildElement {
					Children = new[] { "Otherwise", "When" },
				}
			},
			{
				"Import", new MSBuildElement {
					Attributes = new[] { "Condition", "Project" },
				}
			},
			{
				"ImportGroup", new MSBuildElement {
					Children = new[] { "Import" },
					Attributes = new[] { "Condition" },
				}
			},
			{
				"Item", new MSBuildElement {
					IsSpecial = true,
					SpecialChildName = "ItemMetadata",
					Attributes = new[] { "Condition", "Exclude", "Include", "Remove" },
				}
			},
			{
				"ItemDefinitionGroup", new MSBuildElement {
					SpecialChildName = "Item",
					Attributes = new[] { "Condition" },
				}
			},
			{
				"ItemGroup", new MSBuildElement {
					SpecialChildName = "Item",
					Attributes = new[] { "Condition" },
				}
			},
			{
				"ItemMetadata", new MSBuildElement {
					IsSpecial = true,
					Attributes = new[] { "Condition" },
				}
			},
			{
				"OnError", new MSBuildElement {
					Attributes = new[] { "Condition", "ExecuteTargets" },
				}
			},
			{
				"Otherwise", new MSBuildElement {
					Children = new[] { "Choose", "ItemGroup", "PropertyGroup" },
				}
			},
			{
				"Output", new MSBuildElement {
					Attributes = new[] { "Condition", "ItemName", "PropertyName", "TaskParameter" },
				}
			},
			{
				"Parameter", new MSBuildElement {
					IsSpecial = true,
					Attributes = new[] { "Output", "ParameterType", "Required" },
				}
			},
			{
				"ParameterGroup", new MSBuildElement {
					SpecialChildName = "Parameter",
				}
			},
			{
				"Project", new MSBuildElement {
					Children = new[] {
						"Choose", "Import", "ItemGroup", "ProjectExtensions", "PropertyGroup", "Target", "UsingTask"
					},
					Attributes = new[] {
						"DefaultTargets", "InitialTargets", "ToolsVersion", "TreatAsLocalProperty", "xmlns"
					},
				}
			},
			{
				"ProjectExtensions", new MSBuildElement {
					SpecialChildName = "Data",
				}
			},
			{
				"Property", new MSBuildElement {
					IsSpecial = true,
					Attributes = new[] { "Condition" },
				}
			},
			{
				"PropertyGroup", new MSBuildElement {
					SpecialChildName = "Property",
					Attributes = new[] { "Condition" },
				}
			},
			{
				"Target", new MSBuildElement {
					SpecialChildName = "Task",
					Children = new[] { "OnError", "ItemGroup", "PropertyGroup" },
					Attributes = new[] {
						"AfterTargets", "BeforeTargets", "Condition", "DependsOnTargets", "Inputs",
						"KeepDuplicateOutputs", "Name", "Outputs", "Returns"
					},
				}
			},
			{
				"Task", new MSBuildElement {
					IsSpecial = true,
					Children = new[] { "Output" },
					Attributes = new[] { "Condition", "ContinueOnError", "Parameter" },
				}
			},
			{
				"TaskBody", new MSBuildElement {
					SpecialChildName = "Data",
					Attributes = new[] { "Evaluate" },
				}
			},
			{
				"UsingTask", new MSBuildElement {
					Children = new[] { "ParameterGroup", "TaskBody" },
					Attributes = new[] { "AssemblyFile", "AssemblyName", "Condition", "TaskFactory", "TaskName" },
				}
			},
			{
				"When", new MSBuildElement {
					Children = new[] { "Choose", "ItemGroup", "PropertyGroup" },
					Attributes = new[] { "Condition" },
				}
			},
		};
	}
}