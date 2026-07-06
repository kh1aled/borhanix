(function () {
    const payloadInput = document.getElementById("Payload");
    const form = document.getElementById("scanForm");
    const reader = document.getElementById("reader");

    if (!payloadInput || !form || !reader || !window.Html5QrcodeScanner) {
        return;
    }

    const scanner = new Html5QrcodeScanner(
        "reader",
        { fps: 10, qrbox: { width: 240, height: 240 }, rememberLastUsedCamera: true },
        false);

    scanner.render((decodedText) => {
        payloadInput.value = decodedText;
        form.submit();
    });
})();
