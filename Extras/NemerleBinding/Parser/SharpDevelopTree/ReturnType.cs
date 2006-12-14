// created on 04.08.2003 at 18:08

using MonoDevelop.Projects.Parser;
using Nemerle.Completion;
using NCC = Nemerle.Compiler;

namespace NemerleBinding.Parser.SharpDevelopTree
{
	public class ReturnType : DefaultReturnType
	{
		public ReturnType (string fullyQualifiedName): base (fullyQualifiedName)
		{
		}

        public ReturnType(NCC.MType type)
        {
            base.arrayDimensions = new int[0];
            base.pointerNestingLevel = 0;
            
            if (type is NCC.MType.Class)
            {
                NCC.MType.Class t = (NCC.MType.Class)type;
                base.FullyQualifiedName = t.tycon.FrameworkTypeName
                        .Replace ("`1", "")
                        .Replace ("`2", "")
                        .Replace ("`3", "")
                        .Replace ("`4", "");
                        
                if (t.args.Length > 0)
                {
                    base.genericArguments = new ReturnTypeList ();
                    foreach (NCC.TyVar tyvar in t.args)
                    {
                        base.genericArguments.Add (new ReturnType (tyvar.Fix ()));
                    }
                }
            }
            else if (type is NCC.MType.TyVarRef)
            {
                base.FullyQualifiedName = ((NCC.MType.TyVarRef)type).tyvar.Name;
            }
            else if (type is NCC.MType.Fun)
            {
                // Use the plain type until Ambience works correctly
                base.FullyQualifiedName = Engine.GetNameFromType (type);
            }
            else if (type is NCC.MType.Tuple)
            {
                // Use the plain type until Ambience works correctly
                base.FullyQualifiedName = Engine.GetNameFromType (type);
            }
            else if (type is NCC.MType.Array)
            {
                NCC.MType.Array a = (NCC.MType.Array)type;
                ReturnType rtx = new ReturnType (a.t.Fix ());
                this.FullyQualifiedName = rtx.FullyQualifiedName;
                this.arrayDimensions = new int[rtx.ArrayDimensions.Length + 1];
                this.arrayDimensions[0] = a.rank;
                for (int i = 0; i < rtx.ArrayDimensions.Length; i++)
                    this.arrayDimensions[i+1] = rtx.ArrayDimensions[i];
            }
            else if (type is NCC.MType.Void)
            {
                base.FullyQualifiedName = "System.Void";
            }
            else if (type is NCC.MType.Ref)
            {
                ReturnType rtx = new ReturnType (((NCC.MType.Ref)type).t.Fix ());
                this.FullyQualifiedName = rtx.FullyQualifiedName;
                this.arrayDimensions = rtx.ArrayDimensions;
            }
            else if (type is NCC.MType.Out)
            {
                ReturnType rtx = new ReturnType (((NCC.MType.Out)type).t.Fix ());
                this.FullyQualifiedName = rtx.FullyQualifiedName;
                this.arrayDimensions = rtx.ArrayDimensions;
            }
        }
        
        public ReturnType(System.Type type)
        {
            try
            {
                if (type.IsGenericParameter)
                {
                    base.FullyQualifiedName = type.Name;
                }
                else
                {
                    base.FullyQualifiedName = type.FullName
                        .Replace ("`1", "")
                        .Replace ("`2", "")
                        .Replace ("`3", "")
                        .Replace ("`4", "");
                }
                if (type.IsArray)
                    base.arrayDimensions = new int[] { 1 };
                    
                if (type.GetGenericArguments().Length > 0)
                {
                    base.genericArguments = new ReturnTypeList ();
                    foreach (System.Type gt in type.GetGenericArguments())
                    {
                        base.genericArguments.Add (new ReturnType (gt));
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine (ex.Message);
                System.Console.WriteLine (ex.StackTrace);
                base.FullyQualifiedName = "??";
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
