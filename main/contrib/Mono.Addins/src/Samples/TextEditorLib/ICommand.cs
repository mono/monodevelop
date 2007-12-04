
using System;
using Mono.Addins;

namespace TextEditor
{
	[TypeExtensionPoint ("/TextEditor/StartupCommands")]
	public interface ICommand
	{
		void Run ();
	}
}
