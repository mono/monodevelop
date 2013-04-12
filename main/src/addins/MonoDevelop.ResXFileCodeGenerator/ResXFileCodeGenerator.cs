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

