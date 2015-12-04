//
// AspNetStringTagProvider.cs
//
// Author:
//       Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (c) 2015 Xamarin Inc.
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
using System.Collections.Generic;
using MonoDevelop.Core.StringParsing;

namespace MonoDevelop.AspNet.Projects
{
	[Mono.Addins.Extension]
	class AspNetStringTagProvider : StringTagProvider<AspNetAppProject>
	{
		public override IEnumerable<StringTagDescription> GetTags ()
		{
			yield return new StringTagDescription ("UsesAspNetMvc", "Whether the project uses ASP.NET MVC");
			yield return new StringTagDescription ("UsesAspNetWebForms", "Whether the project uses ASP.NET Web Forms");
			yield return new StringTagDescription ("UsesAspNetWebApi", "Whether the project uses ASP.NET Web API");
		}

		public override object GetTagValue (AspNetAppProject instance, string tag)
		{
			switch (tag) {
			case "USESASPNETMVC":
				return instance.IsAspMvcProject;
			case "USESASPNETWEBFORMS":
				return instance.IsAspWebFormsProject;
			case "USESASPNETWEBAPI":
				return instance.IsAspWebApiProject;
			}
			throw new InvalidOperationException ();
		}
	}
}
