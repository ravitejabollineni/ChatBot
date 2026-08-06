window.chatBot = window.chatBot || {};

window.chatBot.scrollToBottom = (element) => {
    if (!element) {
        return;
    }

    const targetTop = element.scrollHeight ?? 0;

    if (typeof element.scrollTo === 'function') {
        element.scrollTo({
            top: targetTop,
            behavior: 'smooth'
        });
        return;
    }

    element.scrollTop = targetTop;
};

window.chatBot.initializeComposer = (textarea, sendButton) => {
    if (!textarea || textarea.dataset.chatbotInitialized === 'true') {
        return;
    }

    textarea.dataset.chatbotInitialized = 'true';

    textarea.addEventListener('keydown', (event) => {
        if (event.key !== 'Enter' || event.shiftKey) {
            return;
        }

        event.preventDefault();

        if (!sendButton || sendButton.disabled) {
            return;
        }

        sendButton.click();
    });

    // field-sizing: content is Chromium-only; everywhere else, grow the textarea manually.
    if (!window.CSS?.supports?.('field-sizing', 'content')) {
        window.chatBot.autoGrow(textarea);
        textarea.addEventListener('input', () => window.chatBot.autoGrow(textarea));
    }
};

window.chatBot.autoGrow = (textarea) => {
    if (!textarea) {
        return;
    }

    const maxHeight = 180;
    textarea.style.height = 'auto';
    textarea.style.height = `${Math.min(textarea.scrollHeight, maxHeight)}px`;
};

// #components-reconnect-modal's class names are Blazor-internal, not a public contract.
// If a future Blazor release renames them, this silently stops firing (stale view after
// a long blip, not a crash).
window.chatBot.observeReconnect = (dotNetRef) => {
    const modal = document.getElementById('components-reconnect-modal');

    if (!modal || !dotNetRef) {
        return null;
    }

    let wasShowing = modal.classList.contains('components-reconnect-show');

    const observer = new MutationObserver(() => {
        const isShowing = modal.classList.contains('components-reconnect-show');

        if (wasShowing && !isShowing) {
            dotNetRef.invokeMethodAsync('OnCircuitReconnected');
        }

        wasShowing = isShowing;
    });

    observer.observe(modal, { attributes: true, attributeFilter: ['class'] });

    return observer;
};

window.chatBot.disconnectObserver = (observer) => {
    if (observer) {
        observer.disconnect();
    }
};

window.chatBot.highlightCodeBlocks = (rootElement) => {
    if (!rootElement || !window.hljs) {
        return;
    }

    rootElement.querySelectorAll('code[class*="language-"]').forEach((codeElement) => {
        window.hljs.highlightElement(codeElement);
    });
};

window.chatBot.initializeCodeCopy = (containerElement) => {
    if (!containerElement || containerElement.dataset.chatbotCodeCopyInitialized === 'true') {
        return;
    }

    containerElement.dataset.chatbotCodeCopyInitialized = 'true';

    containerElement.addEventListener('click', (event) => {
        const button = event.target.closest('.code-block__copy');

        if (!button) {
            return;
        }

        const codeElement = button.closest('.code-block')?.querySelector('pre > code');

        if (!codeElement || !navigator.clipboard?.writeText) {
            return;
        }

        navigator.clipboard.writeText(codeElement.innerText)
            .then(() => showCopyFeedback(button))
            .catch(() => {
                // Clipboard access denied (permissions/insecure context) — no fallback,
                // the button simply won't show "Copied".
            });
    });
};

window.chatBot.copyText = async (button, text) => {
    if (!navigator.clipboard || typeof navigator.clipboard.writeText !== 'function') {
        return;
    }

    try {
        await navigator.clipboard.writeText(text ?? '');
    } catch (err) {
        console.error('Unable to copy to clipboard:', err);
        return;
    }

    if (!button) {
        return;
    }

    const icon = button.querySelector('i');

    window.clearTimeout(Number(button.dataset.chatbotCopyTimeout) || undefined);
    button.classList.add('message-actions__btn--copied');
    icon?.classList.replace('bi-clipboard', 'bi-clipboard-check');

    const timeoutId = window.setTimeout(() => {
        button.classList.remove('message-actions__btn--copied');
        icon?.classList.replace('bi-clipboard-check', 'bi-clipboard');
    }, 1500);

    button.dataset.chatbotCopyTimeout = String(timeoutId);
};

function showCopyFeedback(button) {
    if (button.dataset.chatbotCopyPending === 'true') {
        return;
    }

    button.dataset.chatbotCopyPending = 'true';

    const label = button.querySelector('span');
    const icon = button.querySelector('i');
    const originalLabel = label?.textContent ?? null;
    const originalIconClass = icon?.className ?? null;

    if (label) {
        label.textContent = 'Copied';
    }
    if (icon) {
        icon.className = 'bi bi-clipboard-check';
    }
    button.classList.add('code-block__copy--copied');

    setTimeout(() => {
        if (label && originalLabel !== null) {
            label.textContent = originalLabel;
        }
        if (icon && originalIconClass !== null) {
            icon.className = originalIconClass;
        }
        button.classList.remove('code-block__copy--copied');
        button.dataset.chatbotCopyPending = 'false';
    }, 1500);
}
