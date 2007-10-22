
using System;
using System.Diagnostics;
using Mono.Addins;
using SimpleApp;

namespace CommandExtension
{
	public class CommandExtensionNode: TypeExtensionNode, IWriter
	{
		[NodeAttribute]
		string title;
		
		[NodeAttribute]
		string command;
		
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
	}
}
