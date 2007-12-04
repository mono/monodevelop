
using System;
using System.IO;
using Mono.Addins;

namespace TextEditor
{
	public class FileTemplateNode: ExtensionNode
	{
		[NodeAttribute]
		string resource;
		
		[NodeAttribute]
		string name;
		
		public string Name {
			get { return name != null ? name : Id; }
		}
		
		public virtual string GetContent ()
		{
			using (StreamReader sr = new StreamReader(Addin.GetResource (resource))) {
				return sr.ReadToEnd (); 
			}
		}
	}
}
