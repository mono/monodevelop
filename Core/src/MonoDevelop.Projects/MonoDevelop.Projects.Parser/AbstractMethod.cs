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
		
		protected GenericParameterList genericParameters;
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
		
		/// <summary>
		/// Contains a list of formal parameters to a generic method. 
		/// <p>If this property returns null or an empty collection, the method
		/// is not generic.</p>
		/// </summary>
		public virtual GenericParameterList GenericParameters {
			get {
				return genericParameters;
			}
			set {
				genericParameters = value;
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
			
			cmp = DiffUtility.Compare(Parameters, ((IMethod)value).Parameters);
			if (cmp != 0)
				return cmp;
				
			if (GenericParameters == ((IMethod)value).GenericParameters)
				return 0;
			else
				return DiffUtility.Compare(GenericParameters, ((IMethod)value).GenericParameters);
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
