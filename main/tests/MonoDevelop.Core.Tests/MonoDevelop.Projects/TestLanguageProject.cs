//
// TestLanguageProject.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2017 Microsoft
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
using MonoDevelop.Projects.Extensions;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core;

namespace MonoDevelop.Projects
{
	class TestLanguageTypeNode : ProjectTypeNode
	{
		public TestLanguageTypeNode()
		{
			Guid = "{3DBD0453-40CF-462C-A289-1994F91A32A4}";
			Extension = "tlproj";
		}

		public override Type ItemType
		{
			get
			{
				return typeof(MyProject);
			}
		}
	}

	public class TestLanguageProject: Project
	{
		protected override ProjectRunConfiguration OnCreateRunConfiguration(string name)
		{
			return base.OnCreateRunConfiguration(name);
		}
	}

	public class TestLanguageProjectConfiguration: ProjectConfiguration
	{
		public TestLanguageProjectConfiguration(string id) : base(id)
		{
		}

		[ItemProperty]
		public int WarningLevel { get; set; }

		[ItemProperty(DefaultValue = "Default")]
		public string LangVersion { get; set; } = "Default";

		[ItemProperty("AllowUnsafeBlocks", DefaultValue = false)]
		public bool UnsafeCode { get; set; }
	}
}
