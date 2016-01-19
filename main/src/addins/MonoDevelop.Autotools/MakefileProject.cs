
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
		
		protected override SolutionItemConfiguration OnCreateConfiguration (string id, ConfigurationKind kind)
		{
			return new MakefileProjectConfiguration (id);
		}

		protected override void OnGetTypeTags (HashSet<string> types)
		{
			base.OnGetTypeTags (types);
			types.Add ("MakefileProject");
		}
	}
	
	public class MakefileProjectConfiguration: ProjectConfiguration
	{
		public MakefileProjectConfiguration (string id) : base (id)
		{
		}
	}
}
