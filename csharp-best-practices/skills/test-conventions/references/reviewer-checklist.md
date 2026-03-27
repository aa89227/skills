# Test Conventions — Reviewer Checklist

When reviewing test code, you **must** use the Todo tool or create a checklist file to track each item and ensure every check is completed.

## Checklist

### [Description] Attribute
- [ ] Every `[Test]` method has a `[Description]` attribute
- [ ] `[Description]` content follows Given/When/Then format
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
