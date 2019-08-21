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
				var file = (context?.Instance as ProjectFileDescriptor)?.file;

				// determine which registered tools are valid for this file
				var registeredTools = AddinManager.GetExtensionNodes<CustomToolExtensionNode> (
					"/MonoDevelop/Ide/CustomTools"
				);

				// this shouldn't ever happen, but handle it just in case
				if (file == null) {
					var arr = registeredTools.Select (n => n.Name).Distinct ().ToArray ();
					Array.Sort (arr, StringComparer.Ordinal);
					return arr;
				}

				var extension = file.FilePath.Extension;
				var dedup = new HashSet<string> (StringComparer.OrdinalIgnoreCase);
				var values = new List<string> ();
				var toolsMap = registeredTools.ToDictionary (n => n.Name, StringComparer.OrdinalIgnoreCase);

				//add sections with values that other files in the project have
				//first, a section with values that files with this file's extension have
				//then,  a section with  values that other files have
				if (file?.Project != null) {
					var otherExtTools = new HashSet<string> ();

					// first, add all the tools that other files with this extension have
					// as these are the most likely to be the the ones that the user wants
					foreach (var f in file.Project.Files) {
						if (!string.IsNullOrEmpty (f.Generator)) {
							if (f.FilePath.HasExtension (extension)) {
								if (dedup.Add (f.Generator)) {
									values.Add (f.Generator);
									toolsMap.Remove (f.Generator);
								}
							} else {
								otherExtTools.Add (f.Generator);
							}
						}
					}

					// next, add all the tools that other files with this extension have, as there may be custom
					// tools used in the project that we're not aware of.
					if (otherExtTools.Count > 0) {
						foreach (var toolName in otherExtTools) {
							// however, skip them if they match one of the registered tools and are not marked
							// as compatible with this file's extension.
							if (toolsMap.TryGetValue (toolName, out var t) && !IsCompatible (t)) {
								continue;
							}
							if (dedup.Add (toolName)) {
								values.Add (toolName);
								toolsMap.Remove (toolName);
							}
						}
					}
					values.Sort (StringComparer.OrdinalIgnoreCase);
				}

				//add a section with any remaining registered tools that can handle the file
				if (toolsMap.Count > 0) {
					var tools = registeredTools
						.Where (t => dedup.Add (t.Name) && IsCompatible (t))
						.Select (t => t.Name)
						.ToList ();

					if (tools.Count > 0) {
						tools.Sort (StringComparer.OrdinalIgnoreCase);
						values.Add ("--");
						values.AddRange (tools);
					}
				}

				bool IsCompatible (CustomToolExtensionNode node)
				{
					if (node.Extensions == null || node.Extensions.Length == 0) {
						return true;
					}
					foreach (var ext in node.Extensions) {
						if (string.Equals (extension, ext, StringComparison.OrdinalIgnoreCase)) {
							return true;
						}
					}
					return false;
				}

				return values;
			}
		}
	}
}
