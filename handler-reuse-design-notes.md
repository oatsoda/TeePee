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
