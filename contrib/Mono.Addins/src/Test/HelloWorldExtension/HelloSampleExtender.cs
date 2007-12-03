
using System;
using SimpleApp;
using Mono.Addins;

namespace HelloWorldExtension
{
	[Extension (Id="HelloExt")]
	public class HelloSampleExtender: ISampleExtender
	{
		public void Dispose ()
		{
			
		}

		public string Text {
			get { return "HelloSampleExtender"; }
		}
	}
}
