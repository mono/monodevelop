
using System;
using MonoDevelop.Projects;
using System.Collections.Generic;

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
		
		public override IEnumerable<string> GetProjectTypes ()
		{
			yield return "MakefileProject";
		}
	}
	
	public class MakefileProjectConfiguration: ProjectConfiguration
	{
	}
}
