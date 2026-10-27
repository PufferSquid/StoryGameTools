window.parserInterop = {
    registerDropZone: function (element, dotNetRef) {
        element.addEventListener('dragover', e => e.preventDefault());
        element.addEventListener('drop', e => {
            e.preventDefault();
            for (const file of Array.from(e.dataTransfer?.files ?? [])) {
                if (!file.name.endsWith('.txt')) continue;
                const reader = new FileReader();
                reader.onload = () => dotNetRef.invokeMethodAsync(
                    'OnFileDropped', file.name, reader.result);
                reader.readAsText(file);
            }
        });
    }
};