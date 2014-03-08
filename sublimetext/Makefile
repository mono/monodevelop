bin_d = $(abspath ./bin)
fsharp_d = $(abspath ./FSharp)
bund_d = $(fsharp_d)/bundled


getdeps:
	wget https://bitbucket.org/guillermooo/fsac/downloads/fsautocomplete_mono.zip -O $(bund_d)/fsautocomplete.zip

install: getdeps

	pushd $(fsharp_d)
		python builder.py --release dev
	popd

	pushd $(bin_d)
		# Publishes tests too.
		./publish.sh
	popd
