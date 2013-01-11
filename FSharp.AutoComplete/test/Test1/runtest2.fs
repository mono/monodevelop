open System.Diagnostics

// let p = Process.Start("mono", "../../bin/Debug/fsautocomplete.exe")
// p.RedirectStandardInput  <- true
// p.RedirectStandardOutput <- true

// let sr = new StreamReader(p.StandardOutput)
// let sw = new StreamWriter(p.StandardInput)

// fprintf sw "quit\n"

let p = new System.Diagnostics.Process()
p.StartInfo.FileName  <- "mono"
p.StartInfo.Arguments <- "../../bin/Debug/fsautocomplete.exe"
p.StartInfo.RedirectStandardOutput <- true
p.StartInfo.RedirectStandardInput <- true
p.StartInfo.UseShellExecute <- false
p.Start()

fprintf p.StandardInput "help\n"
//p.StandardOutput.ReadToEnd()
fprintf p.StandardInput "quit\n"
printfn "output: %s" (p.StandardOutput.ReadToEnd())

p.WaitForExit ()

