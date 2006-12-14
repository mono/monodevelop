// created on 06.08.2003 at 12:37

using System;
using System.Diagnostics;
using System.Collections;
using SR = System.Reflection;
using System.Collections.Generic;

using MonoDevelop.Projects;
using System.Xml;

using MonoDevelop.Projects.Parser;
using Nemerle.Completion;
using NCC = Nemerle.Compiler;
using NemerleBinding.Parser;

namespace NemerleBinding.Parser.SharpDevelopTree
{
    public class Class : DefaultClass
    {
        NCC.TypeInfo tinfo;
        internal XmlDocument xmlHelp;
        
        public Class(string name, DefaultCompilationUnit cu): base (cu)
        {
            this.FullyQualifiedName = name;
            this.modifiers = (ModifierEnum)0;
        }
        
        void LoadXml ()
        {
            if (TParser.xmlCache.ContainsKey (this.FullyQualifiedName))
                xmlHelp = TParser.xmlCache [this.FullyQualifiedName];
            else
            {
                xmlHelp = Services.DocumentationService != null ?
                    Services.DocumentationService.GetHelpXml (this.FullyQualifiedName) : null;
                if (xmlHelp != null)
                {
                    TParser.xmlCache.Add (this.FullyQualifiedName, xmlHelp);
                    XmlNode node = xmlHelp.SelectSingleNode ("/Type/Docs/summary");
                    if (node != null)
                    {
                        this.Documentation = node.InnerXml;
                    }
                }
            } 
        }
        
        public Class(System.Type tinfo, DefaultCompilationUnit cu): base (cu)
        {
            this.tinfo = null;
            this.FullyQualifiedName = tinfo.FullName.TrimEnd('*');
            if (this.FullyQualifiedName.Contains("`"))
                this.FullyQualifiedName = this.FullyQualifiedName.TrimEnd ('1', '2', '3', '4').TrimEnd('`');
            
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
            
            foreach (System.Type i in tinfo.GetNestedTypes())
            {
                Class nested = new Class (i, cu);
                innerClasses.Add (nested);
            }
            
            LoadXml ();
        }
        
        public Class(NCC.TypeInfo tinfo, DefaultCompilationUnit cu)
          : this (tinfo, cu, true)
        { }
        
        public Class(NCC.TypeInfo tinfo, DefaultCompilationUnit cu, bool addMembers): base (cu)
        {
            this.tinfo = tinfo;
            
            this.FullyQualifiedName = tinfo.FrameworkTypeName.TrimEnd('*');
            if (this.FullyQualifiedName.Contains("`"))
                this.FullyQualifiedName = this.FullyQualifiedName.TrimEnd ('1', '2', '3', '4').TrimEnd('`');

            
            if (tinfo.IsEnum)
                classType = ClassType.Enum;
            else if (tinfo.IsInterface)
                classType = ClassType.Interface;
            else if (tinfo.IsValueType)
                classType = ClassType.Struct;
            else if (tinfo.IsDelegate)
                classType = ClassType.Delegate;
            else
                classType = ClassType.Class;
            
            this.region = GetRegion (tinfo.Location);
            this.bodyRegion = GetRegion (tinfo.Location);
            
            ModifierEnum mod = (ModifierEnum)0;
            if ((tinfo.Attributes & NCC.NemerleAttributes.Private) != 0)
                mod |= ModifierEnum.Private;
            if ((tinfo.Attributes & NCC.NemerleAttributes.Internal) != 0)
                mod |= ModifierEnum.Internal;
            if ((tinfo.Attributes & NCC.NemerleAttributes.Protected) != 0)
                mod |= ModifierEnum.Protected;
            if ((tinfo.Attributes & NCC.NemerleAttributes.Public) != 0)
                mod |= ModifierEnum.Public;
            if ((tinfo.Attributes & NCC.NemerleAttributes.Abstract) != 0)
                mod |= ModifierEnum.Abstract;
            if ((tinfo.Attributes & NCC.NemerleAttributes.Sealed) != 0)
                mod |= ModifierEnum.Sealed;
                
            modifiers = mod;
            
            if (tinfo.Typarms.Length > 0)
            {
                this.genericParamters = new GenericParameterList ();
                foreach (NCC.StaticTyVar typarm in tinfo.Typarms)
                {
                    genericParamters.Add (GetGenericParameter (typarm));
                }
            }
            
            if (addMembers || tinfo.IsDelegate)
            {
                foreach (NCC.IMember member in tinfo.GetMembers ())
                {
                    if (member.Name.StartsWith ("_N") || member.Location.Line == tinfo.Location.Line)
                        continue;
                        
                    NCC.MemberKind m = member.GetKind ();
                    
                    if (m is NCC.MemberKind.Field)
                    {
                        NCC.MemberKind.Field f = (NCC.MemberKind.Field)m;
                        if (f.field.Name != "value__")
                            fields.Add (new Field (this, f.field));
                    }
                    else if (m is NCC.MemberKind.Method)
                    {
                        NCC.MemberKind.Method mt = (NCC.MemberKind.Method)m;
                        if (mt.method.Name.StartsWith ("get_") || mt.method.Name.StartsWith ("set_") || 
                            mt.method.Name.StartsWith ("add_") || mt.method.Name.StartsWith ("remove_"))
                            continue;
                        
                        NCC.FunKind fk = mt.method.GetFunKind ();
                        if (fk is NCC.FunKind.Constructor || fk is NCC.FunKind.StaticConstructor)
                            methods.Add (new Constructor (this, mt.method));
                        else
                            methods.Add (new Method (this, mt.method));
                    }
                    else if (m is NCC.MemberKind.Property)
                    {
                        NCC.MemberKind.Property px = (NCC.MemberKind.Property)m;
                        if (px.prop.IsIndexer)
                            indexer.Add (new Indexer (this, px.prop));
                        else
                            properties.Add (new Property (this, px.prop));
                    }
                    else if (m is NCC.MemberKind.Event)
                        events.Add (new Event (this, ((NCC.MemberKind.Event)m).body));
                    else if (m is NCC.MemberKind.Type)
                        innerClasses.Add (new Class ( ((NCC.MemberKind.Type)m).tycon, cu));
                }
            }
            
            foreach (NCC.MType.Class mt in tinfo.GetDirectSuperTypes ())
            {
                if (mt.tycon.FrameworkTypeName != "System.Object" &&
                    mt.tycon.FrameworkTypeName != "System.ValueType" &&
                    mt.tycon.FrameworkTypeName != "System.Enum" &&
                    mt.tycon.FrameworkTypeName != "System.Delegate" &&
                    mt.tycon.FrameworkTypeName != "System.MulticastDelegate")
                    baseTypes.Add (new ReturnType(mt));
            }
            
            LoadXml ();
        }
        
        public static DefaultRegion GetRegion (NCC.Location cloc)
        {
            try
            {
                DefaultRegion reg = new DefaultRegion (cloc.Line, cloc.Column,
                    cloc.EndLine, cloc.EndColumn);
                reg.FileName = cloc.File;
                return reg;
            }
            catch
            {
                return GetRegion ();
            }
        }
        
        public static DefaultRegion GetRegion ()
        {
            DefaultRegion rd = new DefaultRegion (0, 0, 0, 0);
            rd.FileName = "";
            return rd;
        }
        
        internal static GenericParameter GetGenericParameter (NCC.StaticTyVar tyvar)
        {
            ReturnTypeList constraints = new ReturnTypeList ();
            foreach (NCC.MType constraint in tyvar.Constraints)
                constraints.Add (new ReturnType (constraint));
                
            return new GenericParameter (tyvar.Name, constraints, tyvar.SpecialConstraints);
        }
    }
}
