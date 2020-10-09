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




## Manual Injection




## Auto Injection


## Examples

[TeePee Examples](https://github.com/oatsoda/TeePee/tree/master/Examples)
