window.chatBot = window.chatBot || {};

// scrollToBottom is called on every streaming flush (~75ms cadence) while a response is
// generating, plus once on load/new-message. Auto-scrolling unconditionally on every call
// fights the user: scrolling up mid-stream to reread something gets yanked back to the bottom
// on the very next flush.
//
// A naive fix — only scroll if already within N px of the bottom — doesn't work: the instant
// content grows past the viewport for the first time, scrollTop is still 0 (from before there
// was anything to scroll) while scrollHeight has already grown, so "distance from bottom" reads
// as large on that very render even though the user never touched anything. That would
// permanently kill auto-follow for every fresh conversation.
//
// So instead this tracks *actual user scroll input* via a 'scroll' listener, ignoring scroll
// events that fire as a side effect of our own programmatic scrollTo (flagged via
// `state.programmatic`) — those are content growth, not the user reading history.
const nearBottomThresholdPx = 80;
const autoScrollState = new WeakMap();

// Must run once per message-list element before scrollToBottom is called, so user scroll
// input is actually being observed.
window.chatBot.initializeAutoScroll = (element) => {
    if (!element || element.dataset.chatbotAutoScrollInitialized === 'true') {
        return;
    }

    element.dataset.chatbotAutoScrollInitialized = 'true';
    autoScrollState.set(element, { shouldFollow: true, programmatic: false });

    element.addEventListener('scroll', () => {
        const state = autoScrollState.get(element);

        if (!state || state.programmatic) {
            return;
        }

        const distanceFromBottom = element.scrollHeight - element.scrollTop - element.clientHeight;
        state.shouldFollow = distanceFromBottom <= nearBottomThresholdPx;
    });
};

// `force` bypasses the "user scrolled away" check and always jumps to the bottom — used for
// the one-time scroll when a conversation is first opened or switched to, where there's no
// "user's current reading position" to respect yet, unlike every subsequent flush during
// streaming. It also re-arms auto-follow, so switching conversations recovers from a previous
// conversation's "user scrolled away" state.
//
// Scrolling is instant, not smooth: this runs on every streaming flush (~75ms), so a smooth
// animation would just get interrupted and restarted by the next flush before it ever finishes
// — pure stutter, never an actual smooth motion.
window.chatBot.scrollToBottom = (element, force) => {
    if (!element) {
        return;
    }

    const state = autoScrollState.get(element) ?? { shouldFollow: true, programmatic: false };

    if (!force && !state.shouldFollow) {
        return;
    }

    // Setting scrollTop itself fires a 'scroll' event synchronously in most browsers; flag it
    // as programmatic first so the listener above doesn't mistake this for the user scrolling
    // away (scrollTop starts this call already at the old, now-"far" position for one instant).
    state.programmatic = true;
    autoScrollState.set(element, state);

    element.scrollTop = element.scrollHeight ?? 0;

    state.programmatic = false;
    state.shouldFollow = true;
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
