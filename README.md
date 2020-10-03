# TeePee

A fluent API to configure HttpClients for unit testing.

CI: [![Build Status](https://oatsoda.visualstudio.com/TeePee/_apis/build/status/oatsoda.TeePee?branchName=master)](https://oatsoda.visualstudio.com/TeePee/_build/latest?definitionId=2&branchName=master)

Full: [![Build Status](https://oatsoda.visualstudio.com/TeePee/_apis/build/status/oatsoda.TeePee?branchName=master)](https://oatsoda.visualstudio.com/TeePee/_build/latest?definitionId=3&branchName=master)


# TODO

- Non StringContent: Multipart / FormUrl
- Cover DI registrations
- Authentication? If prod code normally uses Delegating Handlers, then need to allow Tracking Handler to sit AFTER this.  Is this where DI needed?
- Other HttpContent (Form, File etc.)
- Refit Extensions
- Refit Authentication / DI


# Test Examples needed

A. HttpClientFactory
   1. Basic - depend on IHttpClientFactory - call CreateClient() each time
   2. Named - as per basic, but CreateClient takes name
   3. Typed - depend on HttpClient - injected by DI (transient)

B. Refit with HttpClientFactory

Include examples tests that use ServiceCollection (test registrations too)
        