
using System;
using MonoDevelop.Projects;

namespace MonoDevelop.Autotools
{
	public class MakefileProject: Project
	{
		public MakefileProject()
		{
		}
		
		public override SolutionItemConfiguration CreateConfiguration (string name)
		{
			MakefileProjectConfiguration conf = new MakefileProjectConfiguration ();
			conf.Name = name;
			return conf;
		}
		
		public override string ProjectType {
			get { return "MakefileProject"; }
		}


	}
	
	public class MakefileProjectConfiguration: ProjectConfiguration
	{
	}
}
