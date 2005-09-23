// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Reflection;

namespace MonoDevelop.Internal.Parser {

	[Serializable]
	public abstract class AbstractProperty : AbstractMember, IProperty
	{
		protected IRegion bodyRegion;
		
		protected IRegion     getterRegion;
		protected IRegion     setterRegion;

		protected IMethod     getterMethod;
		protected IMethod     setterMethod;
		protected ParameterCollection parameters = new ParameterCollection();
		
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
			get {
				return getterRegion;
			}
		}

		public IRegion SetterRegion {
			get {
				return setterRegion;
			}
		}

		public IMethod GetterMethod {
			get {
				return getterMethod;
			}
		}

		public IMethod SetterMethod {
			get {
				return setterMethod;
			}
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

		public virtual int CompareTo(IProperty value)
		{
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
				if(0 != (cmp = ReturnType.CompareTo(value.ReturnType)))
					return cmp;
			}
			
			if(0 != (cmp = Region.CompareTo(value.Region)))
				return cmp;
			
			if(SetterRegion != null && value.SetterRegion == null)
				return 1;
			
			if(SetterRegion == null && value.SetterRegion != null)
				return -1;
			
			if(GetterRegion != null && value.GetterRegion == null)
				return 1;
			
			if(GetterRegion == null && value.GetterRegion != null)
				return -1;
			
			return 0;
		}
		
		int IComparable.CompareTo(object value) {
			return CompareTo((IProperty)value);
		}
	}
}
