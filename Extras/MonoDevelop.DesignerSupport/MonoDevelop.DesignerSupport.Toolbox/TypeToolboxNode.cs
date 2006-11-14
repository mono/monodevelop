
using System;
using MonoDevelop.Projects.Serialization;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	[Serializable]
	[DataInclude (typeof(TypeReference))]
	public class TypeToolboxNode : ItemToolboxNode
	{
		[ItemProperty ("type")]
		TypeReference type;
		
		//for deserialisation
		public TypeToolboxNode ()
		{
		}
		
		public TypeToolboxNode (TypeReference typeRef)
		{
			this.type = typeRef;
		}
		
		public TypeToolboxNode (string typeName, string assemblyName)
		  : this (typeName, assemblyName, string.Empty)
		{
		}
		
		public TypeToolboxNode (string typeName, string assemblyName, string assemblyLocation)
		{
			this.type = new TypeReference (typeName, assemblyName, assemblyLocation);
		}
		
		public TypeToolboxNode (Type type)
		{
			this.type = new TypeReference (type);
		}
		
		public TypeReference Type {
			get { return type; }
			set { type = value; }
		}
		
		public override bool Equals (object o)
		{
			TypeToolboxNode node = o as TypeToolboxNode;
			return (node != null)
			    && (this.type == null? node.type == null : this.type.Equals (node.type))
			    && base.Equals (node);
		}
		
		
		public bool Equals (TypeToolboxNode node)
		{
			return (node != null)
			    && (this.type == null? node.type == null : this.type.Equals (node.type))
			    && base.Equals (node);
    	}
		
		
	}
	
	[Serializable]
	public class TypeReference
	{
		[ItemProperty ("location")]
		string assemblyLocation = "";
		[ItemProperty ("assembly")]
		string assemblyName = "";
		[ItemProperty ("name")]
		string typeName = "";
		
		//for deserialisation
		public TypeReference ()
		{
		}
		
		public override bool Equals (System.Object obj)
		{
			TypeReference other = obj as TypeReference;
			return (other != null)
			    && (this.typeName == other.typeName)
			    && (this.assemblyName == other.assemblyName)
			    && (this.assemblyLocation == other.assemblyLocation);
		}
		
		public bool Equals (TypeReference other)
		{
			return (other != null)
			    && (this.typeName == other.typeName)
			    && (this.assemblyName == other.assemblyName)
			    && (this.assemblyLocation == other.assemblyLocation);
		}
		
		public TypeReference (string typeName, string assemblyName)
		{
			this.typeName = typeName;
			this.assemblyName = assemblyName;
		}
		
		public TypeReference (Type type)
			: this (type.FullName, type.Assembly.FullName)
		{
			if (!type.Assembly.GlobalAssemblyCache)
				assemblyLocation = type.Assembly.Location;
		}
		
		public TypeReference (string typeName, string assemblyName, string assemblyLocation)
			: this (typeName, assemblyName)
		{
			this.assemblyLocation = assemblyLocation;
		}
		
		public string AssemblyName {
			get { return assemblyName; }
			set { assemblyName = value; }
		}
		
		public string TypeName {
			get { return typeName; }
			set { typeName = value; }
		}
		
		public string AssemblyLocation {
			get { return assemblyLocation; }
			set { assemblyLocation = value; }
		}
		
		//FIXME: three likely exceptions in here: need to handle them
		public Type Load ()
		{
			System.Reflection.Assembly assem = null;
			
			if (string.IsNullOrEmpty (assemblyLocation))
				//GAC assembly
				assem = System.Reflection.Assembly.Load (assemblyName);
			else
				//local assembly
				assem = System.Reflection.Assembly.LoadFile (assemblyLocation);
			
			Type type = assem.GetType (typeName, true);
			
			return type;
		}
	}
}
