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
