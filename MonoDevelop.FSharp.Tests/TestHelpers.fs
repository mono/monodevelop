namespace MonoDevelopTests
open System
open System.IO
open System.Reflection
open System.Globalization
open Microsoft.FSharp.Reflection

module Reflection =   
  // Various flags configurations for Reflection
  let staticFlags = BindingFlags.NonPublic ||| BindingFlags.Public ||| BindingFlags.Static 
  let instanceFlags = BindingFlags.NonPublic ||| BindingFlags.Public ||| BindingFlags.Instance
  let ctorFlags = instanceFlags
  let inline asMethodBase(a:#MethodBase) = a :> MethodBase
  
  let (?) (o:obj) name : 'R =
    // The return type is a function, which means that we want to invoke a method
    if FSharpType.IsFunction(typeof<'R>) then
      let argType, resType = FSharpType.GetFunctionElements(typeof<'R>)
      FSharpValue.MakeFunction(typeof<'R>, fun args ->
        // We treat elements of a tuple passed as argument as a list of arguments
        // When the 'o' object is 'System.Type', we call static methods
        let methods, instance, args, owner = 
          let args = 
            if Object.Equals(argType, typeof<unit>) then [| |]
            elif not(FSharpType.IsTuple(argType)) then [| args |]
            else FSharpValue.GetTupleFields(args)
          if (typeof<System.Type>).IsAssignableFrom(o.GetType()) then 
            let methods = (unbox<Type> o).GetMethods(staticFlags) |> Array.map asMethodBase
            let ctors = (unbox<Type> o).GetConstructors(ctorFlags) |> Array.map asMethodBase
            let owner = (unbox<Type> o).Name + " (static)"
            Array.concat [ methods; ctors ], null, args, owner
          else 
            let owner = o.GetType().Name + " (instance)"
            o.GetType().GetMethods(instanceFlags) |> Array.map asMethodBase, o, args, owner
        
        // A simple overload resolution based on the name and number of parameters only
        let methods = 
          [ for m in methods do
              if m.Name = name && m.GetParameters().Length = args.Length then yield m 
              if m.Name = name && m.IsGenericMethod &&
                 m.GetGenericArguments().Length + m.GetParameters().Length = args.Length then yield m ]
        match methods with 
        | [] -> failwithf "No method '%s' with %d arguments found in %s" name args.Length owner
        | _::_::_ -> failwithf "Multiple methods '%s' with %d arguments found %s" name args.Length owner
        | [:? ConstructorInfo as c] -> c.Invoke(args)
        | [ m ] when m.IsGenericMethod ->
            let tyCount = m.GetGenericArguments().Length
            let tyArgs = args |> Seq.take tyCount 
            let actualArgs = args |> Seq.skip tyCount
            let gm = (m :?> MethodInfo).MakeGenericMethod [| for a in tyArgs -> unbox a |]
            gm.Invoke(instance, Array.ofSeq actualArgs)
        | [ m ] -> m.Invoke(instance, args) ) |> unbox<'R>
    else
      // When the 'o' object is 'System.Type', we access static properties
      let typ, flags, instance = 
        if (typeof<System.Type>).IsAssignableFrom(o.GetType()) then unbox o, staticFlags, null
        else o.GetType(), instanceFlags, o
      
      // Find a property that we can call and get the value
      let prop = typ.GetProperty(name, flags)
      if Object.Equals(prop, null) then 
        // Find a field that we can read
        let fld = typ.GetField(name, flags)
        if Object.Equals(fld, null) then
          // Try nested type...
          let nested = typ.Assembly.GetType(typ.FullName + "+" + name)
          if Object.Equals(nested, null) then 
            failwithf "Property, field or nested type '%s' not found in '%s' using flags '%A'." name typ.Name flags
          elif not ((typeof<'R>).IsAssignableFrom(typeof<System.Type>)) then
            failwithf "Cannot return nested type '%s' as value of type '%s'." nested.Name (typeof<'R>.Name)
          else nested |> box |> unbox<'R>
        else
          // Get field value
          fld.GetValue(instance) |> unbox<'R>
      else
        // Call property
        let meth = prop.GetGetMethod(true)
        if prop = null then failwithf "Property '%s' found, but doesn't have 'get' method." name
        try meth.Invoke(instance, [| |]) |> unbox<'R>
        with err -> failwithf "Failed to get value of '%s' property (of type '%s'), error: %s" name typ.Name (err.ToString())

open NUnit.Framework
open MonoDevelop.FSharp
open MonoDevelop.Core
open MonoDevelop.Ide.Gui
open MonoDevelop.Ide.Gui.Content
open MonoDevelop.Projects
open MonoDevelop.Ide.TypeSystem
open FsUnit
open MonoDevelop.Debugger
open MonoDevelopTests
open Reflection

module TestHelpers =

    let createDoc (text:string) references defines =
        let content = new TestViewContent()
        let tww = TestWorkbenchWindow(content)

        content.ContentName <- "/a.fsx"
        content.Data.MimeType <- "text/x-fsharp"

        let endPos = text.IndexOf ('$')
        let text = 
            if endPos > 0 then text.Substring (0, endPos) + text.Substring (endPos + 1)
            else text

        let project = Services.ProjectService.CreateDotNetProject ("F#")
        project.FileName <- FilePath("test.fsproj")
        project.References.AddRange references
        project.Files.Add (new ProjectFile ("/a.fsx", BuildAction.None))

        let projectConfig = DotNetProjectConfiguration("Debug")
        let cp = FSharpCompilerParameters(DefineConstants = defines)
        projectConfig.CompilationParameters <- cp

        project.Configurations.Add projectConfig
        project.DefaultConfiguration <- projectConfig

        use solution = new MonoDevelop.Projects.Solution ()
        solution.AddConfiguration ("", true) |> ignore
        solution.DefaultSolutionFolder.AddItem (project)
        using ( new MonoDevelop.Core.ProgressMonitor ())
            (fun monitor -> 
                 let typeSystemService = typeof<MonoDevelop.Ide.TypeSystem.TypeSystemService>
                 typeSystemService?Load (solution, monitor) |> Async.AwaitTask)

        content.Project <- project

        content.Text <- text
        content.CursorPosition <- max 0 endPos
        let doc = Document(tww)
        doc?SetProject (project)

        let compExt = new FSharpTextEditorCompletion()
        compExt.Initialize(doc.Editor, doc)
        content.Contents.Add(compExt)

        let pd = doc.UpdateParseDocument()
        typeof<MonoDevelop.Ide.TypeSystem.TypeSystemService>?Unload(solution)
        doc, content