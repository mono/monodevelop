#! /bin/bash -e
echo "Configuring..."
fsharpi configure.fsx "$@"
echo "Getting nuget packages..."
mozroots --import --sync --quiet
(cd MonoDevelop.FSharpBinding && mono ../../lib/nuget/NuGet.exe restore MonoDevelop.FSharp.mac-linux.sln)
