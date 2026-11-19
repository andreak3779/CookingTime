# Phase 17 — UI robustness and cleanup

| | |
| --- | --- |
| **Branch** | `phase/17-ui-cleanup` |
| **Cuts from** | `main` (which contains Phases 14–16) |
| **Checkpoint** | `checkpoint(phase-17): fix Reset stale validation state + name-keyed meal lookup + add logging + remove dead API + phase doc` |
| **Tag** | `v0.17-ui-cleanup` (after merge) |
| **Production code** | `Pages/CookingTime.razor` (Reset), `Domain/Abstractions/IMealFactory.cs` (`TryGetByName`), `Infrastructure/Meals/MealFactory.cs` (catalog built once + name index), `Application/Services/CookingTimeCalculator.cs` (lookup + logging), `Application/ServiceCollectionExtensions.cs` (`AddLogging`), `Domain/Meals/MealTypeRegistry.cs` (dead API removed) |
| **Tests** | New facts in `tests/CookingTime.ComponentTests/CookingTimeRazorTests.cs` (+2), `tests/CookingTime.IntegrationTests/CookingTimeFeatureTests.cs` (+3), `tests/CookingTime.UnitTests/Infrastructure/MealFactoryTests.cs` (+6); updated `tests/CookingTime.UnitTests/Application/CookingTimeCalculatorTests.cs`; obsolete facts removed from `tests/CookingTime.UnitTests/Domain/Meals/MealTypeRegistryTests.cs` (−5, +1). **Total: 248 facts** (was 241) |
| **Docs** | This file; updated `docs/phases/README.md`; updated `README.md`; updated `AGENTS.md` |
| **CI** | Unchanged. `dotnet build -c Release` → 0 warnings / 0 errors; `dotnet test -c Release` → 248 facts green |

## Goal

The last of the four review phases: the two remaining **user-facing**
defects, one performance wart, one observability gap, and one piece of dead
API. Everything here was verified empirically before it was fixed.

## What shipped

### 1. `Reset()` left stale validation messages on screen

This one was found by probing the component, not by reading it.

`Reset()` used to mutate the view model in place:

```csharp
// before
private void Reset()
{
    _model.Meal = "";
    _model.Weight = 1M;
    _model.Unit = WeightUnit.Pounds;
    _result = null;
}
```

`EditForm` holds an `EditContext` **over the model instance**, and
`DataAnnotationsValidator` caches per-field messages in its
`ValidationMessageStore`. Mutating the model does **not** clear that store.
The bUnit probe showed exactly what the user saw:

```
[PROBE] validation-message elements BEFORE reset : [Please select a meal. | Please select a meal.]
[PROBE] validation-errors li BEFORE reset        : [Please select a meal.]
[PROBE] validation-message elements AFTER reset  : [Please select a meal. | Please select a meal.]   <-- unchanged
[PROBE] validation-errors li AFTER reset         : [Please select a meal.]
[PROBE] STALE MESSAGES REMAIN AFTER RESET? YES -> BUG CONFIRMED
```

The fields cleared, the result cleared, and the form *looked* reset — but
"Please select a meal." was still there, referring to a field the user had
already emptied.

The fix assigns a **fresh** model instance, which makes `EditForm` bind a new
model and the stale messages go with it:

```csharp
// after
private void Reset()
{
    _model = new CookingTimeViewModel();
    _result = null;
}
```

Verified with the same probe: after the change, `validation-message` went
from `[Please select a meal. | Please select a meal.]` to `[]`.

The regression test was verified to **fail** on the pre-fix code:

```
Failed CookingTimeRazorTests.Reset_AfterFailedSubmit_ClearsValidationMessages
  Expected ctx.FindAll(".validation-message") to be empty because Reset must
  clear the validation state, not just the field values, but found at least
  one item AngleSharp.Html.Dom.HtmlListItemElement
```

### 2. The catalog was rebuilt on every calculation

`CookingTimeCalculator` looked a meal up like this:

```csharp
// before
var meal = _mealFactory.CreateAll()
    .FirstOrDefault(m => string.Equals(m.Name, mealName, StringComparison.OrdinalIgnoreCase));
```

`CreateAll()` **re-invokes every factory delegate** — it is not a cached
read, it is a rebuild. So every single "Calculate" click rebuilt all twelve
meals (ten of which are `RangeMeal` instances constructed from config) just
to find one by name.

The factory already holds the catalog, so the factory answers the question.
`IMealFactory` gains:

```csharp
bool TryGetByName(string name, out IMeal? meal);
```

and `MealFactory` now materialises the catalog **once** in its constructor
and builds a case-insensitive name index alongside it:

```csharp
var _meals = registry.CreateAll();
_byName = new Dictionary<string, IMeal>(StringComparer.OrdinalIgnoreCase);
foreach (var meal in _meals)
{
    // First registration wins, matching the previous
    // FirstOrDefault-by-name behaviour exactly.
    _byName.TryAdd(meal.Name, meal);
}
```

The "first registration wins" detail matters: the old code used
`FirstOrDefault`, so a duplicate name resolved to the first match. The new
index preserves that exactly. (Phase 15's validator rejects duplicate names
at startup, so the case should not arise — but the behaviour is unchanged if
it does.)

One visible consequence: a malformed catalog entry now throws when the
factory is **constructed**, rather than when the catalog is first read. That
is the earlier and clearer failure point, and Phase 15's startup validation
now catches it before the app runs at all.

### 3. The calculator logs the two failures that are not plain validation errors

There was **no logging anywhere in the production code**. In a
client-side-only app that is not a vulnerability — but it did leave two real
gaps:

- An **unknown meal** is the one failure a user can trigger that is not a
  plain validation error. It usually means a mis-configured catalog, and it
  was completely invisible.
- An **out-of-range weight** is expected user input, but a systematic spike
  means the UI's allowed range and the meal's range have drifted apart.

`CookingTimeCalculator` now takes an optional `ILogger` and logs the first
at `Warning` and the second at `Debug`:

```csharp
_logger.LogWarning("Calculation requested for unknown meal {MealName}.", mealName);
_logger.LogDebug("Weight {Weight} lbs is outside the supported range for {MealName}.",
                 weight.Pounds, meal.Name);
```

The logger is **optional** in the constructor and defaults to
`NullLogger`, so the existing unit tests still construct the calculator
without a logging pipeline — logging is an improvement here, not a new
requirement. `AddCookingTime` calls `services.AddLogging()`.

### 4. The never-called API was removed

`MealTypeRegistry.TryCreate` and `MealTypeRegistry.RegisteredKinds` were
public API used **only by their own tests**. Nothing in production code
called either. They were maintenance surface for a feature nothing used, so
they were removed along with their five facts, and replaced with one fact
that covers the same ground through the surviving API:

```csharp
[Fact]
public void CreateAll_UnknownKind_IsSimplyAbsent()
```

## Why no test caught the Reset defect

| Layer | Result | Why |
| --- | --- | --- |
| `dotnet build` | green | Not a compile error. |
| `dotnet test` | 241 facts green | The suite had **no test that clicked Reset at all**. `RendersCalculateAndResetButtons` only asserted the button *exists* and says "Reset". |
| A bUnit probe | found it | Only driving the component — submit, then reset, then inspect — surfaced the stale messages. |

This is the same lesson as Phases 12 and 14, third time: the defect lives in
the space between the pieces, and only an interaction test reaches it.

## New tests (11 facts)

| Suite | File | Facts | Covers |
| --- | --- | --- | --- |
| Component | `CookingTimeRazorTests.cs` | +2 | Reset clears the validation messages **and** the summary; Reset restores the default model values |
| Integration | `CookingTimeFeatureTests.cs` | +3 | A **config-driven** meal resolves by name end-to-end; a config-driven meal rejects an out-of-range weight; every catalog meal is findable by name |
| Unit | `MealFactoryTests.cs` | +6 | Case-insensitive lookup; unknown name; blank name; whitespace trimming; the catalog returns the **same instances** on every call |
| Unit | `MealTypeRegistryTests.cs` | −5 +1 | Obsolete facts removed with the API they tested; one replacement fact |

The two integration facts are the important ones: they exercise the **real**
`appsettings.json` through the **production** composition root, so the new
lookup is verified against the shipped catalog — not a mock. The
out-of-range one is Phase 14 and Phase 17 verified together.

## Validation

```
dotnet build -c Release   → 0 Warning(s), 0 Error(s)
dotnet test  -c Release   → UnitTests 191 · ComponentTests 23 · IntegrationTests 34
                             = 250 facts, 0 failed, 0 skipped   (was 241)
coverage gate             → UnitTests 86.01%  (gate 75%)  PASS
```

Note that `MealFactory.CreateAll()` now returns the **same instances** on
every call. The meals are stateless, so this is safe — and it is what the
"catalog is built once" change means.

## Anti-patterns to avoid

- **Don't "reset" a Blazor form by mutating the bound model.** `EditForm`'s
  `EditContext` and the validator's message store are keyed to the model
  *instance*. Assign a new instance.
- **Don't rebuild a collection to look one item up.** If a component already
  holds the data, give it a lookup. `CreateAll().FirstOrDefault(...)` is a
  rebuild in disguise — and it was re-invoking twelve factory delegates on
  every click.
- **Don't keep public API that nothing calls.** `TryCreate` and
  `RegisteredKinds` existed only for their own tests. That is a maintenance
  cost with no benefit.
- **Don't make logging a hard dependency of a class that does not need it.**
  The optional `ILogger` + `NullLogger` default keeps the constructor easy
  to call from tests while making the behaviour available in production.

## Out of scope

- The Dockerfile, nginx layer and HTTPS/HSTS headers — Phase 9, which now
  has a CI-enforced publish command to build on.
- ADR 0009 (accepted risks) — Phase 18.
