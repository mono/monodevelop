

using System;

namespace Mono.Addins
{
	public interface IAddinInstaller
	{
		void InstallAddins (AddinRegistry reg, string message, string[] addinIds);
	}
}
