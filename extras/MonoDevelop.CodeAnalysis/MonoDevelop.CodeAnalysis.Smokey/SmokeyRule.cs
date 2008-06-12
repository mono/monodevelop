using System;
using System.Collections.Generic;

using CA = MonoDevelop.CodeAnalysis;

namespace MonoDevelop.CodeAnalysis.Smokey {
	
	public class SmokeyRule : CA.IRule {
		private string id, name, description;

		public SmokeyRule (string id, string name, string description)
		{
			this.id = id;
			this.name = name;
			this.description = description;
		}

		
		public string Id {
			get { return id; }
		}

		public string Name {
			get { return name; }
		}

		public string Description {
			get { return description; }
		}
	}
}
