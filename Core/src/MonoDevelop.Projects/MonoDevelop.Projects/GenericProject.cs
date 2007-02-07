
using System;
using System.Xml;

namespace MonoDevelop.Projects
{
	public class GenericProject: Project
	{
		public GenericProject ()
		{
		}
		
		public GenericProject (ProjectCreateInformation info, XmlElement projectOptions)
		{
			Configurations.Add (CreateConfiguration ("Default"));
		}
		
		public override IConfiguration CreateConfiguration (string name)
		{
			GenericProjectConfiguration conf = new GenericProjectConfiguration ();
			conf.Name = name;
			return conf;
		}
		
		public override string ProjectType {
			get { return "GenericProject"; }
		}
	}
	
	public class GenericProjectConfiguration: AbstractProjectConfiguration
	{
	}
}
