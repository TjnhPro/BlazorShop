# Transactional Message Core.todo

Generated: 2026-07-17

Source plan: `Transactional Message Core.md`

Status: Phase 7 complete. Phase 8 not started.

Scope: add practical transactional message infrastructure for active V2 Commerce Node. Replace hard-coded direct email calls with template-driven queued messages for account activation, password recovery, order placed confirmation, payment/fulfillment hooks, and contact form delivery. This is not a marketing automation, newsletter campaign, or visual email builder phase.

## Scope Lock

Approved:

- [ ] Message template core:
  - [ ] template system name.
  - [ ] localized subject/body.
  - [ ] store-specific template override.
  - [ ] active/inactive state.
  - [ ] audit timestamps.
  - [ ] seeded default templates for core transactional messages.
- [ ] Token/model binding:
  - [ ] safe token dictionary.
  - [ ] deterministic token replacement.
  - [ ] missing-token validation/warnings.
  - [ ] no arbitrary code execution in templates.
- [ ] Sender selection:
  - [ ] keep existing SMTP `EmailSettings` as secret/config source.
  - [ ] use store/admin from-display fields for safe sender metadata.
  - [ ] allow template/store sender display override without storing SMTP password in DB.
- [ ] Queued delivery:
  - [ ] queued message record.
  - [ ] retry/failure state.
  - [ ] attempt count and next attempt timestamp.
  - [ ] sent/failed timestamps.
  - [ ] safe error message.
  - [ ] idempotency/correlation key.
  - [ ] use existing `CommerceTaskWorker`.
- [ ] Attachment hook:
  - [ ] model-level hook for future invoice/files.
  - [ ] no full attachment storage UI in this phase.
- [ ] Core transactional messages:
  - [ ] account activation.
  - [ ] password recovery.
  - [ ] order placed confirmation.
  - [ ] payment status notification hook.
  - [ ] fulfillment/shipping status notification hook.
  - [ ] contact form delivery.
- [ ] API/contract hardening:
  - [ ] explicit admin DTOs.
  - [ ] stable OpenAPI operation IDs.
  - [ ] validation metadata.
  - [ ] no SMTP secrets in API responses.
  - [ ] contract tests and snapshots.

Deferred:

- [ ] Marketing campaign engine.
- [ ] Bulk newsletter campaign sending.
- [ ] Visual email template designer.
- [ ] Drag/drop template builder.
- [ ] Customer notification preference center.
- [ ] SMS, push, WhatsApp, or multi-channel provider abstraction.
- [ ] Multi-provider failover.
- [ ] Advanced Liquid/Razor scripting engine.
- [ ] Arbitrary template code execution.
- [ ] Full attachment storage workflow.
- [ ] Invoice PDF generation.
- [ ] Full contact page/component implementation.
- [ ] Legacy `AppDbContext` changes.
- [ ] Legacy Presentation route changes.
- [ ] Active V2 `api/internal/*` changes.

## Current Baseline

Existing email sender:

- [ ] `IEmailService` exists with `Task SendEmailAsync(string to, string subject, string body)`.
- [ ] `EmailService` uses MailKit SMTP directly.
- [ ] `EmailService` reads `From`.
- [ ] `EmailService` reads `DisplayName`.
- [ ] `EmailService` reads `SmtpServer`.
- [ ] `EmailService` reads `Port`.
- [ ] `EmailService` reads `UseSsl`.
- [ ] `EmailService` reads `Username`.
- [ ] `EmailService` reads `Password`.
- [ ] `EmailSettingsOptionsValidator` validates production SMTP settings outside Development.
- [ ] `IEmailService` is registered for Commerce Node infrastructure.
- [ ] `IEmailService` is registered for Control Plane infrastructure.

Existing notification settings:

- [ ] `AdminSettings` contains `SmtpHost`.
- [ ] `AdminSettings` contains `SmtpFromEmail`.
- [ ] `AdminSettings` contains `SmtpFromDisplayName`.
- [ ] `CommerceNodeAdminSettingsService.UpdateNotificationsAsync` updates only safe SMTP display/config fields.
- [ ] `NotificationSettingsDto` exposes `SecretsConfigured`.
- [ ] `NotificationSettingsDto` does not expose SMTP password.
- [ ] `NotificationSettingsDto` does not expose SMTP username.

Existing hard-coded email calls:

- [ ] `AuthenticationService.SendConfirmationEmail` sends `"Confirm your email"` through `IEmailService` with hard-coded HTML.
- [ ] `NewsletterService.SubscribeAsync` sends welcome email fire-and-forget after subscriber creation.
- [ ] `Application.Services.Payment.CartService` sends bank transfer instructions through direct `IEmailService` calls in an older checkout path.
- [ ] Legacy `Infrastructure.Services.OrderTrackingService` sends tracking emails using `AppDbContext`.
- [ ] Active V2 `CommerceNodeOrderTrackingService` does not send email.
- [ ] Current direct calls are not template-driven.
- [ ] Current direct calls are not queued.

Existing task queue foundation:

- [ ] `CommerceTask` exists in Commerce Node.
- [ ] `CommerceTask` stores task type.
- [ ] `CommerceTask` stores status.
- [ ] `CommerceTask` stores payload JSON.
- [ ] `CommerceTask` stores idempotency key.
- [ ] `CommerceTask` stores attempt count/max attempts.
- [ ] `CommerceTask` stores next attempt timestamp.
- [ ] `CommerceTask` stores started/completed timestamps.
- [ ] `CommerceTask` stores error code/message.
- [ ] `CommerceTask` stores worker metadata.
- [ ] `CommerceTaskService.EnqueueAsync` queues task rows.
- [ ] `CommerceTaskWorker` polls pending/waiting tasks.
- [ ] `CommerceTaskWorker` dispatches registered `ICommerceTaskHandler` implementations.
- [ ] Existing handlers cover product import, media import, store create/deploy, exchange-rate update, and test tasks.
- [ ] No separate transactional message worker is needed.

Existing security/privacy hooks:

- [ ] `StoreSecurityPrivacySettings` contains captcha flags for newsletter.
- [ ] `StoreSecurityPrivacySettings` contains captcha flags for password recovery.
- [ ] `StoreSecurityPrivacySettings` contains captcha flags for contact.
- [ ] Storefront newsletter endpoint exists under `api/storefront/stores/{storeKey}/newsletter/subscribe`.
- [ ] Contact captcha setting exists.
- [ ] No Storefront contact delivery endpoint/service exists yet.

Existing order/payment hooks:

- [ ] Order Placement Core plans queueing notifications outside the main transaction.
- [ ] Payment Core has payment attempt/audit foundations.
- [ ] V2 shipping/tracking updates exist through Commerce Node admin order services.
- [ ] V2 shipping notification delivery is not implemented.

## Core Decisions

- [ ] D1: Transactional messages are Commerce Node-owned runtime data.
- [ ] D2: Keep `IEmailService` as SMTP transport adapter.
- [ ] D3: Store message-specific queue state, but reuse `CommerceTaskWorker`.
- [ ] D4: Use deterministic allowlisted token replacement only.
- [ ] D5: Do not store SMTP secrets in DB.
- [ ] D6: Seed defaults before building any full template editor.
- [ ] D7: Hooks enqueue messages; they are not workflow engines.

## Boundary Rules

- [ ] Templates, queued messages, and delivery state belong to `CommerceNodeDbContext`.
- [ ] Admin template APIs stay under `api/commerce/*`.
- [ ] Admin template APIs are called through Control Plane API.
- [ ] Storefront message-producing APIs stay under `api/storefront/stores/{storeKey}/*`.
- [ ] Store scope comes from active Commerce Node store context or `{storeKey}` route.
- [ ] SMTP credentials are never returned by APIs.
- [ ] Templates never execute arbitrary code.
- [ ] Message delivery failures do not roll back order placement.
- [ ] Message delivery failures do not roll back account registration.
- [ ] Message delivery failures do not roll back password recovery commands.
- [ ] Legacy `AppDbContext` is reference only.

## Data Model Checklist

Add `MessageTemplate`:

- [ ] `Id`.
- [ ] `PublicId`.
- [ ] `SystemName`.
- [ ] nullable `StoreId` for global/default template.
- [ ] nullable `LanguageCode` for default language.
- [ ] `SubjectTemplate`.
- [ ] `BodyHtmlTemplate`.
- [ ] `IsActive`.
- [ ] `Description`.
- [ ] `CreatedAtUtc`.
- [ ] `UpdatedAtUtc`.
- [ ] Unique `(SystemName, StoreId, LanguageCode)`.
- [ ] Index `(StoreId, SystemName)`.
- [ ] Index `(SystemName, IsActive)`.
- [ ] Store-specific template resolves before global template.
- [ ] Language-specific template resolves before default language.

Add `QueuedMessage`:

- [ ] `Id`.
- [ ] `PublicId`.
- [ ] `StoreId`.
- [ ] `TemplateSystemName`.
- [ ] `TemplateId`.
- [ ] `LanguageCode`.
- [ ] `ToEmail`.
- [ ] `ToName`.
- [ ] `FromEmail`.
- [ ] `FromName`.
- [ ] `ReplyToEmail`.
- [ ] `Subject`.
- [ ] `BodyHtml`.
- [ ] `Status`.
- [ ] `Priority`.
- [ ] `AttemptCount`.
- [ ] `MaxAttempts`.
- [ ] `NextAttemptAtUtc`.
- [ ] `LastAttemptAtUtc`.
- [ ] `SentAtUtc`.
- [ ] `FailedAtUtc`.
- [ ] `ErrorCode`.
- [ ] `ErrorMessage`.
- [ ] `CorrelationId`.
- [ ] `IdempotencyKey`.
- [ ] `RelatedEntityType`.
- [ ] `RelatedEntityId`.
- [ ] `AttachmentMetadataJson`.
- [ ] `CreatedAtUtc`.
- [ ] `UpdatedAtUtc`.
- [ ] Status `pending`.
- [ ] Status `sending`.
- [ ] Status `sent`.
- [ ] Status `waiting_retry`.
- [ ] Status `failed`.
- [ ] Status `cancelled`.
- [ ] Unique `PublicId`.
- [ ] Optional unique `IdempotencyKey` when present.
- [ ] Index `(StoreId, Status, NextAttemptAtUtc)`.
- [ ] Index `(StoreId, TemplateSystemName, CreatedAtUtc)`.
- [ ] Index `(StoreId, RelatedEntityType, RelatedEntityId)`.
- [ ] Index `(CorrelationId)`.

Attachment hook metadata only:

- [ ] file name.
- [ ] content type.
- [ ] media asset/public id.
- [ ] purpose.
- [ ] Do not persist binary attachments in this phase.

## Template Catalog

Seed required templates:

- [ ] `customer.account_activation`.
- [ ] `customer.password_recovery`.
- [ ] `order.placed`.
- [ ] `order.payment_status_changed`.
- [ ] `order.fulfillment_status_changed`.
- [ ] `storefront.contact_form`.

Optional later templates:

- [ ] `newsletter.welcome`.
- [ ] `payment.bank_transfer_instructions`.

## Token Model Checklist

Common tokens:

- [ ] `Store.Name`.
- [ ] `Store.Url`.
- [ ] `Store.SupportEmail`.
- [ ] `Store.SupportPhone`.
- [ ] `Customer.Email`.
- [ ] `Customer.FullName`.
- [ ] `Customer.FirstName`.
- [ ] `Customer.LastName`.

Account tokens:

- [ ] `Account.ActivationUrl`.
- [ ] `Account.PasswordResetUrl`.

Order tokens:

- [ ] `Order.Reference`.
- [ ] `Order.CreatedAt`.
- [ ] `Order.Total`.
- [ ] `Order.Currency`.
- [ ] `Order.Status`.
- [ ] `Order.PaymentStatus`.
- [ ] `Order.ShippingStatus`.
- [ ] `Order.DetailUrl`.
- [ ] `Order.ReceiptUrl`.

Shipping tokens:

- [ ] `Shipment.Carrier`.
- [ ] `Shipment.TrackingNumber`.
- [ ] `Shipment.TrackingUrl`.

Contact tokens:

- [ ] `Contact.Name`.
- [ ] `Contact.Email`.
- [ ] `Contact.Subject`.
- [ ] `Contact.Message`.

Rules:

- [ ] HTML encode token values by default.
- [ ] Only code-marked safe tokens can bypass encoding.
- [ ] Unknown tokens stay visible in preview or create validation warnings.
- [ ] Production send fails only if required tokens are missing.
- [ ] Templates cannot access arbitrary object properties by reflection without allowlist.
- [ ] Sensitive token values such as reset tokens are not logged in message error logs.

## Target API Checklist

Commerce Admin template APIs under `api/commerce/message-templates`:

- [ ] `GET /message-templates?storeKey={storeKey}`.
- [ ] `GET /message-templates/{publicId}?storeKey={storeKey}`.
- [ ] `PUT /message-templates/{publicId}?storeKey={storeKey}`.
- [ ] `POST /message-templates/{publicId}/reset?storeKey={storeKey}`.
- [ ] `POST /message-templates/preview?storeKey={storeKey}`.
- [ ] Required `storeKey` query.
- [ ] No SMTP password/username in requests or responses.
- [ ] No arbitrary system name changes after creation.
- [ ] Subject/body length limits.
- [ ] Language code validation.
- [ ] Store scope comes from query/store context, not trusted body.

Commerce Admin queue APIs under `api/commerce/queued-messages`:

- [ ] `GET /queued-messages?storeKey={storeKey}&status=&templateSystemName=&skip=&take=`.
- [ ] `GET /queued-messages/{publicId}?storeKey={storeKey}`.
- [ ] `POST /queued-messages/{publicId}/retry?storeKey={storeKey}`.
- [ ] `POST /queued-messages/{publicId}/cancel?storeKey={storeKey}`.
- [ ] Return subject/status/timestamps/error summary.
- [ ] Do not return raw password reset token URLs to admin by default after send.
- [ ] Do not expose SMTP credentials.

Storefront contact API under `api/storefront/stores/{storeKey}/contact`:

- [ ] `POST /contact`.
- [ ] Request has name.
- [ ] Request has email.
- [ ] Request has subject.
- [ ] Request has message.
- [ ] Request has captcha token when enabled.
- [ ] Consent evidence hook deferred until required.
- [ ] Rate limited and captcha-aware.
- [ ] Returns generic success on accepted delivery.
- [ ] Stores no long-term contact inquiry record unless support workflow requires it.
- [ ] Enqueues `storefront.contact_form` to store support email.

## API Contract Checklist

Every new or changed API must satisfy:

- [ ] Stable `operationId`.
- [ ] Short summary.
- [ ] Explicit request/response DTOs.
- [ ] Standard error response schemas.
- [ ] Required request body metadata.
- [ ] Validation metadata for email, subject/body lengths, language code, and paging.
- [ ] Security metadata for admin endpoints.
- [ ] Store scope from route/query/server context.
- [ ] No domain entities in public schemas.
- [ ] No SMTP username/password in responses.
- [ ] Side-effecting operations use `POST` or `PUT`, not `GET`.
- [ ] Contract tests and snapshots updated.

## Phase 0 - Baseline Guardrails

Goal: lock down current direct email behavior before replacing it.

Implementation checklist:

- [x] Inventory all `SendEmailAsync` call sites. 2026-07-17 Phase 0: `TransactionalMessageBaselineTests.DirectEmailCallSiteInventory_MatchesKnownBaseline`.
- [x] Mark active V2 direct email paths. 2026-07-17 Phase 0: active shared/V2 paths are `AuthenticationService`, `NewsletterService`, and older `Application.Services.Payment.CartService`; active CommerceNode checkout service does not depend on `IEmailService`.
- [x] Mark legacy/reference direct email paths. 2026-07-17 Phase 0: `Infrastructure.Services.OrderTrackingService` is legacy `AppDbContext`; `EmailService` is the transport adapter.
- [x] Add tests or documentation for current account activation direct email behavior. 2026-07-17 Phase 0: existing `AuthenticationServiceTests.SendConfirmationEmail_SendsEmail` remains in the focused baseline.
- [x] Confirm `EmailSettings` validation still works outside Development. 2026-07-17 Phase 0: existing `EmailSettingsOptionsValidatorTests` remains in the focused baseline.
- [x] Confirm notification settings do not expose SMTP username/password. 2026-07-17 Phase 0: `NotificationSettingsDto_DoesNotExposeSmtpSecrets`.
- [x] Confirm CommerceTask worker is enabled/configurable in Commerce Node. 2026-07-17 Phase 0: `CommerceNodeRuntime_RegistersCommerceTaskWorker`.
- [x] Add/update QA checklist entries for Transactional Message Core. 2026-07-17 Phase 0.

Verification checklist:

- [x] Current auth registration/confirmation behavior remains working. 2026-07-17 Phase 0.
- [x] No legacy direct-email code is extended. 2026-07-17 Phase 0.
- [x] Direct call site list is captured in implementation notes or tests. 2026-07-17 Phase 0.

Exit criteria:

- [x] Baseline is documented before replacing direct send paths. 2026-07-17 Phase 0.
- [x] Existing tests pass. 2026-07-17 Phase 0: focused transactional baseline/auth/settings tests passed.

Suggested commit:

```text
test(transactional-message): lock email baseline
```

## Phase 1 - Message Data Model And Seeds

Goal: add durable templates and queued message rows without changing send behavior.

Implementation checklist:

- [x] Add `MessageTemplate` entity. 2026-07-17 Phase 1.
- [x] Add `QueuedMessage` entity. 2026-07-17 Phase 1.
- [x] Add EF configuration in `CommerceNodeDbContext`. 2026-07-17 Phase 1.
- [x] Add Commerce Node migration. 2026-07-17 Phase 1: `CommerceNodeTransactionalMessages`.
- [x] Add status constants/check constraints. 2026-07-17 Phase 1: queued message statuses and attempt/priority constraints.
- [x] Add indexes. 2026-07-17 Phase 1: public id, scoped template lookup, idempotency, retry queue, related entity, and correlation indexes.
- [x] Seed default global templates for approved system names. 2026-07-17 Phase 1.
- [x] Add template resolver interface. 2026-07-17 Phase 1: `IMessageTemplateResolver`.
- [x] Add template resolver implementation. 2026-07-17 Phase 1: `MessageTemplateResolver`.
- [x] Add queued message repository/service contract. 2026-07-17 Phase 1: `IMessageQueueService` contract added for Phase 3 implementation.

Verification checklist:

- [x] Migration applies to `CommerceNodeDbContext`. 2026-07-17 Phase 1: CommerceNode API build passed after migration.
- [x] Seeded templates are idempotent. 2026-07-17 Phase 1: EF `HasData` seeds deterministic IDs/system names.
- [x] Existing app starts with no SMTP/template runtime behavior change. 2026-07-17 Phase 1: no send path changed.
- [x] No data is added to `AppDbContext`. 2026-07-17 Phase 1.

Exit criteria:

- [x] Templates and queued messages can be persisted safely. 2026-07-17 Phase 1.

Suggested commit:

```text
feat(transactional-message): add message templates and queue schema
```

## Phase 2 - Token Rendering And Preview

Goal: make templates render safely and predictably.

Implementation checklist:

- [x] Add `IMessageTokenRenderer`. 2026-07-17 Phase 2.
- [x] Implement allowlisted token replacement. 2026-07-17 Phase 2: only dictionary-provided token names are replaced.
- [x] Add HTML encoding by default. 2026-07-17 Phase 2.
- [x] Add missing-token detection. 2026-07-17 Phase 2.
- [x] Add preview DTOs for admin preview. 2026-07-17 Phase 2: render request/result/warning DTOs added for Phase 7 preview endpoint.
- [x] Add normal token replacement tests. 2026-07-17 Phase 2.
- [x] Add HTML encoding tests. 2026-07-17 Phase 2.
- [x] Add unknown token behavior tests. 2026-07-17 Phase 2.
- [x] Add missing required token warning tests. 2026-07-17 Phase 2.
- [x] Add no arbitrary expression/code execution tests. 2026-07-17 Phase 2.

Verification checklist:

- [x] Templates render deterministically. 2026-07-17 Phase 2.
- [x] Unsafe user input is encoded. 2026-07-17 Phase 2.
- [x] Preview can render sample models without sending email. 2026-07-17 Phase 2: renderer has no SMTP/queue dependency.

Exit criteria:

- [x] Template rendering is safe enough for account/order/contact messages. 2026-07-17 Phase 2.

Suggested commit:

```text
feat(transactional-message): add safe token renderer
```

## Phase 3 - Queue And Delivery Handler

Goal: enqueue and send messages asynchronously using existing CommerceTask infrastructure.

Implementation checklist:

- [x] Add `IMessageQueueService`. 2026-07-17 Phase 3: Phase 1 contract now has `MessageQueueService` implementation.
- [x] Add `IMessageDeliveryService`. 2026-07-17 Phase 3.
- [x] Add `MessageDeliverTaskHandler` with task type `message.deliver`. 2026-07-17 Phase 3.
- [x] Enqueue creates `QueuedMessage`. 2026-07-17 Phase 3: `MessageQueueServiceTests.QueueAsync_CreatesQueuedMessageAndDeliveryTask`.
- [x] Enqueue creates `CommerceTask`. 2026-07-17 Phase 3: task type/payload/idempotency asserted.
- [x] Handler loads `QueuedMessage`. 2026-07-17 Phase 3: handler delegates by queued message public id to `IMessageDeliveryService`.
- [x] Handler marks message `sending`. 2026-07-17 Phase 3: `MessageDeliveryService` persists `sending` before transport send.
- [x] Handler calls `IEmailService`. 2026-07-17 Phase 3: `MessageDeliveryServiceTests.DeliverAsync_SendsEmailAndMarksMessageSent`.
- [x] Handler marks message `sent` on success. 2026-07-17 Phase 3.
- [x] Handler marks retry/failure state on failure. 2026-07-17 Phase 3: retryable and terminal SMTP failure tests passed.
- [x] Idempotency prevents duplicate queue rows for same command. 2026-07-17 Phase 3: `QueueAsync_WithSameIdempotencyKey_ReturnsExistingQueuedMessage`.
- [x] Failed delivery does not throw back into order/auth transactions. 2026-07-17 Phase 3: enqueue writes queue/task only; task handler returns retryable/terminal task result instead of coupling delivery to source commands.
- [x] Add admin retry service method. 2026-07-17 Phase 3: `IMessageDeliveryService.RetryAsync`.
- [x] Add admin cancel service method. 2026-07-17 Phase 3: `IMessageDeliveryService.CancelAsync`.

Verification checklist:

- [x] Queued message can be delivered by `CommerceTaskWorker`. 2026-07-17 Phase 3: `MessageDeliverTaskHandler` registered as `ICommerceTaskHandler`; CommerceNode API build passed.
- [x] Retryable failure updates task and message state. 2026-07-17 Phase 3: `DeliverAsync_WhenSmtpFailsBeforeMaxAttempts_MarksWaitingRetry`.
- [x] Terminal failure updates task and message state. 2026-07-17 Phase 3: `DeliverAsync_WhenSmtpFailsAtMaxAttempts_MarksFailed`.
- [x] Manual retry requeues delivery safely. 2026-07-17 Phase 3: `RetryAndCancel_UpdateQueuedMessageState`.
- [x] Duplicate idempotency key does not enqueue duplicate message. 2026-07-17 Phase 3.

Exit criteria:

- [x] Transactional messages can be delivered through existing worker infrastructure. 2026-07-17 Phase 3.

Suggested commit:

```text
feat(transactional-message): deliver queued messages
```

## Phase 4 - Account Messages

Goal: move account activation and password recovery onto templates and queue.

Implementation checklist:

- [x] Replace direct confirmation email send path with `IMessageQueueService`. 2026-07-17 Phase 4: `AuthenticationService` now dispatches through `IAccountEmailDispatcher`; CommerceNode overrides it with `QueuedAccountEmailDispatcher`.
- [x] Keep fallback direct email only if queue enqueue fails before migration hardening. 2026-07-17 Phase 4: fallback is limited to non-CommerceNode/default `DirectAccountEmailDispatcher`; active CommerceNode does not direct-send account messages in request path.
- [x] Log fallback clearly if retained. 2026-07-17 Phase 4: no CommerceNode fallback retained; queue failures are logged as enqueue failures.
- [x] Add password recovery template integration for Customer Identity Account phase. 2026-07-17 Phase 4: password recovery queues `customer.password_recovery`.
- [x] Redact reset tokens from logs. 2026-07-17 Phase 4: logs include email/user id/error code only, not reset URL/token.
- [x] Redact activation tokens from logs. 2026-07-17 Phase 4: logs include email/user id/error code only, not activation URL/token.
- [x] Redact reset/activation token URLs from admin queue detail where appropriate. 2026-07-17 Phase 4: no admin queue detail endpoint exists yet; Phase 7 must preserve this redaction before exposing queued bodies.
- [x] Add anti-enumeration behavior for password recovery response. 2026-07-17 Phase 4: known and unknown emails still return the same generic response.

Verification checklist:

- [x] Account activation email is queued. 2026-07-17 Phase 4: `QueuedAccountEmailDispatcherTests.SendActivationAsync_QueuesStoreScopedTemplateMessage`.
- [x] Account activation email is template-rendered. 2026-07-17 Phase 4: queued activation uses seeded `customer.account_activation` template and renderer from Phase 2/3.
- [x] Password recovery email is queued when endpoint exists. 2026-07-17 Phase 4: `ForgotPassword_WithKnownEmail_GeneratesIdentityTokenAndSendsGenericSuccess` and queued dispatcher tests passed.
- [x] Registration/auth command succeeds or returns controlled error when email provider is down. 2026-07-17 Phase 4: source command only enqueues; unconfirmed-login resend failure returns a controlled error.
- [x] User creation is not left half-complete because SMTP is down. 2026-07-17 Phase 4: SMTP delivery is async through `message.deliver`; enqueue failure still rolls back strict confirmation registration.

Exit criteria:

- [x] Account messages use queued template delivery. 2026-07-17 Phase 4.

Suggested commit:

```text
feat(transactional-message): queue account emails
```

## Phase 5 - Order And Payment/Fulfillment Hooks

Goal: enqueue core commerce transactional notifications after state changes.

Implementation checklist:

- [x] Add `order.placed` enqueue call after order is created. 2026-07-17 Phase 5: `OrderCreatedTaskHandler` queues `order.placed` through `ICommerceTransactionalMessageService`.
- [x] Ensure order notification enqueue happens outside main order transaction or through post-commit/outbox-safe point. 2026-07-17 Phase 5: order placement still writes durable `order.created` CommerceTask; worker handler queues message afterward.
- [x] Add payment status changed hook after payment/order payment transitions. 2026-07-17 Phase 5: `PaymentAttemptService` queues after state save/commit when attempt has an order.
- [x] Add fulfillment/shipping status changed hook in V2 Commerce Node order tracking/admin shipping updates. 2026-07-17 Phase 5: shipment upsert and order tracking status updates queue fulfillment notifications after save.
- [x] Use order snapshot data for message content. 2026-07-17 Phase 5: notification service uses order/customer/store snapshots first, with store record only for missing support/default-culture metadata.
- [x] Do not use live mutable product/customer data for historical message content. 2026-07-17 Phase 5: tokens come from order snapshot fields, not product/customer tables.
- [x] Add idempotency keys based on store/order/message event. 2026-07-17 Phase 5: keys use order id plus event/status.

Verification checklist:

- [x] Order placed confirmation queues once per order. 2026-07-17 Phase 5: order.created task idempotency remains per order and handler registration is guarded.
- [x] Retried checkout/place-order does not send duplicate order confirmation. 2026-07-17 Phase 5: existing order placement idempotency plus message idempotency key `order.placed:{orderId}`.
- [x] Payment hooks do not fire duplicate messages for no-op status updates. 2026-07-17 Phase 5: payment transition no-op returns before hook; message idempotency includes payment status.
- [x] Fulfillment hooks do not fire duplicate messages for no-op status updates. 2026-07-17 Phase 5: message idempotency includes shipping status.
- [x] Message delivery failure does not roll back order/payment/shipping state. 2026-07-17 Phase 5: payment and shipment hook tests inject queue failure and assert state still saves.

Exit criteria:

- [x] Core commerce events can enqueue transactional messages safely. 2026-07-17 Phase 5.

Suggested commit:

```text
feat(transactional-message): add commerce notification hooks
```

## Phase 6 - Contact Form Delivery Contract

Goal: provide backend delivery support for future Storefront/WASM contact component.

Implementation checklist:

- [x] Add Storefront contact request DTO. 2026-07-17 Phase 6: `StorefrontContactRequest`.
- [x] Add Storefront contact response DTO. 2026-07-17 Phase 6: `StorefrontContactResponse`.
- [x] Add `POST api/storefront/stores/{storeKey}/contact`. 2026-07-17 Phase 6: `StorefrontScopedContactController.Submit`.
- [x] Validate name length. 2026-07-17 Phase 6: required and max 160.
- [x] Validate email format. 2026-07-17 Phase 6: required email with max 254.
- [x] Validate subject length. 2026-07-17 Phase 6: required and max 200.
- [x] Validate message length. 2026-07-17 Phase 6: required and max 4000.
- [x] Apply contact captcha when enabled. 2026-07-17 Phase 6: uses `CaptchaTargetNames.Contact`.
- [x] Apply Storefront rate-limit policy. 2026-07-17 Phase 6: reuses the existing public-form Storefront newsletter limiter policy.
- [x] Resolve recipient from store support email. 2026-07-17 Phase 6.
- [x] Fall back to store company email/admin notification email if support email missing. 2026-07-17 Phase 6: implemented safe `CompanyEmail` fallback; admin notification-email fallback remains deferred until a Storefront-safe field exists.
- [x] Enqueue `storefront.contact_form`. 2026-07-17 Phase 6: `StorefrontContactMessageService` queues through `IMessageQueueService`.
- [x] Return generic accepted/success response. 2026-07-17 Phase 6.

Verification checklist:

- [x] Contact delivery can be used by future Storefront component. 2026-07-17 Phase 6: request/response DTOs and OpenAPI metadata are generator-safe.
- [x] Endpoint does not require full content page implementation. 2026-07-17 Phase 6: backend-only contract endpoint.
- [x] Captcha/rate limiting are available. 2026-07-17 Phase 6: focused captcha/rate-limit tests passed with contact endpoint included.
- [x] Store support recipient is validated. 2026-07-17 Phase 6: missing support/company recipient returns conflict and does not queue.

Exit criteria:

- [x] Storefront contact delivery contract exists and is spam-aware. 2026-07-17 Phase 6: focused contact service and Storefront OpenAPI/captcha/rate-limit tests passed 70/70.

Suggested commit:

```text
feat(transactional-message): add contact delivery endpoint
```

## Phase 7 - Admin Management And Observability

Goal: make templates and queued messages manageable without building a full designer.

Implementation checklist:

- [x] Add Commerce Admin template list endpoint. 2026-07-17 Phase 7: `GET api/commerce/admin/message-templates`.
- [x] Add Commerce Admin template detail endpoint. 2026-07-17 Phase 7: `GET api/commerce/admin/message-templates/{publicId}`.
- [x] Add Commerce Admin template update endpoint. 2026-07-17 Phase 7: `PUT api/commerce/admin/message-templates/{publicId}` creates/updates store override without mutating global defaults.
- [x] Add Commerce Admin template reset endpoint. 2026-07-17 Phase 7: `POST api/commerce/admin/message-templates/{publicId}/reset`.
- [x] Add Commerce Admin template preview endpoint. 2026-07-17 Phase 7: `POST api/commerce/admin/message-templates/preview`.
- [x] Add Commerce Admin queued message list endpoint. 2026-07-17 Phase 7: `GET api/commerce/admin/queued-messages`.
- [x] Add Commerce Admin queued message detail endpoint. 2026-07-17 Phase 7: `GET api/commerce/admin/queued-messages/{publicId}`.
- [x] Add Commerce Admin queued message retry endpoint. 2026-07-17 Phase 7: `POST api/commerce/admin/queued-messages/{publicId}/retry`.
- [x] Add Commerce Admin queued message cancel endpoint. 2026-07-17 Phase 7: `POST api/commerce/admin/queued-messages/{publicId}/cancel`.
- [x] Add OpenAPI metadata. 2026-07-17 Phase 7: `CommerceTransactionalMessageAdminOperationMetadataFilter`.
- [x] Add contract tests. 2026-07-17 Phase 7: static Swagger metadata test plus service contract tests passed.
- [x] Add minimal Control Plane gateway/client support only if admin UI needs it in this phase. 2026-07-17 Phase 7: no Control Plane UI/gateway added because this phase only exposes backend admin contract.
- [x] Add audit log entries for template update/reset. 2026-07-17 Phase 7.
- [x] Add audit log entries for message retry/cancel. 2026-07-17 Phase 7.
- [x] Redact sensitive token values in admin queue detail. 2026-07-17 Phase 7: queued message admin detail intentionally omits rendered `BodyHtml` and `IdempotencyKey`.

Verification checklist:

- [x] Admin can inspect templates safely. 2026-07-17 Phase 7.
- [x] Admin can update templates safely. 2026-07-17 Phase 7: store override behavior covered by `TransactionalMessageAdminServiceTests`.
- [x] Admin can see failed queued messages. 2026-07-17 Phase 7: queue list/detail DTOs expose status/error summary only.
- [x] Admin can retry/cancel queued messages. 2026-07-17 Phase 7: retry store-scope test passed; cancel path uses the same service scope and audit pattern.
- [x] Swagger does not expose secrets. 2026-07-17 Phase 7: no SMTP credentials or rendered body are present in admin queue DTOs.
- [x] Control Plane Web still calls Control Plane API, not Commerce Node directly. 2026-07-17 Phase 7: no Control Plane Web changes.

Exit criteria:

- [x] Transactional messages are observable and administrable without secret exposure. 2026-07-17 Phase 7: CommerceNode API build passed; focused admin service/Swagger tests passed 11/11.

Suggested commit:

```text
feat(transactional-message): expose message administration
```

## Phase 8 - QA, Contracts, And Cleanup

Goal: close the phase with reliable tests and no accidental legacy extension.

Implementation checklist:

- [ ] Add/update Commerce Node API contract tests.
- [ ] Add template resolution service tests.
- [ ] Add token rendering service tests.
- [ ] Add queue idempotency tests.
- [ ] Add delivery success/failure tests.
- [ ] Add retry/cancel tests.
- [ ] Add integration test for `message.deliver` task handler.
- [ ] Add account activation queue test.
- [ ] Add order placed queue test when Order Placement Core is integrated.
- [ ] Add contact endpoint test if Phase 6 is implemented.
- [ ] Update `QA-CommerceNode.todo.md`.
- [ ] Update `QA-StorefrontV2.todo.md` if contact/account UI paths are touched.
- [ ] Remove or stop using active V2 direct email calls once queue path is stable.

Verification checklist:

- [ ] Focused tests pass.
- [ ] OpenAPI validates.
- [ ] No new V2 feature uses legacy `AppDbContext`.
- [ ] Direct email calls remain only in legacy/reference paths or consciously deferred compatibility paths.

Exit criteria:

- [ ] Transactional Message Core is contract-protected and queue-backed.
- [ ] QA checklists contain tested evidence.

Suggested commit:

```text
test(transactional-message): verify message core
```

## QA Checklist Seeds

### Commerce Node

- [ ] Message templates are seeded idempotently.
- [ ] Store template override resolves before global template.
- [ ] Language override resolves before default language.
- [ ] Inactive template does not send unless fallback is valid.
- [ ] Token renderer encodes user input.
- [ ] Unknown tokens are reported safely.
- [ ] Required missing tokens produce warnings/errors.
- [ ] Queue service writes one row per idempotency key.
- [ ] `message.deliver` task sends pending message.
- [ ] SMTP failure updates retry/failure state without throwing into source command.
- [ ] Account activation queues template email.
- [ ] Password recovery queues template email when endpoint exists.
- [ ] Order placed queues once per order.
- [ ] Payment status hook queues once per effective state change.
- [ ] Fulfillment status hook queues once per effective state change.
- [x] Contact endpoint validates input and captcha/rate policy. 2026-07-17 Phase 6: Storefront contact DTO validation, captcha target, public-form rate-limit metadata, and OpenAPI snapshot are covered.
- [x] Admin APIs expose no SMTP secrets. 2026-07-17 Phase 7: admin DTOs avoid SMTP username/password and rendered message body.
- [ ] OpenAPI contract tests pass.

### Storefront V2

- [x] Contact form endpoint can be consumed by future component. 2026-07-17 Phase 6: Storefront OpenAPI snapshot includes `StorefrontContact_Submit`.
- [x] Contact submission returns generic accepted/success response. 2026-07-17 Phase 6.
- [x] Contact submission does not expose SMTP or support internals. 2026-07-17 Phase 6: response only returns accepted/message in the standard envelope.
- [ ] Account activation/password recovery pages keep existing UX when queue-backed email is enabled.
- [ ] Browser network does not expose reset/activation tokens except intended user-facing URLs.

### Control Plane

- [ ] ControlPlane Web does not call CommerceNode message APIs directly.
- [ ] ControlPlane API gateway uses Commerce Admin message routes with `storeKey`.
- [ ] Admin template update/reset writes audit.
- [ ] Admin message retry/cancel writes audit.
- [ ] No message runtime data is stored in `ControlPlaneDbContext`.

## Failure Modes To Design Against

- [ ] SMTP outage blocks checkout/order placement.
- [ ] Duplicate order confirmation after checkout retry.
- [ ] Password reset token leaks in logs/admin UI.
- [ ] Template executes arbitrary code.
- [ ] Store A sends Store B template/sender.
- [ ] SMTP credentials leak through API.
- [ ] Missing template breaks account activation.
- [ ] Message queue and CommerceTask state diverge.
- [ ] Contact endpoint becomes spam target.
- [ ] Admin edits invalid HTML/tokens.

## Test Map

- [ ] Data model tests:
  - [ ] migration.
  - [ ] indexes.
  - [ ] check constraints.
  - [ ] seed idempotency.
- [ ] Template resolution tests:
  - [ ] store override.
  - [ ] language override.
  - [ ] global fallback.
  - [ ] inactive template.
- [ ] Token rendering tests:
  - [ ] encoding.
  - [ ] unknown tokens.
  - [ ] required missing tokens.
  - [ ] safe URL token handling.
- [ ] Queue service tests:
  - [ ] create queued row.
  - [ ] idempotency.
  - [ ] correlation.
  - [ ] state transitions.
- [ ] Delivery handler tests:
  - [ ] success.
  - [ ] SMTP failure.
  - [ ] retryable failure.
  - [ ] terminal failure.
- [ ] Account activation tests:
  - [ ] registration queues activation email.
  - [ ] email body includes activation link.
- [ ] Password recovery tests:
  - [ ] generic response.
  - [ ] known account queues reset email.
  - [ ] unknown account does not leak.
- [ ] Order placed tests:
  - [ ] order creation enqueues once.
  - [ ] retry does not duplicate.
- [ ] Payment/fulfillment hook tests:
  - [ ] status change queues once.
  - [ ] no-op does not queue.
- [x] Contact form tests:
  - [x] validation. 2026-07-17 Phase 6.
  - [x] captcha branch. 2026-07-17 Phase 6.
  - [x] rate-limit metadata. 2026-07-17 Phase 6.
  - [x] generic success. 2026-07-17 Phase 6.
- [x] Admin API tests:
  - [x] operation IDs. 2026-07-17 Phase 7.
  - [x] schemas. 2026-07-17 Phase 7.
  - [x] security. 2026-07-17 Phase 7: Commerce Admin middleware and Swagger filters retain node credential and storeKey requirements.
  - [x] no secrets. 2026-07-17 Phase 7.

## Migration And Compatibility

- [ ] Use additive CommerceNode migrations only.
- [ ] Existing SMTP config remains in `EmailSettings`.
- [ ] Existing admin notification settings keep exposing `SecretsConfigured` only.
- [ ] Existing CommerceTask worker remains the only background worker for message delivery.
- [ ] Existing direct email calls continue until their active V2 path is migrated.
- [ ] Legacy direct email paths are not extended.
- [ ] Existing newsletter/contact UI behavior is not changed unless explicitly touched.
- [ ] Existing order/auth/payment commands do not depend on SMTP availability.

## Dependency Notes

- [ ] Depends on Customer Identity Account for password recovery endpoint and account links.
- [ ] Depends on Order Placement Core for durable order placed event/notification enqueue point.
- [ ] Depends on Payment Core for payment state transition hooks.
- [ ] Depends on Shipping Core / Order Placement Core for fulfillment status/tracking hooks.
- [ ] Depends on Security Privacy for captcha targets and rate-limit behavior on contact/password recovery.
- [ ] Aligns with Configuration and Feature State Core for future secret/public config separation.

## Out Of Scope Backlog

- [ ] Marketing campaigns.
- [ ] Newsletter bulk send.
- [ ] Customer notification preferences.
- [ ] Multi-channel messaging.
- [ ] Multi-SMTP-account secret management UI.
- [ ] Provider failover.
- [ ] Email open/click tracking.
- [ ] Bounce processing.
- [ ] Full attachment storage.
- [ ] Invoice PDF generation.
- [ ] Visual designer.
- [ ] Rich template scripting.
- [ ] Full contact page/component implementation.

## Recommended Implementation Order

- [x] Phase 0 - baseline guardrails. 2026-07-17: focused transactional baseline/auth/settings tests passed.
- [x] Phase 1 - message data model and seeds. 2026-07-17: CommerceNode API build passed and focused model/resolver tests passed 34/34.
- [x] Phase 2 - token rendering and preview. 2026-07-17: CommerceNode API build passed; focused renderer/model/resolver tests passed 43/43.
- [x] Phase 3 - queue and delivery handler. 2026-07-17: CommerceNode API build passed; focused queue/delivery/renderer/resolver tests passed 18/18.
- [x] Phase 4 - account messages. 2026-07-17: CommerceNode API build passed; focused auth/dispatcher/queue/delivery/baseline tests passed 63/63.
- [x] Phase 5 - order and payment/fulfillment hooks. 2026-07-17: focused commerce notification/payment/shipment/task baseline tests passed 25/25.
- [x] Phase 6 - contact form delivery contract. 2026-07-17: CommerceNode API build passed; focused contact service/OpenAPI/captcha/rate-limit tests passed 70/70.
- [x] Phase 7 - admin management and observability. 2026-07-17: CommerceNode API build passed; focused admin service/Swagger metadata tests passed 11/11.
- [ ] Phase 8 - QA, contracts, and cleanup.

## Acceptance Criteria

- [ ] Default transactional templates are seeded.
- [ ] Account activation uses queued template delivery.
- [ ] Password recovery can queue reset email when endpoint exists.
- [ ] Order placed confirmation is queued once per order.
- [ ] Payment notification hook exists without workflow expansion.
- [ ] Fulfillment notification hook exists without workflow expansion.
- [x] Contact form delivery endpoint/contract exists if selected for implementation. 2026-07-17 Phase 6.
- [ ] Queued messages have retry/failure/audit state.
- [x] Admin can inspect templates without seeing SMTP secrets. 2026-07-17 Phase 7.
- [x] Admin can inspect queued messages without seeing SMTP secrets. 2026-07-17 Phase 7.
- [ ] New APIs satisfy V2 API contract standards.
- [ ] Focused tests and QA checklists are updated.
- [ ] No active V2 behavior is added to legacy `AppDbContext`.
- [ ] No active V2 behavior is added to legacy Presentation projects.
