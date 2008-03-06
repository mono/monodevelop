#!/bin/sh
cp MonoDevelop.XmlEditor.addin.xml ../../build/AddIns/XmlEditor
mkdir -p ../../build/AddIns/XmlEditor/schemas
cp -r schemas/*.xsd ../../build/AddIns/XmlEditor/schemas
cp -r schemas/*.txt ../../build/AddIns/XmlEditor/schemas
cp -r schemas/*.html ../../build/AddIns/XmlEditor/schemas
