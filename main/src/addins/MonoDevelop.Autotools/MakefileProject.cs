
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
		
		protected override SolutionItemConfiguration OnCreateConfiguration (string name, ConfigurationKind kind)
		{
			MakefileProjectConfiguration conf = new MakefileProjectConfiguration ();
			conf.Name = name;
			return conf;
		}

		protected override void OnGetTypeTags (HashSet<string> types)
		{
			base.OnGetTypeTags (types);
			types.Add ("MakefileProject");
		}
	}
	
	public class MakefileProjectConfiguration: ProjectConfiguration
	{
	}
}
