# Monodevelop binding assembly resolution

In order to build options (which is what I need):

    LanguageService.fs:716:        let args = CompilerArguments.generateCompilerOptions

generateCompilerOptions at CompilerArguments:136

calls generateReferences, defined at 102

Which given a ProjectItemCollection called items:

    for ref in items.GetAll<ProjectReference>() do
      for file in ref.GetReferencedFileNames(configSelector) do

So what types are `ref` and `file`

    ref : ProjectReference

as suggested by the generic type instantiation and
 
    file : string
    
but this just seems to be set by `asm.FullName` in ProjectReference.cs:121, so it seems the project is parsed and assemblies resolved when read in. But the earlier constructor takes a string and then calls UpdatePackageReference which uses AssemblyContext.cs

Feels like a wild goose chase anyway

# Xbuild

in `Main.cs` we end up using `Engine.GlobalEngine` and invoking `BuildProjectFile` on that.

we find this in `mono/mcs/class/Microsoft.Build.Engine/Microsoft.Build.BuildEngine`

which ends up calling BuildProjectFileInternal in Engine.cs:250

which calls `project.Load` and `project.Build`

BuildItemGroupCollection seems like a possible candidate

# ResolveAssemblyReference

Setting ``HintPath`` using the Metadata from the BuildItem whe creating the TaskItem only works if the current working directory is correct w.r.t the HintPath. That is, if the HintPath is relative, then it is resolved w.r.t. the current working directory. Other paths in SearchPaths do nothing!

# Other interesting facts

    > System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory() 
    "/Library/Frameworks/Mono.framework/Versions/3.0.0/lib/mono/4.5"
    
    
# Current direction

## For better understanding

* Sketch a quick overview of `Program.fs`: the agent, the parser, the command handling.
  - type RequestOptions for calling compiler service
  - IntelliSenseAgent contains the 'checker' (compiler service). Sets up a MailBox and waits for messages requesting it use the compiler service in particular ways. Has helper methods that send these messages.
  - module CommandInput offers various parsers for reading stdin and determining the actual command entered
  - module Main loops on checking stdin for commands using CommandInput and keeps a state of RequestOptions and a project (now that I've changed it). Each command is acted on appropriately.  

* How does the parser actually work?

## For autocompletion of project files

Need to modify `Program.fs`:

1. Remove 'Script' load ability.
2. Completion command should take: `Filename * Position * Line * Timeout`
3. If the filename is in the current project then complete as normal, otherwise consider treating it as a script c.f. Monodevelop binding switch. See `LanguageService.fs:674` (GetCheckerOptions). 

### Breakdown of Number 2

1. Change CommandInput to read a filename for completion/tooltip
2. Update Main so the types are correct.
3. Change CommandInput to read the line as well and update Main.
   - Note: If we are passing in the line, do we need the position as well?
           Answer: Yes because we have to pass it to the compiler binding. 
                   (see FSharpCompiler.fs:509)


## More in-depth discussion of number 3

It seems that we need a way of invoking a parse of particular files, and passing in the source text of that file. Perhaps we should store the (un)typed info, giving results on the basis of this and updating it only when asked by the provision of new file text. But should we then update all files that depend on it, and how should we determine those?

## For later, remember

* Need a way to reload projects if modified
* Probably need to ensure ordering is correct for files

## Example CheckOptions

Checkoptions: ProjectFileName: /Users/robnea/Projects/TestProjectFSharp/TestProjectFSharp/TestProjectFSharp.fsproj, ProjectFileNames: [|"/Users/robnea/Projects/TestProjectFSharp/TestProjectFSharp/AssemblyInfo.fs";
  "/Users/robnea/Projects/TestProjectFSharp/TestProjectFSharp/Program.fs"|], ProjectOptions: [|"--noframework"; "--define:DEBUG"; "--debug+"; "--optimize-"; "--tailcalls-";
  "-r:/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/4.0/FSharp.Core.dll";
  "-r:/Library/Frameworks/Mono.framework/Versions/3.0.0/lib/mono/4.0/mscorlib.dll";
  "-r:/Library/Frameworks/Mono.framework/Versions/3.0.0/lib/mono/4.0/System.dll";
  "-r:/Library/Frameworks/Mono.framework/Versions/3.0.0/lib/mono/4.0/System.Core.dll";
  "-r:/Library/Frameworks/Mono.framework/Versions/3.0.0/lib/mono/4.0/System.Numerics.dll"|], IsIncompleteTypeCheckEnvironment: false, UseScriptResolutionRules: false
