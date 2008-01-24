using System;

namespace Stetic {

	public class GladeException : ApplicationException {

		public GladeException (string message) : base (message) { }

		public GladeException (string message, string className) :
			this (message + " (class " + className + ")")
		{
			this.className = className;
		}

		public GladeException (string message, string className,
				       bool childprop, string propName) :
			this (message + " (class " + className + ", " + (childprop ? "child " : "") + "property " + propName + ")")
		{
			this.childprop = childprop;
			this.propName = propName;
		}

		public GladeException (string message, string className,
				       bool childprop, string propName, string propVal) :
			this (message + " (class " + className + ", " + (childprop ? "child " : "") + "property " + propName + ", value " + propVal + ")")
		{
			this.childprop = childprop;
			this.propName = propName;
			this.propVal = propVal;
		}

		string className, propName, propVal;
		bool childprop;

		public string ClassName {
			get {
				return className;
			}
		}

		public bool ChildProp {
			get {
				return childprop;
			}
		}

		public string PropName {
			get {
				return propName;
			}
		}

		public string PropVal {
			get {
				return propVal;
			}
		}
	}
}
