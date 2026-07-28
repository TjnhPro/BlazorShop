(function () {
  const productPurchaseRootSelector = "[data-storefront-product-purchase]";
  const productPurchaseSubmitSelector = "[data-storefront-product-purchase-submit]";
  const addressSelectSelector = "[data-storefront-address-select]";
  const manualAddressSelector = "[data-storefront-manual-address]";
  const manualAddressFieldSelector = "[data-storefront-manual-address-field]";
  const productGallerySelector = "[data-storefront-product-gallery]";
  const galleryThumbnailSelector = "[data-storefront-gallery-thumbnail]";
  const galleryMainImageSelector = "[data-storefront-gallery-main-image]";
  const galleryPlaceholderSelector = "[data-storefront-gallery-placeholder]";
  const galleryPreviousSelector = "[data-storefront-gallery-prev]";
  const galleryNextSelector = "[data-storefront-gallery-next]";
  const toastRegionSelector = "[data-storefront-toast-region]";
  const toastTemplateSelector = "[data-storefront-toast-template]";
  const pendingToastStorageKey = "blazorshop:storefront:pending-toast";
  const buttonResetDelayMs = 1600;
  const toastDurationMs = 5000;
  const buttonResetTimers = new WeakMap();

  function parseInteger(value, fallback = 0) {
    const parsed = Number.parseInt(value, 10);
    return Number.isFinite(parsed) ? parsed : fallback;
  }

  function setText(element, text) {
    if (element instanceof HTMLElement) {
      element.textContent = text || "";
    }
  }

  function toggleHidden(element, hidden) {
    if (element instanceof HTMLElement) {
      element.classList.toggle("hidden", Boolean(hidden));
    }
  }

  function resolveToastTheme(level) {
    switch ((level || "info").toLowerCase()) {
      case "success":
        return { background: "rgba(20, 83, 45, 0.96)", accentBackground: "rgba(187, 247, 208, 0.18)", accentColor: "#dcfce7" };
      case "warning":
        return { background: "rgba(180, 83, 9, 0.96)", accentBackground: "rgba(253, 230, 138, 0.18)", accentColor: "#fef3c7" };
      case "error":
        return { background: "rgba(153, 27, 27, 0.96)", accentBackground: "rgba(254, 202, 202, 0.18)", accentColor: "#fee2e2" };
      default:
        return { background: "rgba(3, 105, 161, 0.96)", accentBackground: "rgba(186, 230, 253, 0.18)", accentColor: "#e0f2fe" };
    }
  }

  function resolveToastIcon(level) {
    switch ((level || "info").toLowerCase()) {
      case "success":
        return '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="h-5 w-5"><path d="m5 13 4 4L19 7" /></svg>';
      case "warning":
        return '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="h-5 w-5"><path d="M12 9v4" /><path d="M12 17h.01" /><path d="M10.29 3.86 1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0Z" /></svg>';
      case "error":
        return '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="h-5 w-5"><circle cx="12" cy="12" r="10" /><path d="m15 9-6 6" /><path d="m9 9 6 6" /></svg>';
      default:
        return '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="h-5 w-5"><circle cx="12" cy="12" r="10" /><path d="M12 16v-4" /><path d="M12 8h.01" /></svg>';
    }
  }

  function showToast(level, heading, message, duration = toastDurationMs) {
    const region = document.querySelector(toastRegionSelector);
    const template = document.querySelector(toastTemplateSelector);
    if (!(region instanceof HTMLElement) || !(template instanceof HTMLTemplateElement)) {
      return;
    }

    const fragment = template.content.cloneNode(true);
    const toast = fragment.querySelector("[data-storefront-toast]");
    const accent = fragment.querySelector("[data-storefront-toast-accent]");
    const headingElement = fragment.querySelector("[data-storefront-toast-heading]");
    const messageElement = fragment.querySelector("[data-storefront-toast-message]");
    const closeButton = fragment.querySelector("[data-storefront-toast-close]");

    if (!(toast instanceof HTMLElement) || !(accent instanceof HTMLElement) || !(headingElement instanceof HTMLElement) || !(messageElement instanceof HTMLElement)) {
      return;
    }

    const theme = resolveToastTheme(level);
    toast.style.backgroundColor = theme.background;
    accent.style.backgroundColor = theme.accentBackground;
    accent.style.color = theme.accentColor;
    accent.innerHTML = resolveToastIcon(level);
    headingElement.textContent = heading || "Info";
    messageElement.textContent = message || "An event occurred.";

    const dismiss = () => {
      if (toast.dataset.dismissed === "true") {
        return;
      }

      toast.dataset.dismissed = "true";
      toast.style.opacity = "0";
      toast.style.transform = "translateY(-8px)";
      window.setTimeout(() => toast.remove(), 180);
    };

    if (closeButton instanceof HTMLButtonElement) {
      closeButton.addEventListener("click", dismiss);
    }

    region.appendChild(fragment);
    window.requestAnimationFrame(() => {
      toast.style.opacity = "1";
      toast.style.transform = "translateY(0)";
    });

    window.setTimeout(dismiss, Math.max(1500, parseInteger(duration, toastDurationMs)));
  }

  function flushQueuedToast() {
    try {
      const raw = window.sessionStorage.getItem(pendingToastStorageKey);
      if (!raw) {
        return;
      }

      window.sessionStorage.removeItem(pendingToastStorageKey);
      const toast = JSON.parse(raw);
      if (!toast || !toast.message) {
        return;
      }

      showToast(toast.level, toast.heading, toast.message, toast.duration);
    } catch {
      window.sessionStorage.removeItem(pendingToastStorageKey);
    }
  }

  function setFeedback(rootElement, submitter, message, isError) {
    const selector = submitter?.dataset.feedbackTarget || rootElement?.dataset.feedbackTarget;
    const feedbackElement = selector
      ? document.querySelector(selector)
      : rootElement?.querySelector("[data-storefront-purchase-feedback], [data-storefront-selection-message]");
    if (!(feedbackElement instanceof HTMLElement)) {
      return;
    }

    feedbackElement.textContent = message || "";
    feedbackElement.classList.remove("text-emerald-700", "text-red-700");
    feedbackElement.classList.add(isError ? "text-red-700" : "text-emerald-700");
  }

  function flashButton(button) {
    if (!(button instanceof HTMLButtonElement)) {
      return;
    }

    const defaultLabel = button.dataset.defaultLabel || button.textContent.trim();
    const successLabel = button.dataset.successLabel || "Added";
    button.dataset.defaultLabel = defaultLabel;
    button.textContent = successLabel;

    const existingTimer = buttonResetTimers.get(button);
    if (existingTimer) {
      window.clearTimeout(existingTimer);
    }

    const timer = window.setTimeout(() => {
      button.textContent = button.dataset.defaultLabel || defaultLabel;
      buttonResetTimers.delete(button);
    }, buttonResetDelayMs);

    buttonResetTimers.set(button, timer);
  }

  function findPurchaseRoot(source) {
    if (!(source instanceof Element)) {
      return null;
    }

    const root = source.matches(productPurchaseRootSelector)
      ? source
      : source.closest(productPurchaseRootSelector);
    return root instanceof HTMLElement ? root : null;
  }

  function syncGalleryMainImage(container, imageUrl) {
    const resolvedImageUrl = (imageUrl || "").trim();
    if (!resolvedImageUrl || !(container instanceof HTMLElement)) {
      return;
    }

    const scope = container.closest("main") || document;
    const gallery = scope.querySelector(productGallerySelector);
    if (!(gallery instanceof HTMLElement)) {
      return;
    }

    const thumbnails = resolveGalleryThumbnails(gallery);
    const thumbnailIndex = thumbnails.findIndex((thumbnail) => thumbnail.dataset.imageUrl === resolvedImageUrl);
    if (thumbnailIndex >= 0) {
      selectGalleryIndex(gallery, thumbnailIndex);
      return;
    }

    const mainImage = gallery.querySelector(galleryMainImageSelector);
    if (!(mainImage instanceof HTMLImageElement)) {
      return;
    }

    const placeholder = gallery.querySelector(galleryPlaceholderSelector);
    mainImage.hidden = false;
    if (placeholder instanceof HTMLElement) {
      placeholder.hidden = true;
    }

    mainImage.src = resolvedImageUrl;
    thumbnails.forEach((thumbnail) => {
      thumbnail.dataset.selected = "false";
      thumbnail.setAttribute("aria-current", "false");
      thumbnail.setAttribute("aria-selected", "false");
    });
  }

  function applySelectionVisual(rootElement, detail) {
    const selection = detail?.selection || {};
    const scope = rootElement?.closest("main") || document;
    const price = scope.querySelector("[data-storefront-selection-price]");
    const compare = scope.querySelector("[data-storefront-selection-compare]");
    const stock = scope.querySelector("[data-storefront-selection-stock]");
    const sku = scope.querySelector("[data-storefront-selection-sku]");
    const submitter = detail?.submitter instanceof HTMLButtonElement
      ? detail.submitter
      : rootElement?.querySelector(productPurchaseSubmitSelector);

    if (selection.valid) {
      setText(price, selection.priceText || "");
      setText(compare, selection.comparePriceText || "");
      toggleHidden(compare, !selection.comparePriceText);
      setText(stock, selection.stockText || "");
      setText(sku, selection.skuText || "");
      toggleHidden(sku, !selection.skuText);
      syncGalleryMainImage(rootElement, selection.mainImageUrl);
      if (rootElement instanceof HTMLElement) {
        rootElement.dataset.mainImageUrl = selection.mainImageUrl || rootElement.dataset.mainImageUrl || "";
      }
    }

    if (rootElement instanceof HTMLElement) {
      const suppressUntil = parseInteger(rootElement.dataset.cartFeedbackSuppressUntil, 0);
      if (Date.now() >= suppressUntil) {
        setFeedback(rootElement, submitter, selection.message || "", !selection.ready);
      }
    }

    if (submitter instanceof HTMLButtonElement) {
      submitter.disabled = !selection.ready;
    }
  }

  function syncManualAddressFields(select) {
    if (!(select instanceof HTMLSelectElement)) {
      return;
    }

    const manualAddress = document.querySelector(manualAddressSelector);
    const useSavedAddress = Boolean((select.value || "").trim());
    toggleHidden(manualAddress, useSavedAddress);
    document.querySelectorAll(manualAddressFieldSelector).forEach((field) => {
      if (field instanceof HTMLInputElement || field instanceof HTMLSelectElement || field instanceof HTMLTextAreaElement) {
        field.disabled = useSavedAddress;
      }
    });
  }

  function initCheckoutAddressSelection() {
    document.querySelectorAll(addressSelectSelector).forEach((select) => {
      syncManualAddressFields(select);
    });
  }

  function resolveGalleryThumbnails(gallery) {
    return [...gallery.querySelectorAll(galleryThumbnailSelector)]
      .filter((thumbnail) => thumbnail instanceof HTMLButtonElement)
      .sort((left, right) => parseInteger(left.dataset.galleryIndex, 0) - parseInteger(right.dataset.galleryIndex, 0));
  }

  function resolveSelectedGalleryIndex(gallery, thumbnails = resolveGalleryThumbnails(gallery)) {
    const selected = thumbnails.find((thumbnail) => thumbnail.dataset.selected === "true");
    return selected ? Math.max(0, thumbnails.indexOf(selected)) : 0;
  }

  function setGalleryButtonState(button, disabled) {
    if (!(button instanceof HTMLButtonElement)) {
      return;
    }

    button.disabled = disabled;
    button.setAttribute("aria-disabled", disabled ? "true" : "false");
  }

  function updateGalleryControls(gallery, selectedIndex, itemCount) {
    setGalleryButtonState(gallery.querySelector(galleryPreviousSelector), selectedIndex <= 0);
    setGalleryButtonState(gallery.querySelector(galleryNextSelector), selectedIndex >= itemCount - 1);
  }

  function selectGalleryIndex(gallery, index) {
    if (!(gallery instanceof HTMLElement)) {
      return;
    }

    const mainImage = gallery.querySelector(galleryMainImageSelector);
    if (!(mainImage instanceof HTMLImageElement)) {
      return;
    }

    const thumbnails = resolveGalleryThumbnails(gallery);
    if (thumbnails.length === 0) {
      return;
    }

    const selectedIndex = Math.min(Math.max(parseInteger(index, 0), 0), thumbnails.length - 1);
    const selectedThumbnail = thumbnails[selectedIndex];
    const imageUrl = selectedThumbnail.dataset.imageUrl;
    if (!imageUrl) {
      return;
    }

    const placeholder = gallery.querySelector(galleryPlaceholderSelector);
    mainImage.hidden = false;
    if (placeholder instanceof HTMLElement) {
      placeholder.hidden = true;
    }

    mainImage.src = imageUrl;
    mainImage.alt = selectedThumbnail.dataset.alt || mainImage.alt || "Product image";
    thumbnails.forEach((thumbnail, thumbnailIndex) => {
      const selected = thumbnailIndex === selectedIndex;
      thumbnail.dataset.selected = selected ? "true" : "false";
      thumbnail.setAttribute("aria-current", selected ? "true" : "false");
      thumbnail.setAttribute("aria-selected", selected ? "true" : "false");
    });

    updateGalleryControls(gallery, selectedIndex, thumbnails.length);
    selectedThumbnail.scrollIntoView({ block: "nearest", inline: "nearest" });
  }

  function showGalleryImageFallback(image) {
    if (!(image instanceof HTMLImageElement)) {
      return;
    }

    const gallery = image.closest(productGallerySelector);
    if (!(gallery instanceof HTMLElement)) {
      return;
    }

    image.hidden = true;
    image.src = "data:image/svg+xml,%3Csvg xmlns=%22http://www.w3.org/2000/svg%22 width=%221%22 height=%221%22/%3E";

    const fallback = image.nextElementSibling;
    if (fallback instanceof HTMLElement) {
      fallback.hidden = false;
    }
  }

  function selectGalleryThumbnail(button) {
    const gallery = button.closest(productGallerySelector);
    if (!(gallery instanceof HTMLElement)) {
      return;
    }

    selectGalleryIndex(gallery, parseInteger(button.dataset.galleryIndex, resolveSelectedGalleryIndex(gallery)));
  }

  function moveGallery(gallery, step) {
    if (!(gallery instanceof HTMLElement)) {
      return;
    }

    const thumbnails = resolveGalleryThumbnails(gallery);
    if (thumbnails.length === 0) {
      return;
    }

    selectGalleryIndex(gallery, resolveSelectedGalleryIndex(gallery, thumbnails) + step);
  }

  function handleClick(event) {
    const target = event.target;
    if (!(target instanceof Element)) {
      return;
    }

    const previous = target.closest(galleryPreviousSelector);
    if (previous instanceof HTMLButtonElement) {
      event.preventDefault();
      moveGallery(previous.closest(productGallerySelector), -1);
      return;
    }

    const next = target.closest(galleryNextSelector);
    if (next instanceof HTMLButtonElement) {
      event.preventDefault();
      moveGallery(next.closest(productGallerySelector), 1);
      return;
    }

    const thumbnail = target.closest(galleryThumbnailSelector);
    if (thumbnail instanceof HTMLButtonElement) {
      event.preventDefault();
      selectGalleryThumbnail(thumbnail);
    }
  }

  function handleKeyDown(event) {
    const target = event.target;
    if (!(target instanceof Element)) {
      return;
    }

    const thumbnail = target.closest(galleryThumbnailSelector);
    if (!(thumbnail instanceof HTMLButtonElement)) {
      return;
    }

    const gallery = thumbnail.closest(productGallerySelector);
    if (!(gallery instanceof HTMLElement)) {
      return;
    }

    if (event.key === "ArrowLeft") {
      event.preventDefault();
      moveGallery(gallery, -1);
    } else if (event.key === "ArrowRight") {
      event.preventDefault();
      moveGallery(gallery, 1);
    }
  }

  function handleGalleryImageError(event) {
    showGalleryImageFallback(event.target);
  }

  function handleChange(event) {
    const target = event.target;
    if (target instanceof HTMLSelectElement && target.matches(addressSelectSelector)) {
      syncManualAddressFields(target);
    }
  }

  function handleSelectionChanged(event) {
    const rootElement = event.detail?.root instanceof HTMLElement
      ? event.detail.root
      : findPurchaseRoot(event.target);
    if (rootElement) {
      applySelectionVisual(rootElement, event.detail);
    }
  }

  function handleSelectionError(event) {
    const rootElement = event.detail?.root instanceof HTMLElement ? event.detail.root : null;
    const submitter = event.detail?.submitter instanceof HTMLButtonElement ? event.detail.submitter : null;
    if (rootElement) {
      setFeedback(rootElement, submitter, event.detail?.message || "This selection could not be previewed.", true);
    }

    if (submitter) {
      submitter.disabled = true;
    }
  }

  function handleAddLineSucceeded(event) {
    const rootElement = event.detail?.root instanceof HTMLElement ? event.detail.root : null;
    const submitter = event.detail?.submitter instanceof HTMLButtonElement ? event.detail.submitter : null;
    const message = event.detail?.message || "Product added to cart";

    if (rootElement) {
      rootElement.dataset.cartFeedbackSuppressUntil = String(Date.now() + buttonResetDelayMs);
      setFeedback(rootElement, submitter, message, false);
    }

    showToast("success", "Cart", message);
    flashButton(submitter);
  }

  function handleAddLineFailed(event) {
    const rootElement = event.detail?.root instanceof HTMLElement ? event.detail.root : null;
    const submitter = event.detail?.submitter instanceof HTMLButtonElement ? event.detail.submitter : null;
    const message = event.detail?.message || "Cart could not be updated.";

    if (rootElement) {
      setFeedback(rootElement, submitter, message, true);
    }

    showToast("error", "Cart", message);
  }

  function initialize() {
    flushQueuedToast();
    initCheckoutAddressSelection();
    document.addEventListener("click", handleClick);
    document.addEventListener("keydown", handleKeyDown);
    document.addEventListener("error", handleGalleryImageError, true);
    document.addEventListener("change", handleChange);
    document.addEventListener("storefront:product-purchase:selection-changed", handleSelectionChanged);
    document.addEventListener("storefront:product-purchase:selection-error", handleSelectionError);
    document.addEventListener("storefront:product-purchase:add-line-succeeded", handleAddLineSucceeded);
    document.addEventListener("storefront:product-purchase:add-line-failed", handleAddLineFailed);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", initialize, { once: true });
  } else {
    initialize();
  }
})();
