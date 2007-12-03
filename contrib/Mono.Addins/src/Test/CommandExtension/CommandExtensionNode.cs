
using System;
using System.Diagnostics;
using Mono.Addins;
using SimpleApp;

namespace CommandExtension
{
	public class CommandExtensionNode: TypeExtensionNode, IWriter
	{
		[NodeAttribute (Localizable=true)]
		protected string title;
		
		[NodeAttribute]
		protected string command;
		
		public override object CreateInstance ()
		{
			return this;
		}
		
		public string Title {
			get { return title; }
		}
		
		public string Write ()
		{
			return "cmd:" + command;
		}
		
		public override string ToString ()
		{
			return title;
		}
		
		public string Test (string test)
		{
			return test;
		}
	}
}
