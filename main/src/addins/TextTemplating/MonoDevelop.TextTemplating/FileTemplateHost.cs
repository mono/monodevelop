//
// FileTemplateHost.cs
//
// Author:
//       Michael Hutchinson <mhutch@xamarin.com>
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
using Microsoft.VisualStudio.TextTemplating;
using MonoDevelop.Core;
using MonoDevelop.Core.StringParsing;

namespace MonoDevelop.TextTemplating
{
	//this is the public interface we expose for templates
	public interface IFileTemplateHost
	{
		bool IsTrue (string key);
		IFileTagProvider Tags { get; }
	}

	public interface IFileTagProvider
	{
		string this [string key] { get; }
	}

	class FileTemplateHost : MonoDevelopTemplatingHost, IFileTemplateHost, IFileTagProvider
	{
		readonly IStringTagModel tags;

		public FileTemplateHost (IStringTagModel tags)
		{
			this.tags = tags;

			Refs.Add (typeof (FileTemplateHost).Assembly.Location);
		}

		protected override string SubstitutePlaceholders (string s)
		{
			return StringParserService.Parse (s, tags);
		}

		public override IEnumerable<IDirectiveProcessor> GetAdditionalDirectiveProcessors ()
		{
			yield return new FileTemplateDirectiveProcessor ();
		}

		public override Type SpecificHostType {
			get { return typeof (IFileTemplateHost); }
		}

		// implementation of public interfaces

		public IFileTagProvider Tags {
			get { return this; }
		}

		public string this[string key] {
			get {
				var val = tags.GetValue (key);
				return val != null ? val.ToString () : null;
			}
		}

		public bool IsTrue (string key)
		{
			if (string.Equals (this [key], "true", StringComparison.OrdinalIgnoreCase))
				return true;
			return false;
		}
	}
}
