
RUNTIME = mono
NAMESPACE = ICSharpCode.SharpRefactory.Parser
GRAMMAR = cs.ATG
SHARPCOCO = ../../../../Tools/SharpCoco/SharpCoco.exe

all: Parser.cs

Parser.cs: $(GRAMMAR) $(SHARPCOCO)
	$(RUNTIME) $(SHARPCOCO) -namespace $(NAMESPACE) $(GRAMMAR)
	rm -f Parser.old.cs

