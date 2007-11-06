//  Parser.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Andrea Paatz <andrea@icsharpcode.net>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
using System;
using System.Text;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects;
using NemerleBinding.Parser.SharpDevelopTree;
using Nemerle.Completion;
using NCC = Nemerle.Compiler;
using System.Xml;

namespace NemerleBinding.Parser
{
    public class TParser : IParser
    {
        Engine engine;
        internal static Dictionary<string, XmlDocument> xmlCache;
        
        public TParser ()
        {
            lock (syncObject)
            {
                engine = new Engine ();
                xmlCache = new Dictionary<string, XmlDocument> ();
            }
        }
        
        ///<summary>IParser Interface</summary> 
        string[] lexerTags;
        public string[] LexerTags {
            get {
                return lexerTags;
            }
            set {
                lexerTags = value;
            }
        }
        
        public IExpressionFinder CreateExpressionFinder(string fileName)
        {
            return new ExpressionFinder(fileName);
        }
        
        public bool CanParse(string fileName)
        {
            return System.IO.Path.GetExtension(fileName).ToUpper() == ".N";
        }
        
        private void ReloadReferences ()
        {
            Project currentProj = IdeApp.ProjectOperations.CurrentSelectedProject;
            foreach (ProjectReference refer in currentProj.ProjectReferences)
            {
                if (!engine.References.ContainsKey (refer.Reference))
                    engine.References.Add (refer.Reference, refer.Reference);
            }
            foreach (string s in engine.References.GetKeys ())
            {
                bool deleteReference = true;
                foreach (ProjectReference refer in currentProj.ProjectReferences)
                {
                    if (refer.Reference == s)
                    {
                        deleteReference = false;
                        break;
                    }
                }
                if (deleteReference)
                    engine.References.Remove (s);
            }
        }
        
        private void ReloadFiles (string reload, string newContents)
        {
            Project currentProj = IdeApp.ProjectOperations.CurrentSelectedProject;
            foreach (ProjectFile file in currentProj.ProjectFiles)
            {
                if (file.Name.EndsWith (".N") || file.Name.EndsWith (".n"))
                {
                    if (!engine.Sources.ContainsKey (file.Name) ||
                        file.Name == reload)
                        engine.Sources.Add (file.Name, newContents ?? file.Data);
                }
            }
            foreach (string s in engine.Sources.GetKeys ())
            {
                bool deleteFile = true;
                foreach (ProjectFile file in currentProj.ProjectFiles)
                {
                    if (file.Name == s)
                    {
                        deleteFile = false;
                        break;
                    }
                }
                if (deleteFile)
                    engine.Sources.Remove (s);
            }
        }
        
        static object syncObject = new object ();
        DefaultCompilationUnit cu;
        private ICompilationUnitBase parse_the_file (string fileName, string contents)
        {
            if (IdeApp.ProjectOperations.CurrentSelectedProject == null)
                return null;
            
            lock (syncObject)
            {
                try
                {
                    ReloadReferences ();
                    ReloadFiles (fileName, contents);
                    
                    CompletionStageHandler handler = new CompletionStageHandler (make_type);
                    cu = new DefaultCompilationUnit ();
                    engine.GetTypeTree (handler);
                    return cu;
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine ("ERROR");
                    System.Console.WriteLine (ex.Message);
                    System.Console.WriteLine (ex.StackTrace);
                    return null;
                }
            }
        }

        public ICompilationUnitBase Parse(string fileName)
        {
            return parse_the_file (fileName, null);
        }
        
        public ICompilationUnitBase Parse(string fileName, string fileContent)
        {            
            return parse_the_file (fileName, fileContent);
        }
        
        void make_type (NCC.TypeInfo ti)
        {
            cu.Classes.Add (new Class (ti, cu));
        }
        
        public LanguageItemCollection CtrlSpace(IParserContext parserContext, int caretLine, int caretColumn, string fileName)
        {
            Project currentProj = IdeApp.ProjectOperations.CurrentSelectedProject;
            string fileContent = "";
            foreach (ProjectFile file in currentProj.ProjectFiles)
            {
                if (file.Name == fileName)
                {
                    fileContent = file.Data;
                    break;
                }
            }
            ResolveResult res = real_resolve (parserContext, caretLine, caretColumn, fileName, fileContent, true);
            return res.Members;
        }

        public LanguageItemCollection IsAsResolve (IParserContext parserContext, string expression, int caretLineNumber, int caretColumn, string fileName, string fileContent)
        {
            return null;
        }

        public ResolveResult Resolve (IParserContext parserContext, string expression, int caretLineNumber, int caretColumn, string fileName, string fileContent)
        {
            return real_resolve (parserContext, caretLineNumber, caretColumn, fileName, fileContent, false);
        }
        
        public ResolveResult real_resolve (IParserContext parserContext, int caretLineNumber, int caretColumn, string fileName, string fileContent, bool completeLocals)
        {
            try
            {
                DefaultCompilationUnit comp = (DefaultCompilationUnit)parserContext.GetParseInformation (fileName).MostRecentCompilationUnit;
                Class the_class = null;
                foreach (DefaultClass cl in comp.Classes)
                {
                    if (cl.BodyRegion.BeginLine <= caretLineNumber &&
                        cl.BodyRegion.EndLine >= caretLineNumber)
                    {
                        the_class = (Class)cl;
                    }
                    foreach (DefaultClass nc in cl.InnerClasses)
                    {
                        if (nc.BodyRegion.BeginLine <= caretLineNumber &&
                            nc.BodyRegion.EndLine >= caretLineNumber)
                        {
                            the_class = (Class)nc;
                        } 
                    }
                    if (the_class != null) break;
                }
                
                if (the_class == null)
                    return null;
                else
                {
                    INemerleMethod the_method = null;
                    int line = 0, column = 0, end_line = 0, end_column = 0;
                    foreach (DefaultMethod m in the_class.Methods)
                    {
                        if (m.BodyRegion.BeginLine <= caretLineNumber &&
                            m.BodyRegion.EndLine >= caretLineNumber &&
                            m.BodyRegion.BeginLine != the_class.BodyRegion.BeginLine)
                        {
                            the_method = (INemerleMethod)m;
                            line = m.BodyRegion.BeginLine;
                            column = m.BodyRegion.BeginColumn;
                            end_line = m.BodyRegion.EndLine;
                            end_column = m.BodyRegion.EndColumn;
                            break;
                        }
                    }
                    
                    if (the_method == null)
                    {
                        // Try with properties
                        foreach (Property p in the_class.Properties)
                        {
                            if (p.GetterRegion != null)
                            {
                                if (p.GetterRegion.BeginLine <= caretLineNumber &&
                                    p.GetterRegion.EndLine >= caretLineNumber)
                                {
                                    the_method = (INemerleMethod)p.Getter;
                                    line = p.GetterRegion.BeginLine;
                                    column = p.GetterRegion.BeginColumn;
                                    end_line = p.BodyRegion.EndLine;
                                    end_column = p.BodyRegion.EndColumn;
                                    break;
                                }
                            }
                            
                            if (p.SetterRegion != null)
                            {
                                if (p.SetterRegion.BeginLine <= caretLineNumber &&
                                    p.SetterRegion.EndLine >= caretLineNumber)
                                {
                                    the_method = (INemerleMethod)p.Setter;
                                    line = p.SetterRegion.BeginLine;
                                    column = p.SetterRegion.BeginColumn;
                                    end_line = p.BodyRegion.EndLine;
                                    end_column = p.BodyRegion.EndColumn;
                                    break;
                                }
                            }
                        }
                        
                        foreach (Indexer p in the_class.Indexer)
                        {
                            if (p.GetterRegion != null)
                            {
                                if (p.GetterRegion.BeginLine <= caretLineNumber &&
                                    p.GetterRegion.EndLine >= caretLineNumber)
                                {
                                    the_method = (INemerleMethod)p.Getter;
                                    line = p.GetterRegion.BeginLine;
                                    column = p.GetterRegion.BeginColumn;
                                    end_line = p.BodyRegion.EndLine;
                                    end_column = p.BodyRegion.EndColumn;
                                    break;
                                }
                            }
                            
                            if (p.SetterRegion != null)
                            {
                                if (p.SetterRegion.BeginLine <= caretLineNumber &&
                                    p.SetterRegion.EndLine >= caretLineNumber)
                                {
                                    the_method = (INemerleMethod)p.Setter;
                                    line = p.SetterRegion.BeginLine;
                                    column = p.SetterRegion.BeginColumn;
                                    end_line = p.BodyRegion.EndLine;
                                    end_column = p.BodyRegion.EndColumn;
                                    break;
                                }
                            }
                        }
                    }
                    
                    if (the_method == null)
                        return null;
                    else
                    {   
                        // Recover the text from the start of the method to cursor
                        string method_start = Crop (fileContent, line, column, caretLineNumber, caretColumn);
                        string method_end = Crop (fileContent, caretLineNumber, caretColumn, end_line, end_column) + "}";
                        // System.Console.WriteLine (method_start + method_end);
                        NCC.CompletionResult infox = engine.RunCompletionEngine ((NCC.MethodBuilder)the_method.Member,
                            method_start + method_end, method_start.Length);
                        
                        return GetResults (infox, comp, completeLocals);                        
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine (ex.Message);
                Console.WriteLine (ex.StackTrace);
                return null;
            }
        }
        
        ResolveResult GetResults (NCC.CompletionResult results, DefaultCompilationUnit cu, bool completeLocals)
        {
            try
            {
            if (results == null || results.Elems.Count == 0)
                return null;
                        
            if (results.Elems [0] is NCC.Elem.Node)
            {
                List<string> alreadyAdded = new List<string> ();
                List<string> namespaces = new List<string> ();
                LanguageItemCollection lang = new LanguageItemCollection ();
                
                foreach (NCC.Elem elem in results.Elems)
                {
                    if (!(elem is NCC.Elem.Node))
                        continue;
                    
                    NCC.Elem.Node enode = (NCC.Elem.Node)elem;
                    if (enode.node.Value is NCC.NamespaceTree.TypeInfoCache.NamespaceReference)
                    {
                        namespaces.Add (enode.Name);
                    }
                    else if (enode.node.Value is NCC.NamespaceTree.TypeInfoCache.Cached)
                    {
                        if (!alreadyAdded.Contains (enode.Name))
                        {
                            alreadyAdded.Add (enode.Name);
                            lang.Add (new Class (((NCC.NamespaceTree.TypeInfoCache.Cached)enode.node.Value).tycon, cu, false));
                        }
                    }
                }
                return new ResolveResult (namespaces.ToArray (), lang);
            }
            else
            {
                Class declaring = GetTheRealType (results.ObjectType, cu);
                
                /*if (declaring.FullyQualifiedName == "System.Object")
                {
                    // Try with any other member
                    NCC.TypeInfo found = null;
                    foreach (NCC.OverloadPossibility ov in results.Overloads)
                    {
                        if (ov.Member.DeclaringType.FrameworkTypeName != "System.Object")
                        {
                            found = ov.Member.DeclaringType;
                            break;
                        }
                    }
                    if (found != null)
                        declaring = new Class (found, cu, false);
                }*/
                
                LanguageItemCollection lang = new LanguageItemCollection ();
                
                foreach (NCC.Elem elem in results.Elems)
                {
                    if (elem is NCC.Elem.Local)
                    {
                        if (!completeLocals)
                            continue;
                        
                        NCC.Elem.Local lvalue = (NCC.Elem.Local)elem;
/*                        lang.Add (new NemerleBinding.Parser.SharpDevelopTree.Local 
                            (new Class ("LOCALS", cu), lvalue.Value));
*/                    }
                    else if (elem is NCC.Elem.Overloads)
                    {
                        NCC.Elem.Overloads lvalue = (NCC.Elem.Overloads)elem;
                        foreach (NCC.OverloadPossibility ov in lvalue.Values)
                            AddMember (declaring, lang, ov.Member); 
                    }
                    else if (elem is NCC.Elem.Overload)
                    {
                        NCC.Elem.Overload lvalue = (NCC.Elem.Overload)elem;
                        AddMember (declaring, lang, lvalue.Value.Member);
                    }
                    else if (elem is NCC.Elem.Member)
                    {
                        NCC.Elem.Member lvalue = (NCC.Elem.Member)elem;
                        AddMember (declaring, lang, lvalue.member);
                    }
                }
                
                return new ResolveResult (declaring, lang);
            }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine (ex.GetType().FullName);
                System.Console.WriteLine (ex.Message);
                System.Console.WriteLine (ex.StackTrace);
                if (ex.InnerException != null)
                {
                    System.Console.WriteLine (ex.InnerException.GetType().FullName);
                    System.Console.WriteLine (ex.InnerException.Message);
                    System.Console.WriteLine (ex.InnerException.StackTrace);
                }
                return null;
            }
        }
        
        private void AddMember (Class declaring, LanguageItemCollection lang, NCC.IMember member)
        {
            // Do not add property getters and setters, not events adders and removers,
            // nor overloaded operators, nor enum value__, not Nemerle internal methods
            if (member.Name.StartsWith("_N") || member.Name.StartsWith("get_") ||
                member.Name.StartsWith("set_") || member.Name == "value__" ||
                member.Name.StartsWith("op_") || member.Name.StartsWith("add_") ||
                member.Name.StartsWith("remove_"))
                return;
                  
            try
            {
                if (member is NCC.IField)
                    lang.Add (new NemerleBinding.Parser.SharpDevelopTree.Field (declaring, (NCC.IField)member));
                else if (member is NCC.IMethod)
                    lang.Add (new Method (declaring, (NCC.IMethod)member));
                else if (member is NCC.IProperty)
                {
                    NCC.IProperty prop = (NCC.IProperty)member;
                    if (prop.IsIndexer)
                        lang.Add (new Indexer (declaring, prop));
                    else
                        lang.Add (new Property (declaring, prop));
                }
                else if (member is NCC.IEvent)
                    lang.Add (new Event (declaring, (NCC.IEvent)member));
            }
            catch (Exception e)
            {
                System.Console.WriteLine (e.Message);
            }
        }
        
        private Class GetTheRealType (NCC.MType objectType, DefaultCompilationUnit cu)
        {
            if (objectType is NCC.MType.Class)
            {
                return new Class (((NCC.MType.Class)objectType).tycon, cu, false);
            }
            else if (objectType is NCC.MType.Array)
            {
                return new Class ("System.Array", cu);
            }
            else if (objectType is NCC.MType.Fun)
            {
                return GetTheRealType (((NCC.MType.Fun)objectType).to.Fix (), cu);
            }
            else if (objectType is NCC.MType.Ref)
            {
                return GetTheRealType (((NCC.MType.Ref)objectType).t.Fix (), cu);
            }
            else if (objectType is NCC.MType.Out)
            {
                return GetTheRealType (((NCC.MType.Out)objectType).t.Fix (), cu);
            }
            else
            {
                return null;
            }
        }
        
        public ILanguageItem ResolveIdentifier (IParserContext parserContext, string id, int caretLineNumber, int caretColumn, string fileName, string fileContent)
        {
            return null;
        }
        
        string Crop (string content, int startLine, int startColumn, int endLine, int endColumn)
        {
            string[] lines = content.Split ('\n');
            StringBuilder sb = new StringBuilder ();
            for (int i = startLine - 1; i < endLine; i++)
            {
                if (i == (startLine - 1) && i == (endLine - 1))
                {
                    sb.Append (lines[i].Substring (startColumn - 1, endColumn - startColumn));
                    break;
                }
                else if (i == (startLine - 1))
                    sb.Append (lines[i].Substring (startColumn - 1) + "\n");
                else if (i == (endLine - 1) || i >= lines.Length)
                {
                    sb.Append (lines[i].Substring (0, endColumn - 1));
                    break;
                }
                else
                    sb.Append (lines[i] + "\n");
            }
            return sb.ToString (); //.TrimStart (' ', '{');
        }
        
        ///////// IParser Interface END
    }
}
