//
// VBNetResourceIdBuilder.cs
//
// Author:
//   Ankit Jain <jankit@novell.com>
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using MonoDevelop.Projects;
using System;
using System.IO;
using System.Text;
using MonoDevelop.Projects.Extensions;
using MonoDevelop.Projects.Formats.MSBuild;

namespace VBBinding
{
	class VBNetResourceIdBuilder : IResourceHandler
	{
		public string GetDefaultResourceId (ProjectFile pf)
		{
			if (String.IsNullOrEmpty (pf.DependsOn))
				return MSBuildProjectService.GetDefaultResourceId (pf);

			string ns = null;
			string classname = null;

			using (StreamReader rdr = new StreamReader (pf.DependsOn)) {
				while (true) {
					string tok = GetNextToken (rdr);
					if (tok == null)
						break;

					if (String.Compare (tok, "namespace", true) == 0) {
						string t = GetNextToken (rdr);
						/* 'namespace' can be a attribute param also, */
						if (t == ":" && GetNextToken (rdr) == "=")
							continue;
						ns = t;
					}

					if (String.Compare (tok, "class", true) == 0) {
						string t = GetNextToken (rdr);
						/* 'class' can be a attribute param also, */
						if (t == ":" && GetNextToken (rdr) == "=")
							continue;
						classname = t;
						break;
					}
				}

				if (classname == null)
					return MSBuildProjectService.GetDefaultResourceId (pf);

				string culture, extn, only_filename;
				if (MSBuildProjectService.TrySplitResourceName (pf.RelativePath, out only_filename, out culture, out extn))
					extn = "." + culture + ".resources";
				else
					extn = ".resources";

				string rname;
				if (ns == null)
					rname = classname + extn;
				else
					rname = ns + '.' + classname + extn;
				
				DotNetProject dp = pf.Project as DotNetProject;
				if (dp == null || String.IsNullOrEmpty (dp.DefaultNamespace))
					return rname; 
				else
					return dp.DefaultNamespace + "." + rname;
			}
		}



		/* Special parser for VB.NET files
		 * Assumes that the file is compilable
		 * skips comments, 
		 * skips strings "foo"
		 */
		string GetNextToken (StreamReader sr)
		{
			StringBuilder sb = new StringBuilder ();

			while (true) {
				int c = sr.Peek ();
				if (c == -1)
					return null;

				if (c == '\r' || c == '\n') {
					sr.ReadLine ();
					if (sb.Length > 0)
						break;

					continue;
				}

				if (c == '\'') {
					/* comment */
					sr.ReadLine ();
					if (sb.Length > 0)
						return sb.ToString ();

					continue;
				}

				if (c == '"') {
					/* String */
					sr.Read ();
					while (true) {
						int n = sr.Peek ();
						if (n == '\r' || n == '\n' || n == -1)
							throw new Exception ("String literal not closed");

						if (n == '"') {
							if (sb.Length > 0) {
								sr.Read ();
								return sb.ToString ();
							}

							break;
						}
						sr.Read ();
					}
				} else {
					if (Char.IsLetterOrDigit ((char) c) || c == '_' || c == '.') {
						sb.Append ((char) c);
					} else {
						if (sb.Length > 0)
							break;

						if (c != ' ' && c != '\t') {
							sr.Read ();
							return ((char) c).ToString ();
						}
					}
				}

				sr.Read ();
			}

			return sb.ToString ();
		}

	}
}
