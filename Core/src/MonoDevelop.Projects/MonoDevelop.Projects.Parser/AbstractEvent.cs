// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Reflection;
using MonoDevelop.Projects.Utility;

namespace MonoDevelop.Projects.Parser {
	[Serializable]
	public class DefaultEvent : AbstractMember, IEvent
	{
		protected IRegion          bodyRegion;
		protected EventAttributes  eventAttributes;
		protected IMethod          addMethod;
		protected IMethod          removeMethod;
		protected IMethod          raiseMethod;
		
		public DefaultEvent ()
		{
		}
		
		public DefaultEvent (string name, IReturnType type, ModifierEnum m, IRegion region, IRegion bodyRegion)
		{
			FullyQualifiedName = name;
			returnType         = type;
			this.region        = region;
			this.bodyRegion    = bodyRegion;
			modifiers          = m;
		}
		
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

		public override int CompareTo (object ob) 
		{
			IEvent value = (IEvent) ob;
			return base.CompareTo (value);
		}
		
		public override bool Equals (object ob)
		{
			IEvent other = ob as IEvent;
			if (other == null) return false;
			return CompareTo (other) == 0;
		}
		
		public override int GetHashCode ()
		{
			int c = base.GetHashCode () + (int) eventAttributes;
			if (bodyRegion != null)
				c += bodyRegion.GetHashCode ();
			if (addMethod != null)
				c += addMethod.GetHashCode ();
			if (removeMethod != null)
				c += removeMethod.GetHashCode ();
			if (raiseMethod != null)
				c += raiseMethod.GetHashCode ();
				
			return c; 
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
