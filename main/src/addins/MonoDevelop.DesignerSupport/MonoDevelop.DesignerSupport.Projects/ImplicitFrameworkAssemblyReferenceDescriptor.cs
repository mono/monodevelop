//
// ImplicitFrameworkAssemblyReferenceDescriptor.cs
//
// Author:
//       Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (c) 2013 Xamarin Inc.
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
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Core;

namespace MonoDevelop.DesignerSupport.Projects
{
	class ImplicitFrameworkAssemblyReferenceDescriptor : CustomDescriptor
	{
		readonly ImplicitFrameworkAssemblyReference aref;

		public ImplicitFrameworkAssemblyReferenceDescriptor (ImplicitFrameworkAssemblyReference aref)
		{
			this.aref = aref;
		}

		[LocalizedCategory ("Reference")]
		[LocalizedDisplayName ("Assembly Name")]
		[LocalizedDescription ("Name of the assembly.")]
		public string FullName {
			get {
				return aref.Assembly.FullName;
			}
		}

		[LocalizedCategory ("Reference")]
		[LocalizedDisplayName ("Path")]
		[LocalizedDescription ("Path to the assembly.")]
		public string Path {
			get {
				return aref.Assembly.Location;
			}
		}

		[LocalizedCategory ("Reference")]
		[LocalizedDisplayName ("Framework")]
		[LocalizedDescription ("Framework that provides this reference.")]
		public string Package {
			get {
				return aref.Assembly.Package.TargetFramework.ToString ();
			}
		}
	}
}
