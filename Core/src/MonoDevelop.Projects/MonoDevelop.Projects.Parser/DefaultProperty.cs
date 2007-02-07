// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Reflection;

namespace MonoDevelop.Projects.Parser {

	[Serializable]
	public class DefaultProperty : AbstractMember, IProperty
	{
		protected IRegion bodyRegion;
		
		protected IRegion     getterRegion;
		protected IRegion     setterRegion;

		protected IMethod     getterMethod;
		protected IMethod     setterMethod;
		protected ParameterCollection parameters = new ParameterCollection();
		
		public DefaultProperty ()
		{
		}
		
		public DefaultProperty (string name, IReturnType type, ModifierEnum m, IRegion region, IRegion bodyRegion)
		{
			this.Name = name;
			returnType = type;
			this.region = region;
			this.bodyRegion = bodyRegion;
			modifiers = m;
		}
		
		public virtual IRegion BodyRegion {
			get {
				return bodyRegion;
			}
		}

		public virtual ParameterCollection Parameters {
			get {
				return parameters;
			}
			set {
				parameters = value;
			}
		}

		public IRegion GetterRegion {
			get { return getterRegion; }
			set { getterRegion = value; }
		}

		public IRegion SetterRegion {
			get { return setterRegion; }
			set { setterRegion = value; }
		}

		public IMethod GetterMethod {
			get { return getterMethod; }
			set { getterMethod = value; }
		}

		public IMethod SetterMethod {
			get { return setterMethod; }
			set { setterMethod = value; }
		}

		public virtual bool CanGet {
			get {
				return GetterRegion != null;
			}
		}

		public virtual bool CanSet {
			get {
				return SetterRegion != null;
			}
		}

		public override int CompareTo (object ob)
		{
			int cmp;
			IProperty value = (IProperty) ob;
			
			if(0 != (cmp = base.CompareTo (value)))
				return cmp;
			
			if (SetterRegion != null) {
				if (value.SetterRegion == null)	return 1;
				cmp = SetterRegion.CompareTo (value.SetterRegion);
				if (cmp != 0) return cmp;
			} else if (value.SetterRegion != null)
				return -1;
			
			if (GetterRegion != null) {
				if (value.GetterRegion == null)	return 1;
				cmp = GetterRegion.CompareTo (value.GetterRegion);
				if (cmp != 0) return cmp;
			} else if (value.GetterRegion != null)
				return -1;
			
			return 0;
		}
		
		public override bool Equals (object ob)
		{
			IProperty other = ob as IProperty;
			if (other == null) return false;
			return CompareTo (other) == 0;
		}
		
		public override int GetHashCode ()
		{
			int c = base.GetHashCode ();
			if (SetterRegion != null) c += SetterRegion.GetHashCode ();
			if (GetterRegion != null) c += GetterRegion.GetHashCode ();
			return c;
		}
	}
}
