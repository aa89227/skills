---
name: test-conventions
description: |
  General test structure conventions using NUnit [Description] with Given/When/Then BDD format
  and Arrange/Act/Assert comments inside test methods. Use when writing or reviewing any test class
  to ensure consistent test structure and readability.
  Trigger phrases: "test convention", "test structure", "Given When Then", "Arrange Act Assert",
  "test description", "Scenario test", "BDD test", "write test".
license: MIT
metadata:
  author: aa89227
  version: "1.0"
  tags: ["testing", "conventions", "nunit", "bdd", "best-practices"]
---

# Test Conventions

**Framework:** NUnit 4.x

## General Rules

- Every test method **must** have a `[Description]` attribute describing the scenario in Given/When/Then format.
- Test method name is the **Scenario** summary in the domain language.
- Inside the method, use `// Arrange`, `// Act`, `// Assert` comments to separate phases.

## Test Structure

### [Description] Format

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

### Expect List (optional — under Then)

When `Then` alone cannot clearly express what the test focuses on (e.g., complex responses, framework-level assertions), add a bullet list of **behavior-level** expectations:

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

1. **`[Description]` is mandatory** — every `[Test]` method must have one.
2. **Scenario as method name** — concise, in domain language, describes the scenario.
3. **`// Arrange`, `// Act`, `// Assert`** — always present, in this order.
4. **Multi-step**: repeat `// Act` and `// Assert` pairs when verifying sequential behaviors.
5. **One scenario per test** — don't combine unrelated assertions.
6. **Expect list is optional** — only add when `Then` alone cannot clarify what the test focuses on. List behavior-level expectations, not field-level details.

## Additional Resources

### Reference Files

- **`references/reviewer-checklist.md`** — Reviewer checklist for verifying test convention compliance
