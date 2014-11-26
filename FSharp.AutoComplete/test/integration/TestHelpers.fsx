open System
open System.Diagnostics

type FSharpAutoCompleteWrapper() =

  let p = new System.Diagnostics.Process()

  do
    p.StartInfo.FileName <-
      IO.Path.Combine(__SOURCE_DIRECTORY__,
                      "../../bin/Debug/fsautocomplete.exe")
    p.StartInfo.RedirectStandardOutput <- true
    p.StartInfo.RedirectStandardError  <- true
    p.StartInfo.RedirectStandardInput  <- true
    p.StartInfo.UseShellExecute <- false
    p.Start () |> ignore

  member x.project (s: string) : unit =
    fprintf p.StandardInput "project \"%s\"\n" s

  member x.parse (s: string) : unit =
    let text = if IO.File.Exists s then IO.File.ReadAllText(s) else ""
    fprintf p.StandardInput "parse \"%s\" sync\n%s\n<<EOF>>\n" s text

  member x.completion (fn: string) (line: int) (col: int) : unit =
    fprintf p.StandardInput "completion \"%s\" %d %d\n" fn line col

  member x.tooltip (fn: string) (line: int) (col: int) : unit =
    fprintf p.StandardInput "tooltip \"%s\" %d %d\n" fn line col

  member x.finddeclaration (fn: string) (line: int) (col: int) : unit =
    fprintf p.StandardInput "finddecl \"%s\" %d %d\n" fn line col

  member x.declarations (fn: string) : unit =
    fprintf p.StandardInput "declarations \"%s\"\n" fn

  member x.send (s: string) : unit =
    fprintf p.StandardInput "%s" s

  member x.finalOutput () : string =
    let s = p.StandardOutput.ReadToEnd()
    let t = p.StandardError.ReadToEnd()
    p.WaitForExit()
    s + t

let installNuGetPkg s v =
  let p = new System.Diagnostics.Process()

  p.StartInfo.FileName <- IO.Path.Combine(__SOURCE_DIRECTORY__,
                                          "../../../lib/nuget/NuGet.exe")
  p.StartInfo.Arguments <- " install -ExcludeVersion -Version " + v + " " + s
  p.StartInfo.UseShellExecute <- false
  p.Start () |> ignore
  if not (p.WaitForExit(5 * 60 * 1000)) then
    try
      p.Kill()
    with
      | :? System.SystemException as e ->
            printfn "Warning: NuGet installation threw an exception: %A" e
  
