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
