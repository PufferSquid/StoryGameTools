window.imageDrop = {
    register: function (element, dotNetRef) {
        if (element.dataset.bound) return;
        element.dataset.bound = '1';

        element.addEventListener('dragenter', e => {
            e.preventDefault();
            element.classList.add('drag-over');
        });

        element.addEventListener('dragleave', e => {
            // dragleave also fires when crossing into a child element; ignore those
            if (!element.contains(e.relatedTarget))
                element.classList.remove('drag-over');
        });

        element.addEventListener('dragover', e => e.preventDefault());

        element.addEventListener('drop', e => {
            e.preventDefault();
            element.classList.remove('drag-over');
            const file = e.dataTransfer?.files?.[0];
            if (!file || !file.name.toLowerCase().endsWith('.png')) return;

            const reader = new FileReader();
            reader.onload = () => dotNetRef.invokeMethodAsync(
                'OnImageDropped', reader.result.split(',')[1]);
            reader.readAsDataURL(file);
        });
    }
};