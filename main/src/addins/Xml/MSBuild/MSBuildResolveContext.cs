//
// MSBuildResolveContext.cs
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
using MonoDevelop.Xml.Editor;
using MonoDevelop.Xml.Dom;

namespace MonoDevelop.Xml.MSBuild
{
	class MSBuildResolveContext
	{
		static readonly XName xnProject = new XName ("Project");

		readonly Dictionary<string,HashSet<string>> items = new Dictionary<string, HashSet<string>> ();
		readonly Dictionary<string,HashSet<string>> tasks = new Dictionary<string, HashSet<string>> ();
		readonly HashSet<string> properties = new HashSet<string> ();
		readonly HashSet<string> imports = new HashSet<string> ();
		public readonly DateTime TimeStampUtc = DateTime.UtcNow;

		public IEnumerable<string> GetItems ()
		{
			foreach (var task in items.Keys)
				yield return task;
		}

		public IEnumerable<string> GetItemMetadata (string itemName)
		{
			HashSet<string> metadata;
			items.TryGetValue (itemName, out metadata);
			if (metadata != null)
				foreach (var m in metadata)
					yield return m;
		}

		public IEnumerable<string> GetTasks ()
		{
			foreach (var task in tasks.Keys)
				yield return task;
		}

		public IEnumerable<string> GetTaskParameters (string taskName)
		{
			HashSet<string> taskParameters;
			items.TryGetValue (taskName, out taskParameters);
			if (taskParameters != null)
				foreach (var parameter in taskParameters)
					yield return parameter;
		}

		public IEnumerable<string> GetProperties ()
		{
			foreach (var property in properties)
				yield return property;
		}

		public static MSBuildResolveContext Create (XmlParsedDocument doc, MSBuildResolveContext previous)
		{
			var ctx = new MSBuildResolveContext ();
			ctx.Populate (doc.XDocument);
			if (doc.GetErrorsAsync().Result.Count > 0)
				ctx.Merge (previous);
			return ctx;
		}

		void Merge (MSBuildResolveContext other)
		{
			foreach (var otherItem in other.items) {
				HashSet<string> item;
				if (items.TryGetValue (otherItem.Key, out item)) {
					foreach (var att in otherItem.Value)
						item.Add (att);
				} else {
					items [otherItem.Key] = otherItem.Value;
				}
			}
			foreach (var otherTask in other.tasks) {
				HashSet<string> task;
				if (tasks.TryGetValue (otherTask.Key, out task)) {
					foreach (var att in otherTask.Value)
						task.Add (att);
				} else {
					tasks [otherTask.Key] = otherTask.Value;
				}
			}
			foreach (var prop in other.properties) {
				properties.Add (prop);
			}
			foreach (var imp in other.imports) {
				imports.Add (imp);
			}
		}

		void Populate (XDocument doc)
		{
			var project = doc.Nodes.OfType<XElement> ().FirstOrDefault (x => x.Name == xnProject);
			if (project == null)
				return;
			var pel = MSBuildElement.Get ("Project");
			foreach (var el in project.Nodes.OfType<XElement> ())
				Populate (el, pel);
		}

		void Populate (XElement el, MSBuildElement parent)
		{
			if (el.Name.Prefix != null)
				return;

			var name = el.Name.Name;
			var msel = MSBuildElement.Get (name, parent);
			if (msel == null || !msel.IsSpecial) {
				foreach (var child in el.Nodes.OfType<XElement> ())
					Populate (child, msel);
				return;
			}

			switch (parent.ChildType) {
			case "Item":
				HashSet<string> item;
				if (!items.TryGetValue (name, out item))
					items [name] = item = new HashSet<string> ();
				foreach (var metadata in el.Nodes.OfType<XElement> ())
					if (!metadata.Name.HasPrefix)
						item.Add (metadata.Name.Name);
				break;
			case "Property":
				properties.Add (name);
				break;
			case "Import":
				var import = el.Attributes [xnProject];
				if (import != null && string.IsNullOrEmpty (import.Value))
					imports.Add (import.Value);
				break;
			case "Task":
				HashSet<string> task;
				if (!tasks.TryGetValue (name, out task))
					tasks [name] = task = new HashSet<string> ();
				foreach (var att in el.Attributes)
					if (!att.Name.HasPrefix)
						task.Add (att.Name.Name);
				break;
			case "Parameter":
				//TODO: Parameters
				break;
			}
		}
	}
}