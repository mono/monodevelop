
using System;

namespace MonoDevelop.Projects
{
	public enum CustomCommandType
	{
		BeforeBuild,
		Build,
		AfterBuild,
		BeforeExecute,
		Execute,
		AfterExecute,
		BeforeClean,
		Clean,
		AfterClean,
		Custom
	}
}
