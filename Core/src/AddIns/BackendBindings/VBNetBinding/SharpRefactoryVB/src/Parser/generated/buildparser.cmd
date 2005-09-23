@echo off
SharpCoco -namespace ICSharpCode.SharpRefactory.Parser.VB VBNET.ATG
del Parser.old.cs
pause