#!/bin/bash

pushd ../../../../external/libgit2/
LIBGIT2SHA=`cat ../libgit-binary/libgit2_hash.txt`
SHORTSHA=${LIBGIT2SHA:0:7}

if [[ -d build ]]
then
    pushd build

    if [[ -n $(ls libgit2-${SHORTSHA}.*) ]]
    then
        exit 0
    else
        popd
        rm -rf build
    fi
fi

mkdir build
pushd build

cmake -DCMAKE_BUILD_TYPE:STRING=RelWithDebInfo \
      -DTHREADSAFE:BOOL=ON \
      -DBUILD_CLAR:BOOL=OFF \
      -DUSE_SSH=ON \
      -DLIBGIT2_FILENAME=git2-$SHORTSHA \
      -DCMAKE_OSX_ARCHITECTURES="i386;x86_64" \
      -DCMAKE_SKIP_RPATH=TRUE \
      ..

cmake --build .
popd
popd

