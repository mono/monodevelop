DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

if [ ! -f $DIR/configure.exe ]; then
	mcs $DIR/configure.cs -out:$DIR/configure.exe
fi

LANG=C mono $DIR/configure.exe "$@"
