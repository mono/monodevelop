#! /bin/bash -e
echo "Configuring..."
fsharpi configure.fsx "$@"
mozroots --import --sync --quiet || echo 'Could not import mozroots, proceeding anyway'
