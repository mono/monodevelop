
using System;
using MonoDevelop.Projects;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace MonoDevelop.Autotools
{
	public class MakefileProject: Project
	{
		public MakefileProject()
		{
		}
		
		protected override SolutionItemConfiguration OnCreateConfiguration (string name)
		{
			MakefileProjectConfiguration conf = new MakefileProjectConfiguration ();
			conf.Name = name;
			return conf;
		}

		protected override ImmutableHashSet<string> OnGetProjectTypes ()
		{
			return base.OnGetProjectTypes ().Add ("MakefileProject");
		}
	}
	
	public class MakefileProjectConfiguration: ProjectConfiguration
	{
	}
}
