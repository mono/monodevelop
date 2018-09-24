//
// WorkspaceId.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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

namespace MonoDevelop.Ide.TypeSystem
{
	readonly struct WorkspaceId : IEquatable<WorkspaceId>
	{
		static uint n = 0;

		public readonly uint     Number;
		public readonly DateTime DateTime;

		public static WorkspaceId Empty = new WorkspaceId (0, default(DateTime));

		WorkspaceId (uint number, DateTime dateTime) : this()
		{
			this.Number = number;
			this.DateTime = dateTime;
		}

		public static WorkspaceId Next()
		{
			return new WorkspaceId (n++, DateTime.UtcNow);
		}

		public override bool Equals (object obj)
		{
			if (!(obj is WorkspaceId))
				return false;
			return Equals ((WorkspaceId)obj);
		}

		public override int GetHashCode ()
		{
			unchecked {
				return Number.GetHashCode () ^ DateTime.GetHashCode ();
			}
		}

		public bool Equals (WorkspaceId other)
		{
			return Number == other.Number && DateTime == other.DateTime;
		}
	}
}