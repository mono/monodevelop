// created on 06.08.2003 at 12:37

using System;
using System.Diagnostics;
using System.Collections;
using SR = System.Reflection;

using MonoDevelop.Projects.Parser;
using Nemerle.Completion;

namespace NemerleBinding.Parser.SharpDevelopTree
{
	public class Class : AbstractClass
	{
		ICompilationUnit cu;
        DeclaredTypeInfo tinfo;
        
        public Class(string name)
        {
            this.FullyQualifiedName = name;
            this.modifiers = (ModifierEnum)0;
        }
        
        public Class(Type tinfo, CompilationUnit cu)
        {
            this.cu = cu;
            this.tinfo = null;
            this.FullyQualifiedName = tinfo.FullName.TrimEnd('*').TrimEnd ('1', '2', '3', '4').TrimEnd('`');
            
            if (tinfo.IsEnum)
                classType = ClassType.Enum;
            else if (tinfo.IsInterface)
                classType = ClassType.Interface;
            else if (tinfo.IsValueType)
                classType = ClassType.Struct;
            else if (tinfo.IsSubclassOf(typeof(System.Delegate)) ||
                tinfo.IsSubclassOf(typeof(System.MulticastDelegate)))
                classType = ClassType.Delegate;
            else
                classType = ClassType.Class;
                
            this.region = GetRegion ();
			this.bodyRegion = GetRegion ();
            
            ModifierEnum mod = (ModifierEnum)0;
            if (tinfo.IsNotPublic)
                mod |= ModifierEnum.Private;
            if (tinfo.IsPublic)
                mod |= ModifierEnum.Public;
            if (tinfo.IsAbstract)
                mod |= ModifierEnum.Abstract;
            if (tinfo.IsSealed)
                mod |= ModifierEnum.Sealed;
            
			modifiers = mod;
			
			if (tinfo.IsEnum)
            {
                foreach (SR.FieldInfo field in tinfo.GetFields())
                {
                    if (field.Name != "value__" && !field.Name.StartsWith("_N"))
                        fields.Add (new Field (this, field));
                }
            }
            else
            {
                foreach (SR.FieldInfo field in tinfo.GetFields())
                {
                    if (!field.Name.StartsWith("_N"))
                        fields.Add (new Field (this, field));
                }
            }
            foreach (SR.MethodInfo method in tinfo.GetMethods())
            {
                if (method.Name.StartsWith("_N") || method.Name.StartsWith("get_") || method.Name.StartsWith("set_") ||
                    method.Name.StartsWith("add_") || method.Name.StartsWith("remove_"))
                    continue;
                if (method.IsConstructor)
                    continue; //methods.Add (new Constructor (this, method));
                else
                    methods.Add (new Method (this, method));
            }
            foreach (SR.PropertyInfo prop in tinfo.GetProperties())
            {
                properties.Add (new Property (this, prop));
            }
            foreach (SR.EventInfo ev in tinfo.GetEvents())
            {
                events.Add (new Event (this, ev));
            }
            
            foreach (Type i in tinfo.GetNestedTypes())
            {
                Class nested = new Class (i, cu);
                innerClasses.Add (nested);
            }
        }
		
		public Class(DeclaredTypeInfo tinfo, CompilationUnit cu)
		{
            this.cu = cu;
            this.tinfo = tinfo;
            
            if (tinfo.IsNested)
                this.FullyQualifiedName = tinfo.Namespace + "." + 
                    tinfo.DeclaringType.Name + "." + tinfo.Name.TrimEnd('*').TrimEnd ('1', '2', '3', '4').TrimEnd('`');
            else
                this.FullyQualifiedName = tinfo.Namespace + "." + tinfo.Name.TrimEnd('*').TrimEnd ('1', '2', '3', '4').TrimEnd('`');
            
            if (tinfo.IsEnum)
                classType = ClassType.Enum;
            else if (tinfo.IsInterface)
                classType = ClassType.Interface;
            else if (tinfo.IsStruct)
                classType = ClassType.Struct;
            else if (tinfo.IsDelegate)
                classType = ClassType.Delegate;
            else
                classType = ClassType.Class;
            
			this.region = GetRegion (tinfo.Location);
			this.bodyRegion = GetRegion (tinfo.Location);
            
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
            if (tinfo.IsSealed)
                mod |= ModifierEnum.Sealed;
            
			modifiers = mod;
            
            // Add members
            if (tinfo.IsEnum)
            {
                foreach (FieldInfo field in tinfo.Fields)
                {
                    if (field.Name != "value__" && !field.Name.StartsWith("_N") &&
                        field.Location.Line != tinfo.Location.Line)
                        fields.Add (new Field (this, field));
                }
            }
            else
            {
                foreach (FieldInfo field in tinfo.Fields)
                {
                    if (!field.Name.StartsWith("_N")&&
                        field.Location.Line != tinfo.Location.Line)
                        fields.Add (new Field (this, field));
                }
            }
            foreach (MethodInfo method in tinfo.Methods)
            {
                if (method.Name.StartsWith("_N") || method.Name.StartsWith("get_") || method.Name.StartsWith("set_") ||
                    method.Location.Line == tinfo.Location.Line || method.Name.StartsWith("add_") || method.Name.StartsWith("remove_"))
                    continue;
                if (method.IsConstructor || method.IsStaticConstructor)
                    methods.Add (new Constructor (this, method));
                else
                    methods.Add (new Method (this, method));
            }
            foreach (PropertyInfo prop in tinfo.Properties)
            {
                if (prop.Location.Line != tinfo.Location.Line)
                    properties.Add (new Property (this, prop));
            }
            foreach (EventInfo ev in tinfo.Events)
            {
                if (ev.Location.Line != tinfo.Location.Line)
                    events.Add (new Event (this, ev));
            }
            
            ArrayList typex = new ArrayList();
            foreach (DeclaredTypeInfo var in tinfo.VariantOptions)
            {
                if (!typex.Contains (var.Name))
                {
                    Class nested = new Class (var, cu);
                    innerClasses.Add (nested);
                    typex.Add (var.Name);
                }
            }
            foreach (DeclaredTypeInfo i in tinfo.NestedTypes)
            {
                if (!typex.Contains (i.Name))
                {
                    Class nested = new Class (i, cu);
                    innerClasses.Add (nested);
                    typex.Add (i.Name);
                }
            }        
		}
		
        public static DefaultRegion GetRegion (CodeLocation cloc)
        {
            try
            {
                DefaultRegion reg = new DefaultRegion (cloc.Line, cloc.Column,
                    cloc.EndLine, cloc.EndColumn);
                reg.FileName = cloc.Filename;
                return reg;
            }
            catch
            {
                DefaultRegion rd = new DefaultRegion (0, 0, 0, 0);
                rd.FileName = "";
                return rd;
            }
        }
        
        public static DefaultRegion GetRegion ()
        {
            DefaultRegion rd = new DefaultRegion (0, 0, 0, 0);
            rd.FileName = "";
            return rd;
        }
        
        public override ICompilationUnit CompilationUnit
        {
            get { return cu; }
        }
	}
}
