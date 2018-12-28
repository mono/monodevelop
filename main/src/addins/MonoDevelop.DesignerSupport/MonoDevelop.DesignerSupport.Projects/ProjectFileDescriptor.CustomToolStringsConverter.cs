//
//Copyright (c) Microsoft Corp (https://www.microsoft.com)
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
//
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Mono.Addins;
using MonoDevelop.Ide.Extensions;

namespace MonoDevelop.DesignerSupport
{
	partial class ProjectFileDescriptor
	{
		[Components.PropertyGrid.PropertyEditors.StandardValuesSeparator ("--")]
		class CustomToolStringsConverter : StandardStringsConverter
		{
			public override ICollection GetStandardStrings (ITypeDescriptorContext context)
			{
				var dedup = new HashSet<string> (StringComparer.OrdinalIgnoreCase);
				var values = new List<string> ();

				var file = (context?.Instance as ProjectFileDescriptor)?.file;

				//add sections with values that other files in the project have
				//first, a section with values that files with this file's extension have
				//then,  a section with  values that other files have
				if (file?.Project != null) {
					var extension = file.FilePath.Extension;
					var defer = new HashSet<string> ();
					foreach (var f in file.Project.Files) {
						if (!string.IsNullOrEmpty (f.Generator)) {
							if (f.FilePath.HasExtension (extension)) {
								if (dedup.Add (f.Generator)) {
									values.Add (f.Generator);
								}
							} else {
								defer.Add (f.Generator);
							}
						}
					}
					if (defer.Count > 0) {
						bool addSep = values.Count > 0;
						foreach (var v in defer) {
							if (dedup.Add (v)) {
								if (addSep) {
									values.Add ("--");
									addSep = false;
								}
								values.Add (v);
							}
						}
					}
				}

				//add a section with values that extensions can handle
				var nodes = AddinManager.GetExtensionNodes<CustomToolExtensionNode> ("/MonoDevelop/Ide/CustomTools");
				if (nodes.Count > 0) {
					bool addSep = values.Count > 0;
					foreach (var n in nodes) {
						if (dedup.Add (n.Name)) {
							if (addSep) {
								values.Add ("--");
								addSep = false;
							}
							values.Add (n.Name);
						}
					}

				}

				return values;
			}
		}
	}
}
