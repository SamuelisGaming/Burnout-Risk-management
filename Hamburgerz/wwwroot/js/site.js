document.addEventListener("DOMContentLoaded", function () {
    const avatarUrl = document.body?.dataset.avatarUrl || "";
    const avatarRoots = Array.from(document.querySelectorAll("[data-avatar-root]"));
    const uploadTrigger = document.querySelector("[data-avatar-upload-trigger]");
    const uploadInput = document.querySelector("[data-avatar-upload-input]");
    const antiForgeryInput = document.querySelector('input[name="__RequestVerificationToken"]');

    if (!avatarRoots.length) {
        return;
    }

    const showFallback = function (root) {
        const image = root.querySelector("[data-avatar-image]");
        const fallback = root.querySelector("[data-avatar-fallback]");

        if (!image || !fallback) {
            return;
        }

        image.removeAttribute("src");
        image.hidden = true;
        fallback.hidden = false;
        root.classList.remove("has-image");
    };

    const showImage = function (root, source) {
        const image = root.querySelector("[data-avatar-image]");
        const fallback = root.querySelector("[data-avatar-fallback]");

        if (!image || !fallback) {
            return;
        }

        image.onload = function () {
            image.hidden = false;
            fallback.hidden = true;
            root.classList.add("has-image");
        };

        image.onerror = function () {
            showFallback(root);
        };

        image.src = source;

        if (image.complete) {
            if (image.naturalWidth > 0) {
                image.hidden = false;
                fallback.hidden = true;
                root.classList.add("has-image");
            } else {
                showFallback(root);
            }
        }
    };

    const applyAvatarSource = function (source) {
        avatarRoots.forEach(function (root) {
            if (source) {
                showImage(root, source);
            } else {
                showFallback(root);
            }
        });
    };

    const readFileAsDataUrl = function (file) {
        return new Promise(function (resolve, reject) {
            const reader = new FileReader();

            reader.onload = function () {
                resolve(typeof reader.result === "string" ? reader.result : "");
            };

            reader.onerror = function () {
                reject(new Error("Nepavyko nuskaityti paveikslelio failo."));
            };

            reader.readAsDataURL(file);
        });
    };

    const loadImage = function (dataUrl) {
        return new Promise(function (resolve, reject) {
            const image = new Image();

            image.onload = function () {
                resolve(image);
            };

            image.onerror = function () {
                reject(new Error("Nepavyko apdoroti paveikslelio."));
            };

            image.src = dataUrl;
        });
    };

    const buildAvatarDataUrl = function (image) {
        const size = 320;
        const canvas = document.createElement("canvas");
        const context = canvas.getContext("2d");

        if (!context) {
            throw new Error("Nepavyko paruošti paveikslelio apdorojimo.");
        }

        canvas.width = size;
        canvas.height = size;

        const scale = Math.max(size / image.width, size / image.height);
        const scaledWidth = image.width * scale;
        const scaledHeight = image.height * scale;
        const offsetX = (size - scaledWidth) / 2;
        const offsetY = (size - scaledHeight) / 2;

        context.imageSmoothingEnabled = true;
        context.imageSmoothingQuality = "high";
        context.drawImage(image, offsetX, offsetY, scaledWidth, scaledHeight);

        return canvas.toDataURL("image/jpeg", 0.88);
    };

    const dataUrlToBlob = function (dataUrl) {
        const parts = dataUrl.split(",");

        if (parts.length !== 2) {
            throw new Error("Nepavyko paruošti paveikslelio įkėlimui.");
        }

        const mimeMatch = parts[0].match(/data:(.*?);base64/);
        const mimeType = mimeMatch && mimeMatch[1] ? mimeMatch[1] : "image/jpeg";
        const binary = window.atob(parts[1]);
        const length = binary.length;
        const bytes = new Uint8Array(length);

        for (let index = 0; index < length; index += 1) {
            bytes[index] = binary.charCodeAt(index);
        }

        return new Blob([bytes], { type: mimeType });
    };

    const handleAvatarFile = async function (file) {
        if (!file || !file.type.startsWith("image/")) {
            throw new Error("Pasirinktas failas nėra paveikslėlis.");
        }

        const rawDataUrl = await readFileAsDataUrl(file);
        const image = await loadImage(rawDataUrl);
        return buildAvatarDataUrl(image);
    };

    const uploadAvatar = async function (avatarDataUrl) {
        const uploadUrl = uploadTrigger instanceof HTMLElement
            ? uploadTrigger.dataset.avatarUploadUrl || ""
            : "";

        if (!uploadUrl) {
            throw new Error("Nerastas avataro įkėlimo adresas.");
        }

        const formData = new FormData();
        formData.append("avatar", dataUrlToBlob(avatarDataUrl), "avatar.jpg");

        if (antiForgeryInput instanceof HTMLInputElement && antiForgeryInput.value) {
            formData.append("__RequestVerificationToken", antiForgeryInput.value);
        }

        const response = await window.fetch(uploadUrl, {
            method: "POST",
            body: formData,
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            }
        });

        const payload = await response.json().catch(function () {
            return null;
        });

        if (!response.ok) {
            throw new Error(payload && payload.message ? payload.message : "Nepavyko įkelti paveikslėlio.");
        }

        return payload && payload.avatarUrl ? payload.avatarUrl : avatarUrl;
    };

    applyAvatarSource(avatarUrl);

    if (uploadTrigger instanceof HTMLElement && uploadInput instanceof HTMLInputElement) {
        uploadTrigger.addEventListener("click", function () {
            uploadInput.click();
        });

        uploadInput.addEventListener("change", async function () {
            const selectedFile = uploadInput.files && uploadInput.files[0];

            if (!selectedFile) {
                return;
            }

            uploadTrigger.classList.add("is-loading");

            try {
                const avatarDataUrl = await handleAvatarFile(selectedFile);
                const savedAvatarUrl = await uploadAvatar(avatarDataUrl);
                applyAvatarSource(savedAvatarUrl);
            } catch (error) {
                console.error(error);
                const message = error instanceof Error
                    ? error.message
                    : "Nepavyko įkelti paveikslėlio. Pabandykite kitą nuotrauką.";

                window.alert(message);
            } finally {
                uploadInput.value = "";
                uploadTrigger.classList.remove("is-loading");
            }
        });
    }
});
