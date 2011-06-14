// 
// NativeReferencePropertyProvider.cs
//  
// Author:
//       Michael Hutchinson <m.j.hutchinson@gmail.com>
// 
// Copyright (c) 2011 Michael Hutchinson
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
using MonoDevelop.Core;
using MonoDevelop.DesignerSupport;
using System.Collections.Generic;

namespace MonoDevelop.MacDev.NativeReferences
{
	class NativeReferencePropertyProvider: IPropertyProvider
	{
		public object CreateProvider (object obj)
		{
			return new NativeReferenceDescriptor ((NativeReference)obj);
		}

		public bool SupportsObject (object obj)
		{
			return obj is NativeReference;
		}
	}
	
	class NativeReferenceDescriptor: CustomDescriptor
	{
		NativeReference nr;
		
		public NativeReferenceDescriptor (NativeReference nr)
		{
			this.nr = nr;
		}
		
		[LocalizedCategory ("Reference")]
		[LocalizedDisplayName ("Name")]
		[LocalizedDescription ("Name of the reference.")]
		public string Name {
			get { return nr.Path.FileNameWithoutExtension; }
		}
		
		[LocalizedCategory ("Reference")]
		[LocalizedDisplayName ("Path")]
		[LocalizedDescription ("Path of the reference.")]
		[MonoDevelop.Components.PropertyGrid.PropertyEditors.FilePathIsFolder]
		public FilePath LibPath {
			get { return nr.Path; }
			set { nr.Path = value; }
		}
		
		[LocalizedCategory ("Reference")]
		[LocalizedDisplayName ("Is C++")]
		[LocalizedDescription ("Whether the library is a C++ library.")]
		public bool IsCxx {
			get { return nr.IsCxx; }
			set { nr.IsCxx = value; }
		}
		
		[LocalizedCategory ("Reference")]
		[LocalizedDisplayName ("Kind")]
		[LocalizedDescription ("The kind of native reference.")]
		public NativeReferenceKind Kind {
			get { return nr.Kind; }
			set { nr.Kind = value; }
		}
	}
}