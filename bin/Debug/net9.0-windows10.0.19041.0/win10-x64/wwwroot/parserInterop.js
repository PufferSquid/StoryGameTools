window.parserInterop = {
    _pendingFiles: [],

    registerDropZone: function (element) {
        element.addEventListener('drop', async function (e) {
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
    },

    getDroppedFiles: async function () {
        // Small delay to ensure the JS drop handler has finished
        await new Promise(r => setTimeout(r, 50));
        const files = window.parserInterop._pendingFiles;
        window.parserInterop._pendingFiles = [];
        return files;
    }
};