# TJProcessor Emission Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship five fixes against the updated `pub-api.mark.tj` integration: `format` on emission requests, GS strip on UI download, GS insert on application/aggregation codes, `productionDate` + `locationUuid` on application body, and corrected emission-status mapping so auto-download fires.

**Architecture:** Surgical inline edits to existing touch-points, plus one new pure static helper (`GS1CodeHelper`) that owns all GS insert/strip logic. One new xUnit test project scoped to `GS1CodeHelper` only. Configuration added under the existing `TJConnection` section. No DI abstractions, no feature flags, no schema changes.

**Tech Stack:** .NET 8, EF Core 9, MassTransit 8.3, System.Text.Json, xUnit.

**Spec:** `docs/superpowers/specs/2026-04-15-tjprocessor-emission-fixes-design.md`

---

## Pre-flight

- [ ] **Step 0.1: Read the spec**

Read `docs/superpowers/specs/2026-04-15-tjprocessor-emission-fixes-design.md` end-to-end. Every task in this plan implements a specific section of that spec and must match it.

- [ ] **Step 0.2: Confirm `appsettings.json` exists locally**

`TJConnector.Api/appsettings.json` is gitignored. Confirm the file exists locally and contains a `TJConnection` section with real `BaseURL` + `Token`. If it does not, ask the user for credentials before proceeding — do NOT commit credentials.

Run:
```bash
ls TJConnector.Api/appsettings.json
```
Expected: the file exists.

---

## Task 1: Create `TJConnector.StateSystem.Tests` xUnit project

**Files:**
- Create: `TJConnector.StateSystem.Tests/TJConnector.StateSystem.Tests.csproj`
- Create: `TJConnector.StateSystem.Tests/GS1CodeHelperTests.cs` (empty placeholder this task)
- Modify: `TJConnector.sln`

- [ ] **Step 1.1: Scaffold the project**

Run from repo root:
```bash
dotnet new xunit -o TJConnector.StateSystem.Tests -n TJConnector.StateSystem.Tests --framework net8.0
```
Expected: creates `.csproj` and a sample `UnitTest1.cs`.

- [ ] **Step 1.2: Delete the scaffold sample test file**

Remove `TJConnector.StateSystem.Tests/UnitTest1.cs`.

- [ ] **Step 1.3: Add project reference to `TJConnector.StateSystem`**

Run:
```bash
dotnet add TJConnector.StateSystem.Tests/TJConnector.StateSystem.Tests.csproj reference TJConnector.StateSystem/TJConnector.StateSystem.csproj
```
Expected: reference added, no error.

- [ ] **Step 1.4: Add test project to solution**

Run:
```bash
dotnet sln TJConnector.sln add TJConnector.StateSystem.Tests/TJConnector.StateSystem.Tests.csproj
```
Expected: project added to `.sln`.

- [ ] **Step 1.5: Create empty test file**

Create `TJConnector.StateSystem.Tests/GS1CodeHelperTests.cs` with:

```csharp
using TJConnector.StateSystem.Helpers;
using Xunit;

namespace TJConnector.StateSystem.Tests;

public class GS1CodeHelperTests
{
}
```

- [ ] **Step 1.6: Build to confirm project compiles**

Run:
```bash
dotnet build TJConnector.StateSystem.Tests/TJConnector.StateSystem.Tests.csproj
```
Expected: **FAIL** — compiler error `CS0234` because `TJConnector.StateSystem.Helpers.GS1CodeHelper` does not exist yet. This is the correct "red" state for TDD.

- [ ] **Step 1.7: Commit**

```bash
git add TJConnector.sln TJConnector.StateSystem.Tests/
git commit -m "test: add TJConnector.StateSystem.Tests xunit project"
```

---

## Task 2: TDD `GS1CodeHelper.StripGroupSeparators`

**Files:**
- Create: `TJConnector.StateSystem/Helpers/GS1CodeHelper.cs`
- Modify: `TJConnector.StateSystem.Tests/GS1CodeHelperTests.cs`

- [ ] **Step 2.1: Write the failing tests**

Replace the body of `TJConnector.StateSystem.Tests/GS1CodeHelperTests.cs` with:

```csharp
using TJConnector.StateSystem.Helpers;
using Xunit;

namespace TJConnector.StateSystem.Tests;

public class GS1CodeHelperTests
{
    // ── StripGroupSeparators ────────────────────────────────────────────

    [Fact]
    public void Strip_NullInput_ReturnsNull()
    {
        Assert.Null(GS1CodeHelper.StripGroupSeparators(null!));
    }

    [Fact]
    public void Strip_EmptyInput_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, GS1CodeHelper.StripGroupSeparators(string.Empty));
    }

    [Fact]
    public void Strip_NoGS_ReturnsInputUnchanged()
    {
        const string input = "0104600200000000214567890193abcd";
        Assert.Equal(input, GS1CodeHelper.StripGroupSeparators(input));
    }

    [Fact]
    public void Strip_SingleGS_RemovesIt()
    {
        const string input  = "010460020000000021+UyLGXyZ\u001d93444b155b";
        const string expect = "010460020000000021+UyLGXyZ93444b155b";
        Assert.Equal(expect, GS1CodeHelper.StripGroupSeparators(input));
    }

    [Fact]
    public void Strip_MultipleGS_RemovesAll()
    {
        const string input  = "abc\u001ddef\u001dghi";
        const string expect = "abcdefghi";
        Assert.Equal(expect, GS1CodeHelper.StripGroupSeparators(input));
    }
}
```

- [ ] **Step 2.2: Run tests and confirm they fail to compile**

Run:
```bash
dotnet test TJConnector.StateSystem.Tests/TJConnector.StateSystem.Tests.csproj
```
Expected: **build FAIL** — `GS1CodeHelper` does not exist (`CS0103` / `CS0234`).

- [ ] **Step 2.3: Create `GS1CodeHelper.cs` with `StripGroupSeparators` only**

Create `TJConnector.StateSystem/Helpers/GS1CodeHelper.cs`:

```csharp
namespace TJConnector.StateSystem.Helpers;

public static class GS1CodeHelper
{
    private const char GS = '\u001d';

    /// <summary>
    /// Removes every GS (0x1D) character from the code. Safe no-op on null or empty.
    /// </summary>
    public static string StripGroupSeparators(string code)
        => string.IsNullOrEmpty(code) ? code : code.Replace("\u001d", string.Empty);
}
```

- [ ] **Step 2.4: Run tests and confirm all 5 pass**

Run:
```bash
dotnet test TJConnector.StateSystem.Tests/TJConnector.StateSystem.Tests.csproj
```
Expected: **PASS** — 5/5 tests green.

- [ ] **Step 2.5: Commit**

```bash
git add TJConnector.StateSystem/Helpers/GS1CodeHelper.cs TJConnector.StateSystem.Tests/GS1CodeHelperTests.cs
git commit -m "feat: GS1CodeHelper.StripGroupSeparators with tests"
```

---

## Task 3: TDD `GS1CodeHelper.TryInsertGroupSeparator`

**Files:**
- Modify: `TJConnector.StateSystem/Helpers/GS1CodeHelper.cs`
- Modify: `TJConnector.StateSystem.Tests/GS1CodeHelperTests.cs`

- [ ] **Step 3.1: Write the failing tests — append to `GS1CodeHelperTests.cs`**

Add the following methods inside the existing `GS1CodeHelperTests` class (below the Strip tests):

```csharp
    // ── TryInsertGroupSeparator ────────────────────────────────────────

    [Fact]
    public void TryInsert_NullInput_ReturnsFalse()
    {
        var ok = GS1CodeHelper.TryInsertGroupSeparator(null!, out var result);
        Assert.False(ok);
        Assert.Null(result);
    }

    [Fact]
    public void TryInsert_EmptyInput_ReturnsFalse()
    {
        var ok = GS1CodeHelper.TryInsertGroupSeparator(string.Empty, out var result);
        Assert.False(ok);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void TryInsert_WellFormedCode_InsertsGSAtIndex26()
    {
        // 01 + 14 digit GTIN + 21 + 8 char serial + 93 + rest
        const string input  = "010460020000000021+UyLGXyZ93444b155b";
        const string expect = "010460020000000021+UyLGXyZ\u001d93444b155b";
        var ok = GS1CodeHelper.TryInsertGroupSeparator(input, out var result);
        Assert.True(ok);
        Assert.Equal(expect, result);
    }

    [Fact]
    public void TryInsert_CodeAlreadyHasGS_IdempotentReturnsInputUnchanged()
    {
        const string input = "010460020000000021+UyLGXyZ\u001d93444b155b";
        var ok = GS1CodeHelper.TryInsertGroupSeparator(input, out var result);
        Assert.True(ok);
        Assert.Equal(input, result);
    }

    [Fact]
    public void TryInsert_TooShort_ReturnsFalse()
    {
        // 27 chars, < 28 — no room for 93 at index 26..27
        const string input = "010460020000000021+UyLGXyZ9";
        var ok = GS1CodeHelper.TryInsertGroupSeparator(input, out var result);
        Assert.False(ok);
        Assert.Equal(input, result);
    }

    [Fact]
    public void TryInsert_NoAI93AtIndex26_ReturnsFalse()
    {
        // 28 chars but index 26..27 is "10", not "93"
        const string input = "010460020000000021+UyLGXyZ10444b155b";
        var ok = GS1CodeHelper.TryInsertGroupSeparator(input, out var result);
        Assert.False(ok);
        Assert.Equal(input, result);
    }
}
```

- [ ] **Step 3.2: Run tests and confirm the 6 new tests fail**

Run:
```bash
dotnet test TJConnector.StateSystem.Tests/TJConnector.StateSystem.Tests.csproj
```
Expected: **build FAIL** — `TryInsertGroupSeparator` does not exist.

- [ ] **Step 3.3: Add `TryInsertGroupSeparator` to `GS1CodeHelper.cs`**

Append inside the `GS1CodeHelper` class (below `StripGroupSeparators`):

```csharp
    /// <summary>
    /// Inserts a GS (0x1D) before the "93" AI in a GS1 pack code.
    /// Expected input (without GS): 01&lt;14-digit GTIN&gt;21&lt;8-char serial&gt;93&lt;rest&gt;
    /// Fixed prefix length before "93" = 2 + 14 + 2 + 8 = 26.
    /// Returns true on success (or idempotently when the code already contains a GS).
    /// Returns false when the input doesn't match the expected shape — caller must log and abort, not send.
    /// </summary>
    public static bool TryInsertGroupSeparator(string code, out string result)
    {
        result = code;
        if (string.IsNullOrEmpty(code)) return false;
        if (code.Contains(GS)) return true;
        if (code.Length < 28) return false;
        if (code[26] != '9' || code[27] != '3') return false;
        result = code.Insert(26, "\u001d");
        return true;
    }
```

- [ ] **Step 3.4: Run tests and confirm all 11 pass**

Run:
```bash
dotnet test TJConnector.StateSystem.Tests/TJConnector.StateSystem.Tests.csproj
```
Expected: **PASS** — 11/11 tests green.

- [ ] **Step 3.5: Commit**

```bash
git add TJConnector.StateSystem/Helpers/GS1CodeHelper.cs TJConnector.StateSystem.Tests/GS1CodeHelperTests.cs
git commit -m "feat: GS1CodeHelper.TryInsertGroupSeparator with tests"
```

---

## Task 4: Add `format` field to `EmissionCreateRequest`

**Files:**
- Modify: `TJConnector.StateSystem/Model/ExternalRequests/MarkingCode/EmissionCreateRequest.cs`

- [ ] **Step 4.1: Replace file contents**

Overwrite `TJConnector.StateSystem/Model/ExternalRequests/MarkingCode/EmissionCreateRequest.cs` with:

```csharp
using System.Text.Json.Serialization;

namespace TJConnector.StateSystem.Model.ExternalRequests.MarkingCode
{
    public class EmissionCreateRequest
    {
        public int codesCount { get; set; }
        public Guid? productUuid { get; set; }
        public Guid? markingLineUuid { get; set; }
        public Guid? factoryUuid { get; set; }
        public sbyte Type { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? format { get; set; }
    }
}
```

- [ ] **Step 4.2: Build the solution**

Run:
```bash
dotnet build TJConnector.sln
```
Expected: build succeeds (no new usages of `format` yet, so no compile errors).

- [ ] **Step 4.3: Commit**

```bash
git add TJConnector.StateSystem/Model/ExternalRequests/MarkingCode/EmissionCreateRequest.cs
git commit -m "feat: add nullable format field to EmissionCreateRequest"
```

---

## Task 5: Add `productionDate` field to `ApplicationCreateRequest`

**Files:**
- Modify: `TJConnector.StateSystem/Model/ExternalRequests/MarkingCode/ApplicationCreateRequest.cs`

- [ ] **Step 5.1: Replace file contents**

Overwrite `TJConnector.StateSystem/Model/ExternalRequests/MarkingCode/ApplicationCreateRequest.cs` with:

```csharp
namespace TJConnector.StateSystem.Model.ExternalRequests.MarkingCode
{
    public class ApplicationCreateRequest
    {
        public DateTimeOffset? applicationDate { get; set; }
        public DateTimeOffset? productionDate { get; set; }
        public string[] codes { get; set; } = new string[0];
        public Guid factoryUuid { get; set; }
        public List<GroupCode> groupCodes { get; set; } = new List<GroupCode>();
        public Guid? locationUuid { get; set; }
        public Guid? markingLineUuid { get; set; }
        public sbyte result { get; set; } = 0;
        public sbyte type { get; set; } = 2;
    }
    public class GroupCode
    {
        public string groupCode { get; set; } = string.Empty;
        public string[] codes { get; set; } = new string[0];
    }
}
```

- [ ] **Step 5.2: Build the solution**

Run:
```bash
dotnet build TJConnector.sln
```
Expected: build succeeds.

- [ ] **Step 5.3: Commit**

```bash
git add TJConnector.StateSystem/Model/ExternalRequests/MarkingCode/ApplicationCreateRequest.cs
git commit -m "feat: add productionDate to ApplicationCreateRequest"
```

---

## Task 6: Add `EmissionCodeFormat` config section

**Files:**
- Modify: `TJConnector.Api/appsettings.json` (gitignored — do NOT commit credentials)

- [ ] **Step 6.1: Open `TJConnector.Api/appsettings.json`**

Read the file. Find the existing `TJConnection` section.

- [ ] **Step 6.2: Add `EmissionCodeFormat` subsection**

Inside the existing `TJConnection` object, alongside `BaseURL` and `Token`, add:

```json
"EmissionCodeFormat": {
  "Pack":   0,
  "Bundle": 1
}
```

Final shape (example — keep whatever real `BaseURL` / `Token` values already exist, do not overwrite them):
```json
"TJConnection": {
  "BaseURL": "<existing>",
  "Token":   "<existing>",
  "EmissionCodeFormat": {
    "Pack":   0,
    "Bundle": 1
  }
}
```

- [ ] **Step 6.3: Do NOT commit**

`appsettings.json` is in `.gitignore`. Confirm with:
```bash
git status TJConnector.Api/appsettings.json
```
Expected: file does not appear in git status (ignored). If it does appear, STOP and ask — do not commit credentials.

---

## Task 7: Wire `format` in `OrderService.CreateOrderAsync`

**Files:**
- Modify: `TJConnector.Api/Services/OrderService.cs`

- [ ] **Step 7.1: Add `Microsoft.Extensions.Configuration` import and `IConfiguration` field**

In `TJConnector.Api/Services/OrderService.cs`, ensure the `using` block includes:

```csharp
using Microsoft.Extensions.Configuration;
```

Replace the `OrderService` class field block (currently `_context`, `_externalEmission`, `_logger`) and its constructor with:

```csharp
public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _context;
    private readonly IExternalEmission _externalEmission;
    private readonly ILogger<OrderService> _logger;
    private readonly IConfiguration _config;

    public OrderService(
        ApplicationDbContext context,
        IExternalEmission externalEmission,
        ILogger<OrderService> logger,
        IConfiguration config)
    {
        _context = context;
        _externalEmission = externalEmission;
        _logger = logger;
        _config = config;
    }
```

- [ ] **Step 7.2: Wire the `format` value into the `EmissionCreateRequest` literal**

In `CreateOrderAsync`, replace the current `emissionRequest` construction (around line 185–192) with:

```csharp
        int? format = order.Type switch
        {
            0 => _config.GetValue<int>("TJConnection:EmissionCodeFormat:Pack",   0),
            1 => _config.GetValue<int>("TJConnection:EmissionCodeFormat:Bundle", 1),
            _ => null
        };

        var emissionRequest = new EmissionCreateRequest
        {
            codesCount      = order.CodesCount,
            productUuid     = order.ProductUuid,
            markingLineUuid = order.MarkingLineUuid,
            factoryUuid     = order.FactoryUuid,
            Type            = (order.Type == 3 ? (sbyte)0 : order.Type),
            format          = format
        };
```

- [ ] **Step 7.3: Build the solution**

Run:
```bash
dotnet build TJConnector.sln
```
Expected: succeeds. `IConfiguration` is already registered by ASP.NET Core's default DI, so no `Program.cs` change is needed.

- [ ] **Step 7.4: Commit**

```bash
git add TJConnector.Api/Services/OrderService.cs
git commit -m "feat: wire EmissionCodeFormat config into CreateOrderAsync"
```

---

## Task 8: Fix emission status mapping in `OrderService.GetExternalOrderByIdAsync`

**Files:**
- Modify: `TJConnector.Api/Services/OrderService.cs`

- [ ] **Step 8.1: Replace the switch expression**

In `TJConnector.Api/Services/OrderService.cs`, inside `GetExternalOrderByIdAsync`, replace:

```csharp
        localOrder.Status = externalOrder.Content.status switch
        {
            0 => -2,
            1 => 2,
            3 => 3,
            4 => 4,
            5 => -3,
            6 => 5,
            _ => -4
        };
```

with:

```csharp
        localOrder.Status = externalOrder.Content.status switch
        {
            0 => -2,
            1 => 2,
            3 => 3,
            4 => 4,   // success — ready for download (was 6 in the old external API)
            5 => -3,
            _ => -4
        };
```

Note: the only change is removal of the `6 => 5` line and addition of the inline comment on `4 => 4`.

- [ ] **Step 8.2: Build the solution**

Run:
```bash
dotnet build TJConnector.sln
```
Expected: succeeds.

- [ ] **Step 8.3: Commit**

```bash
git add TJConnector.Api/Services/OrderService.cs
git commit -m "fix: remove obsolete emission status 6 mapping so auto-download fires on 4"
```

---

## Task 9: `EmissionServiceConsumer` — location lookup + productionDate

**Files:**
- Modify: `TJConnector.Api/Transit/4 EmissionServiceConsumer.cs`

- [ ] **Step 9.1: Read the current consumer file end-to-end**

Open `TJConnector.Api/Transit/4 EmissionServiceConsumer.cs` and read it entirely. You need to know where factory and markingLine are loaded so that the new location fetch is adjacent, and where `applicationBody` is constructed.

- [ ] **Step 9.2: Add location fetch + early-fail immediately after the existing `markingLine` load**

After the block that loads `factory` and `markingLine` and does the early-return if either is missing, add:

```csharp
        var location = await _context.Locations.FirstOrDefaultAsync();
        if (location == null)
        {
            _logger.LogError($"Package {package.SSCCCode}: no location row in DB.");
            package.Comment = "No location configured";
            package.AddStatus(-4);
            _context.Entry(package).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return;
        }
```

- [ ] **Step 9.3: Update the `ApplicationCreateRequest` literal**

Replace the current `applicationBody` construction:

```csharp
        var applicationBody = new ApplicationCreateRequest
        {
            applicationDate = DateTimeOffset.UtcNow.AddHours(-4),
            factoryUuid = factory.ExternalUid,
            markingLineUuid = markingLine.ExternalUid,
            result = 0,
            type = 2,
            groupCodes = package.Content.Select(x => new GroupCode()
            {
                groupCode = x.Bundle,
                codes = x.Packs.ToArray()
            }).ToList()
        };
```

with (note: `groupCodes` is left as a placeholder for now; Task 10 replaces it with the GS-insert loop):

```csharp
        var applicationBody = new ApplicationCreateRequest
        {
            applicationDate = DateTimeOffset.UtcNow.AddHours(-4),
            productionDate  = DateTimeOffset.UtcNow.AddHours(-4),
            factoryUuid     = factory.ExternalUid,
            markingLineUuid = markingLine.ExternalUid,
            locationUuid    = location.ExternalUid,
            result          = 0,
            type            = 2,
            groupCodes      = package.Content.Select(x => new GroupCode()
            {
                groupCode = x.Bundle,
                codes     = x.Packs.ToArray()
            }).ToList()
        };
```

- [ ] **Step 9.4: Build the solution**

Run:
```bash
dotnet build TJConnector.sln
```
Expected: succeeds.

- [ ] **Step 9.5: Commit**

```bash
git add "TJConnector.Api/Transit/4 EmissionServiceConsumer.cs"
git commit -m "feat: populate locationUuid and productionDate on application body"
```

---

## Task 10: `EmissionServiceConsumer` — GS insert loop with hard-fail

**Files:**
- Modify: `TJConnector.Api/Transit/4 EmissionServiceConsumer.cs`

- [ ] **Step 10.1: Add `using` import**

At the top of `TJConnector.Api/Transit/4 EmissionServiceConsumer.cs`, ensure the following using is present:

```csharp
using TJConnector.StateSystem.Helpers;
```

- [ ] **Step 10.2: Build the `groupCodes` list with GS insertion, before the `applicationBody` literal**

Immediately **before** the `applicationBody` construction (the literal added in Task 9.3), insert:

```csharp
        var badCodes = new List<string>();
        var groupCodes = new List<GroupCode>(package.Content.Count);

        foreach (var pc in package.Content)
        {
            var packs = new string[pc.Packs.Length];
            for (int i = 0; i < pc.Packs.Length; i++)
            {
                if (!GS1CodeHelper.TryInsertGroupSeparator(pc.Packs[i], out var inserted))
                    badCodes.Add(pc.Packs[i]);
                packs[i] = inserted;
            }
            groupCodes.Add(new GroupCode { groupCode = pc.Bundle, codes = packs });
        }

        if (badCodes.Count > 0)
        {
            var preview = string.Join(", ", badCodes.Take(5));
            _logger.LogError(
                $"Package {package.SSCCCode}: {badCodes.Count} malformed pack codes, cannot send to apply. Examples: {preview}");
            package.Comment = $"Malformed pack codes ({badCodes.Count}): {preview}";
            package.AddStatus(-4);
            _context.Entry(package).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return;
        }
```

- [ ] **Step 10.3: Update `applicationBody.groupCodes` to use the pre-built `groupCodes` list**

Change the `groupCodes = package.Content.Select(...)` assignment inside the `applicationBody` literal to just:

```csharp
            groupCodes = groupCodes
```

So the final literal looks like:

```csharp
        var applicationBody = new ApplicationCreateRequest
        {
            applicationDate = DateTimeOffset.UtcNow.AddHours(-4),
            productionDate  = DateTimeOffset.UtcNow.AddHours(-4),
            factoryUuid     = factory.ExternalUid,
            markingLineUuid = markingLine.ExternalUid,
            locationUuid    = location.ExternalUid,
            result          = 0,
            type            = 2,
            groupCodes      = groupCodes
        };
```

- [ ] **Step 10.4: Build the solution**

Run:
```bash
dotnet build TJConnector.sln
```
Expected: succeeds.

- [ ] **Step 10.5: Commit**

```bash
git add "TJConnector.Api/Transit/4 EmissionServiceConsumer.cs"
git commit -m "feat: insert GS into application pack codes, hard-fail on malformed"
```

---

## Task 11: Strip GS in `OrderController.DownloadOrderContent`

**Files:**
- Modify: `TJConnector.Api/Controllers/OrderController.cs`

- [ ] **Step 11.1: Add `using` import**

At the top of `TJConnector.Api/Controllers/OrderController.cs`, ensure:

```csharp
using TJConnector.StateSystem.Helpers;
```

- [ ] **Step 11.2: Apply `StripGroupSeparators` to each code before join**

In `DownloadOrderContent`, replace:

```csharp
        var fileContent = string.Join(Environment.NewLine, content.OrderContent);
        return File(Encoding.UTF8.GetBytes(fileContent), "text/plain", $"codes_{id}.txt");
```

with:

```csharp
        var cleaned = content.OrderContent.Select(GS1CodeHelper.StripGroupSeparators);
        var fileContent = string.Join(Environment.NewLine, cleaned);
        return File(Encoding.UTF8.GetBytes(fileContent), "text/plain", $"codes_{id}.txt");
```

- [ ] **Step 11.3: Build the solution**

Run:
```bash
dotnet build TJConnector.sln
```
Expected: succeeds.

- [ ] **Step 11.4: Commit**

```bash
git add TJConnector.Api/Controllers/OrderController.cs
git commit -m "feat: strip GS separators on order download"
```

---

## Task 12: Strip GS in `BatchController.DownloadBatchContent`

**Files:**
- Modify: `TJConnector.Api/Controllers/BatchController.cs`

- [ ] **Step 12.1: Add `using` import**

At the top of `TJConnector.Api/Controllers/BatchController.cs`, ensure:

```csharp
using TJConnector.StateSystem.Helpers;
```

- [ ] **Step 12.2: Apply `StripGroupSeparators` to each code in the accumulation loop**

In `DownloadBatchContent`, replace:

```csharp
            if (!string.IsNullOrEmpty(content))
            {
                content += Environment.NewLine;
            }
            content += string.Join(Environment.NewLine, orderContent.OrderContent);
```

with:

```csharp
            if (!string.IsNullOrEmpty(content))
            {
                content += Environment.NewLine;
            }
            var cleaned = orderContent.OrderContent.Select(GS1CodeHelper.StripGroupSeparators);
            content += string.Join(Environment.NewLine, cleaned);
```

- [ ] **Step 12.3: Build the solution**

Run:
```bash
dotnet build TJConnector.sln
```
Expected: succeeds.

- [ ] **Step 12.4: Commit**

```bash
git add TJConnector.Api/Controllers/BatchController.cs
git commit -m "feat: strip GS separators on batch download"
```

---

## Task 13: Verify aggregation consumers and apply GS insert if needed

**Files:**
- Read: `TJConnector.Api/Transit/7 ContainerAggregationConsumer.cs`
- Read: `TJConnector.Api/Transit/9 ProcessAggregationConsumer.cs`
- Read: `TJConnector.StateSystem/Model/ExternalRequests/Container/ContainerOperationCreateRequest.cs`
- Possibly modify: one or both of the above consumers

- [ ] **Step 13.1: Read both aggregation consumer files end-to-end**

Open both files. Identify whether either builds a request body that contains pack codes sourced from `package.Content.Packs` (the same source that `EmissionServiceConsumer` uses).

Decision matrix:
- If **neither** consumer sends pack codes → skip to Step 13.5 (commit nothing, task closed).
- If **one or both** send pack codes → apply the exact same pattern as Task 10 (pre-build `groupCodes` / `codes` list via `TryInsertGroupSeparator`, hard-fail with log + `AddStatus(-4)` + `package.Comment`, then inject into the request body).

- [ ] **Step 13.2: (Conditional) Add `using` import**

If a modification is needed, ensure:

```csharp
using TJConnector.StateSystem.Helpers;
```

is present at the top of the modified file.

- [ ] **Step 13.3: (Conditional) Insert the GS loop before the request body construction**

Use the exact same block as in Task 10.2 (adjust only the variable names `package` / `package.Content` if the consumer uses different locals). Do not deviate — the helper contract, the hard-fail status code (`-4`), the log format, and the comment format must match.

- [ ] **Step 13.4: (Conditional) Build the solution**

```bash
dotnet build TJConnector.sln
```
Expected: succeeds.

- [ ] **Step 13.5: Commit (or note no change)**

If no modifications were made, document the finding in the commit message for Task 14 (or skip committing here). If modifications were made:

```bash
git add "TJConnector.Api/Transit/7 ContainerAggregationConsumer.cs" "TJConnector.Api/Transit/9 ProcessAggregationConsumer.cs"
git commit -m "feat: insert GS into aggregation pack codes, hard-fail on malformed"
```

---

## Task 14: Run the full test suite

**Files:** none.

- [ ] **Step 14.1: Run tests**

Run from repo root:
```bash
dotnet test TJConnector.StateSystem.Tests/TJConnector.StateSystem.Tests.csproj
```
Expected: **PASS** — 11/11.

- [ ] **Step 14.2: Build the whole solution**

Run:
```bash
dotnet build TJConnector.sln
```
Expected: succeeds with no errors.

---

## Task 15: Manual verification checklist

**Files:** none — all runtime checks.

This task is **mandatory** and must be executed interactively. Do NOT mark the plan done without completing each check.

Prerequisites: PostgreSQL running locally; `appsettings.json` configured with real credentials; external SQL Server accessible (for Flow B checks).

- [ ] **Step 15.1: Start API**

Run:
```bash
cd TJConnector.Api && dotnet run
```
API should start on `http://localhost:5166`. Leave running.

- [ ] **Step 15.2: Start Web**

In a new terminal:
```bash
cd TJConnector.Web && dotnet run
```

- [ ] **Step 15.3: Verify `format` on pack emission (Type 0)**

Use Swagger (`http://localhost:5166/swagger`) or the UI to create a `Type = 0` order. Capture the outgoing HTTP request to `markingCode/emission` via Serilog file log (`%ProgramData%\TJConnectorAPI\servicelog.txt`) or Fiddler. Confirm the JSON body contains:
```json
{ "type": 0, "format": 0, ... }
```

- [ ] **Step 15.4: Verify `format` on bundle emission (Type 1)**

Create a `Type = 1` order. Confirm body contains:
```json
{ "type": 1, "format": 1, ... }
```

- [ ] **Step 15.5: Verify NO `format` on container emission (Type 3)**

Create a `Type = 3` order. Confirm outgoing body to `container/emission` contains:
```json
{ "type": 0, ... }
```
and **does not** contain a `format` key at all.

- [ ] **Step 15.6: Verify GS strip on bundle download**

For a completed `Type = 1` bundle order that has downloaded codes, click Download in the UI. Open the downloaded `codes_*.txt` file in a hex viewer:

Windows PowerShell:
```powershell
Format-Hex codes_<id>.txt | Select-Object -First 5
```

Expected: **no** `1D` byte anywhere in the output. Each line should match the `bundles.txt` sample shape but with the GS stripped.

- [ ] **Step 15.7: Verify GS injection on apply**

Upload a `(Code, SSCC)` pair on the Containers page whose legacy SQL row has a well-formed GS-less pack code. Capture the outgoing request to `markingCode/report/apply`. Confirm:
- Each pack in `groupCodes[*].codes` contains `\u001d` at index 26.
- Body contains `productionDate` (close to current time).
- Body contains `locationUuid` matching the first row of the `Locations` table in PostgreSQL.

- [ ] **Step 15.8: Verify hard-fail on malformed code**

Insert a deliberately malformed row (e.g. `pack = "shortcode"`) into the legacy `ContentTable` for a test container. Upload the `(Code, SSCC)` pair that resolves to that container. Confirm in PostgreSQL:
```sql
SELECT "SSCCCode", "Status", "Comment" FROM "Packages" WHERE "Status" = -4;
```
- `Status` = -4
- `Comment` = `Malformed pack codes (1): shortcode`
- Serilog error log contains the same message.
- No outgoing request to `markingCode/report/apply` was made (verify via Fiddler or log absence).

Clean up the test row after verification.

- [ ] **Step 15.9: Verify auto-download on emission status 4**

Create a pack emission order. Let `ProcessOrdersConsumer` cycle until `GetExternalOrderByIdAsync` returns external status 4. Confirm:
- Internal status transitions `0 → 2 → 3 → 4 → 5`.
- A row appears in `CodeOrdersContents` for the order.
- No order was marked Done (5) without having content.

- [ ] **Step 15.10: Report results**

Paste a brief result summary (which checks passed, which failed) in the conversation. If anything failed, diagnose before claiming the plan is complete.

---

## Self-review

Spec coverage check (each spec section 4.x should map to a task):

- **Req 1 (`format` on emission)** → Task 4 (DTO) + Task 6 (config) + Task 7 (wiring) + Task 15.3–15.5 (verify). ✓
- **Req 2 (GS strip on download)** → Task 11 + Task 12 + Task 15.6 (verify). ✓
- **Req 3 (GS insert on apply/aggregate)** → Task 2 + Task 3 (helper) + Task 10 (apply) + Task 13 (aggregation) + Task 15.7–15.8 (verify). ✓
- **Req 4 (`productionDate` + `locationUuid`)** → Task 5 (DTO) + Task 9 (wiring). ✓
- **Req 5 (status 6 → 4)** → Task 8 + Task 15.9 (verify). ✓
- **Test project** → Task 1. ✓
- **`GS1CodeHelper` unit tests** → Tasks 2 and 3. ✓

No placeholders, no "handle edge cases" prose, no forward references to undefined symbols, no contradictions between tasks. Type and naming consistency verified: `TryInsertGroupSeparator` signature is identical in Task 3 and Task 10, `StripGroupSeparators` signature identical in Task 2, 11, 12.
