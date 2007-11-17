
using System;
using SimpleApp;
using Mono.Addins;

namespace FileContentExtension
{
	[Extension (InsertAfter="HelloExt")]
	public class FileSampleExtender: ISampleExtender
	{
		public string Text {
			get { return "FileSampleExtender"; }
		}
	}
}
