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