window.controlPlaneDownloads = {
    downloadBytes: function (fileName, contentType, bytes) {
        const blob = new Blob([new Uint8Array(bytes)], { type: contentType || "application/octet-stream" });
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement("a");
        anchor.href = url;
        anchor.download = fileName || "download";
        anchor.click();
        anchor.remove();
        URL.revokeObjectURL(url);
    }
};
