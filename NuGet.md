TeePee is a fluent API to configure HttpClients for unit testing.

```
var teePeeBuilder = new TeePeeBuilder();

teePeeBuilder.ForRequest("https://some.api/path/resource", HttpMethod.Post)
             .WithBody(new { Value = 12 })
             .ContainingQueryParam("filter", "those")
             .ContainingHeader("ApiKey", "123abc-xyz987");
             
var subjectUnderTest = Resolve.WithTypedClient<UserController, GitHubApiClient>(
                          services => 
                          {
                             var configuration = UnitTestConfig.LoadUnitTestConfig();
                             services.AddGitHubApi(configuration);                          
                          },
                          teePeeBuilder);
```

[Documentation](https://github.com/oatsoda/TeePee/blob/master/README.md#documentation) | [Examples](https://github.com/oatsoda/TeePee/tree/master/Examples)
