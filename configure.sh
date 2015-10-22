#! /bin/bash -e
echo "Configuring..."
fsharpi configure.fsx "$@"
mozroots --import --sync --quiet || echo 'Could not import mozroots, proceeding anyway'
echo "Restoring nuget packages with paket..."
mono .paket/paket.exe restore
