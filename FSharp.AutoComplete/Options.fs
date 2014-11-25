// --------------------------------------------------------------------------------------
// (c) Robin Neatherway
// --------------------------------------------------------------------------------------
namespace FSharp.InteractiveAutocomplete

open System

module Version =
  let string = "FSharp.AutoComplete 0.13.3"

module Options =

  let verbose = ref false
  let timeout = ref 10

  let verboseFilter : Ref<Option<Set<string>>> = ref None

  let p = new NDesk.Options.OptionSet()
  Seq.iter (fun (s:string,d:string,a:string -> unit) -> ignore (p.Add(s,d,a)))
    [
      "version", "display versioning information",
        fun _ -> printfn "%s" Version.string;
                 exit 0

      "v|verbose", "enable verbose mode",
        fun _ -> Debug.verbose := true

      "l|logfile=", "send verbose output to specified log file",
        fun s -> try
                   Debug.output := (IO.File.CreateText(s) :> IO.TextWriter)
                 with
                   | e -> printfn "Bad log file: %s" e.Message
                          exit 1

      "vfilter=", "apply a comma-separated {FILTER} to verbose output",
        fun v -> Debug.categories := v.Split(',') |> set |> Some

      "h|?|help", "display this help!",
        fun _ -> printfn "%s" Version.string;
                 p.WriteOptionDescriptions(stdout);
                 exit 0
    ]
