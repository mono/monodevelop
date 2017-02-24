//
// NuGetFileDescriptor.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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

using MonoDevelop.Core;
using MonoDevelop.DesignerSupport;
using MonoDevelop.Projects;

namespace MonoDevelop.Packaging
{
	class NuGetFileDescriptor : CustomDescriptor
	{
		ProjectFile projectFile;

		public NuGetFileDescriptor (ProjectFile file)
		{
			projectFile = file;
		}

		[LocalizedCategory ("NuGet")]
		[LocalizedDescription ("Specifies whether the file will be included in the package. Supported for None items only.")]
		[LocalizedDisplayName ("Include in Package")]
		public bool IncludeInPackage {
			get {
				return projectFile.Metadata.GetValue<bool> (nameof (IncludeInPackage), false);
			}
			set {
				if (value) {
					projectFile.Metadata.SetValue (nameof (IncludeInPackage), value, false);
				} else {
					// HACK: Removing the property does not seem to work. Nor does setting the
					// value back to its default value of false work which also removes the
					// property. The saved project file still has the property. For now
					// working around this by setting it to false but changing the default value to
					// true so the property is not removed.
					projectFile.Metadata.SetValue (nameof (IncludeInPackage), value, true);
				}
			}
		}
	}
}

