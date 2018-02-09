# BestTest
A smart replacement to the old, decaying mstest ;)

## How to use it

```cmd
besttest [<options>] <assembly1spec> {<assembly2spec>...<assemblyNspec>}
```
Assemblies can be specified by wildcard (`*Test.dll` for example)

## Current features

* Runs tests in parallel
* Tests are isolated by assembly (this can be disabled)
* Inconclusive tests can be considered as succeeded
* Test timeout can be changed (defaults to 5mn)
