#!/bin/bash

pushd ../../../../external/libgit2sharp/
LIBGIT2SHA=`cat LibGit2Sharp/libgit2_hash.txt`
SHORTSHA=${LIBGIT2SHA:0:7}

if [[ -d libgit2/build ]]
then
    pushd libgit2/build

    if [[ -n $(ls libgit2-${SHORTSHA}.*) ]]
    then
        exit 0
    else
        popd
        rm -rf libgit2/build
    fi
fi

cp libgit2/CMakeLists.txt libgit2/.CMakeLists.txt.mdcopy
echo 'SET(CMAKE_SKIP_RPATH TRUE)' > libgit2/CMakeLists.txt
cat libgit2/.CMakeLists.txt.mdcopy >> libgit2/CMakeLists.txt

mkdir libgit2/build
pushd libgit2/build

cmake -DCMAKE_BUILD_TYPE:STRING=RelWithDebInfo \
      -DTHREADSAFE:BOOL=ON \
      -DBUILD_CLAR:BOOL=OFF \
      -DUSE_SSH=ON \
      -DLIBGIT2_FILENAME=git2-$SHORTSHA \
      -DCMAKE_OSX_ARCHITECTURES="i386;x86_64" \
      ..

cmake --build .
popd

mv libgit2/.CMakeLists.txt.mdcopy libgit2/CMakeLists.txt
popd
