AnalysisCore currently provides several services -

* The AnalysisService, a service for analyzing arbitrary input objects 
  based on extensible rules.
  
* a TextEditorExtension that uses the AnalysisService to process the
  editor's ParsedDocument when it's updated by the ProjectDomService,
  and shows any resulting errors or warnings as underlines in the editor.

== The AnalysisService ==

AnalysisService is fundamentally based on the idea of an "Analyzer tree". 
This is a tree where each node contains a "rule". Each rule is a Func<T,TRet>
that transforms an input object to an output object. The tree is constructed 
in such a way that the output type of each node's rule matches the input type 
of its children, and leaf nodes always output IEnumerable<Result>. Thus, when
Analyze (someInputObject) is called on the root node, the intermediates 
objects propagate down the tree, and the Results are aggregated and returned.

The trees are constructed based on rule nodes registered in the addin file. 
Each node had an input and output "type", which is a string alias for a real 
CLR type. Multiple aliases can be registered for one real type - the aliases 
specify how the rule nodes can be connected, and the CLR types simply allow
strongly typing the rule functions.

== Extension Points ==

The is an extension point "/MonoDevelop/AnalysisCore/Type" where type aliases 
must be registered before use. Although aliases may only be registered once, 
they may be used by any number of rules.

There are two kinds of rule extension nodes, <Adaptor> and <Rule>. Both may be
registered at the same extension point, "/MonoDevelop/AnalysisCore/Rules". 
Both kinds of node must specify an input alias and a function name, which
must correspond to a static method in the addin assembly. The static method's
argument and return type must match the CLR types to which the input and 
output aliases map.

== Rules ==

Rules always have an implicit output type of "Results", which is the built-in 
alias for IEnumerable<Result>, and they also have a user-readable, localizable 
name value, so that they may be displayed in UI for configuring which rules 
are enabled.

== Adaptors ==

Adaptors must specify an output type, but are not user-visible so are not named.
Their purpose is to do processing work that can be shared by multiple rules - 
for example, an adaptor can generate a special type that is known to several 
rules and includes additional preprocessing result, so that the rules so not 
have to repeat the work. Adaptors may also be used to map input types to types 
that are known by existing rules - for example, an adaptor could process the 
output of the ASP.NET parser to prodice input known to existing C# rules.
Adaptors may also be used to make branches conditional - if they output null, 
then none of their children will be executed.

When building the tree, the possible extension nodes used are filtered using
optional filenames on the extension nodes. If an adaptor is filtered out, 
then none of the nodes that consume its output can be used, so they need not 
be filtered explicitly. For example, if there were an adaptor that cast an
ICompilationUnit input to a CSharpCompilationUnit output, then it should be 
filtered on the .cs file extension, and any rules with a CSharpCompilationUnit
input would not be used.

Adaptors can produce more than one output quite easily. If the outputs are
a collection, then simply make the collection type the output type. If there
are several different output objects, they can be aggregated on properties of
a new type which can be used as the output type. Consumers may consume this
type directly, or multiple additional adaptor rules may be used to pick off 
the individual objects.