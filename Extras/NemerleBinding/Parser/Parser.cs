// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Andrea Paatz" email="andrea@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
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

namespace NemerleBinding.Parser
{
    public class TParser : IParser
    {
        Engine engine;
        
        public TParser ()
        {
            lock (syncObject)
            {
                engine = new Engine ();
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
        CompilationUnit cu;
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
                    cu = new CompilationUnit ();
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
            return null;
        }

        public LanguageItemCollection IsAsResolve (IParserContext parserContext, string expression, int caretLineNumber, int caretColumn, string fileName, string fileContent)
        {
            return null;
        }
        
        public ResolveResult Resolve (IParserContext parserContext, string expression, int caretLineNumber, int caretColumn, string fileName, string fileContent)
        {
            try
            {
                CompilationUnit comp = (CompilationUnit)parserContext.GetParseInformation (fileName).MostRecentCompilationUnit;
                Class the_class = null;
                foreach (AbstractClass cl in comp.Classes)
                {
                    if (cl.BodyRegion.BeginLine <= caretLineNumber &&
                        cl.BodyRegion.EndLine >= caretLineNumber)
                    {
                        the_class = (Class)cl;
                    }
                    foreach (AbstractClass nc in cl.InnerClasses)
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
                    int line = 0, column = 0;
                    foreach (AbstractMethod m in the_class.Methods)
                    {
                        if (m.BodyRegion.BeginLine <= caretLineNumber &&
                            m.BodyRegion.EndLine >= caretLineNumber &&
                            m.BodyRegion.BeginLine != the_class.BodyRegion.BeginLine)
                        {
                            the_method = (INemerleMethod)m;
                            line = m.BodyRegion.BeginLine;
                            column = m.BodyRegion.BeginColumn;
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
                        string method_content = Crop (fileContent, line, column, caretLineNumber, caretColumn);
                        NCC.OverloadPossibility[] infox = engine.RunCompletionEngine (the_method.Member, method_content);
                        
                        return GetResults (infox, comp);

                        /*else
                        {
                            CompletionTypes the_types = (CompletionTypes)infox;
                            LanguageItemCollection lang = new LanguageItemCollection (); 
                            CompilationUnit cu = new CompilationUnit ();
                            
                            foreach (NemerleTypeInfo clasea in the_types.Types)
                            {
                                if (clasea.TypeKind == NemerleTypeKind.DeclaredType)
                                    lang.Add (new Class ((DeclaredTypeInfo)clasea, cu));
                                else
                                    lang.Add (new Class (((ReferencedTypeInfo)clasea).Type, cu));
                            } 
                            
                            return new ResolveResult (the_types.Namespaces, lang);
                        }*/
                        
                    }
                }
                
                return null;
            }
            catch(Exception ex)
            {
                Console.WriteLine (ex.Message);
                Console.WriteLine (ex.StackTrace);
                return null;
            }
            
            return null;
        }
        
        ResolveResult GetResults (NCC.OverloadPossibility[] ovs, CompilationUnit cu)
        {
            if (ovs.Length == 0)
                return null;
            
            bool complete_types = false;
            NCC.OverloadPossibility head = ovs[0];
            if (head.Member.Name == ".ctor" || head.Member.Name == ".cctor" || head.Member is NCC.TypeInfo)
                complete_types = true;
            
            if (complete_types)
            {
                NCC.NamespaceTree.Node nsNode = head.From.tycon.NamespaceNode.Parent;
                List<string> alreadyAdded = new List<string> ();
                List<string> namespaces = new List<string> ();
                LanguageItemCollection lang = new LanguageItemCollection ();
                foreach (KeyValuePair<string, NCC.NamespaceTree.Node> node in nsNode.Children)
                {
                    if (node.Value.Value is NCC.NamespaceTree.TypeInfoCache.NamespaceReference)
                    {
                        namespaces.Add (node.Key);
                    }
                    else if (node.Value.Value is NCC.NamespaceTree.TypeInfoCache.Cached)
                    {
                        if (!alreadyAdded.Contains (node.Key))
                        {
                            alreadyAdded.Add (node.Key);
                            lang.Add (new Class (((NCC.NamespaceTree.TypeInfoCache.Cached)node.Value.Value).tycon, cu));
                        }
                    }
                }
                
                return new ResolveResult (namespaces.ToArray (), lang);
            }
            else
            {
                LanguageItemCollection lang = new LanguageItemCollection ();
                foreach (NCC.OverloadPossibility ov in ovs)
                {
                    // Do not add property getters and setters, not events adders and removers,
                    // nor overloaded operators, nor enum value__, not Nemerle internal methods
                    if (ov.Member.Name.StartsWith("_N") || ov.Member.Name.StartsWith("get_") ||
                        ov.Member.Name.StartsWith("set_") || ov.Member.Name == "value__" ||
                        ov.Member.Name.StartsWith("op_") || ov.Member.Name.StartsWith("add_") ||
                        ov.Member.Name.StartsWith("remove_"))
                        continue;
                    
                    try
                    {
                        if (ov.Member is NCC.IField)
                        {
                            lang.Add (new Field (null, (NCC.IField)ov.Member));
                        }
                        else if (ov.Member is NCC.IMethod)
                        {
                            lang.Add (new Method (null, (NCC.IMethod)ov.Member));
                        }
                        else if (ov.Member is NCC.IProperty)
                        {
                            NCC.IProperty prop = (NCC.IProperty)ov.Member;
                            if (prop.IsIndexer)
                                lang.Add (new Indexer (null, prop));
                            else
                                lang.Add (new Property (null, prop));
                        }
                        else if (ov.Member is NCC.IEvent)
                        {
                            lang.Add (new Event (null, (NCC.IEvent)ov.Member));
                        }
                    }
                    catch (Exception e)
                    {
                        System.Console.WriteLine (e.Message);
                    }
                }
                return new ResolveResult ((Class)null, lang);
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
                    sb.Append (lines[i].Substring (startColumn - 1, endColumn - startColumn));
                else if (i == (startLine - 1))
                    sb.Append (lines[i].Substring (startColumn - 1) + "\n");
                else if (i == (endLine - 1))
                    sb.Append (lines[i].Substring (0, endColumn - 1));
                else
                    sb.Append (lines[i] + "\n");
            }
            return sb.ToString ().TrimStart (' ', '{');
        }
        
        ///////// IParser Interface END
    }
}
