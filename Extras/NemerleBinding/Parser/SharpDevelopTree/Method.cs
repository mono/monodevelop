// created on 06.08.2003 at 12:35
using System;
using MonoDevelop.Projects.Parser;
using Nemerle.Completion;
using SR = System.Reflection;

namespace NemerleBinding.Parser.SharpDevelopTree
{
    public interface INemerleMethod
    {
        MethodInfo Member { get; }
    }
    
	public class Method : AbstractMethod, INemerleMethod
	{
		public void AddModifier(ModifierEnum m)
		{
			modifiers = modifiers | m;
		}
		
		MethodInfo _member;
		public MethodInfo Member
		{
		    get { return _member; }
		}
		
		public Method (IClass declaringType, SR.MethodInfo tinfo)
		{
		    this.declaringType = declaringType;
		
		    ModifierEnum mod = (ModifierEnum)0;
            if (tinfo.IsPrivate)
                mod |= ModifierEnum.Private;
            if (tinfo.IsAssembly)
                mod |= ModifierEnum.Internal;
            if (tinfo.IsFamily)
                mod |= ModifierEnum.Protected;
            if (tinfo.IsPublic)
                mod |= ModifierEnum.Public;
            if (tinfo.IsAbstract)
                mod |= ModifierEnum.Abstract;
            if (tinfo.IsFinal)
                mod |= ModifierEnum.Sealed;
            if (tinfo.IsStatic)
                mod |= ModifierEnum.Static;
            if (tinfo.IsVirtual)
                mod |= ModifierEnum.Virtual;
                
			modifiers = mod;
			
			this.FullyQualifiedName = tinfo.Name;
			
			if (tinfo.Name == "op_Addition") this.FullyQualifiedName = "@+";
			else if (tinfo.Name == "op_Subtraction") this.FullyQualifiedName = "@-";
			else if (tinfo.Name == "op_Multiply") this.FullyQualifiedName = "@*";
			else if (tinfo.Name == "op_Division") this.FullyQualifiedName = "@/";
			else if (tinfo.Name == "op_Modulus") this.FullyQualifiedName = "@%";
			else if (tinfo.Name == "op_ExclusiveOr") this.FullyQualifiedName = "@^";
			else if (tinfo.Name == "op_BitwiseAnd") this.FullyQualifiedName = "@&";
			else if (tinfo.Name == "op_BitwiseOr") this.FullyQualifiedName = "@|";
			else if (tinfo.Name == "op_LogicalAnd") this.FullyQualifiedName = "@&&";
			else if (tinfo.Name == "op_LogicalOr") this.FullyQualifiedName = "@||";
			else if (tinfo.Name == "op_Assign") this.FullyQualifiedName = "@=";
			else if (tinfo.Name == "op_LeftShift") this.FullyQualifiedName = "@<<";
			else if (tinfo.Name == "op_RightShift") this.FullyQualifiedName = "@>>";
			else if (tinfo.Name == "op_Equality") this.FullyQualifiedName = "@==";
			else if (tinfo.Name == "op_GreaterThan") this.FullyQualifiedName = "@>";
			else if (tinfo.Name == "op_LessThan") this.FullyQualifiedName = "@<";
			else if (tinfo.Name == "op_Inequality") this.FullyQualifiedName = "@!=";
			else if (tinfo.Name == "op_GreaterThanOrEqual") this.FullyQualifiedName = "@>=";
			else if (tinfo.Name == "op_LessThanOrEqual") this.FullyQualifiedName = "@<=";
			else if (tinfo.Name == "op_MultiplicationAssignment") this.FullyQualifiedName = "@*=";
			else if (tinfo.Name == "op_SubtractionAssignment") this.FullyQualifiedName = "@-=";
			else if (tinfo.Name == "op_ExclusiveOrAssignment") this.FullyQualifiedName = "@^=";
			else if (tinfo.Name == "op_LeftShiftAssignment") this.FullyQualifiedName = "@<<=";
			else if (tinfo.Name == "op_ModulusAssignment") this.FullyQualifiedName = "@%=";
			else if (tinfo.Name == "op_AdditionAssignment") this.FullyQualifiedName = "@+=";
			else if (tinfo.Name == "op_BitwiseAndAssignment") this.FullyQualifiedName = "@&=";
			else if (tinfo.Name == "op_BitwiseOrAssignment") this.FullyQualifiedName = "@|=";
			else if (tinfo.Name == "op_Comma") this.FullyQualifiedName = "@,";
			else if (tinfo.Name == "op_DivisionAssignment") this.FullyQualifiedName = "@/=";
			else if (tinfo.Name == "op_Implicit") this.FullyQualifiedName = "@:";
			else if (tinfo.Name == "op_Explicit") this.FullyQualifiedName = "@:>";
			else if (tinfo.Name == "op_UnaryPlus") this.FullyQualifiedName = "@+";
			else if (tinfo.Name == "op_UnaryNegation") this.FullyQualifiedName = "@-";
			else if (tinfo.Name == "op_Decrement") this.FullyQualifiedName = "@--";
			else if (tinfo.Name == "op_Increment") this.FullyQualifiedName = "@++";
			else if (tinfo.Name == "op_OnesComplement") this.FullyQualifiedName = "@~";
			else if (tinfo.Name == "op_LogicalNot") this.FullyQualifiedName = "@!";		
			
			returnType = new ReturnType(tinfo.ReturnType);
			this.region = Class.GetRegion();
			this.bodyRegion = Class.GetRegion();
			this._member = null;
			    
			// Add parameters
			foreach (SR.ParameterInfo pinfo in tinfo.GetParameters())
			    parameters.Add(new Parameter(this, pinfo));
		}
		
		public Method (IClass declaringType, MethodInfo tinfo)
		{
		    this.declaringType = declaringType;
		
		    ModifierEnum mod = (ModifierEnum)0;
            if (tinfo.IsPrivate)
                mod |= ModifierEnum.Private;
            if (tinfo.IsInternal)
                mod |= ModifierEnum.Internal;
            if (tinfo.IsProtected)
                mod |= ModifierEnum.Protected;
            if (tinfo.IsPublic)
                mod |= ModifierEnum.Public;
            if (tinfo.IsAbstract)
                mod |= ModifierEnum.Abstract;
            if (tinfo.IsFinal)
                mod |= ModifierEnum.Sealed;
            if (tinfo.IsStatic)
                mod |= ModifierEnum.Static;
            if (tinfo.IsOverride)
                mod |= ModifierEnum.Override;
            if (tinfo.IsVirtual)
                mod |= ModifierEnum.Virtual;
            if (tinfo.IsNew)
                mod |= ModifierEnum.New;
            if (tinfo.IsExtern)
                mod |= ModifierEnum.Extern;
                
			modifiers = mod;
			
			this.FullyQualifiedName = tinfo.Name;
			
			if (tinfo.Name == "op_Addition") this.FullyQualifiedName = "@+";
			else if (tinfo.Name == "op_Subtraction") this.FullyQualifiedName = "@-";
			else if (tinfo.Name == "op_Multiply") this.FullyQualifiedName = "@*";
			else if (tinfo.Name == "op_Division") this.FullyQualifiedName = "@/";
			else if (tinfo.Name == "op_Modulus") this.FullyQualifiedName = "@%";
			else if (tinfo.Name == "op_ExclusiveOr") this.FullyQualifiedName = "@^";
			else if (tinfo.Name == "op_BitwiseAnd") this.FullyQualifiedName = "@&";
			else if (tinfo.Name == "op_BitwiseOr") this.FullyQualifiedName = "@|";
			else if (tinfo.Name == "op_LogicalAnd") this.FullyQualifiedName = "@&&";
			else if (tinfo.Name == "op_LogicalOr") this.FullyQualifiedName = "@||";
			else if (tinfo.Name == "op_Assign") this.FullyQualifiedName = "@=";
			else if (tinfo.Name == "op_LeftShift") this.FullyQualifiedName = "@<<";
			else if (tinfo.Name == "op_RightShift") this.FullyQualifiedName = "@>>";
			else if (tinfo.Name == "op_Equality") this.FullyQualifiedName = "@==";
			else if (tinfo.Name == "op_GreaterThan") this.FullyQualifiedName = "@>";
			else if (tinfo.Name == "op_LessThan") this.FullyQualifiedName = "@<";
			else if (tinfo.Name == "op_Inequality") this.FullyQualifiedName = "@!=";
			else if (tinfo.Name == "op_GreaterThanOrEqual") this.FullyQualifiedName = "@>=";
			else if (tinfo.Name == "op_LessThanOrEqual") this.FullyQualifiedName = "@<=";
			else if (tinfo.Name == "op_MultiplicationAssignment") this.FullyQualifiedName = "@*=";
			else if (tinfo.Name == "op_SubtractionAssignment") this.FullyQualifiedName = "@-=";
			else if (tinfo.Name == "op_ExclusiveOrAssignment") this.FullyQualifiedName = "@^=";
			else if (tinfo.Name == "op_LeftShiftAssignment") this.FullyQualifiedName = "@<<=";
			else if (tinfo.Name == "op_ModulusAssignment") this.FullyQualifiedName = "@%=";
			else if (tinfo.Name == "op_AdditionAssignment") this.FullyQualifiedName = "@+=";
			else if (tinfo.Name == "op_BitwiseAndAssignment") this.FullyQualifiedName = "@&=";
			else if (tinfo.Name == "op_BitwiseOrAssignment") this.FullyQualifiedName = "@|=";
			else if (tinfo.Name == "op_Comma") this.FullyQualifiedName = "@,";
			else if (tinfo.Name == "op_DivisionAssignment") this.FullyQualifiedName = "@/=";
			else if (tinfo.Name == "op_Implicit") this.FullyQualifiedName = "@:";
			else if (tinfo.Name == "op_Explicit") this.FullyQualifiedName = "@:>";
			else if (tinfo.Name == "op_UnaryPlus") this.FullyQualifiedName = "@+";
			else if (tinfo.Name == "op_UnaryNegation") this.FullyQualifiedName = "@-";
			else if (tinfo.Name == "op_Decrement") this.FullyQualifiedName = "@--";
			else if (tinfo.Name == "op_Increment") this.FullyQualifiedName = "@++";
			else if (tinfo.Name == "op_OnesComplement") this.FullyQualifiedName = "@~";
			else if (tinfo.Name == "op_LogicalNot") this.FullyQualifiedName = "@!";	
			
			returnType = new ReturnType(tinfo.ReturnType);
			this.region = Class.GetRegion(tinfo.Location);
			this.bodyRegion = Class.GetRegion(tinfo.Location);
			this._member = tinfo;
			    
			// Add parameters
			foreach (ParameterInfo pinfo in tinfo.Parameters)
			    parameters.Add(new Parameter(this, pinfo));
		}
	}
}
