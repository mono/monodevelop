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
* How does the parser actually work?

## For autocompletion of project files

Need to modify `Program.fs`:

1. Remove 'Script' load ability.
2. Completion command should take: `Filename * Position * Line * Timeout`
3. If the filename is in the current project then complete as normal, otherwise consider treating it as a script c.f. Monodevelop binding switch. See `LanguageService.fs:674` (GetCheckerOptions). 



## For later, remember

* Need a way to reload projects if modified
