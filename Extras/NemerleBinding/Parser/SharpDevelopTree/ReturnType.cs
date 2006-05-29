// created on 04.08.2003 at 18:08

using MonoDevelop.Projects.Parser;
using Nemerle.Completion;

namespace NemerleBinding.Parser.SharpDevelopTree
{
	public class ReturnType : AbstractReturnType
	{
		public new int PointerNestingLevel {
			get {
				return base.pointerNestingLevel;
			}
			set {
				base.pointerNestingLevel = value;
			}
		}
		
		public new int[] ArrayDimensions {
			get {
				return base.arrayDimensions;
			}
			set {
				base.arrayDimensions = value;
			}
		}
		
		public ReturnType(string fullyQualifiedName)
		{
			base.FullyQualifiedName = fullyQualifiedName;
		}

        public ReturnType(ConstructedTypeInfo type)
        {
            base.FullyQualifiedName = get_type_name (type);
        }
        
        public ReturnType(System.Type type)
        {
            base.FullyQualifiedName = type_name (type);
        }
        
        public string type_name (System.Type type)
        {
            try
            {
                string namex;
                if (type.IsGenericParameter)
                {
                    namex = type.Name;
                }
                else
                {
                    namex = type.FullName.Replace ("System.Byte", "byte")
                        .Replace ("System.SByte", "sbyte")  
                        .Replace ("System.Int16", "short")
                        .Replace ("System.UInt16", "ushort")
                        .Replace ("System.Int32", "int")
                        .Replace ("System.UInt32", "uint")
                        .Replace ("System.Int64", "long")
                        .Replace ("System.UInt64", "ulong")
                        .Replace ("System.Single", "float")
                        .Replace ("System.Double", "double")
                        .Replace ("System.Decimal", "decimal")
                        .Replace ("System.String", "string")
                        .Replace ("System.Object", "object")
                        .Replace ("System.Boolean", "bool")
                        .Replace ("System.Char", "char")
                        .Replace ("Nemerle.Core.list", "list")
                        .Replace ("System.Void", "void")
                        .Replace ("`1", "")
                        .Replace ("`2", "")
                        .Replace ("`3", "")
                        .Replace ("`4", "");
                    if (type.GetGenericArguments().Length > 0)
                    {
                        namex += "[";
                        foreach (System.Type gt in type.GetGenericArguments())
                        {
                            namex += type_name (gt) + ", ";
                        }
                        namex = namex.TrimEnd (' ', ',') + "]";
                    }
                }
                if (type.IsArray)
                    namex = "array[" + namex + "]";
                return namex;
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine (ex.Message);
                System.Console.WriteLine (ex.StackTrace);
                return "??";
            }
        }
		
		public ReturnType(string fullyQualifiedName, int[] arrayDimensions, int pointerNestingLevel)
		{
			this.FullyQualifiedName  = fullyQualifiedName;
			this.arrayDimensions     = arrayDimensions;
			this.pointerNestingLevel = pointerNestingLevel;
		}

		public ReturnType Clone()
		{
			return new ReturnType(FullyQualifiedName, arrayDimensions, pointerNestingLevel);
		}

        string get_type_name (ConstructedTypeInfo t)
        {
            try
            {
                string name = "";
                if (t is ConstructedTypeInfo.Array)
                {
                    ConstructedTypeInfo.Array ar = (ConstructedTypeInfo.Array)t;
                    name = "array[" + get_type_name(ar.Type) + "]";
                }   
                else if (t is ConstructedTypeInfo.Class)
                {
                    ConstructedTypeInfo.Class cl = (ConstructedTypeInfo.Class)t;
                    string nameByNow = "";
                    if (cl.Type is DeclaredTypeInfo)
                    {
                        DeclaredTypeInfo dti = (DeclaredTypeInfo)cl.Type;
                        if (dti.IsNested)
                            nameByNow = dti.DeclaringType.Namespace + "." + dti.DeclaringType.Name + "+" + dti.Name;
                        else
                            nameByNow = dti.Namespace + "." + dti.Name;
                        if (dti.TypeParameters.Length > 0)
                        {
                            nameByNow += "[";
                            string whereClauses = "";
                            foreach (TypeParameterInfo typarm in dti.TypeParameters)
                                nameByNow += typarm.Name + ", ";
                             nameByNow = nameByNow.TrimEnd(',', ' ') + "]" + whereClauses;
                        }                      
                    }
                    else if (cl.Type is ReferencedTypeInfo)
                    {
                        nameByNow = ((ReferencedTypeInfo)cl.Type).Type.FullName;
                        if (nameByNow == null)
                            nameByNow = ((ReferencedTypeInfo)cl.Type).Type.Name;
                    }
    
                    if (cl.SubstitutedArguments.Length > 0)
                    {
                        nameByNow += "[";
                        foreach (ConstructedTypeInfo cdt in cl.SubstitutedArguments)
                            nameByNow += get_type_name(cdt) + ", ";
                        nameByNow = nameByNow.TrimEnd(',', ' ');
                        nameByNow += "]";
                    }
                    name = nameByNow;
                }
                else if (t is ConstructedTypeInfo.Function)
                {
                    ConstructedTypeInfo.Function fu = (ConstructedTypeInfo.Function)t;
                    name = get_type_name (fu.From) + " -> " + get_type_name(fu.To);
                }
                else if (t is ConstructedTypeInfo.GenericSpecifier)
                {
                    ConstructedTypeInfo.GenericSpecifier gs = (ConstructedTypeInfo.GenericSpecifier)t; 
                    name = gs.Name; // It only shows the name, no constraints
                }
                else if (t is ConstructedTypeInfo.Tuple)
                {
                    ConstructedTypeInfo.Tuple tu = (ConstructedTypeInfo.Tuple)t;  
                    string nameByNow = "";
                    foreach (ConstructedTypeInfo cdt in tu.Types)
                        nameByNow += get_type_name(cdt) + " * ";
                    name = nameByNow.Trim('*', ' ');
                }
                else
                    name = "void";
            
                name = name.Replace ("System.Byte", "byte")
                    .Replace ("System.SByte", "sbyte")  
                    .Replace ("System.Int16", "short")
                    .Replace ("System.UInt16", "ushort")
                    .Replace ("System.Int32", "int")
                    .Replace ("System.UInt32", "uint")
                    .Replace ("System.Int64", "long")
                    .Replace ("System.UInt64", "ulong")
                    .Replace ("System.Single", "float")
                    .Replace ("System.Double", "double")
                    .Replace ("System.Decimal", "decimal")
                    .Replace ("System.String", "string")
                    .Replace ("System.Object", "object")
                    .Replace ("System.Boolean", "bool")
                    .Replace ("System.Char", "char")
                    .Replace ("Nemerle.Core.list", "list")
                    .Replace ("System.Void", "void")
                    .Replace ("`1", "")
                    .Replace ("`2", "")
                    .Replace ("`3", "")
                    .Replace ("`4", "");
            
                return name;
            }
            catch (System.Exception ex)
            {
                return "??";
            }
        }
	}
}
