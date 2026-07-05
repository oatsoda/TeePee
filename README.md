<img src="https://raw.githubusercontent.com/oatsoda/TeePee/main/teepee-icon.png" alt="TeePee Logo" width="64" height="64" />

# TeePee

NuGet: [![NuGet Downloads](https://img.shields.io/nuget/dt/TeePee?label=NuGet%20Downloads)](https://www.nuget.org/packages/TeePee/)

A fluent API to configure HttpClients for unit testing.

[![Build Status](https://dev.azure.com/oatsoda/TeePee/_apis/build/status/TeePee?branchName=main)](https://dev.azure.com/oatsoda/TeePee/_build/latest?definitionId=5&branchName=main)

# TeePee.Refit

Also, [TeePee.Refit](/TeePee.Refit/) an add on adaptor when production code is using Refit.

# Documentation

## Mocking

Everything in TeePee starts by creating a `TeePeeBuilder`.

```csharp
var teePeeBuilder = new TeePeeBuilder();
```

### Matching requests

Add requests that you want to support by using the fluent API to specify as little or as much you want to match on:

```csharp
teePeeBuilder
    .ForRequest("https://some.api/path/resource", HttpMethod.Post)
    .ThatHasBody(new { Value = 12 })
    .ThatContainsQueryParam("filter", "those")
    .ThatContainsHeader("ApiKey", "123abc-xyz987");
```

#### Query strings

Query strings can either be included in the URL:

```csharp
teePeeBuilder.ForRequest("https://some.api/path/resource?filter=those")
```

or by matching using the `ContainsQueryParam`

```csharp
teePeeBuilder
    .ForRequest("https://some.api/path/resource", HttpMethod.Post)
    .ThatContainsQueryParam("filter", "those")
```

You _cannot combine both_. Once you specify `ContainingQueryParam` then incoming requests at execution-time will have their query string removed when attempting to match a rule which is using `ContainingQueryParam`.

### Returning responses

The response to a matching request is set using the `Responds()` fluent method:

```csharp
teePeeBuilder
    .ForRequest("https://some.api/path/resource", HttpMethod.Post)
    .Responds()
    .WithStatus(HttpStatusCode.OK)
    .WithBody(new { Result = "Done" })
    .WithHeader("Set-Cookie", "Yum");
```

#### Defaults

If you don't specify a Status Code in the response, the default is `204 NoContent`. (i.e. it matched, but you didn't tell it what status to return)
If you don't call `Responds()` then the default response Status Code is `202 Accepted`. (i.e. it matched, but you didn't tell it to respond)

### Defaults for no matches & Strict Mode

If there is no match for a request, the default is to response with a Status Code is `404 NotFound`. (This is configurable using the `WithDefaultResponse` on `TeePeeBuilder`)

------

## Unit Testing

It's worth [making sure you fully understand](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-3.1#consumption-patterns) the various `HttpClientFactory` mechanisms for registering and resolving `HttpClient`s before reading this.

### Classicist/Detroit/Black Box vs Mockist/London/White Box

TeePee is focused on the Classicist/Detroit/Black Box approach to unit testing. It can be used for Mockist/London/White Box approaches, but be aware that due to the way `HttpClientFactory` is implemented, you may find there are limitations if you are planning to mock and inject your dependencies into your test subject manually.

### Verifying

When Black Box unit testing, it's recommended to be as passive with mocked dependencies as possible. This means, where possible, not asserting specific details about calls to the HttpClient but instead mocking the requests and responses and instead asserting the outcomes of the Subject Under Test.

This isn't always possible - for example in a Fire and Forget situation where the behaviour is that the Subject Under Test is required to call an external HTTP service, but the SUT itself doesn't indicate this was done correctly.

In this case, you can set up a tracker using `TrackRequest()` to make simple verification of the requests that you set up.

```csharp
var requestTracker = teePeeBuilder
    .ForRequest("https://some.api/path/resource", HttpMethod.Post)
    .TrackRequets();

// Execute SUT

reququestTracker.WasCalled(1);
```

### Injection during Unit Tests

As stated above, TeePee is more focused on the Classicist/Detroit/Black Box of testing approach and this allows unit test coverage of DI registrations for `HttpClientFactory`. You can of course still manually inject should you wish to.

### Manual Injection

Once you have finished setting up one or more requests in you `TeePeeBuilder` then depending on your `HttpClientFactory` approach, you can create the relevant objects to inject:

#### Basic HttpClient

Basic HttpClient usage is very limited and is only really meant for intermediate refactoring stages. You probably won't want to use this in your production code.

```csharp
var httpClientFactory = teePeeBuilder.Manual().CreateHttpClientFactory(""); // Empty string is the name of the Default Client; Use Options.DefaultName if you prefer.
var subjectUnderTest = new UserController(httpClientFactory);
```

#### Named HttpClient

For Named HttpClient instances, pass the _expected_ name of the named client:

```csharp
var httpClientFactory = teePeeBuilder.Manual().CreateHttpClientFactory("GitHub");
var subjectUnderTest = new UserController(httpClientFactory);
```

#### Typed HttpClient

For Typed HttpClient instances, you need to create the HttpClient instead of the HttpClientFactory:

```csharp
var typedHttpClient = new MyTypedHttpClient(teePeeBuilder.Manual().CreateClient());
var subjectUnderTest = new UserController(typedHttpClient);
```

#### HttpClient BaseAddress

If you are using manual injection and your SUT uses relative URLs, then the base URL isn't actually under test. You can either use relative URLs when building your expected
rules, or if you use an arbitrary base address, then you'll need to also configure this on the manually created HttpClient or HttpClientFactory.

To do this, pass a dummy Base Address into the `Manual()` call.

```csharp
var teePeeBuilder = new TeePeeBuilder();
teePeeBuilder
    .ForRequest("https://some.api/path/resource", HttpMethod.Get)
    .Responds()
    .WithStatusCode(HttpStatusCode.OK);

var typedHttpClient = new MyTypedHttpClient(teePeeBuilder.Manual("https://some.api").CreateClient());
var subjectUnderTest = new UserController(typedHttpClient);
```

### Auto Injection

Injecting automatically allows you to cover the startup DI registrations as part of your unit tests, but also lends itself to better testing of expected behaviours
rather than specific implementation details (aside from Typed-clients; see below).

Auto injection using TeePee assumes that you are creating a ServiceCollection for tests and calling the production registrations, and then attaching
the TeePee rules to that.

#### Basic HttpClient

Basic HttpClient usage is very limited and is only really meant for intermediate refactoring stages. You probably won't want to use this in your production code.

```csharp
var serviceCollection = new ServiceCollection()
    .AddProductionRegistrations()
    .AttachToDefaultClient(teePeeBuilder);

var subjectUnderTest = serviceCollection.BuildServiceProvider().GetRequiredService<UserController>();
```

#### Named HttpClient

For Named HttpClient instances, you need to specify the expected Name of the instance:

```csharp
var serviceCollection = new ServiceCollection()
    .AddProductionRegistrations()
    .AttachToNamedClient(teePeeBuilder, "GitHub");
```

#### Typed HttpClient

For Typed HttpClients, your unit tests unfortunately will need to know which Type is the HttpClient (therefore exposing a bit of internal implementation detail into your tests):

```csharp
var serviceCollection = new ServiceCollection()
    .AddProductionRegistrations()
    .AttachToTypedClient<MyTypedHttpClient>(teePeeBuilder, "GitHub");
```

## Multiple HttpClient dependencies

See [Examples](https://github.com/oatsoda/TeePee/tree/main/TeePee/Examples) for demonstrations of how to apply the above Manual or Auto injection when you have multiple HttpClient dependencies.
