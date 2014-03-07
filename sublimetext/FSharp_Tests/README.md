## Sublime Text F# Package - Tests

This package contains tests for the FSharp package for Sublime Text. It is
meant to be used together with it and only during development.


### Usage

The script `..\bin\Publish.ps1` wil take care of publishing tests locally
for dev builds.

Tests can be run via `window.run_command('run_fsharp_tests')`.

Tests are written using Python's *unittest* module.
