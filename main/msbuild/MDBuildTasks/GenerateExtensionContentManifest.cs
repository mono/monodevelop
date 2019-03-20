//
// Copyright (c) Microsoft Corp (https://www.microsoft.com)
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
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace MDBuildTasks
{
	public class GenerateExtensionContentManifest : Task
	{
		[Required]
		public string ManifestFile { get; set; }

		public ITaskItem[] ExtensionDependencies { get; set; }

		public ITaskItem[] ReferenceCopyLocalPaths { get; set; }

		[Required]
		public ITaskItem[] ExtensionContentWithLinkMetadata { get; set; }

		public override bool Execute ()
		{
			var runtime = new XElement ("Runtime");
			var dependencies = new XElement ("Dependencies");

			foreach (var a in ExtensionContentWithLinkMetadata) {
				runtime.Add (new XElement ("Import",
					new XAttribute ("file", a.GetMetadata ("Link").Replace ('\\', '/'))
				));
			}

			if (ReferenceCopyLocalPaths != null) {
				foreach (var r in ReferenceCopyLocalPaths) {
					var p = Path.GetFileName (r.ItemSpec);
					if (!p.EndsWith (".dll", StringComparison.OrdinalIgnoreCase)) {
						continue;
					}

					var subdir = r.GetMetadata ("DestinationSubDirectory");
					if (!string.IsNullOrEmpty (subdir)) {
						p = Path.Combine (subdir, p).Replace ('\\', '/');
					}

					runtime.Add (new XElement ("Import",
						new XAttribute ("assembly", p)
					));
				}
			}

			if (ExtensionDependencies != null) {
				foreach (var d in ExtensionDependencies) {
					dependencies.Add (new XElement ("Addin",
						new XAttribute ("id", "::" + d.ItemSpec),
							new XAttribute ("version", d.GetMetadata ("version"))
						)
					);
				}
			}

			var doc = new XDocument (new XElement ("ExtensionModel", runtime, dependencies));

			var settings = new XmlWriterSettings {
				Encoding = Encoding.UTF8,
				Indent = true
			};

			using (var xmlWriter = XmlWriter.Create (ManifestFile, settings)) {
				doc.Save (xmlWriter);
			}

			return true;
		}
	}
}