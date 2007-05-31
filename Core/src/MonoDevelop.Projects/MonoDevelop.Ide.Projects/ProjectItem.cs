
using System;

namespace MonoDevelop.Ide.Projects
{
	public class ProjectItem
	{
		string include;
		
		public string Include {
			get { return incude; }
			set { include = value; }
		}
		
		public ProjectItem()
		{
		}
		
		public ProjectItem(string include)
		{
			this.Include = include;
		}
	}
}
