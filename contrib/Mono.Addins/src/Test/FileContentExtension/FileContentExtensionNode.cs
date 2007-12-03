
using System;
using Mono.Addins;
using SimpleApp;

namespace FileContentExtension
{
	public class FileContentExtensionNode: TypeExtensionNode, IWriter
	{
		[NodeAttribute]
		protected string fileName;
		
		[NodeAttribute (Localizable=true)]
		protected string title;
		
		public string FileName {
			get { return fileName; }
		}
		
		public string Title {
			get { return title; }
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
		
		public override string ToString ()
		{
			return title;
		}
		
		public string Test (string test)
		{
			return test;
		}
	}
	
	public class ContentExtensionNode: TypeExtensionNode
	{
		[NodeAttribute]
		protected string xpath;
		
		public string XPath {
			get { return xpath; }
		}
		
		public override object CreateInstance ()
		{
			return xpath;
		}
	}
}
