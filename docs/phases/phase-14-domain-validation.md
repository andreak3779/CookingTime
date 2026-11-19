# Phase 14 — Domain input validation

| | |
| --- | --- |
| **Branch** | `phase/14-domain-validation` |
| **Cuts from** | `main` |
| **Checkpoint** | `checkpoint(phase-14): enforce meal weight ranges + duration ordering invariants + phase doc` |
| **Tag** | `v0.14-domain-validation` (after merge) |
| **Production code** | `Domain/Meals/RangeMeal.cs` (weight-range guard), `Domain/ValueObjects/CookingDuration.cs` (minimum ≤ maximum), `Domain/ValueObjects/Weight.cs` (ordering), `Domain/Strategies/RangeCookingStrategy.cs` (max ≥ min) |
| **Tests** | Updated `tests/CookingTime.UnitTests/Domain/Meals/RangeMealTests.cs` (+4 facts), `tests/CookingTime.UnitTests/Domain/ValueObjects/CookingDurationFormatTests.cs` (+2 facts), `tests/CookingTime.UnitTests/Domain/ValueObjects/WeightTests.cs` (+2 facts), `tests/CookingTime.UnitTests/Domain/Strategies/RangeCookingStrategyTests.cs` (+1 fact), `tests/CookingTime.UnitTests/Application/CookingTimeCalculatorTests.cs` (+3 facts). **Total: 170 facts** (was 158) |
| **Docs** | This file; updated `docs/phases/README.md`; updated `README.md` |
| **CI** | Unchanged. `dotnet build -c Release` → 0 warnings / 0 errors; `dotnet test -c Release` → 170 facts green |

## Goal

Close the domain's input-validation holes. The Phase 12 follow-up had already
established that **a green build and a green test suite do not mean the app
behaves correctly** — the two defects fixed there were only visible in a
browser. This phase applies the same lesson one layer down: the domain
accepted inputs it should have rejected, and no test asserted otherwise.

The trigger was a full-codebase review that found the catalog accepting
nonsensical weights and reporting them to the user as successes.

## What shipped

### 1. `RangeMeal` never validated the weight — the critical fix

`RangeMeal` carries a `MinimumWeight` and a `MaximumWeight`, and exposes both
through `IMeal`. It then never used them. `Calculate` delegated straight to
the strategy:

```csharp
// before
public CookingDuration Calculate(Weight weight) => _strategy.Calculate(weight);
```

`RangeCookingStrategy` knows nothing about a meal's range — it is a pure
linear formula. So the guard that was supposed to exist **did not exist at
all** for every meal that comes from configuration. That is the entire shipped
catalog: all 10 roasts and hams in `wwwroot/appsettings.json`.

The user-visible result, reproduced through the real application layer before
the fix:

```
Pork Roast @ 0.5 Pounds   IsSuccess=True   "Cooking time will be between of 0 hrs. and 20 mins. to 0 hrs. and 22 mins."
Pork Roast @ 500 Pounds    IsSuccess=True   "Cooking time will be between of 21 hrs. and 20 mins. to 15 hrs. and 0 mins."
Chicken     @ 0.5 Pounds   IsSuccess=False  ["Weight 0.5 lbs is outside the supported range for Chicken."]
```

Two separate failures are visible there:

1. **No validation.** A 0.5 lb pork roast was given a 20-minute cook time.
2. **An inverted range reached the screen.** At 500 lb the linear formula
   yields a *minimum* of 21h20m and a *maximum* of 15h00m — the minimum is
   **greater** than the maximum — and the application layer reported it as a
   **successful** result.

The third line is the tell: `ChickenMeal` — whose `TableLookupStrategy`
throws outside its table — rejected the same input. The two meal kinds
disagreed about what a valid weight is, and only the code-baked ones were
correct.

The fix puts the guard where the knowledge lives. The meal owns its range, so
the meal enforces it:

```csharp
// after
if (weight < MinimumWeight || weight > MaximumWeight)
    throw new ArgumentOutOfRangeException(
        nameof(weight), weight.Pounds,
        $"Weight is outside the supported range for {Name} " +
        $"({MinimumWeight.Pounds:0.##}-{MaximumWeight.Pounds:0.##} lbs).");
```

`CookingTimeCalculator` already caught `ArgumentOutOfRangeException` and
rendered it as "Weight X lbs is outside the supported range for Y." — so both
meal kinds now produce the same message, and **no change to the application
layer was needed**.

After the fix, the same three probes:

```
Pork Roast @ 0.5 lb -> IsSuccess=False  ["Weight 0.5 lbs is outside the supported range for Pork Roast."]
Pork Roast @ 500 lb  -> IsSuccess=False  ["Weight 500 lbs is outside the supported range for Pork Roast."]
Pork Roast @ 5 lb    -> IsSuccess=True   "Cooking time will be between of 3 hrs. and 20 mins. to 3 hrs. and 45 mins."
```

### 2. `CookingDuration` permitted an inverted range

The value object checked that both bounds were non-negative but never that
`Maximum >= Minimum`, so `new CookingDuration(5h, 1h)` was representable and
formatted as *"between of 5 hrs. and 0 mins. to 1 hrs. and 0 mins."*.

The constructor now rejects it. This is the defence-in-depth layer behind the
`RangeMeal` fix: even if a future strategy produced an inverted range, the
value object refuses to represent it.

### 3. `RangeCookingStrategy` permitted an inverted per-pound range

`new RangeCookingStrategy(45, 40)` was accepted and produced an inverted
duration. The strategy now rejects `max < min` in its constructor, naming the
actual cause — a bad catalog entry — rather than surfacing it later as an
arithmetic error.

This one is **latent, not active**: all 10 entries in the shipped
`appsettings.json` were verified to have `min ≤ max`. It is fixed because the
catalog is configuration, and configuration is exactly the thing that changes
without a code review.

### 4. `Weight` learned to order itself

The range guard needed `weight < MinimumWeight`, and `Weight` is a
`readonly record struct` — it had equality but no ordering. Rather than
reaching into `.Pounds` at every comparison site, `Weight` now implements
`IComparable<Weight>` and the four relational operators.

A value object whose entire job is to carry a magnitude should support
comparing two instances of itself. `CompareTo` orders by `Pounds`; because
the public factories reject non-positive weights, no ordering ambiguity
arises.

## Why no test caught this

| Layer | Result | Why |
| --- | --- | --- |
| `dotnet build` | green | Not a compile error. |
| `dotnet test` | 158 facts green | No test asserted that a Range meal rejects an out-of-range weight. The existing `CalculateAsync_MealRejectsWeight_ReturnsValidationError` test used a **mocked** meal that threw — it proved the *calculator* handled a throw, never that a real meal threw. |
| Review | found it | Only reading `RangeMeal.Calculate` and noticing it never touched its own `MinimumWeight`/`MaximumWeight` surfaced the gap. |

## New tests (12 facts)

| Suite | File | Facts | Covers |
| --- | --- | --- | --- |
| Unit | `Domain/Meals/RangeMealTests.cs` | +4 | Below-minimum throws; above-maximum throws; both boundaries are inclusive |
| Unit | `Domain/ValueObjects/CookingDurationFormatTests.cs` | +2 | `Maximum < Minimum` throws; `Maximum == Minimum` is allowed |
| Unit | `Domain/ValueObjects/WeightTests.cs` | +2 | Relational operators order by pounds; `CompareTo` signs |
| Unit | `Domain/Strategies/RangeCookingStrategyTests.cs` | +1 | `max < min` throws |
| Unit | `Application/CookingTimeCalculatorTests.cs` | +3 | **End-to-end through the real `RangeMeal`**: above-maximum fails, below-minimum fails, in-range still succeeds |

The three application-level tests use the **real** `RangeMeal`, not a mock.
That is the point: the mock-based test in the suite could never have caught
this, because the mock threw on demand.

All six guard tests were verified to **fail** on the pre-fix domain files and
**pass** after the fix — they are real guards, not formalities.

## Validation

```
dotnet build -c Release   → 0 Warning(s), 0 Error(s)
dotnet test  -c Release   → UnitTests 124 · ComponentTests 16 · IntegrationTests 30
                             = 170 facts, 0 failed, 0 skipped   (was 158)
```

## Anti-patterns to avoid

- **Don't let a value object hold a range it never enforces.** `RangeMeal`
  exposed `MinimumWeight`/`MaximumWeight` and then ignored them. A property
  that looks like a constraint but isn't one is worse than no property.
- **Don't put a range check in the wrong layer.** The strategy is a pure
  formula and cannot know the meal's limits; the meal is the only place that
  can. Adding the check to the strategy would have needed the strategy to
  take the limits as parameters — leaking meal knowledge downward.
- **Don't rely on a mock to prove a domain invariant.** A mock that throws on
  demand proves the caller handles a throw. It can never prove the throw
  happens. Domain invariants need the real type.
- **Don't let a value object represent an impossible state.** `CookingDuration`
  could hold a maximum below its minimum. Guard the invariant in the
  constructor so the invalid state is *unrepresentable*.

## Out of scope

- The catalog-lookup refactor (meal lookup by name, duplicate-name rejection)
  — Phase 17.
- Startup validation of `appsettings.json` — Phase 15.
- The `Reset()` stale-validation defect — Phase 17.
