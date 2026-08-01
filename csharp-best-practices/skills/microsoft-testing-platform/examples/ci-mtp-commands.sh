#!/usr/bin/env bash
set -euo pipefail

# Native .NET 10 MTP: global.json selects Microsoft.Testing.Platform, so platform options are
# passed directly. The project must reference the TRX and coverage extensions for these options.
# See references/extensions-and-ci.md#ci-command-patterns.
dotnet test --solution ./MyTests.slnx \
  --results-directory ./TestResults \
  --report-trx \
  --coverage \
  --coverage-output-format cobertura \
  --minimum-expected-tests 1

# Inspect one module without executing it.
dotnet run --project tests/Orders.Tests -- --info
dotnet run --project tests/Orders.Tests -- --list-tests

# Run an already-built MTP DLL directly. Use the DLL with dotnet exec, not the generated EXE.
dotnet exec tests/Orders.Tests/bin/Release/net10.0/Orders.Tests.dll \
  --results-directory ./TestResults/direct \
  --diagnostic \
  --diagnostic-output-directory ./TestResults/direct/diagnostics

# .NET 9-and-earlier compatibility bridge only: the extra separator is required there.
# dotnet test --project tests/Orders.Tests -- --report-trx --results-directory ./TestResults
