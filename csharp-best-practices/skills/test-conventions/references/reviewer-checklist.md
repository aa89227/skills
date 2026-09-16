# Test Conventions — Reviewer Checklist

When reviewing test code, you **must** use the Todo tool or create a checklist file to track each item and ensure every check is completed.

## Checklist

### Framework Selection
- [ ] Target test project's framework is identified from the user choice or existing project convention
- [ ] New projects without a convention explicitly choose NUnit or xUnit before scaffolding
- [ ] Test attributes, lifecycle hooks, fixtures, assertions, and packages are not mixed across frameworks

### Test Declaration and BDD Metadata
- [ ] NUnit uses `[Test]`; xUnit uses `[Fact]` or `[Theory]`
- [ ] NUnit: every `[Test]` method has a `[Description]` attribute
- [ ] NUnit: `[Description]` content follows Given/When/Then format
- [ ] xUnit: method name clearly expresses the Given/When/Then scenario
- [ ] Given describes the initial state and preconditions
- [ ] When describes the action under test
- [ ] Then describes the expected outcome

### Method Naming
- [ ] Method name is a scenario description using domain language

### Method Structure
- [ ] Method body contains `// Arrange`, `// Act`, `// Assert` section comments
- [ ] Three phases are in correct order
- [ ] Multi-step scenarios use repeated `// Act` + `// Assert` pairs

### Scenario Scope
- [ ] Each test method verifies only one scenario
- [ ] No unrelated assertions mixed in a single test
