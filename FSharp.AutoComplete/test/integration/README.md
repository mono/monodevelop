This directory contains mainly integration tests for the project.

To run the tests use `make integration-test` or `./fake
IntegrationTest` from the `FSharp.AutoComplete` directory. This will run
`git status` as its last action. The tests are considered to have
passed if there are no changes to the various `*.{txt,json}` files. If a
feature has been changed, then there may be acceptable changes --
think carefully and then commit them.

For absolute paths, there is some ad-hoc regex trickery to strip off
the beginning of the path.

At the moment tests in JSON mode need to have a sleep command entered
after parsing if completion data is going to be requested. This is
because the AppVeyor virtual machines are pretty slow and the
resulting `helptext` data field ends up sometimes containing "(loading
description)" otherwise. This is pretty hackish, but works for now.
