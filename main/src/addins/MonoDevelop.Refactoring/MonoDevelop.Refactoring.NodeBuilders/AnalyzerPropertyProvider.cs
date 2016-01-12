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
using MonoDevelop.CodeIssues;

namespace MonoDevelop.Refactoring.NodeBuilders
{
	class AnalyzerPropertyProvider : IPropertyProvider
	{
		public object CreateProvider (object obj)
		{
			if (obj is AnalyzersFromAssembly)
				return new AnalyzersFromAssemblyProvider ((AnalyzersFromAssembly)obj);
			return new DiagnosticDescriptorProvider ((DiagnosticDescriptor)obj);
		}

		public bool SupportsObject (object obj)
		{
			return obj is DiagnosticDescriptor || obj is AnalyzersFromAssembly;
		}

		class AnalyzersFromAssemblyProvider
		{
			AnalyzersFromAssembly obj;

			[LocalizedCategory ("Misc")]
			[LocalizedDisplayName ("Name")]
			public string Name {
				get { return obj.Assemblies[0].GetName ().Name; }
			}

			[LocalizedCategory ("Misc")]
			[LocalizedDisplayName ("Path")]
			public string Path {
				get { return obj.Assemblies[0].Location; }
			}

			public AnalyzersFromAssemblyProvider (AnalyzersFromAssembly obj)
			{
				this.obj = obj;
			}
		}

		class DiagnosticDescriptorProvider : CustomDescriptor
		{
			DiagnosticDescriptor obj;

			public DiagnosticDescriptorProvider (DiagnosticDescriptor obj)
			{
				this.obj = obj;
			}

			[LocalizedCategory ("Misc")]
			[LocalizedDisplayName ("Category")]
			public string Category {
				get { return obj.Category; }
			}

			[LocalizedCategory ("Misc")]
			[LocalizedDisplayName ("Default Severity")]
			public DiagnosticSeverity DefaultSeverity {
				get { return obj.DefaultSeverity; }
			}

			[LocalizedCategory ("Misc")]
			[LocalizedDisplayName ("Description")]
			public string Description {
				get { return obj.Description.ToString (); }
			}

			[LocalizedCategory ("Misc")]
			[LocalizedDisplayName ("Enabled by default")]
			public bool IsEnabledByDefault {
				get { return obj.IsEnabledByDefault; }
			}

			[LocalizedCategory ("Misc")]
			[LocalizedDisplayName ("Help link")]
			public string HelpLinkUri {
				get { return obj.HelpLinkUri; }
			}


			[LocalizedCategory ("Misc")]
			[LocalizedDisplayName ("ID")]
			public string Id {
				get { return obj.Id; }
			}

			[LocalizedCategory ("Misc")]
			[LocalizedDisplayName ("Message")]
			public string MessageFormat {
				get { return obj.MessageFormat.ToString (); }
			}

			[LocalizedCategory ("Misc")]
			[LocalizedDisplayName ("Tags")]
			public string Tags {
				get { return string.Join (",", obj.CustomTags); }
			}

			[LocalizedCategory ("Misc")]
			[LocalizedDisplayName ("Title")]
			public string Title {
				get { return obj.Title.ToString (); }
			}
		}
	}
}

