//
// GitReference.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corporation. All rights reserved.
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

namespace MonoDevelop.VersionControl.Git.ClientLibrary
{
	/// <summary>
	/// Base class for named git branches and tags.
	/// </summary>
	public abstract class GitReference : IComparable<GitReference>, IEquatable<GitReference>
	{
		public string Name { get; private set; }

		public abstract string FullName { get; }

		public GitReference (string name)
		{
			Name = name ?? throw new ArgumentNullException (nameof (name));
		}

		public int CompareTo (GitReference other)
			=> other == null ? 1 : string.Compare(FullName, other.FullName, StringComparison.Ordinal);

		public bool Equals (GitReference other)
		{
			if (other == this)
				return true;
			if (other == null || other.GetType () != GetType ())
				return false;
			return Name.Equals (other.Name);
		}
	}

}
