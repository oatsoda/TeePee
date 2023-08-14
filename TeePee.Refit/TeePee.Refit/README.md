<img src="https://raw.githubusercontent.com/oatsoda/TeePee/main/TeePee/teepee-icon.png" alt="TeePee Logo" width="64" height="64" />

# TeePee.Refit

[NuGet Package](https://www.nuget.org/packages/TeePee.Refit/)

Add on adaptor for [TeePee](https://www.nuget.org/packages/TeePee/) when production code is using Refit.

[![Build Status](https://dev.azure.com/oatsoda/TeePee/_apis/build/status/Full.Refit?branchName=main)](https://dev.azure.com/oatsoda/TeePee/_build/latest?definitionId=10&branchName=main)

# Documentation

Setup your mocking with the `TeePeeBuilder` as usual, and then attach to the Refit interface.  

```csharp
var teePeeBuilder = new TeePeeBuilder(); // Set up http mocking

services.AttachToRefitInterface<IMyRefitInterface>(teePeeBuilder.Build());
```
