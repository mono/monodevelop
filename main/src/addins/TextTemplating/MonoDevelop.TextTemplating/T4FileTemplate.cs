//
// T4FileTemplate.cs
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
using System.IO;
using System.Xml;
using MonoDevelop.Core;
using MonoDevelop.Ide.Templates;

namespace MonoDevelop.TextTemplating
{
	public class T4FileTemplate : SingleFileDescriptionTemplate
	{
		FilePath srcFile;

		public override void Load (XmlElement filenode, FilePath baseDirectory)
		{
			base.Load (filenode, baseDirectory);
			var srcAtt = filenode.Attributes["src"];
			if (srcAtt == null) {
				throw new Exception ("T4FileTemplate is missing src attribute");
			}
			srcFile = FileService.MakePathSeparatorsNative (srcAtt.Value);
			if (srcFile.IsNullOrEmpty)
				throw new InvalidOperationException ("Template's Src attribute is empty");
			srcFile = srcFile.ToAbsolute (baseDirectory);
		}

		public override string CreateContent (string language)
		{
			return File.ReadAllText (srcFile);
		}

		protected override string ProcessContent (string content)
		{
			using (var host = new FileTemplateHost (Tags)) {
				string s = srcFile;
				string output;
				host.ProcessTemplate (s, content, ref s, out output);
				if (host.Errors.HasErrors) {
					foreach (var err in host.Errors)
						LoggingService.LogError ("Error in template generator: {0}", err.ToString());
					throw new Exception ("Failed to generate file");
				}
				return output;
			}
		}
	}

	public class FileTemplateHost : MonoDevelopTemplatingHost
	{
		readonly Dictionary<string,string> tags;

		public FileTemplateHost ()
		{
			AddMonoDevelopHostImport ();
			Imports.Add (typeof (FileTemplateHost).Namespace);
			Refs.Add (typeof (FileTemplateHost).Assembly.Location);
		}

		public FileTemplateHost (Dictionary<string,string> tags)
		{
			this.tags = tags;
		}

		//TODO could we expose the tags better e.g. via a directive processor?
		public string this[string key] {
			get {
				string val;
				if (tags.TryGetValue (key, out val))
					return val;
				return null;
			}
		}

		protected override string SubstitutePlaceholders (string s)
		{
			return StringParserService.Parse (s, tags);
		}
	}
}

