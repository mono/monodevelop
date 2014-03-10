bin_d = $(abspath ./bin)
fsharp_d = $(abspath ./FSharp)
bund_d = $(fsharp_d)/bundled


getdeps:
	rm -f $(bund_d)/fsautocomplete.zip
	wget https://bitbucket.org/guillermooo/fsac/downloads/fsautocomplete.zip -O $(bund_d)/fsautocomplete.zip

build:
	# Publishes tests too.
	cd $(bin_d) && ./publish.sh

install: getdeps build
