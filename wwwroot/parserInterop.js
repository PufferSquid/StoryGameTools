window.parserInterop = {
    // Store dropped files from the ondrop event
    _pendingFiles: [],

    // Called by the ondragover/ondrop handlers via a JS event listener
    // We register this on the drop zone element
    getDroppedFiles: async function () {
        return new Promise(resolve => {
            resolve(window.parserInterop._pendingFiles);
            window.parserInterop._pendingFiles = [];
        });
    }
};

// Listen for drop events at the document level and capture file data
document.addEventListener('drop', async function (e) {
    const files = Array.from(e.dataTransfer?.files ?? []);
    const results = [];
    for (const file of files) {
        if (!file.name.endsWith('.txt')) continue;
        const base64 = await new Promise(resolve => {
            const reader = new FileReader();
            reader.onload = () => resolve(reader.result.split(',')[1]);
            reader.readAsDataURL(file);
        });
        results.push({ name: file.name, base64 });
    }
    window.parserInterop._pendingFiles = results;
});