# Tank Decision: Tests for Issues #618 and #619

**Date:** 2026-04-02
**Author:** Tank (Tester)
**Branch:** issue-618-619
**Status:** Tests committed and passing

## Context

Issues #618 (SendEmail Azure Function) and #619 (UserApprovalManager email wiring) implemented by Trinity on branch `issue-618-619`. Tank authored unit tests to cover both features.

## Decisions Made

### 1. `EmailClient` mocked directly — no `IEmailClient` wrapper needed

Trinity's `SendEmail` function injects `Azure.Communication.Email.EmailClient` directly (not an interface). The ACS SDK's `EmailClient` has a `protected EmailClient()` constructor and virtual `SendAsync`, making it mockable via Moq without a wrapper. An `IEmailClient` interface was considered but discarded since Moq can handle the concrete class.

### 2. Return type is `EmailSendOperation`, not `Operation<EmailSendResult>`

The virtual `SendAsync` returns `Task<EmailSendOperation>`, NOT `Task<Operation<EmailSendResult>>`. Mock the operation as `new Mock<EmailSendOperation>()` and set up `Id` on it.

### 3. `Run_InvalidBase64_DoesNotCallEmailClient` (not `ThrowsException`)

The original test spec said invalid Base64 should throw. Trinity's implementation intentionally catches deserialization failures, logs, and returns early — this prevents bad queue messages from triggering infinite retries. Test was renamed to `Run_InvalidBase64_DoesNotCallEmailClient` to correctly describe actual behavior.

**Recommendation:** Consider adding a separate "poison" test case or CI lint to ensure the EmailSender always writes valid Base64 JSON, since deserialization errors are silently dropped.

### 4. `FluentAssertions` added to `Functions.Tests.csproj`

The Functions.Tests project previously only used xUnit-native assertions. FluentAssertions was added to `Functions.Tests.csproj` (version 8.9.0, matching Managers.Tests) to support the new `SendEmailTests.cs` assertions.

### 5. `UserApprovalManagerTests` constructor updated for new dependencies

`UserApprovalManager` gained two new constructor params (`IEmailTemplateManager`, `IEmailSender`) plus `ILogger<UserApprovalManager>`. The existing test class constructor was updated to inject all six dependencies. All 9 original tests continue to pass; 5 new email-notification tests added.

### 6. `IEmailTemplateManager.GetTemplateAsync` (not `GetByNameAsync`)

The spec mentioned `GetByNameAsync` but the actual interface method is `GetTemplateAsync(string name)`. All mocks and verifications use the correct method name.

## Test Coverage Summary

### `SendEmailTests` (4 tests)
| Test | Validates |
|---|---|
| `Run_ValidBase64JsonMessage_CallsEmailClientSendAsync` | Happy path — `EmailClient.SendAsync` called with correct To + Subject |
| `Run_ValidMessage_ExtractsFromAddressFromEmail` | From address comes from domain `Email` model |
| `Run_InvalidBase64_DoesNotCallEmailClient` | Malformed messages dropped, no ACS call |
| `Run_EmailClientThrows_ExceptionPropagates` | ACS failure propagates for retry/poison routing |

### `UserApprovalManagerTests` — new email tests (5 tests)
| Test | Validates |
|---|---|
| `ApproveUserAsync_ValidUser_QueuesApprovalEmail` | `IEmailSender.QueueEmail` called with user email + "UserApproved" template |
| `ApproveUserAsync_TemplateNotFound_DoesNotThrow` | Null template → log warning, no exception |
| `ApproveUserAsync_TemplateNotFound_DoesNotCallEmailSender` | Null template → `IEmailSender` NOT called |
| `RejectUserAsync_ValidUser_QueuesRejectionEmail` | `IEmailSender.QueueEmail` called with "UserRejected" template |
| `RejectUserAsync_TemplateNotFound_DoesNotThrow` | Null template → log warning, no exception |

## Test Results

- Managers.Tests: **89/89 passed** (all original + 5 new)
- Functions.Tests (SendEmail): **4/4 passed**
