function readFileContent() {
    return new Promise((resolve) => {
        const input = document.createElement('input');
        input.type = 'file';
        input.onchange = (e) => {
            const file = e.target.files[0];
            const reader = new FileReader();
            reader.onload = (event) => {
                resolve(event.target.result);
            };
            reader.readAsText(file);
        };
        input.click();
    });
}

window.readFileContent = readFileContent;

window.hideDropdownOnClickOutside = (elementId, dotnetHelper) => {
    document.addEventListener('click', function (event) {
        const inputElement = document.getElementById(elementId);
        if (inputElement && !inputElement.contains(event.target)) {
            dotnetHelper.invokeMethodAsync('HideGtinDropdown');
        }
    });
};

window.downloadFileFromStream = async (fileName, contentStreamReference) => {
    const arrayBuffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer]);
    const url = URL.createObjectURL(blob);
    const anchorElement = document.createElement('a');
    anchorElement.href = url;
    anchorElement.download = fileName ?? '';
    anchorElement.click();
    anchorElement.remove();
    URL.revokeObjectURL(url);
};

window.initSplitPane = (containerId, dividerId, dotnetRef) => {
    const container = document.getElementById(containerId);
    const divider = document.getElementById(dividerId);
    if (!container || !divider) return;

    let isDragging = false;

    divider.addEventListener('mousedown', e => {
        isDragging = true;
        e.preventDefault();
        document.body.style.userSelect = 'none';
        document.body.style.cursor = 'ns-resize';
    });

    document.addEventListener('mousemove', e => {
        if (!isDragging) return;
        const rect = container.getBoundingClientRect();
        const pct = Math.max(15, Math.min(85, (e.clientY - rect.top) / rect.height * 100));
        dotnetRef.invokeMethodAsync('SetTopPaneHeight', pct);
    });

    document.addEventListener('mouseup', () => {
        if (isDragging) {
            isDragging = false;
            document.body.style.userSelect = '';
            document.body.style.cursor = '';
        }
    });
};