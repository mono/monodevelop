#!/bin/sh
cp CSharp/cs.ATG Frames/cs.ATG
mono Frames/SharpCoco.exe -namespace ICSharpCode.NRefactory.Parser.CSharp Frames/cs.ATG
mv Frames/Parser.cs CSharp/Parser.cs
rm Frames/cs.ATG
cp VBNet/VBNET.ATG Frames/VBNET.ATG
mono Frames/SharpCoco.exe -trace GIPXA -namespace ICSharpCode.NRefactory.Parser.VB Frames/VBNET.ATG
mv Frames/Parser.cs VBNet/Parser.cs
rm Frames/VBNET.ATG
