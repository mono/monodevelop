
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
		{
			this.type = new TypeReference (typeName, assemblyName);
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
			return (node != null) && (node.Type == this.Type);
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
		
		public Type Load ()
		{
			Type type = System.Reflection.Assembly.Load (assemblyName).GetType (typeName, false);
			
			if ((type == null) && (assemblyLocation.Length < 1)) {
				System.Reflection.Assembly.LoadFile (assemblyLocation).GetType (typeName, false);
			}
			
			return type;
		}
	}
}
