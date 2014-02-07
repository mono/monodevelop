**MonoDevelop** is a full-featured integrated development environment (IDE) for mono
using Gtk#.

See http://www.monodevelop.com for more info.  

Directory organization
----------------------

There are two main directories:

 * `main`: The core MonoDevelop assemblies and add-ins (all in a single
    tarball/package).
 * `extras`: Additional add-ins (each add-in has its own
    tarball/package).

Compiling
---------

If you are building from Git, make sure that you initialize the submodules
that are part of this repository by executing:
`git submodule update --init --recursive`

To compile execute:
`./configure ; make`

There are two variables you can set when running `configure`:

* The install prefix: `--prefix=/path/to/prefix`

  * To install with the rest of the assemblies, use:
  `--prefix="pkg-config --variable=prefix mono"`

* The build profile: `--profile=profile-name`

  * `stable`: builds the MonoDevelop core and some stable extra add-ins.
  * `core`: builds the MonoDevelop core only.
  * `all`: builds everything
  * You can also create your own profile by adding a file to the profiles
directory containing a list of the directories to build.

Running
-------

You can run MonoDevelop from the build directory by executing:
`make run`

Installing *(Optional)*
----------

You can install MonoDevelop by running:
`make install`

Bear in mind that if you are installing under a custom prefix, you may need to modify your `/etc/ld.so.conf` or `LD_LIBRARY_PATH` to ensure that any required native libraries are found correctly.

*(It's possible that you need to install for your locale to be
correctly set.)*

Packaging for OS X
-----------------

To package MonoDevelop for OS X in a convenient MonoDevelop.app
file, just do this after MonoDevelop has finished building (with
`make`):
`cd main/build/MacOSX ; make MonoDevelop.app`

Dependencies
------------

	Mono >= 3.0.4
	Gtk# >= 2.12.8
	monodoc >= 1.0
	mono-addins >= 0.6

Special Environment Variables
-----------------------------

**BUILD_REVISION**

	If this environment variable exists we assume we are compiling inside wrench.
	We use this to enable raygun only for 'release' builds and not for normal
	developer builds compiled on a dev machine with 'make && make run'.


References
----------

**MonoDevelop website**

http://www.monodevelop.com

**Gnome Human Interface Guidelines (HIG)**

http://developer.gnome.org/projects/gup/hig/1.0/

**freedesktop.org standards**

http://freedesktop.org/Standards/

**Integrating with GNOME** *(a little out of date)*

http://developers.sun.com/solaris/articles/integrating_gnome.html

**Bugzilla**

http://bugzilla.mozilla.org/bugwritinghelp.html

http://bugzilla.mozilla.org/page.cgi?id=etiquette.html

Discussion, Bugs, Patches
-------------------------

monodevelop-list@lists.ximian.com *(questions and discussion)*

monodevelop-patches-list@lists.ximian.com *(track commits to MonoDevelop)*

monodevelop-bugs@lists.ximian.com *(track MonoDevelop bugzilla component)*

http://bugzilla.xamarin.com *(submit bugs and patches here)*

