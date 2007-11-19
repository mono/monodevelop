
using System;
using Mono.Addins;
using SimpleApp;

namespace FileContentExtension
{
	public class FileContentExtensionNode: TypeExtensionNode, IWriter
	{
		[NodeAttribute]
		string fileName;
		
		public string FileName {
			get { return fileName; }
		}
		
		public override object CreateInstance ()
		{
			return this;
		}
		
		string IWriter.Title {
			get { return "File: " + fileName; }
		}
		
		string IWriter.Write ()
		{
			string s = "file:" + fileName;
			foreach (string c in GetChildObjects ())
				s += "[" + c + "]";
			return s;
		}
	}
	
	public class ContentExtensionNode: TypeExtensionNode
	{
		[NodeAttribute]
		string xpath;
		
		public string XPath {
			get { return xpath; }
		}
		
		public override object CreateInstance ()
		{
			return xpath;
		}
	}
}
