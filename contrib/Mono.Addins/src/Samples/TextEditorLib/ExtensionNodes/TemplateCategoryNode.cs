
using System;
using Mono.Addins;

namespace TextEditor
{
	public class TemplateCategoryNode: ExtensionNode
	{
		[NodeAttribute]
		string name;
		
		public string Name {
			get {
				if (name != null && name.Length > 0)
					return name; 
				else
					return Id;
			}
		}
	}
}
