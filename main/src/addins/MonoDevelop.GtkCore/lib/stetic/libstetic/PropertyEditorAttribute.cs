using System;

namespace Stetic {

	[AttributeUsage (AttributeTargets.Class)]
	public sealed class PropertyEditorAttribute : Attribute {

		public PropertyEditorAttribute (string property, string evt)
		{
			this.property = property;
			this.evt = evt;
		}

		public PropertyEditorAttribute (string property) : this (property, property + "Changed") {}

		string property;
		public string Property {
			get {
				return property;
			}
			set {
				property = value;
			}
		}

		string evt;
		public string Event {
			get {
				return evt;
			}
			set {
				evt = value;
			}
		}
	}
}
