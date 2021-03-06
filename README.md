# BestTest
A (hopefully) smart replacement to the old, decaying `mstest` :broken_heart:

## Where to get it

Latest release is [here](https://github.com/ArxOne/BestTest/releases/latest).

## How to use it

```cmd
besttest [<options>] <assembly1spec> {<assembly2spec>...<assemblyNspec>}
```
Assemblies can be specified by wildcard (`*Test.dll` for example).

## Current features

* Runs tests in parallel
* Tests are isolated by assembly (this can be disabled)
* Inconclusive tests can be considered as succeeded
* Test timeout can be changed (defaults to 5mn)

## Planned features

Visit the [issues](https://github.com/ArxOne/BestTest/issues) to see what's going on and suggest new features if you want.

## Any help welcome

As usual!
