document.addEventListener("DOMContentLoaded", function () {
    const storageKey = document.body?.dataset.avatarStorageKey || "";

    if (!storageKey) {
        return;
    }

    const avatarRoots = Array.from(document.querySelectorAll("[data-avatar-root]"));
    const uploadTrigger = document.querySelector("[data-avatar-upload-trigger]");
    const uploadInput = document.querySelector("[data-avatar-upload-input]");

    const applyAvatarSource = function (source) {
        avatarRoots.forEach(function (root) {
            const image = root.querySelector("[data-avatar-image]");
            const fallback = root.querySelector("[data-avatar-fallback]");

            if (!image || !fallback) {
                return;
            }

            if (source) {
                image.src = source;
                image.hidden = false;
                fallback.hidden = true;
                root.classList.add("has-image");
            } else {
                image.removeAttribute("src");
                image.hidden = true;
                fallback.hidden = false;
                root.classList.remove("has-image");
            }
        });
    };

    const readStoredAvatar = function () {
        try {
            return window.localStorage.getItem(storageKey) || "";
        } catch (error) {
            console.warn("Nepavyko perskaityti avataro is localStorage.", error);
            return "";
        }
    };

    const writeStoredAvatar = function (source) {
        try {
            window.localStorage.setItem(storageKey, source);
            return true;
        } catch (error) {
            console.warn("Nepavyko issaugoti avataro i localStorage.", error);
            return false;
        }
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
            throw new Error("Nepavyko paruosti paveikslelio apdorojimo.");
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

    const handleAvatarFile = async function (file) {
        if (!file || !file.type.startsWith("image/")) {
            throw new Error("Pasirinktas failas nera paveikslelis.");
        }

        const rawDataUrl = await readFileAsDataUrl(file);
        const image = await loadImage(rawDataUrl);
        return buildAvatarDataUrl(image);
    };

    const storedAvatar = readStoredAvatar();
    applyAvatarSource(storedAvatar);

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
                const avatarSource = await handleAvatarFile(selectedFile);

                applyAvatarSource(avatarSource);

                if (!writeStoredAvatar(avatarSource)) {
                    console.warn("Avataras parodytas tik sioje sesijoje, nes nepavyko jo issaugoti.");
                }
            } catch (error) {
                console.error(error);
                window.alert("Nepavyko ikelti paveikslelio. Pabandyk kita nuotrauka.");
            } finally {
                uploadInput.value = "";
                uploadTrigger.classList.remove("is-loading");
            }
        });
    }

    window.addEventListener("storage", function (event) {
        if (event.key !== storageKey) {
            return;
        }

        applyAvatarSource(event.newValue || "");
    });
});
