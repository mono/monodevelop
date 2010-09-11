This directory contains the tools to make MonoDevelop packages and releases for Mac.

PREREQUISITES
============

* Builds must be made with the mac profile.
* The "artifacts" directory must be beside the top-level monodevelop directory. This contains some binaries that are embedded into the MD app: the Moonlight SDK and the MonoDoc viewer app.

BUILDING
========

First ensure that MD has been built successfully from the top-level MD directory.

To make an app bundle: make MonoDevelop.app

To make a disk image: make monodevelop.dmg

BEFORE A RELEASE
================

* The version-info file must be updated manually before making a new build to be distributed.
* Changes to the version-info file should be committed to git before making the final build.
* Ensure that any version in the dmg background image is correct.
* Ensure that no desired extras are in the "Missing files" message when building the app.

VERSIONINFO
===========

The version-info file contains a release number.

The format of the release number is Mmmppbbb
where M = major, m = minor, p = point, b = build
and these sub-values must be left-padded with zeroes as necessary.

For example, 2.4.1 build 3 would be 20401003

The intention is that the value can be compared directly as an integer with older releases' numbers.

This format ensures that values of 0-99 are supported for major, minor, point and 0-999 for build.
