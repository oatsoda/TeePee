# TODO
- Move state out of Tracker and RequestMatchRule.
    - Tracker.SetRequestMatchRule would be "Set Execution State Instance"
	- RequestMatchRule becomes immutable
- Figure out / decide which way to separate the Handler/HttpClient from the rules and state.
    - Either Builder.BuildInto, or maybe Handler is created up-front and you call Reset() or Init()/Config().

------

# TeePee Handler Reuse — Design Notes

## Q: If I wanted to make the HttpHandler live beyond one usage of a TeePee, what sort of design changes would I need to make?

There are two distinct problems to solve, and they're both clearly visible in the code:

**Problem 1 — The `HttpClient` disposes the handler** (`TeePee.cs:59`)

```csharp
new(TeePee.HttpHandler)  // disposeHandler defaults to true
```

The handler dies with the first `HttpClient`. Fix: `new HttpClient(TeePee.HttpHandler, disposeHandler: false)`.

**Problem 2 — Execution state is embedded in the rules and tracker**

`RequestMatchRule` owns `m_CurrentResponse` (the sequence pointer, line 12) and the `Tracker` owns `m_MatchedCalls`/`m_AllCalls` (lines 8–9). These accumulate across calls with no way to reset them externally. Since the handler holds a fixed `IReadOnlyList<RequestMatchRule>` and all state lives inside those objects, there's no seam to clear between uses.

**Two approaches to fix problem 2:**

**Option A — Add `Reset()`** (minimal change). Expose a `Reset()` on `TeePeeMessageHandler` (or `TeePee`) that walks the rule list, sets `m_CurrentResponse = 0` on each rule, and clears the tracker lists. Simple, low-risk, but leaves mutable state co-located with the rule definition — the concurrency issue from the assessment remains.

**Option B — Separate execution state from rule definitions** (architectural). Make `RequestMatchRule` truly immutable by removing `m_CurrentResponse` from it. The handler instead holds a `Dictionary<RequestMatchRule, ExecutionState>` where `ExecutionState` tracks the response index and call records for that rule. Between uses you swap or reset the state bag without touching the rules at all. Trackers become views over the state bag rather than owning their own lists. This also makes parallel test isolation possible if you give each concurrent use its own state bag.

Option A is appropriate if reuse is just "same mock config, sequential test runs in one fixture"; Option B if you want any form of parallel safety or want the design to be coherent with the immutability the current code implies.

---

## Q: The idea would be that the Http Handler gets attached but on the next unit test you might want to completely change the rules, but the same Http Handler needs to be used because it has been registered to the Http Client which is scoped for the whole Test Class

The use case is: one `HttpClient` + one `TeePeeMessageHandler` registered at class-fixture scope, but each test needs a **completely fresh set of rules**. The current design can't accommodate this because the handler's rules are sealed at construction and there's no way to replace them.

The changes needed fall into three areas:

**1. The handler needs to be creatable without rules, and reconfigurable**

`m_ConfiguredRules` and `m_DefaultResponse` are set once in the constructor and are effectively `readonly`. You need:
- A way to construct the handler in an "empty" state (no rules yet)
- A `Configure(IReadOnlyList<RequestMatchRule> rules, Func<HttpResponseMessage> defaultResponse)` method that replaces the rule set at runtime

This also requires a thread-safety decision: if `SendAsync` races with `Configure`, the rules reference needs to be `volatile` or swapped atomically (an `Interlocked.Exchange` on the list reference is sufficient for sequential test classes; a `ReaderWriterLockSlim` if tests ever run in parallel).

**2. `TeePeeBuilder.Build()` needs a "build into existing handler" path**

Right now `Build()` always creates a new `TeePee` (and therefore a new handler). You'd add an overload like:

```csharp
await builder.BuildInto(existingHandler);
```

This runs the same rule-building logic (body serialisation, specificity ordering) but calls `existingHandler.Configure(rules, defaultResponse)` instead of constructing a new `TeePee`.

**3. `HttpClient` must not own the handler**

`new HttpClient(TeePee.HttpHandler)` defaults `disposeHandler: true` (`TeePee.cs:59`). Once the first test's client is disposed, the handler dies. Change to `new HttpClient(handler, disposeHandler: false)` with the fixture responsible for handler lifetime.

**What the test fixture API would look like:**

```csharp
// Once, in class fixture setup:
Handler = TeePee.CreateHandler();
HttpClient = new HttpClient(Handler, disposeHandler: false) { BaseAddress = ... };

// In each test:
var builder = new TeePeeBuilder();
var tracker = builder.ForRequest(url, HttpMethod.Get).Responds().WithStatus(200).TrackRequest();
await builder.BuildInto(Handler);  // replaces rules on the existing handler

// ... exercise system under test ...

tracker.WasCalled(1);
```

The existing `Build()` → `Manual().CreateClient()` path stays unchanged for the common single-test case. `BuildInto()` is the new path for the fixture scenario.


------

Scenarios:

1. Production DI code is used and already adds one (or more) of:
   - AddHttpClient() "Default" Basic Usage - SUT will inject IHttpClientFactory and will be called via CreateClient("")
   - AddHttpClient("Name") Named Usage - SUT will inject IHttpClientFactory and will be called via CreateClient("Name")
   - AddHttpClient<T> Typed Usage - SUT will inject T which in turn will have an HttpClient injected into it. T is a Transient registration.

If using Per-Test based Test Setup I could:
   - builder.Build().AttachToHttpClient(Default / "Name") - I think if Name was incorrect/mismatched-with-production then you'd just miss out on mocking.
   - builder.Build().AttachToHttpClient<TypedClient>() - I think if TypedClient was incorrect/mismatched-with-production then you'd just miss out on mocking.

If using Per-Fixture based Test Setup then I need to do the same as Per-Test but somewhere a reset needs to take place? Or do you call 
Build/Attach again and _find the TeePee handler_ and replace the rules?

So do I need Build() ? If you always have to do Attach after? It wouldn't make sense to have Build() in the Fixture. But it could make sense
to have Attach in the Fixture as this is boilerplate. So the implies it's the other way around?
   - builder().AttachTo...() 
 
So in Per-Fixture based Test Setup, it just needs a way to reset? Could this be done somehow in the DI reg when attaching? i.e. under the same DI scope? No
that wouldn't make sense as we don't know if SUT uses multiple scopes. It would need to be a Reset() that the Test class calls?


2. Not using Production DI code, so some SUT will require either:
   - An IHttpClientFactory to be injected into it and expecting the "Default" Basic Client to be availble.
   - An IHttpClientFactory to be injected into it and expecting on or more "Named" Clients available.
   - One or more HttpClient to be injected into a Typed Client

Is this just the same as above, but AttachTo would not expect to match, therefore registering for the HttpClient for the first time? Hmm, I guess that depends
on the following note; the Configure options approach presumably requries the production DI?

NO: Because assumption is manual injection is being used, so it would require the IHttpClientFactoryor HttpClient to be created at test-time. In
which case, is this compatible with a Per-Fixture based Test Setup? Well I suppose you could still have a shared builder, but you wouldn't attach
it to anything, you would just create a new Factory or Client in each test, which means it would automatically be isolated, right?


NOTE: I think there are better ways to chain onto Production code. Either:
    - AddHttpClient(Options.DefaultName or "Name") should give the builder and allow appending new setup.
    - AddHttpClient(typeof(TClient).FullName) should give the builder and allow appending new setup.
    - OR
 
 services.Configure<HttpClientFactoryOptions>(clientName, options =>
        {
            options.HttpMessageHandlerBuilderActions.Add(builder =>
            {
                // resolve handler from the builder's IServiceProvider and add it to the pipeline
                var handler = (DelegatingHandler)builder.Services.GetRequiredService<THandler>();
                builder.AdditionalHandlers.Add(handler);
            });
        });