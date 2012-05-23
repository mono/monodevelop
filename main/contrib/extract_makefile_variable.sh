#!/usr/bin/env bash
sed -e :a -e '/\\$/N; s/\\\n//; ta' -e "/^$2/!d" -e "s/$2 = //" $1
