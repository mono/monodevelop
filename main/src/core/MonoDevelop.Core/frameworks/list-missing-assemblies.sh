#!/bin/sh
if [ $# -ne 2 ]; then
	echo "Usage: list-missing-assemblies.sh assembly-list.txt fx-definition.xml"
	exit 1
fi

for f in `cat $1`; do
	if ! grep -i `echo $f | sed -e 's/.dll/\"/g'` $2 > /dev/null; then
		echo $f
	 fi
done
