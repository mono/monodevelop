# FSharp.AutoComplete

This project provides a command-line interface to the [FSharp.Compiler.Service](https://github.com/fsharp/FSharp.Compiler.Service/) project. It is intended to be used as a backend service for rich editing or 'intellisense' features for editors. Currently it is used by the [emacs](../emacs) support.

This README is targeted at developers.

## Building and testing

There is a [FAKE script](build.fsx) with chain-loaders for [*nix](fake) and [Windows](fake.cmd). This can be used for both building and running the unit and integration tests. It is also the core of the CI builds running on [Travis](../.travis.yml) and [AppVeyor](../appveyor.yml), and so also has the ability to run the Emacs unit and integration tests.

On Linux and OSX, there is a legacy [Makefile](Makefile), which is a bit quicker to run (the overhead of running launching FSI for Fake is a few seconds). For the moment this supports all the same functionality that the FAKE script does, but this will not likely continue to be the case.

The [integration tests](integration) use a simple strategy of running a scripted session with `fsautocomplete.exe` and then comparing the output with that saved in the repository. This requires careful checking when the test is first constructed. On later runs, absolute paths are removed using regular expressions to ensure that the tests are machine-independent.

There are not currently any unit tests, the previously tested functionality of project parsing has been moved upstream to [FSharp.Compiler.Service](https://github.com/fsharp/FSharp.Compiler.Service). The tests were simply constructed using NUnit and FSUnit. If a new test is required, you can look back through the history for the `unit` directory and use that structure.

## Communication protocol

It is expected that the editor will launch this program in the background and communicate over a pipe. It is possible to use interactively, although due to the lack of any readline support it isn't pleasant, and text pasted into the buffer may not be echoed. As a result, use this only for very simple debugging. For more complex scenarios it is better to write another integration test by copying an [existing one](test/integration/Test1).

The available commands can be listed by running `fsautocomplete.exe` and entering `help`. Commands are all on a single line, with the exception of the `parse` command, which should be followed by the current text of the file to parse (which may differ from the contents on disk), and terminated with a line containing only `<<EOF>>`.

There are two formats for data to be returned in: text and JSON. The text support is the default, and provided for backwards compatibility and testing. An example of a session in 'text' mode is:

    project "Test1.fsproj"
    <absolute path removed>/Program.fs
    <<EOF>>
    parse "Program.fs"
    module X =
      let func x = x + 1

    let val2 = X.func 2
    <<EOF>>
    INFO: Background parsing started
    completion "Program.fs" 4 13
    DATA: completion
    func
    <<EOF>>

Notice that the program locations are 1-indexed for both lines and columns (here 4 and 13 to select the point just after `X.`). In this text mode, multiline responses are terminated with `<<EOF>>`. This clumsiness was the reason for moving to JSON.

Editors will want to use the JSON mode for preference, selected by sending the command `outputmode json`. The same simple session using JSON would look like:

    outputmode json
    project "Test1.fsproj"
    {"Kind":"project","Data":{"Files":["<absolute path removed>/Program.fs"],"Output":"<absolute path removed>/bin/Debug/Test1.exe"}}
    parse "Program.fs"
    module X =
      let func x = x + 1

    let val2 = X.func 2
    <<EOF>>
    {"Kind":"INFO","Data":"Background parsing started"}
    completion "Program.fs" 4 13
    {"Kind":"completion","Data":["func"]}

The structured data returned is able to be richer. Note for example that the output of the project is also returned. Parsing is also simplified (given a JSON parser!) because each response is exactly one line. However, it is less human-readable, which is why it is not currently used for most of the integration tests.

For further insight into the communication protocol, have a look over the integration tests, which have examples of all the features. Each folder contains one or more `*Runner.fsx` files which specify a sequence of commands to send, and `*.txt` or `*.json` files, which contain the output.

### Scripts and projects

Currently, intellisense can be offered for any number of scripts and one project at any one time. Intellisense requests are honoured for any script (`.fsx`) file and any `.fs` file for which a project containing it has most recently been loaded using the `project` command. It is an aim to lift this limitation.

