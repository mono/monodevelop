//
// DiagnosticDescriptorPropertyProvider.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
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
using System;
using MonoDevelop.DesignerSupport;
using Microsoft.CodeAnalysis;
using MonoDevelop.Core;

namespace MonoDevelop.Refactoring.NodeBuilders
{
	class AnalyzerPropertyProvider : IPropertyProvider
	{
		public object CreateProvider (object obj)
		{
			return new DiagnosticDescriptorProvider ((DiagnosticDescriptor)obj);
		}

		public bool SupportsObject (object obj)
		{
			return obj is DiagnosticDescriptor;
		}

		class DiagnosticDescriptorProvider : CustomDescriptor
		{
			DiagnosticDescriptor obj;

			public DiagnosticDescriptorProvider (DiagnosticDescriptor obj)
			{
				this.obj = obj;
			}

			[LocalizedCategory ("Misc")]
			[LocalizedDisplayName ("Id")]
			public string Id {
				get { return obj.Id; }
			}

			[LocalizedCategory ("Misc")]
			[LocalizedDisplayName ("Title")]
			public string Title {
				get { return obj.Title.ToString (); }
			}

		}
	}
}

