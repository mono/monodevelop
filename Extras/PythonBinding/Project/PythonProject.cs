using System;
using System.IO;
using System.Collections;
using System.Xml;

using MonoDevelop.Internal.Project;
using MonoDevelop.Internal.Templates;

namespace PythonBinding
{
	public class PythonProject : AbstractProject
	{
		public override string ProjectType {
			get {
				return PythonLanguageBinding.LanguageName;
			}
		}
		
		public override IConfiguration CreateConfiguration ()
		{
			return new PythonCompilerParameters ();
		}
		
		public PythonProject ()
		{
		}
		
		public PythonProject (ProjectCreateInformation info, XmlElement projectOptions)
		{
			if (info != null) {
				Name = info.ProjectName;
				Configurations.Add (CreateConfiguration ("Debug"));
				Configurations.Add (CreateConfiguration ("Release"));
				foreach (PythonCompilerParameters parameter in Configurations) {
					parameter.OutputDirectory = Path.Combine (info.BinPath, parameter.Name);
					parameter.OutputAssembly  = Name;
				}
			}
		}
	}
}

