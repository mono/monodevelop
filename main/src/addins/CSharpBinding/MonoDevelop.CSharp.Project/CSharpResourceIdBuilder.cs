//
// CSharpResourceIdBuilder.cs
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

using MonoDevelop.Core;
using MonoDevelop.Projects;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using MonoDevelop.Projects.Extensions;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.CSharp.Project
{
	class CSharpResourceIdBuilder
	{
		public static string GetDefaultResourceId (ProjectFile pf)
		{
			if (String.IsNullOrEmpty (pf.DependsOn) || !File.Exists (pf.DependsOn) || !string.Equals (Path.GetExtension (pf.DependsOn), ".cs", StringComparison.OrdinalIgnoreCase))
				return null;

			string ns = null;
			string classname = null;

			using (StreamReader rdr = new StreamReader (pf.DependsOn)) {
				int numopen = 0;
				while (true) {
					string tok = GetNextToken (rdr);
					if (tok == null)
						break;

					if (tok == "@") {
						//Handle @namespace, @class
						GetNextToken (rdr);
						continue;
					}
				
					if (String.Compare (tok, "namespace", false) == 0)
						ns = GetNextToken (rdr);

					if (tok == "{")
						numopen ++;

					if (tok == "}") {
						numopen --;
						if (numopen == 0)
							ns = String.Empty;
					}

					if (tok == "class") {
						classname = GetNextToken (rdr);
						break;
					}
				}

				if (classname == null)
					return null;

				string culture, extn, only_filename;
				if (MSBuildProjectService.TrySplitResourceName (pf.ProjectVirtualPath, out only_filename, out culture, out extn))
					extn = "." + culture + ".resources";
				else
					extn = ".resources";

				if (ns == null)
					return classname + extn;
				else
					return ns + '.' + classname + extn;
			}
		}

		/* Special parser for C# files
		 * Assumes that the file is compilable
		 * skips comments, 
		 * skips strings "foo", 
		 * skips anything after a # , eg. #region, #if 
		 * Won't handle #if false etc kinda blocks*/
		static string GetNextToken (StreamReader sr)
		{
			StringBuilder sb = StringBuilderCache.Allocate ();
			try {
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

					if (c == '/') {
						sr.Read ();

						if (sr.Peek () == '*') {
							/* multi-line comment */
							sr.Read ();

							while (true) {
								int n = sr.Read ();
								if (n == -1)
									break;
								if (n != '*')
									continue;

								if (sr.Peek () == '/') {
									/* End of multi-line comment */
									if (sb.Length > 0) {
										sr.Read ();
										return sb.ToString ();
									}
									break;
								}
							}
						} else if (sr.Peek () == '/') {
							//Single line comment, skip the rest of the line
							sr.ReadLine ();
							continue;
						}
					} else if (c == '"') {
						/* String "foo" */
						sr.Read ();
						while (true) {
							int n = sr.Peek ();
							if (n == -1)
								throw new Exception ("String literal not closed");

							if (n == '"') {
								/* end of string */
								if (sb.Length > 0) {
									sr.Read ();
									return sb.ToString ();
								}

								break;
							}
							sr.Read ();
						}
					} else if (c == '#') {
						//skip rest of the line
						sr.ReadLine ();
					} else {
						if (Char.IsLetterOrDigit ((char)c) || c == '_' || c == '.') {
							sb.Append ((char)c);
						} else {
							if (sb.Length > 0)
								break;

							if (c != ' ' && c != '\t') {
								sr.Read ();
								return ((char)c).ToString ();
							}
						}
					}

					sr.Read ();
				}

				return sb.ToString ();
			} finally {
				StringBuilderCache.Free (sb);
			}
		}

	}
}
