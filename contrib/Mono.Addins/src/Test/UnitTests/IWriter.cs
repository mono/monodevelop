
using System;
using Mono.Addins;

[assembly: ExtensionPoint ("/SimpleApp/Writers")]

namespace SimpleApp
{
	public interface IWriter
	{
		string Title { get; }
		string Write ();
	}
}
