
// This file is a set of manual tests you can run through to test the quality of 
// autocomplete, quick info and parameter info.


open System

open System // press '.' here. Only namespaces should appear.

//----------------------------------------------------
// Check some basic completions and declaration lists

System    // press 'space' here

System.Math.Max(3,4)   // press '.' here

System.Char

System.
List.
System.Application
System.Console.Wr  // check formatting of description here

let test1 (x: System.Applicati (* complete here *) ) = () 

Array.map

List.a // Ctrl-space completion here 

System.C  // Ctrl-space completion here

//System.Math.Max(3,4).CompareTo   // BUG press '(' here

//----------------------------------------------------
// Check performance

System // press '.' here - should be instantaneous

System.Console.WriteLine  // type through this line

Microsoft.FSharp.Collections.Array.append  // type through this line


//------------------------------------------------------
// check that type providers give parameter info

#r "/Users/tomaspetricek/Projects/GitHub/fsharp/FSharp.Data/bin/FSharp.Data.dll"

type X = FSharp.Data.CsvProvider  // press '<' here

//------------------------------------------------------
// check parameter info

Console.WriteLine  // press '(' here, parameter/method/overloads info should appear
b
List.map           // press '(' here, parameter/method info should appear

Console.WriteLine(Console.WriteLine(),  // press ',' here 

//-------------------------------------------------------
// Check some experession typings


("")  // press '.' here, expect string menu
("").Length  // press '.' here, expect int32 menu

let x = ""
let y = (x)  // press '.' here, expect string menu

let a = (x). (* press '.' before here *) + (x)

[<EntryPoint>]
let main args = 
    Console.WriteLine("Hello world!")
    0

