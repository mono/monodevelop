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
using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Xml.Completion;
using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Editor;

namespace MonoDevelop.Xml.MSBuild
{
	class MSBuildTextEditorExtension : BaseXmlEditorExtension
	{
		public static readonly string MSBuildMimeType = "application/x-msbuild";

		protected override void GetElementCompletions (CompletionDataList list)
		{
			AddMiscBeginTags (list);

			var path = GetCurrentPath ();

			if (path.Count == 0) {
				list.Add (new XmlCompletionData ("Project", XmlCompletionData.DataType.XmlElement));
				return;
			}

			var rr = ResolveElement (path);
			if (rr == null)
				return;

			foreach (var c in rr.BuiltinChildren)
				list.Add (new XmlCompletionData (c, XmlCompletionData.DataType.XmlElement));

			var inferredChildren = GetInferredChildren (rr);
			if (inferredChildren != null)
				foreach (var c in inferredChildren)
					list.Add (new XmlCompletionData (c, XmlCompletionData.DataType.XmlElement));
		}

		IEnumerable<string> GetInferredChildren (ResolveResult rr)
		{
			if (inferredCompletionData == null)
				return null;

			if (rr.ElementType == "Item") {
				return inferredCompletionData.GetItemMetadata (rr.ElementName);
			}

			if (rr.ChildType != null) {
				switch (rr.ChildType) {
				case "Item":
					return inferredCompletionData.GetItems ();
				case "Task":
					return inferredCompletionData.GetTasks ();
				case "Property":
					return inferredCompletionData.GetProperties ();
				}
			}

			return null;
		}

		protected override CompletionDataList GetAttributeCompletions (IAttributedXObject attributedOb,
			Dictionary<string, string> existingAtts)
		{
			var path = GetCurrentPath ();

			var rr = ResolveElement (path);
			if (rr == null)
				return null;

			var list = new CompletionDataList ();
			foreach (var a in rr.BuiltinAttributes)
				if (!existingAtts.ContainsKey (a))
					list.Add (new XmlCompletionData (a, XmlCompletionData.DataType.XmlAttribute));

			var inferredAttributes = GetInferredAttributes (rr);
			if (inferredAttributes != null)
				foreach (var a in inferredAttributes)
					if (!existingAtts.ContainsKey (a))
						list.Add (new XmlCompletionData (a, XmlCompletionData.DataType.XmlAttribute));

			return list;
		}

		IEnumerable<string> GetInferredAttributes (ResolveResult rr)
		{
			if (inferredCompletionData == null || rr.ElementType != "Task")
				return null;

			return inferredCompletionData.GetTaskParameters (rr.ElementName);
		}

		static ResolveResult ResolveElement (IList<XObject> path)
		{
			//need to look up element by walking how the path, since at each level, if the parent has special children,
			//then that gives us information to identify the type of its children
			MSBuildElement el = null;
			string elName = null, elType = null;
			for (int i = 0; i < path.Count; i++) {
				//if children of parent is known to be arbitrary data, give up on completion
				if (el != null && el.ChildType == "Data")
					return null;
				//code completion is forgiving, all we care about best guess resolve for deepest child
				var xel = path [i] as XElement;
				if (xel != null && xel.Name.Prefix == null) {
					if (el != null)
						elType = el.ChildType;
					elName = xel.Name.Name;
					el = MSBuildElement.Get (elName, el);
					if (el != null)
						continue;
				}
				el = null;
				elName = elType = null;
			}
			if (el == null)
				return null;

			return new ResolveResult {
				ElementName = elName,
				ElementType = elType,
				ChildType = el.ChildType,
				BuiltinAttributes = el.Attributes,
				BuiltinChildren = el.Children,
			};
		}

		class ResolveResult
		{
			public string ElementName;
			public string ElementType;
			public string ChildType;
			public IEnumerable<string> BuiltinAttributes;
			public IEnumerable<string> BuiltinChildren;
		}

		bool inferenceQueued = false;
		MSBuildResolveContext inferredCompletionData;

		void QueueInference ()
		{
			var doc = this.CU as XmlParsedDocument;
			if (doc == null || doc.XDocument == null || inferenceQueued)
				return;
			if (inferredCompletionData != null) {
				if ((doc.LastWriteTimeUtc - inferredCompletionData.TimeStampUtc).TotalSeconds < 5)
					return;
			}
			inferenceQueued = true;
			ThreadPool.QueueUserWorkItem (delegate {
				try {
					inferredCompletionData = MSBuildResolveContext.Create (doc, inferredCompletionData);
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
	}
}