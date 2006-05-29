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
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects;
using NemerleBinding.Parser.SharpDevelopTree;
using Nemerle.Completion;

namespace NemerleBinding.Parser
{
	public class TParser : IParser
	{
	    Engine engine;
	    
	    public TParser ()
	    {
	        Console.WriteLine ("hey");
	        engine = new Engine ();
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

		public ICompilationUnitBase Parse(string fileName)
		{
		    try
            {
                    Project currentProj = IdeApp.ProjectOperations.CurrentSelectedProject;
                
                    engine.References.Clear ();
                    foreach (ProjectReference refer in currentProj.ProjectReferences)
                        if (refer.ReferenceType != ReferenceType.Project)
                            engine.References.Add (refer.Reference, refer.Reference);
                
                    engine.Sources.Clear ();
                    foreach (ProjectFile file in currentProj.ProjectFiles)
                        if (file.Name.ToUpper().EndsWith(".N"))
                            engine.Sources.Add (file.Name, file.Data);
                
                    TypeTree tree = engine.GetTypeTree();
                    return CUFromTree (tree);
            }
            catch
            {
                return null;
            }
		}
		
		public ICompilationUnitBase Parse(string fileName, string fileContent)
		{            
            try
            {
                    Project currentProj = IdeApp.ProjectOperations.CurrentSelectedProject;
                
                    engine.References.Clear ();
                    foreach (ProjectReference refer in currentProj.ProjectReferences)
                        if (refer.ReferenceType != ReferenceType.Project)
                            engine.References.Add (refer.Reference, refer.Reference);
                
                    engine.Sources.Clear ();
                    bool alreadyAddedFile = false;
                    foreach (ProjectFile file in currentProj.ProjectFiles)
                    {
                        if (file.Name.ToUpper().EndsWith(".N"))
                        {
                            if (file.Name == fileName)
                            {
                                engine.Sources.Add (fileName, fileContent);
                                alreadyAddedFile = true;
                            }
                            else
                                engine.Sources.Add (file.Name, file.Data);
                        }
                    }
                
                    if (!alreadyAddedFile)
                        engine.Sources.Add (fileName, fileContent);
    
                    TypeTree tree = engine.GetTypeTree();
                    return CUFromTree (tree);
            }
            catch 
            {
                return null;
            }
        }

        ICompilationUnitBase CUFromTree (TypeTree tree)
        {
            CompilationUnit cu = new CompilationUnit();
            foreach (DeclaredTypeInfo tinfo in tree.Types)
                cu.Classes.Add (new Class (tinfo, cu));
            
            return cu;
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
                ICompilationUnit comp = (ICompilationUnit)parserContext.GetParseInformation (fileName).MostRecentCompilationUnit;
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
                    }
                    
                    if (the_method == null)
                        return null;
                    else
                    {
                        // Recover the text from the start of the method to cursor
                        string method_content = Crop (fileContent, line, column, caretLineNumber, caretColumn);
                        CompletionInfo infox = engine.RunCompletionEngine (the_method.Member, method_content);
                        
                        if (infox.CompletionKind == CompletionKind.Members)
                        {
                            CompletionMembers the_members = (CompletionMembers)infox;
                            Class add_class = null;
                            CompilationUnit cu = new CompilationUnit();
                            
                            LanguageItemCollection lang = new LanguageItemCollection ();
                            foreach (NemerleMemberInfo memb in the_members.Members)
                            {
                                if (memb.Name.StartsWith("_N") || memb.Name.StartsWith("get_") ||
                                    memb.Name.StartsWith("set_") || memb.Name == "value__" ||
                                    memb.Name.StartsWith("op_") || memb.Name.StartsWith("add_") ||
                                    memb.Name.StartsWith("remove_"))
                                    continue;
                                    
                                FieldInfo fi = memb as FieldInfo;
                                MethodInfo mi = memb as MethodInfo;
                                EventInfo ei = memb as EventInfo;
                                PropertyInfo pi = memb as PropertyInfo;
                                if (fi != null)
                                {
                                    Field fx = new Field (add_class, fi);
                                    lang.Add (fx);
                                }
                                else if (mi != null)
                                {
                                    Method mx = new Method (add_class, mi);
                                    lang.Add (mx);
                                }
                                else if (ei != null)
                                {
                                    Event ex = new Event (add_class, ei);
                                    lang.Add (ex);
                                }
                                else if (pi != null)
                                {
                                    Property px = new Property (add_class, pi);
                                    lang.Add (px);
                                }
                            }
                            
                            return new ResolveResult ((Class)null, lang);
                        }
                        else
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
                        }
                        
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
