
using System;
using SimpleApp;
using Mono.Addins;

[assembly: Addin ("HelloWorldExtension", "0.1.0", Namespace="SimpleApp")]
[assembly: AddinDependency ("Core", "0.1.0")]

namespace HelloWorldExtension
{
	[Extension ("/SimpleApp/Writers")]
	public class HelloWorldWriter: IWriter
	{
		public string Title {
			get { return "Hello world message"; }
		}
		
		public string Write ()
		{
			return "Hello world!";
		}
		
		public string Test (string test)
		{
			switch (test) {
			case "currentAddin":
				return AddinManager.CurrentAddin.ToString ();
			}
			return "Unknown test";
		}
	}
}
