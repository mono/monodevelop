## Localizing MonoDevelop

Localizations for MonoDevelop are based on the Gettext system.

The localizations are stored in `*.po` files called _catalogs_, one per
language, and a reference catalog called `messages.po` that contains
all the localizable strings.

### Editing a Localization

To edit a localization, find the find the ISO standard country code for
your translation, e.g. `da` for Danish (Denmark), then open the catalog
matching that code, e.g. `da.po`.

You can then open this catalog in a text file, or a Gettext GUI editor
such as MonoDevelop itself. Unfortunately, testing a localization is
difficult without building MonoDevelop
[from source](http://monodevelop.com/Developers/Building_MonoDevelop).

If the catalog for your language does not exist, make a copy of the
reference catalog and rename it to match the expected name for your
language. To add it to the MonoDevelop build, add it to the following lists:

 * the `ALL_LINGUAS` variable in the `../configure.in` script
 * the `<GettextTranslation>` and `<Translation>` items in the `mo.mdproj` file

Finally, commit your updated localization and make a pull request to
the MonoDevelop respository. Alternatively you may attach it to a
[bug report](http://bugzilla.xamarin.com).

### Updating The Catalogs

On Mac/Linux machines, with MonoDevelop set up to build from source,
executing the `make update-po` command in this directory will regenerate
the reference catalog by scanning the MonoDevelop source, and then use
it to update the all of the language-specific catalogs.

**NOTE**: In general this is not necessary, as the .po files in
the source repository are regularly updated this way.
