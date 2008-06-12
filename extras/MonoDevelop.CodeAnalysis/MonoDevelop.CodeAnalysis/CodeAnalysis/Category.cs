using System;

namespace MonoDevelop.CodeAnalysis {

	public class Category {
		private string id;
		private string name;
		
		public Category (string id, string name)
		{
			Argument.NotNull (id, "id");
			Argument.NotNull (name, "name");
			
			this.id = id;
			this.name = name;
		}
		
		public string Id
		{
			get { return id; }
		}
		
		public string Name
		{
			get { return name; }
		}
	}
}
