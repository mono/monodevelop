// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Reflection;
using System.Collections.Utility;

namespace MonoDevelop.Internal.Parser {
	[Serializable]
	public abstract class AbstractEvent : AbstractMember, IEvent
	{
		protected IRegion          bodyRegion;
		protected EventAttributes  eventAttributes;
		protected IMethod          addMethod;
		protected IMethod          removeMethod;
		protected IMethod          raiseMethod;
		
		public virtual IRegion BodyRegion {
			get {
				return bodyRegion;
			}
		}
		

		public virtual EventAttributes EventAttributes {
			get {
				return eventAttributes;
			}
		}

		public virtual int CompareTo(IEvent value) {
			int cmp;
			
			if(0 != (cmp = base.CompareTo((IDecoration)value)))
				return cmp;
			
			if (FullyQualifiedName != null) {
				cmp = FullyQualifiedName.CompareTo(value.FullyQualifiedName);
				if (cmp != 0) {
					return cmp;
				}
			}
			
			if (ReturnType != null) {
				cmp = ReturnType.CompareTo(value.ReturnType);
				if (cmp != 0) {
					return cmp;
				}
			}
						
			return Region.CompareTo(value.Region);
		}
		
		int IComparable.CompareTo(object value) {
			return CompareTo((IEvent)value);
		}
		
		public virtual IMethod AddMethod {
			get {
				return addMethod;
			}
		}
		
		public virtual IMethod RemoveMethod {
			get {
				return removeMethod;
			}
		}
		
		public virtual IMethod RaiseMethod {
			get {
				return raiseMethod;
			}
		}
		
	}
}
