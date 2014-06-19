//
// AddinInfo.cs
//
// Author:
//       Sergey Khabibullin <sergey@khabibullin.com>
//
// Copyright (c) 2014 Sergey Khabibullin
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
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("NUnit", 
	Namespace = "MonoDevelop",
	Version = MonoDevelop.BuildInfo.Version,
	Category = "Testing")]

[assembly:AddinName ("NUnit support")]
[assembly:AddinDescription ("Integrates NUnit into the MonoDevelop IDE")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]

namespace MonoDevelop.NUnit
{
	public class TestProviderNode: TypeExtensionNode
	{
		public override string ToString ()
		{
			return string.Format ("[TestProviderNode: Id={0}]", Id);
		}
	}

	public class TestDiscovererNode: TypeExtensionNode
	{
		[NodeAttribute]
		string provider;

		public string ProviderId {
			get {
				return provider;
			}
		}

		public override string ToString ()
		{
			return string.Format ("[TestDiscovererNode: Id={0}, ProviderId={1}]", Id, ProviderId);
		}
	}

	public class TestExecutorNode: TypeExtensionNode
	{
		[NodeAttribute]
		string provider;

		public string ProviderId {
			get {
				return provider;
			}
		}

		[NodeAttribute]
		string discoverer;

		public string DiscovererId {
			get {
				return discoverer;
			}
		}

		public override string ToString ()
		{
			return string.Format ("[TestExecutorNode: Id={0}, ProviderId={1}, DiscovererId={2}]",
				Id, ProviderId, DiscovererId);
		}
	}
}