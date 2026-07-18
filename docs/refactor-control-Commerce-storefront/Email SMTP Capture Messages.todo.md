# Email SMTP Capture Messages Todo

Status: in progress  
Date: 2026-07-18  
Scope: Store-scoped SMTP settings managed from Control Plane, capture inbox for QA, password recovery email, and order placed email.

## Goal

- [ ] Mỗi store có SMTP sender/transport riêng.
- [ ] Control Plane là nơi admin cấu hình SMTP theo store.
- [ ] Storefront V2 không giữ SMTP credential, SMTP env, hoặc SMTP admin UI.
- [ ] Commerce Node dùng store SMTP khi gửi queued transactional message.
- [ ] Local/staging có SMTP capture inbox để Playwright kiểm thử recovery và order placed email thật.
- [ ] Password recovery email và order placed email đi qua message queue hiện có, không gửi đồng bộ trong checkout/order transaction.

## Codebase Baseline

- [x] `EmailSettings` global đã tồn tại với `From`, `DisplayName`, `SmtpServer`, `Port`, `UseSsl`, `Username`, `Password`.
- [x] `EmailService` đang dùng MailKit SMTP qua `IEmailService`.
- [x] `EmailSettingsOptionsValidator` đang validate global config ngoài Development.
- [x] `CommerceNodeAdminSettingsService` đang expose safe notification metadata: `SmtpHost`, `SmtpFromEmail`, `SmtpFromDisplayName`, `SecretsConfigured`.
- [x] Notification DTO hiện không expose username/password.
- [x] Message template, queued message, delivery task, `CommerceTaskWorker` đã tồn tại.
- [x] Password recovery đã queue `customer.password_recovery` qua `QueuedAccountEmailDispatcher`.
- [x] Order placed đã dùng `order.created` task để queue `order.placed`.
- [x] Commerce Node admin message template/queued-message endpoints đã tồn tại.
- [x] Control Plane đã có gateway pattern cho commerce settings.
- [ ] Control Plane chưa có flow rõ ràng cho store SMTP settings/message inspection.
- [ ] SMTP runtime hiện chưa store-scoped vì delivery vẫn đọc global `EmailSettings`.
- [ ] Local compose/env chưa có Mailpit/MailHog SMTP capture service.

## Architecture Decisions

- [x] Reuse existing message template, queue, delivery task, recovery, and order placed flow.
- [x] Store SMTP settings live in Commerce Node and are managed through Control Plane gateway.
- [x] Control Plane Web must call Control Plane API only.
- [x] Storefront V2 must not own SMTP settings or secrets.
- [x] SMTP password is write-only and never returned by API/UI/logs.
- [x] Use encrypted secret material or secret reference for store SMTP password.
- [x] Global `EmailSettings` remains only bootstrap/fallback/local capture compatibility, not production multi-store source of truth.

## Target Flow

```text
ControlPlane.Web
  -> ControlPlane.API
      -> CommerceNode.API api/commerce/admin/*
          -> CommerceNodeDbContext
          -> StoreEmailSettings
          -> MessageQueueService
          -> CommerceTaskWorker
              -> MessageDeliverTaskHandler
                  -> IStoreEmailTransportResolver
                      -> SMTP client
                          -> Store SMTP server or Mailpit capture
```

## Phase 0 - Baseline Lock

Purpose: lock current behavior before introducing store-scoped SMTP.

- [x] Add/refresh baseline tests proving password recovery queues through transactional message system. 2026-07-18: `QueuedAccountEmailDispatcherTests.SendPasswordRecoveryAsync_QueuesPasswordRecoveryTemplate` remains the focused guard.
- [x] Add/refresh baseline tests proving order placed queues through `order.created` -> `order.placed`. 2026-07-18: `CommerceTransactionalMessageServiceTests.QueueOrderPlacedAsync_QueuesOrderPlacedTemplateWithOrderTokens` plus task-handler static guard remain the focused guard.
- [x] Confirm direct `IEmailService.SendEmailAsync` call-site inventory remains expected. 2026-07-18: `TransactionalMessageBaselineTests.DirectEmailCallSiteInventory_MatchesKnownBaseline`.
- [x] Mark which direct email call sites are allowed legacy/non-CommerceNode paths. 2026-07-18: baseline locks `DirectAccountEmailDispatcher`, Newsletter, older payment cart, Email transport, and legacy `OrderTrackingService` as explicit current direct paths.
- [x] Confirm Commerce Node recovery/order placed do not bypass queued delivery. 2026-07-18: CommerceNode DI uses `QueuedAccountEmailDispatcher`; `OrderCreatedTaskHandler` calls `QueueOrderPlacedAsync` and has no `SendEmailAsync`.
- [x] Record current SMTP config shape from `appsettings*.json`. 2026-07-18: CommerceNode appsettings do not define `EmailSettings`; legacy/global options still bind for compatibility.
- [x] Record current SMTP config shape from `scripts/env/v2-local.env`. 2026-07-18: no SMTP env keys are configured in V2 local env before capture setup.
- [x] Record current SMTP config shape from compose files. 2026-07-18: `compose.production.yml` uses global `EmailSettings__*`; `compose.commercenode.yml` has no Mailpit/MailHog capture service yet.
- [x] Record current limitation: global `EmailSettings` cannot satisfy production multi-store SMTP. 2026-07-18: limitation kept as Phase 1/2 driver.
- [x] Add QA checklist items for captured recovery email. 2026-07-18: added open QA entries under `QA-CommerceNode.todo.md`.
- [x] Add QA checklist items for captured order placed email. 2026-07-18: added open QA entries under `QA-CommerceNode.todo.md`.

Acceptance:

- [x] Existing focused transactional message tests pass. 2026-07-18: focused CommerceNode transactional message tests pass.
- [x] Baseline test names clearly identify allowed direct email paths. 2026-07-18: direct SMTP inventory and CommerceNode queued dispatcher tests lock intended paths.
- [x] Baseline proves recovery/order placed stay queued, not synchronous. 2026-07-18: recovery/order guard tests pass.

## Phase 1 - Store SMTP Settings Model

Purpose: add store-scoped data model for Control Plane-managed SMTP.

- [x] Add Commerce Node entity/table such as `StoreEmailSettings`. 2026-07-18: `StoreEmailSettings` entity and `store_email_settings` table added.
- [x] Key `StoreEmailSettings` by `StoreId`. 2026-07-18: unique `StoreId` index and FK to `commerce_store`.
- [x] Add `Enabled`.
- [x] Add `SmtpHost`.
- [x] Add `SmtpPort`.
- [x] Add `UseSsl` or explicit secure socket option.
- [x] Add `Username`.
- [x] Add encrypted password field or secret reference. 2026-07-18: database stores `ProtectedPassword`, not plaintext request password.
- [x] Add `FromEmail`.
- [x] Add `FromDisplayName`.
- [x] Add `ReplyToEmail`.
- [x] Add `DeliveryMode` such as `Smtp` or `Capture`.
- [x] Add optional `CaptureRedirectToEmail` guarded for non-production/test mode. 2026-07-18: validator requires explicit `captureModeAllowed`.
- [x] Add audit timestamps and updated-by actor.
- [x] Add safe response DTO with `SecretsConfigured` and `PasswordUpdatedAt`.
- [x] Add update request with optional `Password`.
- [x] Add update request with `ClearPassword`.
- [x] Add update request with `UseExistingPassword` semantics where needed.
- [x] Validate SMTP host.
- [x] Validate SMTP port.
- [x] Validate from email.
- [x] Validate reply-to email.
- [x] Validate username/password length.
- [x] Validate capture mode is blocked in production unless explicitly allowed.
- [x] Add `IStoreEmailSecretProtector` or equivalent.
- [x] Protect stored SMTP password with runtime key/material outside database. 2026-07-18: `DataProtectionStoreEmailSecretProtector` uses ASP.NET Core Data Protection.
- [x] Ensure decrypted SMTP password is never logged. 2026-07-18: Phase 1 service only protects/clears stored value and response serialization tests confirm no raw/protected secret in DTO.
- [x] Add EF migration for Commerce Node. 2026-07-18: `20260718100946_CommerceNodeStoreEmailSettings`.

Acceptance:

- [x] Commerce Node model build passes. 2026-07-18: CommerceNode API build passed.
- [x] Migration applies to `CommerceNodeDbContext`. 2026-07-18: `dotnet ef database update` applied `CommerceNodeStoreEmailSettings` to local PostgreSQL.
- [x] DTO reflection tests prove raw password is absent from all responses. 2026-07-18: `StoreEmailSettingsContractTests`.
- [x] Validation blocks incomplete SMTP config when `Enabled=true`. 2026-07-18: `StoreEmailSettingsContractTests.Validator_BlocksIncompleteEnabledSmtpConfig`.
- [x] Secret clear/rotate paths do not expose old password. 2026-07-18: `StoreEmailSettingsServiceTests` verifies rotate/clear response secrecy.

## Phase 2 - SMTP Transport And Sender Resolution

Purpose: make queued delivery use store SMTP settings instead of global app env.

- [x] Add `IStoreEmailTransportResolver` or equivalent. 2026-07-18: `IStoreEmailTransportResolver`.
- [x] Resolve SMTP transport by queued message `StoreId`. 2026-07-18: `MessageDeliveryService` resolves transport from `message.StoreId`.
- [x] Prefer active `StoreEmailSettings` for the message store. 2026-07-18: resolver tests cover Store A/B different settings.
- [x] Allow global `EmailSettings` fallback only when explicitly enabled. 2026-07-18: `StoreEmailTransportOptions.AllowGlobalEmailSettingsFallback`.
- [x] Return safe `message_delivery.smtp_not_configured` failure when no usable config exists.
- [x] Change `MessageQueueService` to snapshot `FromEmail` from store email settings.
- [x] Change `MessageQueueService` to snapshot `FromName` from store email settings.
- [x] Change `MessageDeliveryService` to send through store-resolved SMTP transport.
- [x] Keep `EmailService` reusable only if store selection remains explicit. 2026-07-18: queued delivery no longer injects global `IEmailService`.
- [x] Add request/model shape for sending with resolved host/port/credentials if needed. 2026-07-18: `StoreEmailTransportSettings`.
- [x] Add test-send service that uses same resolver and transport as real delivery. 2026-07-18: `StoreEmailTestSendService`.
- [x] Keep queued-message admin detail secret-safe.
- [x] Keep reset-token rendered body out of queued-message admin detail.
- [x] Keep idempotency key out of queued-message admin detail.

Acceptance:

- [x] Store A and Store B can use different SMTP hosts. 2026-07-18: `StoreEmailTransportResolverTests.ResolveTransportAsync_UsesStoreSpecificSmtpSettings`.
- [x] Store A and Store B can use different from addresses. 2026-07-18: same resolver test covers per-store sender.
- [x] Missing store SMTP config fails queued delivery into retry/failed state. 2026-07-18: `MessageDeliveryServiceTests.DeliverAsync_WhenStoreSmtpMissing_MarksWaitingRetryWithoutSending`.
- [x] Missing store SMTP config does not roll back recovery/order/checkout source commands. 2026-07-18: queue keeps source commands decoupled; missing SMTP config is handled during `message.deliver`.
- [x] Queued messages snapshot expected store sender. 2026-07-18: `MessageQueueServiceTests.QueueAsync_CreatesQueuedMessageAndDeliveryTask`.
- [x] API response, audit metadata, DOM, and logs do not expose SMTP credentials. 2026-07-18: no API/DOM added in Phase 2; response DTO and service tests continue to assert no password/protected secret serialization.

## Phase 3 - SMTP Capture Runtime Setup

Purpose: make local/staging E2E able to receive real SMTP messages using store SMTP settings.

- [ ] Add Mailpit-compatible capture service to `compose.commercenode.yml`.
- [ ] Expose Mailpit SMTP port for Commerce Node runtime.
- [ ] Expose Mailpit web/API inbox port for QA tooling.
- [ ] Add local seed or setup docs for Store A SMTP -> Mailpit.
- [ ] Add local seed or setup docs for Store B SMTP -> Mailpit if multi-store QA runs locally.
- [ ] Set capture host/port/SSL/from per store, not in Storefront V2.
- [ ] Keep global `EmailSettings` as compatibility fallback only.
- [ ] Document capture inbox URL/port in local run docs.
- [ ] Update production/staging example settings to explain store SMTP is configured through Control Plane.
- [ ] Document runtime env key for SMTP secret protection/encryption key.
- [ ] Document optional global SMTP fallback toggle.
- [ ] Allow capture mode without username/password only in non-production or explicit test mode.

Acceptance:

- [ ] Local `message.deliver` sends to Mailpit through store SMTP settings.
- [ ] Storefront V2 has no SMTP env requirement.
- [ ] Production can disable fallback and require active stores to have valid SMTP settings.

## Phase 4 - Control Plane Store Email Settings And Message Operations Gateway

Purpose: make SMTP/message operations manageable without breaking boundaries.

- [ ] Add or extend Commerce Node admin endpoint to get store email settings.
- [ ] Add or extend Commerce Node admin endpoint to update store email settings.
- [ ] Add endpoint/action to rotate SMTP password.
- [ ] Add endpoint/action to clear SMTP password.
- [ ] Add endpoint/action to send test email.
- [ ] Reuse existing message template list/detail/update/reset/preview endpoints.
- [ ] Reuse existing queued message list/detail/retry/cancel endpoints.
- [ ] Add Control Plane API gateway method for get store email settings.
- [ ] Add Control Plane API gateway method for update store email settings.
- [ ] Add Control Plane API gateway method for test email.
- [ ] Add Control Plane API gateway methods for template operations if missing.
- [ ] Add Control Plane API gateway methods for queued-message operations if missing.
- [ ] Protect read routes with existing `CommerceSettingsRead` or a narrower existing policy.
- [ ] Protect write/test/retry/cancel routes with `CommerceSettingsWrite` or a narrower existing policy.
- [ ] Add Control Plane Web page/tab with store selector.
- [ ] Show SMTP effective status per store.
- [ ] Show `SecretsConfigured` indicator.
- [ ] Show host/port/SSL/username/from/reply-to fields.
- [ ] Add password rotate input that never echoes current password.
- [ ] Add clear password action.
- [ ] Add send test email action.
- [ ] Add template list/preview/update/reset UI if missing.
- [ ] Add queued message list/detail/retry/cancel UI if missing.

Acceptance:

- [ ] Browser network capture proves Control Plane Web calls only Control Plane API.
- [ ] Commerce Node Swagger has stable operation IDs and safe schemas.
- [ ] Control Plane API response envelope stays consistent.
- [ ] Test email sends with selected store SMTP settings.
- [ ] No SMTP password appears in API response, DOM, console, trace, audit metadata, or logs.

## Phase 5 - Recovery Email E2E

Purpose: prove password recovery works from browser to captured store SMTP email.

- [ ] Configure Playwright to read Mailpit/capture inbox.
- [ ] Clear capture inbox before test run or filter by unique synthetic email.
- [ ] Open `/forgot-password` in browser.
- [ ] Submit known synthetic customer email.
- [ ] Wait for `customer.password_recovery` captured email.
- [ ] Extract reset link from captured email.
- [ ] Reset password through browser.
- [ ] Login with new password.
- [ ] Restore original password or recreate synthetic customer.
- [ ] Submit unknown email.
- [ ] Assert unknown email shows same generic sent state.
- [ ] Assert unknown email produces no captured message.
- [ ] Assert recovery email uses current store SMTP sender/from profile.
- [ ] Assert captured recovery email does not leak raw token outside reset URL.
- [ ] Assert queued-message admin detail does not expose rendered reset body.

Acceptance:

- [ ] Known-email recovery passes end to end through real browser and SMTP capture.
- [ ] Unknown-email recovery remains anti-enumeration-safe.
- [ ] Recovery email is store-scoped by sender and recipient behavior.

## Phase 6 - Order Placed Email E2E

Purpose: prove real checkout produces exactly one customer order email.

- [ ] Use synthetic store/product/customer configured for COD checkout.
- [ ] Place real COD order through Playwright browser flow.
- [ ] Wait for `order.created` task.
- [ ] Wait for `order.placed` queued message.
- [ ] Wait for captured order placed email.
- [ ] Assert exactly one order placed email for the order reference.
- [ ] Assert email subject/body contain store name.
- [ ] Assert email subject/body contain order reference.
- [ ] Assert email subject/body contain total amount and currency.
- [ ] Assert email contains account order detail or receipt link.
- [ ] Assert duplicate submit/idempotency creates one order and one order placed email.
- [ ] Simulate SMTP failure and assert order placement still succeeds.
- [ ] Assert failed delivery transitions queued message to retry/failed state.
- [ ] Restore SMTP/capture and retry queued message from admin.
- [ ] Assert retry sends the email successfully.
- [ ] Configure Store A and Store B with different SMTP sender profiles.
- [ ] Assert Store A order uses Store A sender.
- [ ] Assert Store B order uses Store B sender.

Acceptance:

- [ ] Real COD order creates exactly one captured order placed email.
- [ ] SMTP outage does not roll back order placement.
- [ ] Retry from admin sends failed queued message after SMTP/capture is restored.
- [ ] Multi-store SMTP isolation passes.

## Phase 7 - Release QA And Docs

Purpose: make this usable as a production gate.

- [ ] Update `QA-CommerceNode.todo.md` with store SMTP settings checks.
- [ ] Update `QA-CommerceNode.todo.md` with SMTP capture checks.
- [ ] Update `QA-CommerceNode.todo.md` with recovery email checks.
- [ ] Update `QA-CommerceNode.todo.md` with order placed email checks.
- [ ] Update Storefront Playwright release checklist with email capture prerequisites.
- [ ] Update Storefront Playwright release checklist with recovery email E2E.
- [ ] Update Storefront Playwright release checklist with order placed email E2E.
- [ ] Add runbook notes for local capture setup.
- [ ] Add runbook notes for staging capture setup.
- [ ] Add runbook notes for per-store SMTP setup from Control Plane.
- [ ] Add runbook notes for production SMTP secret protection key.
- [ ] Add runbook notes for queued message inspection.
- [ ] Add runbook notes for retry/cancel failed messages.
- [ ] Add actionable SMTP misconfiguration errors: problem, cause, fix.

Acceptance:

- [ ] Operator can verify every active store SMTP readiness without seeing secrets.
- [ ] Playwright release run proves recovery and order placed email before production public release.
- [ ] QA artifacts redact reset tokens and SMTP credentials.

## Not In Scope

- [ ] Marketing/newsletter campaign builder.
- [ ] Attachments.
- [ ] SendGrid/Mailgun/provider adapter abstraction.
- [ ] DKIM/SPF/DMARC management UI.
- [ ] SMS/push notifications.
- [ ] HTML email visual template designer.
- [ ] Plaintext SMTP password storage.
- [ ] SMTP configuration in `BlazorShop.Storefront.V2` env/appsettings.
- [ ] Synchronous email sending inside checkout/order placement transaction.

## Release Gate

- [ ] Store SMTP settings can be configured from Control Plane per store.
- [ ] Control Plane Web never calls Commerce Node directly for SMTP/message management.
- [ ] Storefront V2 has no SMTP credential/config dependency.
- [ ] Raw SMTP password never appears in response DTOs, DOM, logs, audit metadata, traces, or screenshots.
- [ ] Store A and Store B SMTP isolation passes.
- [ ] Password recovery email E2E passes through captured store SMTP email.
- [ ] Order placed email E2E passes through captured store SMTP email.
- [ ] Duplicate order submit creates one order and one order placed email.
- [ ] SMTP failure does not roll back order placement.
- [ ] Failed queued message can be retried after SMTP is fixed.
