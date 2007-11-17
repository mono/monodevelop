using System;
using WriterService;

namespace SampleExtender
{
	public class MyWriter: IWriter
	{
		public string Write ()
		{
			return "Some writer";
		}
	}
	
	public class DebugWriter: IWriter
	{
		public string Write ()
		{
			return "Some debug output";
		}
	}
}
