#!/bin/sh

cd Frames
cp ../CSharp/cs.ATG .
mono SharpCoco.exe -namespace ICSharpCode.NRefactory.Parser.CSharp cs.ATG
mv Parser.cs ../CSharp
rm cs.ATG

cp ../VBNet/VBNET.ATG .
mono SharpCoco.exe -namespace ICSharpCode.NRefactory.Parser.VB VBNET.ATG
mv Parser.cs ../VBNet
rm VBNET.ATG
