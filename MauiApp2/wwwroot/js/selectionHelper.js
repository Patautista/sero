window.selectionHelper = {
    getSelectedText: function () {
        if (window.getSelection) {
            return window.getSelection().toString().trim();
        }
        return '';
    },
    getSelectionPosition: function () {
        if (window.getSelection) {
            const sel = window.getSelection();
            if (sel && sel.rangeCount > 0) {
                const rect = sel.getRangeAt(0).getBoundingClientRect();
                return { x: rect.left + rect.width / 2, y: rect.top };
            }
        }
        return { x: 0, y: 0 };
    }
};
