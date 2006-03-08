// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Reflection;
using MonoDevelop.Projects.Utility;

namespace MonoDevelop.Projects.Parser
{
	[Serializable]
	public abstract class AbstractMethod : AbstractMember, IMethod
	{
		protected IRegion bodyRegion;
		
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

		public virtual bool IsConstructor {
			get {
				return returnType == null || Name == "ctor";
			}
		}

		public override string ToString()
		{
			return String.Format("[AbstractMethod: FullyQualifiedName={0}, ReturnType = {1}, IsConstructor={2}, Modifier={3}]",
			                     FullyQualifiedName,
			                     ReturnType,
			                     IsConstructor,
			                     base.Modifiers);
		}
		
		public override int CompareTo (object value)
		{
			int cmp = base.CompareTo (value);
			if (cmp != 0)
				return cmp;
			
			return DiffUtility.Compare(Parameters, ((IMethod)value).Parameters);
		}
		
		public override bool Equals (object ob)
		{
			IMethod other = ob as IMethod;
			if (other == null) return false;
			return CompareTo (other) == 0;
		}
		
		public override int GetHashCode ()
		{
			return base.GetHashCode ();
		}
	}
}
