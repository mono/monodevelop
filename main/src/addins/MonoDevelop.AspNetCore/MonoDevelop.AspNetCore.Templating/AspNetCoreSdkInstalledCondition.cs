using System;
using System.Linq;
using Mono.Addins;
using MonoDevelop.DotNetCore;

namespace MonoDevelop.AspNetCore
{
	class AspNetCoreSdkInstalledCondition : DotNetCoreSdkInstalledCondition
	{
		// This is to make compile time cross AddIn dependency(so it doesn't silently get renamed/removed
	}
}
