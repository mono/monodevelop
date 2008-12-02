addin_files=`find ../../../.. -name *.addin.xml`

AssemblyInfoWriter.exe: AssemblyInfoWriter.cs
	gmcs AssemblyInfoWriter.cs /out:AssemblyInfoWriter.exe /debug /r:System.Core /r:System.Xml.Linq

update-info: AssemblyInfoWriter.exe
	for f in $(addin_files); do mono --debug ./AssemblyInfoWriter.exe "$$f"; done
