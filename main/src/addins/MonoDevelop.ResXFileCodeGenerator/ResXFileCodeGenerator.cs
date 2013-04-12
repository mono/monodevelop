//
// ResXFileCodeGenerator.cs
//
// Author:
//   Kenneth Skovhede <kenneth@hexad.dk>
//
// Copyright (C) 2013
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

using System;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using System.Text;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide.CustomTools;

namespace MonoDevelop
{
	public class ResXFileCodeGenerator : MonoDevelop.Ide.CustomTools.ISingleFileCustomTool
	{
		public static void GenerateDesignerFile (string resxfile, string @namespace, string classname, string designerfile)
		{
			var doc = XDocument.Load (resxfile).Root;
			
			var filetemplate = new StreamReader (System.Reflection.Assembly.GetExecutingAssembly ().GetManifestResourceStream ("ResxDesignerGenerator.HeaderTemplate.txt")).ReadToEnd ();
			var elementtemplate = new StreamReader (System.Reflection.Assembly.GetExecutingAssembly ().GetManifestResourceStream ("ResxDesignerGenerator.ElementTemplate.txt")).ReadToEnd ();

			var sb = new StringBuilder ();
			foreach (var node in 
				from d in (from n in doc.Descendants() where n.Name == "data" select n)
				let name = d.Attribute("name").Value
				let value = d.Descendants().First().Value
				orderby name
				select new { Name = name, Value = value }
				) {
				sb.Append (elementtemplate.Replace ("{name}", node.Name).Replace ("{value}", System.Web.HttpUtility.HtmlEncode (node.Value.Trim ().Replace ("\r\n", "\n").Replace ("\r", "\n").Replace ("\n", "\r\n        ///"))));
				sb.Append ("\r\n");
			}
				
			using (var w = new StreamWriter(designerfile, false, Encoding.UTF8))
				w.Write (filetemplate.Replace ("{runtime-version}", System.Environment.Version.ToString ()).Replace ("{namespace}",  @namespace).Replace ("{classname}", classname).Replace ("{elementdata}", sb.ToString ().Trim ()));
		}

		internal static string GetNamespaceHint (ProjectFile file, string outputFile)
		{
			string ns = file.CustomToolNamespace;
			if (string.IsNullOrEmpty (ns) && !string.IsNullOrEmpty (outputFile)) {
				var dnp = file.Project as DotNetProject;
				if (dnp != null) {
					ns = dnp.DefaultNamespace;
					if (!string.IsNullOrEmpty (dnp.GetDefaultNamespace (outputFile)))
						ns += "." + dnp.GetDefaultNamespace (outputFile);
				}
			}
			return ns;
		}
		
		#region ISingleFileCustomTool implementation

		public IAsyncOperation Generate (IProgressMonitor monitor, ProjectFile file, SingleFileCustomToolResult result)
		{		
			return new MonoDevelop.TextTemplating.ThreadAsyncOperation (delegate {
				var outputfile = file.FilePath.ChangeExtension (".Designer.cs");
				var ns = GetNamespaceHint (file, outputfile);
				var cn = file.FilePath.FileNameWithoutExtension;
				
				GenerateDesignerFile (file.FilePath.FullPath, ns, cn, outputfile);
				result.GeneratedFilePath = outputfile;
			}, result);		
		}

		#endregion
	}
}

