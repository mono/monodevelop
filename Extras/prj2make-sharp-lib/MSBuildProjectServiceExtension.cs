//
// MSBuildProjectServiceExtension.cs
//
// Author:
//   Ankit Jain <jankit@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects;

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace MonoDevelop.Prj2Make
{
	public class MSBuildProjectServiceExtension : ProjectServiceExtension
	{

		public override ICompilerResult Build (IProgressMonitor monitor, CombineEntry entry)
		{
			//xamlg any SilverLightPages
			DotNetProject project = entry as DotNetProject;
			if (project == null)
				return base.Build (monitor, entry);

			foreach (ProjectFile pf in project.ProjectFiles) {
				if (pf.BuildAction != BuildAction.EmbedAsResource)
					continue;

				//Check for SilverLightPage
				if (pf.ExtendedProperties ["MonoDevelop.MSBuildFileFormat.SilverlightPage"] == null)
					continue;

				string generated_file_name;
				CompilerError error = GenerateXamlPartialClass (pf.Name, out generated_file_name, monitor);
				if (error != null) {
					CompilerResults cr = new CompilerResults (new TempFileCollection ());
					cr.Errors.Add (error);

					monitor.Log.WriteLine (GettextCatalog.GetString("Build complete -- {0} errors, {1} warnings", cr.Errors.Count, 0));
					return new DefaultCompilerResult (cr, String.Empty);
				}
			}

			return base.Build (monitor, entry);
		}

		CompilerError GenerateXamlPartialClass (string fname, out string generated_file_name, IProgressMonitor monitor)
		{
			generated_file_name = fname + ".g.cs";
			using (StringWriter sw = new StringWriter ()) {
				Console.WriteLine ("Generating partial classes for\n{0}$ {1} {2}", Path.GetDirectoryName (fname), "xamlg", fname);
				monitor.Log.WriteLine (GettextCatalog.GetString (
					"Generating partial classes for {0} with {1}", fname, "xamlg"));
				ProcessWrapper pw = null;
				try {
					pw = Runtime.ProcessService.StartProcess (
						"xamlg", String.Format ("\"{0}\"", fname),
						Path.GetDirectoryName (fname),
						sw, sw, null);
				} catch (System.ComponentModel.Win32Exception ex) {
					Console.WriteLine (GettextCatalog.GetString (
						"Error while trying to invoke '{0}' to generate partial classes for '{1}' :\n {2}", "xamlg", fname, ex.ToString ()));
					monitor.Log.WriteLine (GettextCatalog.GetString (
						"Error while trying to invoke '{0}' to generate partial classes for '{1}' :\n {2}", "xamlg", fname, ex.Message));

					return new CompilerError (fname, 0, 0, "", ex.Message);
				}

				//FIXME: Handle exceptions
				pw.WaitForOutput ();

				if (pw.ExitCode != 0) {
					//FIXME: Stop build on error?
					string output = sw.ToString ();
					Console.WriteLine (GettextCatalog.GetString (
						"Unable to generate partial classes ({0}) for {1}. \nReason: \n{2}\n",
						"xamlg", fname, output));
					monitor.Log.WriteLine (GettextCatalog.GetString (
						"Unable to generate partial classes ({0}) for {1}. \nReason: \n{2}\n",
						"xamlg", fname, output));

					//Try to get the line/pos
					int line = 0;
					int pos = 0;
					Match match = RegexErrorLinePos.Match (output);
					if (match.Success && match.Groups.Count == 3) {
						try {
							line = int.Parse (match.Groups [1].Value);
						} catch (FormatException){
						}

						try {
							pos = int.Parse (match.Groups [2].Value);
						} catch (FormatException){
						}
					}

					return new CompilerError (fname, line, pos, "", output);
				}
			}

			//No errors
			return null;
		}

		// Used for parsing "Line 123, position 5" errors from tools
		// like resgen, xamlg
		static Regex regexErrorLinePos;
		static Regex RegexErrorLinePos {
			get {
				if (regexErrorLinePos == null)
					regexErrorLinePos = new Regex (@"Line (\d*), position (\d*)");
				return regexErrorLinePos;
			}
		}

	}
}
