
using System;

namespace MonoDevelop.Components.Commands
{
	public interface ICommandTargetVisitor
	{
		bool Visit (object ob);
	}
}
