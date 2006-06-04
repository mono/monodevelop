// created on 04.08.2003 at 18:08

using MonoDevelop.Projects.Parser;
using Nemerle.Completion;
using Nemerle.Compiler;

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

        public ReturnType(MType type)
        {
            base.FullyQualifiedName = Engine.GetNameFromType (type);
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

	}
}
