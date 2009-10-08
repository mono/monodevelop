#!/bin/sh
echo Generating with coco
set CRFRAMES = "Frames"
cp CSharp/cs.ATG Frames/cs.ATG
mono Frames/SharpCoco.exe -namespace ICSharpCode.NRefactory.Parser.CSharp Frames/cs.ATG
mv Frames/Parser.cs CSharp/Parser.cs

#mono Frames/SharpCoco.exe -trace GIPXA -namespace ICSharpCode.NRefactory.Parser.VB VBNet/VBNET.ATG
