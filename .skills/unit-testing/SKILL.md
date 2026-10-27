---
name: Unit Testing
description: Use when adding, changing, or reviewing unit tests in this repository. Covers the repository's MSTest setup, required assertions, deterministic test data, fakes, and the commands used to verify test changes.
---

# Unit Testing

Use this skill for any task that creates, updates, or reviews tests in this
repository. It describes the test stack and the minimum standards a test change
must meet.

## Test stack and layout

- Tests use **MSTest** from `Microsoft.VisualStudio.TestTools.UnitTesting`.
  Do not introduce xUnit, NUnit, or another test framework unless the project is
  deliberately migrated.
- All tests belong in `Source/ROOT.Shared.Utils.Tests`.
- Production projects target `net10.0` through `Source/Directory.Build.props`.
  Package versions are managed centrally in `Source/Directory.Packages.props`.
  Do not add a `TargetFramework` or versions to individual package references.
- Test files are named after the unit under test, for example
  `TypeDumperTest.cs`, `ObjectDumperTest.cs`, or `TarTest.cs`.
- Use namespace `ROOT.Shared.Utils.Tests` or a subnamespace that mirrors a test
  subfolder, such as `ROOT.Shared.Utils.Tests.IPMI`.

## Required test structure

- Mark test classes with `[TestClass]` and test methods with `[TestMethod]`.
- Use descriptive PascalCase names that state the behavior, for example
  `GenericDump_ForClass_UsesSimpleFormatterByDefault` or
  `Create_ForType_CachesDumpersPerType`.
- Follow Arrange / Act / Assert. Keep each test focused on one behavior.
- **Every test must assert observable behavior.** `Console.WriteLine` may be kept
  for diagnostics, but it is never a substitute for an assertion.
- Assert exact values whenever the behavior is deterministic: use
  `Assert.AreEqual`, `Assert.AreSame`, `Assert.IsTrue`, `Assert.IsNull`,
  `Assert.IsInstanceOfType`, and so on.
- Prefer meaningful assertions over "does not throw". Assert the returned value,
  mutated state, or the exact exception type.
- MSTest 4 is used in this repository. `Assert.ThrowsException<T>` is not
  available; use `Assert.ThrowsExactly<T>` when the exact exception type matters,
  or `Assert.Throws<T>` when a derived exception is acceptable.
- Do not weaken or remove existing assertions just to make a change pass.
- Do not **EVER** add // Arrange, // ACT, // Asert comments

## Determinism and isolation

New tests must produce the same result on every machine and every run.

- Do not use `Guid.NewGuid()`, `DateTime.Now`, `DateTime.UtcNow`, random values,
  or other changing input when asserting output. Use a fixed value, for example
  `Guid.Parse("11111111-2222-3333-4444-555555555555")` or an explicit
  `DateTime` with an explicit `DateTimeKind`.
- Formatting code may depend on the current culture. When asserting formatted
  strings, pin `CultureInfo.CurrentCulture = CultureInfo.InvariantCulture` and
  restore the original culture in a `finally` block. The same applies to
  time-zone-sensitive values.
- Do not depend on live SSH hosts, IPMI endpoints, network services, or external
  commands in a unit test. Use a fake implementation instead; see
  `MockProcessCall.cs` for the repository's hand-written `IProcessCall` fake.
- The repository has no mocking framework. Prefer a small hand-written fake or
  stub. Only add a mocking package if it is clearly justified, and add its
  version to `Directory.Packages.props`.
- If adding a mocking framework, only add Moq. No other mocking framework should be used.
- Use `Path.GetTempPath()` for file-system tests and delete temporary files and
  directories in a `finally` block. See `TarTest.cs`.
- Platform-specific behavior must be guarded. Existing tests use
  `RuntimeInformation.IsOSPlatform(...)`; use `Assert.Inconclusive` if the test
  cannot run on the current platform so the suite reports it as not applicable
  rather than silently passing.
- Tests that require external infrastructure must be marked
  `[Ignore("reason")]`, as done in `IPMIClientTest.cs`, or moved to an explicitly
  separate integration-test suite. Do not add new live-service tests to the
  normal unit-test path.

## Patterns used in this repository

Parameterized tests use `[TestMethod]` with `[DataRow]` and method parameters:

```csharp
[TestMethod]
[DataRow(true)]
[DataRow(false)]
public void ProcessCallExecuteTest(bool useShell)
{
    var sut = new Something(...);
}
```

Factory and cache behavior should verify both behavior and identity:

```csharp
var first = TypeDumper.Create(typeof(MyType));
var second = TypeDumper.Create(typeof(MyType));

Assert.AreSame(first, second);
```

Complex output should be asserted exactly rather than printed:

```csharp
var result = value.Dump(new JsonFormatter());

Assert.AreEqual("{\"Name\":\"test\",\"Count\":5,\"Enabled\":true}", result);
```

When fixing a test that previously used non-deterministic data, replace the
data with a fixed value and assert the resulting output exactly.

## Coverage expectations

- Cover the public API and overloads that changed, including the non-generic and
  generic variants when both exist.
- Include null, empty/default, nested/complex, collection, and invalid-argument
  cases where they are meaningful.
- Test caching, factory selection, and other stateful behavior explicitly.
- Do not write tests that only exercise framework behavior or mirror the
  implementation line by line.

Useful reference tests:

- `ObjectDumperTest.cs` - exact string assertions and deterministic input.
- `TypeDumperTest.cs` - generic and non-generic factories, caching, nulls,
  formatters, and expression helpers.
- `ProcessCallTest.cs` - platform guards, `DataRow`, and fake `IProcessCall`.
- `ServiceCollectionExtensionTest.cs` - dependency-injection resolution tests.
- `MockProcessCall.cs` - hand-written fake pattern.

Some older tests in the repository only print diagnostics or call live hosts.
They are not a template for new tests.

## Verification workflow

From the repository root:

```bash
# Build the solution
dotnet build Source/ROOT.Shared.Utils.slnx

# Run the full test suite
dotnet test Source/ROOT.Shared.Utils.slnx

# Run a focused test class while iterating
dotnet test Source/ROOT.Shared.Utils.Tests/ROOT.Shared.Utils.Tests.csproj \
  --filter FullyQualifiedName~TypeDumperTest
```

Before declaring a test change complete:

1. Every new or changed test has at least one meaningful assertion.
2. The test is deterministic and does not depend on the current time, culture,
   random values, or external services.
3. Temporary files and resources are cleaned up.
4. The focused tests pass.
5. The full test suite passes.
