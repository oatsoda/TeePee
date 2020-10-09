# TeePee

A fluent API to configure HttpClients for unit testing.

CI: [![Build Status](https://dev.azure.com/oatsoda/TeePee/_apis/build/status/CI?branchName=master)](https://dev.azure.com/oatsoda/TeePee/_build/latest?definitionId=4&branchName=master) \
Full: [![Build Status](https://dev.azure.com/oatsoda/TeePee/_apis/build/status/Full?branchName=master)](https://dev.azure.com/oatsoda/TeePee/_build/latest?definitionId=5&branchName=master)

# Documentation

## Mocking

Everything in TeePee starts by creating a `TeePeeBuilder`.  

```
var teePeeBuilder = new TeePeeBuilder();
```


### Matching requests

Add requests that you want to support by using the fluent API to specify as little or as much you want to match one

```
teePeeBuilder.ForRequest("https://some.api/path/resource", HttpMethod.Post)
             .WithBody(new { Value = 12 })
             .ContainingQueryParam("filter", "those")
             .ContainingHeader("ApiKey", "123abc-xyz987");             
```

#### Query strings

Query strings van either be included in the URL:

```
teePeeBuilder.ForRequest("https://some.api/path/resource?filter=those")
```

or by matching using the `ContainsQueryParam`

```
teePeeBuilder.ForRequest("https://some.api/path/resource", HttpMethod.Post)
             .ContainingQueryParam("filter", "those")
```

You cannot combine both though. Once you specify `ContainingQueryParam` then incoming requests at execution-time will have their query string removed when attempting to match a rule which is using `ContainingQueryParam`.


### Returning responses

The response to a matching request is set using the `Responds()` fluent method:

```
teePeeBuilder.ForRequest("https://some.api/path/resource", HttpMethod.Post)
             .Responds()
             .WithStatus(HttpStatusCode.OK)
             .WithBody(new { Result = "Done" })
             .WithHeader("Set-Cookie", "Yum");
```

#### Defaults

If you don't specify a Status Code in the response, the default is `204 NoContent`. (i.e. it matched, but you didn't tell it what status to return)
If you don't call `Responds()` then the default response Status Code is `202 Accepted`. (i.e. it matched, but you didn't tell it to respond)


### Defaults for no matches & Strict Mode

If there is no match for a request, the default is to response with a Status Code is `404 NotFound`.  (This is configurable using the `WithDefaultResponse` on `TeePeeBuilder`)



## Unit Testing 

It's worth [making sure you fully understand](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-3.1#consumption-patterns) the various `HttpClientFactory` mechanisms for registering and resolving `HttpClient`s before reading this.


### Classicist/Detroit/Black Box vs Mockist/London/White Box
TeePee is focused on the Classicist/Detroit/Black Box approach to unit testing.  It can be used for Mockist/London/White Box approaches, but be aware that due to the way `HttpClientFactory` is implemented, you may find there are limitations if you are planning to mock and inject your dependencies into your test subject manually.


### Verifying 

When Black Box unit testing, it's recommended to be as passive with mocked dependencies as possible. This means, where possible, not asserting specific details about calls to the HttpClient but instead mocking the requests and responses and instead asserting the outcomes of the Subject Under Test.

This isn't always possible - for example in a Fire and Forget situation where the behaviour is that the Subject Under Test is required to call an external HTTP service, but the SUT itself doesn't indicate this was done correctly.

In this case, you can set up a tracker using `TrackRequest()` to make simple verification of the requests that you set up.

```
var requestTracker = teePeeBuilder.ForRequest("https://some.api/path/resource", HttpMethod.Post)
                                  .TrackRequets();
                                  
// Execute SUT

reququestTracker.WasCalled(1);                                  
```


### Injection during Unit Tests

As stated above, TeePee is more focused on the Classicist/Detroit/Black Box of testing approach and this allows unit test coverage of DI registrations for `HttpClientFactory`.  You can of course still manually inject should you wish to.


### Manual Injection

Once you have finished setting up one or more requests in you `TeePeeBuilder` then depending on your `HttpClientFactory` approach, you can create the relevant objects to inject:

#### Basic HttpClient

Basic HttpClient usage is very limited and is only really meant for intermediate refactoring stages.  You probably won't want to use this in your production code.

```
var httpClientFactory = teePeeBuilder.Build().Manual().CreateHttpClientFactory();
var subjectUnderTest = new UserController(httpClientFactory);
```

#### Named HttpClient

For Named HttpClient instances, you need to specify the expected Name of the instance when creating the `TeePeeBuilder`:

```
var teePeeBuilder = new TeePeeBuilder("GitHub");

// Setup requests

var httpClientFactory = teePeeBuilder.Build().Manual().CreateHttpClientFactory();
var subjectUnderTest = new UserController(httpClientFactory);
```

#### Typed HttpClient

For Typed HttpClient instances, you need to create the HttpClient instead of the HttpClientFactory:

```
var teePeeBuilder = new TeePeeBuilder();

// Setup requests

var typedHttpClient = new MyTypedHttpClient(teePeeBuilder.Build().Manual().CreateClient());
var subjectUnderTest = new UserController(typedHttpClient);
```

#### HttpClient BaseAddress

If you are wanting to specify the `BaseAddress` in your `HttpClient` and use Relative URLs in your Subject Under Test when calling the HttpClient, you can set TeePee up to ALSO configure this in your tests. (Note, this obviously means you are not covering this in your tests, it is just so that the HttpClient accepts Relative URLs.

To do this, pass a dummy Base Address into the `Manual()` call. 
```
var teePeeBuilder = new TeePeeBuilder("GitHub");
teePeeBuilder.ForRequest("https://some.api/path/resource", HttpMethod.Get)
             .Responds()
             .WithStatusCode(HttpStatusCode.OK);
             
var typedHttpClient = new MyTypedHttpClient(teePeeBuilder.Build().Manual("https://some.api").CreateClient());         
var subjectUnderTest = new UserController(typedHttpClient);
```


### Auto Injection

Injecting automatically allows you to cover the startup DI registrations as part of your unit tests.  This is mostly done using the `Resolve` static class.

#### Basic HttpClient

Basic HttpClient usage is very limited and is only really meant for intermediate refactoring stages.  You probably won't want to use this in your production code.

```
var subjectUnderTest = Resolve.WithDefaultClient<UserController>(teePeeBuilder);
```

#### Named HttpClient

For Named HttpClient instances, you need to specify the expected Name of the instance when creating the `TeePeeBuilder`:

```
var teePeeBuilder = new TeePeeBuilder("GitHub");

// Setup requests

var subjectUnderTest = Resolve.WithNamedClients<UserController>(
                          services => 
                          {
                             // Call your production code/extension methods here - but for this example we're inlining it - see examples for further details
                             // Expect any intermediate dependencies to also be registered
                             services.AddHttpClient("GitHub, c => c.BaseAddress = "https://external.api");                             
                          },
                          teePeeBuilder);
```

#### Typed HttpClient

For Typed HttpClients, your unit tests unfortunately will need to know which Type is the HttpClient (therefore exposing a bit of internal implementation detail into your tests):

```
var teePeeBuilder = new TeePeeBuilder();

// Setup requests

var subjectUnderTest = Resolve.WithTypedClient<UserController, MyTypedHttpClient>(
                          services => 
                          {
                             // Call your production code/extension methods here - but for this example we're inlining it - see examples for further details
                             // Expect any intermediate dependencies to also be registered
                             services.AddHttpClient<MyTypedHttpClient>(c => c.BaseAddress = "https://external.api");                             
                          },
                          teePeeBuilder);
```


## Multiple HttpClient dependencies

See [Examples](https://github.com/oatsoda/TeePee/tree/master/Examples) for demonstrations of how to apply the above Manual or Auto injection when you have multiple HttpClient dependencies.

