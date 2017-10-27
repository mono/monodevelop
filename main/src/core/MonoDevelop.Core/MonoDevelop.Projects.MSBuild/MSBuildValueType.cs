//
// MSBuildValueType.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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

namespace MonoDevelop.Projects.MSBuild
{
	public class MSBuildValueType
	{
		public static readonly MSBuildValueType Default = new MSBuildValueType ();
		public static readonly MSBuildValueType DefaultPreserveCase = new PreserveCaseValueType ();
		public static readonly MSBuildValueType Path = new PathValueType ();
		public static readonly MSBuildValueType Boolean = new PreserveCaseValueType ();
		public static readonly MSBuildValueType Guid = new GuidValueType ();
		internal static readonly MSBuildValueType UnresolvedPath = new PathValueType ();

		public virtual bool Equals (string ob1, string ob2)
		{
			return object.Equals (ob1, ob2);
		}
	}

	class PathValueType: MSBuildValueType
	{
		static readonly char [] pathSep = { '\\' };

		public override bool Equals (string ob1, string ob2)
		{
			if (base.Equals (ob1, ob2))
				return true;
			if (ob1 == null || ob2 == null)
				return string.IsNullOrEmpty (ob1) && string.IsNullOrEmpty (ob2);//Empty or null path is same thing
			return ob1.TrimEnd (pathSep) == ob2.TrimEnd (pathSep);
		}
	}

	class PreserveCaseValueType: MSBuildValueType
	{
		public override bool Equals (string ob1, string ob2)
		{
			return ob1.Equals (ob2, StringComparison.OrdinalIgnoreCase);
		}
	}

	class GuidValueType: MSBuildValueType
	{
		static readonly char [] guidEnclosing = { '{', '}' };

		public override bool Equals (string ob1, string ob2)
		{
			ob1 = ob1.Trim (guidEnclosing);
			ob2 = ob2.Trim (guidEnclosing);
			return ob1.Equals (ob2, StringComparison.OrdinalIgnoreCase);
		}
	}
}

