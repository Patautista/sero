window.chatScroll = {
    _rememberedHeight: 0,
    _rememberedTop: 0,

    scrollToBottom: function (container) {
        if (!container) return;
        container.scrollTop = container.scrollHeight;
    },

    scrollToMessage: function (container, messageId) {
        if (!container) return;
        const element = document.getElementById('chat-message-' + messageId);
        if (element) {
            element.scrollIntoView({ block: 'center' });
        }
    },

    rememberPosition: function (container) {
        if (!container) return;
        this._rememberedHeight = container.scrollHeight;
        this._rememberedTop = container.scrollTop;
    },

    restorePosition: function (container) {
        if (!container) return;
        const newHeight = container.scrollHeight;
        container.scrollTop = this._rememberedTop + (newHeight - this._rememberedHeight);
    }
};
