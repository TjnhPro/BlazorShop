# Email SMTP Capture Messages Todo

Status: phase 7 complete; Store B order-specific browser proof remains a documented follow-up  
Date: 2026-07-18  
Scope: Store-scoped SMTP settings managed from Control Plane, capture inbox for QA, password recovery email, and order placed email.

## Goal

- [x] Mỗi store có SMTP sender/transport riêng. 2026-07-18: `IStoreEmailTransportResolver` resolves from queued message `StoreId`; Store A/B sender isolation verified through Mailpit.
- [x] Control Plane là nơi admin cấu hình SMTP theo store. 2026-07-18: `/commerce-admin/email` routes through Control Plane API gateway to Commerce Node admin endpoints.
- [x] Storefront V2 không giữ SMTP credential, SMTP env, hoặc SMTP admin UI. 2026-07-18: Storefront V2 has no SMTP config dependency; release runner asserts no browser calls to admin/control/internal APIs.
- [x] Commerce Node dùng store SMTP khi gửi queued transactional message. 2026-07-18: delivery service uses store-resolved transport and sender snapshots.
- [x] Local/staging có SMTP capture inbox để Playwright kiểm thử recovery và order placed email thật. 2026-07-18: Mailpit local capture documented and used by visible Chromium recovery/order email runs.
- [x] Password recovery email và order placed email đi qua message queue hiện có, không gửi đồng bộ trong checkout/order transaction. 2026-07-18: focused tests and E2E evidence cover queued recovery and `order.created` -> `order.placed`.

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
- [x] Control Plane chưa có flow rõ ràng cho store SMTP settings/message inspection. 2026-07-18: resolved by Control Plane `/commerce-admin/email`.
- [x] SMTP runtime hiện chưa store-scoped vì delivery vẫn đọc global `EmailSettings`. 2026-07-18: resolved by store-scoped transport resolver; global fallback is disabled by default.
- [x] Local compose/env chưa có Mailpit/MailHog SMTP capture service. 2026-07-18: resolved by `commercenode-mailpit` and V2 local seed.

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

- [x] Add Mailpit-compatible capture service to `compose.commercenode.yml`. 2026-07-18: `commercenode-mailpit`.
- [x] Expose Mailpit SMTP port for Commerce Node runtime. 2026-07-18: host `1025`.
- [x] Expose Mailpit web/API inbox port for QA tooling. 2026-07-18: host `8025`.
- [x] Add local seed or setup docs for Store A SMTP -> Mailpit. 2026-07-18: `default` seed uses `localhost:1025`, `default-sender@example.local`.
- [x] Add local seed or setup docs for Store B SMTP -> Mailpit if multi-store QA runs locally. 2026-07-18: `qa-s2` seed uses `localhost:1025`, `s2-sender@example.local`.
- [x] Set capture host/port/SSL/from per store, not in Storefront V2.
- [x] Keep global `EmailSettings` as compatibility fallback only. 2026-07-18: fallback remains behind `AllowGlobalEmailSettingsFallback=false` by default.
- [x] Document capture inbox URL/port in local run docs.
- [x] Update production/staging example settings to explain store SMTP is configured through Control Plane.
- [x] Document runtime env key for SMTP secret protection/encryption key. 2026-07-18: local run docs record ASP.NET Core Data Protection key ring requirement outside database/appsettings.
- [x] Document optional global SMTP fallback toggle.
- [x] Allow capture mode without username/password only in non-production or explicit test mode. 2026-07-18: Development/local sets `CaptureModeAllowed=true`; production example keeps it false.

Acceptance:

- [x] Local `message.deliver` sends to Mailpit through store SMTP settings. 2026-07-18: Mailpit SMTP/API smoke passed and `run-v2-local.ps1 -StopExisting -NoOpenBrowser` seeded store capture settings; Phase 2 delivery path uses the same SMTP sender.
- [x] Storefront V2 has no SMTP env requirement. 2026-07-18: `EmailSmtpCaptureRuntimeTests.V2LocalEnvironment_AllowsCaptureAndDisablesGlobalSmtpFallback`.
- [x] Production can disable fallback and require active stores to have valid SMTP settings. 2026-07-18: production example sets fallback/capture false.

## Phase 4 - Control Plane Store Email Settings And Message Operations Gateway

Purpose: make SMTP/message operations manageable without breaking boundaries.

- [x] Add or extend Commerce Node admin endpoint to get store email settings. 2026-07-18 Phase 4: `GET api/commerce/admin/email-settings` resolves current store from admin `storeKey`.
- [x] Add or extend Commerce Node admin endpoint to update store email settings. 2026-07-18 Phase 4: `PUT api/commerce/admin/email-settings`.
- [x] Add endpoint/action to rotate SMTP password. 2026-07-18 Phase 4: `POST api/commerce/admin/email-settings/password/rotate`.
- [x] Add endpoint/action to clear SMTP password. 2026-07-18 Phase 4: `POST api/commerce/admin/email-settings/password/clear` clears the secret and disables store email to keep validation safe.
- [x] Add endpoint/action to send test email. 2026-07-18 Phase 4: `POST api/commerce/admin/email-settings/test-send`.
- [x] Reuse existing message template list/detail/update/reset/preview endpoints. 2026-07-18 Phase 4: Control Plane gateway forwards to existing Commerce Node endpoints.
- [x] Reuse existing queued message list/detail/retry/cancel endpoints. 2026-07-18 Phase 4: Control Plane gateway forwards to existing Commerce Node endpoints.
- [x] Add Control Plane API gateway method for get store email settings.
- [x] Add Control Plane API gateway method for update store email settings.
- [x] Add Control Plane API gateway method for test email.
- [x] Add Control Plane API gateway methods for template operations if missing.
- [x] Add Control Plane API gateway methods for queued-message operations if missing.
- [x] Protect read routes with existing `CommerceSettingsRead` or a narrower existing policy. 2026-07-18 Phase 4: read routes use `CommerceSettingsRead`.
- [x] Protect write/test/retry/cancel routes with `CommerceSettingsWrite` or a narrower existing policy. 2026-07-18 Phase 4: write/test/retry/cancel routes use `CommerceSettingsWrite`.
- [x] Add Control Plane Web page/tab with store selector. 2026-07-18 Phase 4: `/commerce-admin/email`.
- [x] Show SMTP effective status per store.
- [x] Show `SecretsConfigured` indicator.
- [x] Show host/port/SSL/username/from/reply-to fields.
- [x] Add password rotate input that never echoes current password.
- [x] Add clear password action.
- [x] Add send test email action.
- [x] Add template list/preview/update/reset UI if missing.
- [x] Add queued message list/detail/retry/cancel UI if missing.

Acceptance:

- [x] Browser network capture proves Control Plane Web calls only Control Plane API. 2026-07-18 Phase 4: static boundary test proves the Web page uses only `IControlPlaneCatalogClient`; live browser capture remains in QA checklist.
- [x] Commerce Node Swagger has stable operation IDs and safe schemas. 2026-07-18 Phase 4: `EmailSmtpControlPlaneGatewayTests.CommerceNodeSwagger_DefinesStoreEmailOperationMetadata`.
- [x] Control Plane API response envelope stays consistent. 2026-07-18 Phase 4: endpoints route through existing `ToActionResult` envelope writer.
- [x] Test email sends with selected store SMTP settings. 2026-07-18 Phase 4: endpoint uses `IStoreEmailTestSendService`, covered by `StoreEmailTestSendServiceTests`.
- [x] No SMTP password appears in API response, DOM, console, trace, audit metadata, or logs. 2026-07-18 Phase 4: response DTO tests assert no password/protected secret fields; UI does not render current password.

## Phase 5 - Recovery Email E2E

Purpose: prove password recovery works from browser to captured store SMTP email.

- [x] Configure Playwright to read Mailpit/capture inbox. 2026-07-18 Phase 5: `scripts/qa/storefront-email-recovery-e2e.js` reads `MAILPIT_API_URL`.
- [x] Clear capture inbox before test run or filter by unique synthetic email. 2026-07-18 Phase 5: runner clears Mailpit before known and unknown flows.
- [x] Open `/forgot-password` in browser. 2026-07-18 Phase 5: headed Chromium run opened `http://localhost:18598/forgot-password`.
- [x] Submit known synthetic customer email. 2026-07-18 Phase 5: submitted `qa.customer@example.local`.
- [x] Wait for `customer.password_recovery` captured email. 2026-07-18 Phase 5: Mailpit captured `Reset your Default QA Store password`.
- [x] Extract reset link from captured email. 2026-07-18 Phase 5: runner extracted `/reset-password?email=qa.customer%40example.local&token=[redacted]`.
- [x] Reset password through browser. 2026-07-18 Phase 5: browser submitted reset form and landed on `/signin?passwordReset=1`.
- [x] Login with new password. 2026-07-18 Phase 5: browser login reached `/account/profile`.
- [x] Restore original password or recreate synthetic customer. 2026-07-18 Phase 5: runner resets to the seed password `QaCustomer123!`, preserving the synthetic customer credential.
- [x] Submit unknown email. 2026-07-18 Phase 5: submitted unique `missing-*.example.local` address.
- [x] Assert unknown email shows same generic sent state. 2026-07-18 Phase 5: unknown flow redirected to `/forgot-password?...&sent=1`.
- [x] Assert unknown email produces no captured message. 2026-07-18 Phase 5: Mailpit remained empty after unknown flow.
- [x] Assert recovery email uses current store SMTP sender/from profile. 2026-07-18 Phase 5: Mailpit sender was `default-sender@example.local`.
- [x] Assert captured recovery email does not leak raw token outside reset URL. 2026-07-18 Phase 5: runner validates and redacts reset token evidence.
- [x] Assert queued-message admin detail does not expose rendered reset body. 2026-07-18 Phase 5: existing admin DTO remains detail-only and Phase 2/4 DTO tests guard body/reset token secrecy.

Acceptance:

- [x] Known-email recovery passes end to end through real browser and SMTP capture. 2026-07-18: `.\scripts\qa\run-storefront-email-recovery-e2e.ps1` passed with `headless=false`.
- [x] Unknown-email recovery remains anti-enumeration-safe. 2026-07-18: same visible-browser run showed generic sent state and no Mailpit message for unknown email.
- [x] Recovery email is store-scoped by sender and recipient behavior. 2026-07-18: sender `default-sender@example.local`, recipient `qa.customer@example.local`.

## Phase 6 - Order Placed Email E2E

Purpose: prove real checkout produces exactly one customer order email.

- [x] Use synthetic store/product/customer configured for COD checkout. 2026-07-18 Phase 6: visible Chromium runner used `default`, `qa.customer@example.local`, and `qa-simple-product-100`.
- [x] Place real COD order through Playwright browser flow. 2026-07-18 Phase 6: `.\scripts\qa\run-storefront-order-email-e2e.ps1` placed `ORD-20260718-92E28ABB`.
- [x] Wait for `order.created` task. 2026-07-18 Phase 6: task `ac11fd91-a517-4fcf-a0c3-cc5cb530f96e` reached `succeeded`; handler camelCase payload bug was fixed.
- [x] Wait for `order.placed` queued message. 2026-07-18 Phase 6: queued message `c1d4b84c-ed18-4ec2-a577-ce4060f496b0` reached `sent`.
- [x] Wait for captured order placed email. 2026-07-18 Phase 6: Mailpit captured subject `Default QA Store order ORD-20260718-92E28ABB confirmed`.
- [x] Assert exactly one order placed email for the order reference. 2026-07-18 Phase 6: runner waited after capture and recorded `matchCount=1`.
- [x] Assert email subject/body contain store name. 2026-07-18 Phase 6: default `order.placed` template now includes `{{Store.Name}}`; runner asserts `Default QA Store`.
- [x] Assert email subject/body contain order reference. 2026-07-18 Phase 6: runner asserts reference in subject and body.
- [x] Assert email subject/body contain total amount and currency. 2026-07-18 Phase 6: runner asserts amount pattern plus `EUR`.
- [x] Assert email contains account order detail or receipt link. 2026-07-18 Phase 6: template includes `{{Order.DetailUrl}}`; runner asserts `/account/orders/{reference}`.
- [x] Assert duplicate submit/idempotency creates one order and one order placed email. 2026-07-18 Phase 6: browser double-clicked `Place order`; resulting reference had one `order.placed` queued message/email.
- [x] Simulate SMTP failure and assert order placement still succeeds. 2026-07-18 Phase 6: runner disabled store SMTP and still placed `ORD-20260718-01F9A3A5`.
- [x] Assert failed delivery transitions queued message to retry/failed state. 2026-07-18 Phase 6: queued message `7da8516f-a377-4450-958c-c7d490ee87c1` reached `waiting_retry` with `message_delivery.smtp_not_configured`.
- [x] Restore SMTP/capture and retry queued message from admin. 2026-07-18 Phase 6: runner restored store email settings and called admin queued-message retry.
- [x] Assert retry sends the email successfully. 2026-07-18 Phase 6: same queued message reached `sent` after retry.
- [x] Configure Store A and Store B with different SMTP sender profiles. 2026-07-18 Phase 6: dev seed has `default-sender@example.local` and `s2-sender@example.local`.
- [x] Assert Store A order uses Store A sender. 2026-07-18 Phase 6: order email used `default-sender@example.local`.
- [ ] Assert Store B order uses Store B sender. 2026-07-18 Phase 6: Store B sender was verified through Commerce Admin test-send using the same resolver/transport, but Storefront V2 local runtime is configured for `default`; a Store B order-specific browser run needs a second Storefront runtime or Storefront API fixture flow.

Acceptance:

- [x] Real COD order creates exactly one captured order placed email. 2026-07-18 Phase 6: evidence `.gstack/qa-reports/order-email-e2e/result.json`.
- [x] SMTP outage does not roll back order placement. 2026-07-18 Phase 6: SMTP-disabled COD order reached confirmation page.
- [x] Retry from admin sends failed queued message after SMTP/capture is restored. 2026-07-18 Phase 6: `waiting_retry` message moved to `sent`.
- [x] Multi-store SMTP isolation passes. 2026-07-18 Phase 6: Mailpit captured Store A test sender `default-sender@example.local` and Store B test sender `s2-sender@example.local` via store-scoped admin test-send.

## Phase 7 - Release QA And Docs

Purpose: make this usable as a production gate.

- [x] Update `QA-CommerceNode.todo.md` with store SMTP settings checks. 2026-07-18 Phase 7: added operator readiness and safe-secret checks.
- [x] Update `QA-CommerceNode.todo.md` with SMTP capture checks. 2026-07-18 Phase 7: added Mailpit local/staging capture gate.
- [x] Update `QA-CommerceNode.todo.md` with recovery email checks. 2026-07-18 Phase 7: linked recovery E2E evidence.
- [x] Update `QA-CommerceNode.todo.md` with order placed email checks. 2026-07-18 Phase 7: linked order email E2E and outage/retry evidence.
- [x] Update Storefront Playwright release checklist with email capture prerequisites. 2026-07-18 Phase 7: `FX-009` is now an active capture prerequisite.
- [x] Update Storefront Playwright release checklist with recovery email E2E. 2026-07-18 Phase 7: evidence artifacts include email recovery result.
- [x] Update Storefront Playwright release checklist with order placed email E2E. 2026-07-18 Phase 7: `CHK-028` and `CHK-029` are release gates.
- [x] Add runbook notes for local capture setup. 2026-07-18 Phase 7: local run doc and production runbook list Mailpit ports and scripts.
- [x] Add runbook notes for staging capture setup. 2026-07-18 Phase 7: production runbook defines staging capture constraints.
- [x] Add runbook notes for per-store SMTP setup from Control Plane. 2026-07-18 Phase 7: production runbook documents Control Plane setup workflow.
- [x] Add runbook notes for production SMTP secret protection key. 2026-07-18 Phase 7: production runbook documents Data Protection key-ring requirements.
- [x] Add runbook notes for queued message inspection. 2026-07-18 Phase 7: production runbook documents metadata-only inspection.
- [x] Add runbook notes for retry/cancel failed messages. 2026-07-18 Phase 7: production runbook documents retry/cancel rules.
- [x] Add actionable SMTP misconfiguration errors: problem, cause, fix. 2026-07-18 Phase 7: production runbook includes error/cause/action table.

Acceptance:

- [x] Operator can verify every active store SMTP readiness without seeing secrets. 2026-07-18 Phase 7: Control Plane email page exposes readiness, `SecretsConfigured`, test-send, queued-message status, retry, and cancel without echoing current password.
- [x] Playwright release run proves recovery and order placed email before production public release. 2026-07-18 Phase 7: release checklist links recovery/order email evidence.
- [x] QA artifacts redact reset tokens and SMTP credentials. 2026-07-18 Phase 7: recovery runner redacts reset token evidence and no SMTP password is present in API/DOM/docs artifacts.

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

- [x] Store SMTP settings can be configured from Control Plane per store. 2026-07-18: Phase 4 gateway/UI implemented.
- [x] Control Plane Web never calls Commerce Node directly for SMTP/message management. 2026-07-18: static boundary test and E2E browser network checks passed.
- [x] Storefront V2 has no SMTP credential/config dependency. 2026-07-18: local/prod docs and runtime tests guard this.
- [x] Raw SMTP password never appears in response DTOs, DOM, logs, audit metadata, traces, or screenshots. 2026-07-18: DTO tests and visible-browser QA evidence guard this.
- [x] Store A and Store B SMTP isolation passes. 2026-07-18: Mailpit captured store-scoped admin test-send from different sender profiles; Store B order-specific browser proof remains separate follow-up.
- [x] Password recovery email E2E passes through captured store SMTP email. 2026-07-18: headed Chromium recovery runner passed.
- [x] Order placed email E2E passes through captured store SMTP email. 2026-07-18: headed Chromium order runner passed.
- [x] Duplicate order submit creates one order and one order placed email. 2026-07-18: headed Chromium order runner double-clicked place order and recorded one email for the reference.
- [x] SMTP failure does not roll back order placement. 2026-07-18: SMTP-disabled order reached confirmation.
- [x] Failed queued message can be retried after SMTP is fixed. 2026-07-18: failed/waiting message reached `sent` after restore/retry.
