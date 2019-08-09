//
// TargetFrameworkNode.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using MonoDevelop.Projects;

namespace MonoDevelop.DotNetCore.NodeBuilders
{
	class FrameworkReferenceNode
	{
		FrameworkReference reference;
		string referenceName;

		public FrameworkReferenceNode (FrameworkReference reference)
		{
			this.reference = reference;
			referenceName = reference.Include;
		}

		public FrameworkReferenceNode (string referenceName)
		{
			this.referenceName = referenceName;
		}

		public string Name {
			get { return referenceName; }
		}

		public string GetLabel ()
		{
			return GLib.Markup.EscapeText (Name);
		}

		public string GetSecondaryLabel ()
		{
			if (reference == null)
				return string.Empty;

			string targetingPackVersion = reference.TargetingPackVersion;
			if (!string.IsNullOrEmpty (targetingPackVersion))
				return string.Format ("({0})", targetingPackVersion);

			return string.Empty;
		}

		public IconId GetIconId ()
		{
			return new IconId ("md-reference-package");
		}
	}
}
