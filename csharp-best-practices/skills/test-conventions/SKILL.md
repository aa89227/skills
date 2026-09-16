---
name: test-conventions
description: |
  Framework-neutral test structure conventions for NUnit or xUnit using Given/When/Then BDD format
  and Arrange/Act/Assert comments inside test methods. Use when writing or reviewing any test class
  to ensure the selected framework is applied consistently with readable scenarios.
  Trigger phrases: "test convention", "test structure", "Given When Then", "Arrange Act Assert",
  "test description", "Scenario test", "BDD test", "write test", "NUnit test", "xUnit test".
license: MIT
metadata:
  author: aa89227
  version: "1.1"
  tags: ["testing", "conventions", "nunit", "xunit", "bdd", "best-practices"]
---

# Test Conventions

## Instruction Retention

Read this file when this skill is first activated for the current task. Once read, retain and
follow its instructions without rereading the file on every turn or before every action.

Reread it only when the file may have changed, the current context no longer contains its
instructions (for example after context compaction or a new session), the instructions are
ambiguous or conflicting, or exact wording must be verified.

**Supported frameworks:** NUnit or xUnit.net. Choose one framework for the target test project and
apply its attributes, lifecycle, assertions, and test-project packages consistently.

## Choose a Test Framework First

Before writing or scaffolding tests:

1. Inspect the target test project and nearby tests for an existing framework. Preserve an
   established NUnit or xUnit convention; do not migrate it implicitly.
2. If the project is new or has no established convention and the user has not specified one, ask
   the user to choose **NUnit or xUnit** before generating test code or installing test packages.
3. Use only the selected framework's test attributes, lifecycle hooks, fixtures, assertions, and
   integration packages. Do not mix NUnit and xUnit APIs in one test project.
4. The order of the examples in this file is not a default. Never select NUnit merely because an
   example or a template appears first.

## General Rules

- Every test method **must** express its scenario in Given/When/Then form. NUnit may carry the
  text in `[Description]`; xUnit should express the same intent in a descriptive method name and,
  when useful, searchable traits.
- Test method name is the **Scenario** summary in the domain language.
- Inside the method, use `// Arrange`, `// Act`, `// Assert` comments to separate phases.

## Framework Mapping

| Concern | NUnit | xUnit.net |
|---|---|---|
| Test declaration | `[Test]` | `[Fact]` or `[Theory]` |
| Scenario metadata | `[Description("""...""")]` | Descriptive method name; optional `[Trait]` |
| Per-test lifecycle | `[SetUp]` / `[TearDown]` when needed | Constructor or `IAsyncLifetime` when needed |
| Assertions | `Assert.That(actual, Is.EqualTo(expected))` | `Assert.Equal(expected, actual)` |

## Test Structure

### NUnit: `[Description]` Format

```csharp
[Test]
[Description("""
Given: A category exists in the system
And: The category contains two products
When: Query the product list for that category
Then: Should return two products
""")]
public async Task QueryProductListForCategory()
{
    // Arrange
    var categoryId = await Server.CreateCategory();
    await Server.CreateProduct(categoryId, "Widget A");
    await Server.CreateProduct(categoryId, "Widget B");

    // Act
    var response = await Client.Product().GetProductsByCategoryAsync(categoryId);

    // Assert
    response.EnsureSuccessStatusCode();
    var json = await response.Content.ReadAsStringAsync();
    await VerifyJsonSnapshotAsync(json, "Product.QueryByCategory");
}
```

### xUnit: method-name BDD format

xUnit has no built-in equivalent of NUnit's multiline `[Description]`. Keep the same BDD content
in the method name and use `[Fact]` or `[Theory]`:

```csharp
[Fact]
public async Task QueryProductListForCategory_WhenCategoryHasTwoProducts_ReturnsTwoProducts()
{
    // Arrange
    var categoryId = await Server.CreateCategory();
    await Server.CreateProduct(categoryId, "Widget A");
    await Server.CreateProduct(categoryId, "Widget B");

    // Act
    var response = await Client.Product().GetProductsByCategoryAsync(categoryId);

    // Assert
    response.EnsureSuccessStatusCode();
    var json = await response.Content.ReadAsStringAsync();
    await VerifyJsonSnapshotAsync(json, "Product.QueryByCategory");
}
```

### Expect List (optional — under Then)

When `Then` alone cannot clearly express what the test focuses on (e.g., complex responses, framework-level assertions), add a bullet list of **behavior-level** expectations:

The example below uses NUnit's `[Description]`. For xUnit, omit the NUnit attributes and encode the
same scenario in the test method name as shown in the xUnit example above.

```csharp
[Test]
[Description("""
Given: A category exists in the system
And: The category contains two products and one archived product
When: Query the product list for that category
Then: Should return the active product list
- contains exactly 2 items
- excludes archived products
- items sorted by name ascending
""")]
public async Task QueryActiveProductListForCategory()
{
    // Arrange
    var categoryId = await Server.CreateCategory();
    await Server.CreateProduct(categoryId, "Widget B");
    await Server.CreateProduct(categoryId, "Widget A");
    await Server.CreateArchivedProduct(categoryId, "Old Widget");

    // Act
    var response = await Client.Product().GetProductsByCategoryAsync(categoryId);

    // Assert
    response.EnsureSuccessStatusCode();
    var json = await response.Content.ReadAsStringAsync();
    await VerifyJsonSnapshotAsync(json, "Product.QueryActiveByCategory");
}
```

Rules:
- List items describe **behavior / focus**, not field-level details (field validation is the snapshot's job).
- Only add when `Then` alone is ambiguous — simple scenarios don't need it.
- Each bullet should answer "what aspect of the response are we verifying?"

### Multi-step Scenario (optional When/Then)

When a test needs to verify a sequence of behaviors:

The example below uses NUnit. For xUnit, use `[Fact]`, a descriptive method name, and the xUnit
assertion API while retaining the repeated `// Act` and `// Assert` phases.

```csharp
[Test]
[Description("""
Given: A category exists in the system
And: The category contains one tag
When: Remove the tag from the category
Then: The tag should be removed successfully
When: Query the category tags
Then: Should return an empty list
""")]
public async Task RemoveTagThenQueryShouldBeEmpty()
{
    // Arrange
    var categoryId = await Given.CategoryExists();
    var tagId = await Given.TagExistsInCategory(categoryId);

    // Act
    var deleteResponse = await Client.Category().RemoveTagAsync(categoryId, tagId);

    // Assert
    deleteResponse.EnsureSuccessStatusCode();

    // Act
    var queryResponse = await Client.Category().GetTagsAsync<List<object>>(categoryId);

    // Assert
    Assert.That(queryResponse, Is.Empty);
}
```

### Description Field Reference

| Field | Required | Description |
|---|---|---|
| `Given:` | Yes | Initial state / preconditions |
| `And:` | No | Additional preconditions or steps (repeatable) |
| `When:` | Yes | The action being tested |
| `Then:` | Yes | Expected outcome |
| Additional `When:/Then:` | No | For multi-step behavior verification |
| Expect list (under `Then:`) | No | Behavior-level bullets clarifying test focus when `Then` is ambiguous |

## Rules

1. **Framework is explicit** — preserve the repository convention or obtain the user's NUnit/xUnit
   choice before scaffolding a new test project.
2. **Use the selected declaration** — `[Test]` for NUnit; `[Fact]`/`[Theory]` for xUnit.
3. **Describe the scenario** — NUnit uses a mandatory `[Description]`; xUnit uses a descriptive
   method name because it has no equivalent built-in multiline description attribute.
4. **`// Arrange`, `// Act`, `// Assert`** — always present, in this order.
5. **Multi-step**: repeat `// Act` and `// Assert` pairs when verifying sequential behaviors.
6. **One scenario per test** — don't combine unrelated assertions.
7. **Expect list is optional** — only add when `Then` alone cannot clarify what the test focuses on.
   List behavior-level expectations, not field-level details.

## Additional Resources

### Reference Files

- **`references/reviewer-checklist.md`** — Reviewer checklist for verifying test convention compliance
