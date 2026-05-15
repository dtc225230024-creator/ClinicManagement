(() => {
    const cleanupStaleModalState = () => {
        if (document.querySelector(".modal.show")) {
            return;
        }

        document.querySelectorAll(".modal-backdrop").forEach(backdrop => backdrop.remove());
        document.body.classList.remove("modal-open");
        document.body.style.removeProperty("overflow");
        document.body.style.removeProperty("padding-right");
    };

    cleanupStaleModalState();
    window.addEventListener("pageshow", cleanupStaleModalState);
    document.addEventListener("hidden.bs.modal", cleanupStaleModalState);
})();

(() => {
    const modalElement = document.getElementById("confirmationModal");
    const titleElement = document.getElementById("confirmationModalTitle");
    const messageElement = document.getElementById("confirmationModalMessage");
    const confirmButton = document.getElementById("confirmationModalSubmit");

    if (!modalElement || !titleElement || !messageElement || !confirmButton || typeof bootstrap === "undefined") {
        return;
    }

    const modal = new bootstrap.Modal(modalElement);
    const styleClasses = {
        danger: ["btn-danger"],
        warning: ["btn-warning", "text-dark"],
        primary: ["btn-primary"],
        success: ["btn-success"]
    };

    let pendingSubmitter = null;

    function applyConfirmButtonStyle(style) {
        confirmButton.className = "btn";
        (styleClasses[style] || styleClasses.danger).forEach(cssClass => confirmButton.classList.add(cssClass));
    }

    document.addEventListener("click", event => {
        const submitter = event.target.closest("[data-confirm-modal]");
        if (!(submitter instanceof HTMLElement) || !(submitter instanceof HTMLButtonElement || submitter instanceof HTMLInputElement)) {
            return;
        }

        const form = submitter.form;
        if (!form) {
            return;
        }

        event.preventDefault();
        pendingSubmitter = submitter;

        titleElement.textContent = submitter.dataset.confirmTitle || "Xác nhận thao tác";
        messageElement.textContent = submitter.dataset.confirmMessage || "Bạn có chắc muốn tiếp tục?";
        confirmButton.textContent = submitter.dataset.confirmText || "Xác nhận";
        applyConfirmButtonStyle(submitter.dataset.confirmStyle || "danger");

        modal.show();
    });

    confirmButton.addEventListener("click", () => {
        if (!pendingSubmitter?.form) {
            modal.hide();
            return;
        }

        const submitter = pendingSubmitter;
        pendingSubmitter = null;
        modal.hide();

        if (typeof submitter.form.requestSubmit === "function") {
            submitter.form.requestSubmit(submitter);
        } else {
            submitter.form.submit();
        }
    });

    modalElement.addEventListener("hidden.bs.modal", () => {
        pendingSubmitter = null;
    });
})();

(() => {
    if (typeof bootstrap === "undefined") {
        return;
    }

    document.querySelectorAll(".toast[data-auto-show='true']").forEach(element => {
        bootstrap.Toast.getOrCreateInstance(element).show();
    });
})();

(() => {
    document.querySelectorAll("[data-password-toggle='true']").forEach(button => {
        if (!(button instanceof HTMLButtonElement)) {
            return;
        }

        const field = button.closest(".password-field");
        const input = field?.querySelector("[data-password-toggle-target='true']");
        if (!(input instanceof HTMLInputElement)) {
            return;
        }

        const updateState = () => {
            const isVisible = input.type === "text";
            button.classList.toggle("is-visible", isVisible);
            button.setAttribute("aria-pressed", String(isVisible));
            button.setAttribute("aria-label", isVisible ? "Ẩn mật khẩu" : "Hiện mật khẩu");
        };

        button.addEventListener("click", () => {
            const selectionStart = input.selectionStart;
            const selectionEnd = input.selectionEnd;
            input.type = input.type === "password" ? "text" : "password";
            updateState();
            input.focus();

            try {
                if (selectionStart !== null && selectionEnd !== null) {
                    input.setSelectionRange(selectionStart, selectionEnd);
                }
            } catch {
                // Some password managers can block restoring the caret position.
            }
        });

        updateState();
    });
})();

(() => {
    document.querySelectorAll("form[data-quick-range-form='true']").forEach(form => {
        const presetInput = form.querySelector("[data-range-preset-input='true']");
        const dateInputs = Array.from(form.querySelectorAll("input[type='date']"));

        if (!(presetInput instanceof HTMLInputElement) || dateInputs.length === 0) {
            return;
        }

        const syncPresetState = () => {
            const hasDateValue = dateInputs.some(input => input.value);
            presetInput.value = hasDateValue ? "custom" : "all";
        };

        dateInputs.forEach(input => input.addEventListener("change", syncPresetState));
    });
})();

(() => {
    const reasonInput = document.querySelector("[data-ai-department-source='true']");
    const departmentList = document.querySelector("[data-ai-department-list='true']");

    if (!(reasonInput instanceof HTMLTextAreaElement) || !(departmentList instanceof HTMLElement)) {
        return;
    }

    const suggestionUrl = reasonInput.dataset.aiDepartmentUrl;
    const choices = Array.from(departmentList.querySelectorAll("[data-department-choice='true']"))
        .filter(choice => choice instanceof HTMLElement);

    if (!suggestionUrl || choices.length === 0) {
        return;
    }

    choices.forEach((choice, index) => {
        choice.dataset.originalOrder = String(index + 1);
        choice.style.order = String(index + 1);
    });

    let debounceTimer = null;
    let requestVersion = 0;

    const clearSuggestions = () => {
        choices.forEach(choice => {
            choice.classList.remove("ai-suggested");
            choice.style.order = choice.dataset.originalOrder || "1";
            const marker = choice.querySelector(".ai-choice-marker");
            if (marker) {
                marker.textContent = "Đề xuất";
            }
        });
    };

    const applySuggestions = data => {
        if (!data?.hasReason || !Array.isArray(data.departments)) {
            clearSuggestions();
            return;
        }

        const ranked = new Map(data.departments.map(item => [String(item.id), item]));

        choices.forEach(choice => {
            const departmentId = choice.dataset.departmentId || "";
            const suggestion = ranked.get(departmentId);
            const isSuggested = Boolean(suggestion?.isSuggested);
            const marker = choice.querySelector(".ai-choice-marker");
            choice.style.order = suggestion?.rank ? String(suggestion.rank) : choice.dataset.originalOrder || "99";
            choice.classList.toggle("ai-suggested", isSuggested);

            if (marker && isSuggested) {
            marker.textContent = suggestion.rank === 1 ? "Đề xuất ưu tiên" : `Đề xuất #${suggestion.rank}`;
            }
        });
    };

    const refreshSuggestions = async () => {
        const reason = reasonInput.value.trim();
        if (!reason) {
            clearSuggestions();
            return;
        }

        const currentVersion = ++requestVersion;
        const url = `${suggestionUrl}?reason=${encodeURIComponent(reason)}`;

        try {
            const response = await fetch(url, {
                headers: { "Accept": "application/json" }
            });

            if (!response.ok || currentVersion !== requestVersion) {
                return;
            }

            applySuggestions(await response.json());
        } catch {
            clearSuggestions();
        }
    };

    reasonInput.addEventListener("input", () => {
        window.clearTimeout(debounceTimer);
        debounceTimer = window.setTimeout(refreshSuggestions, 350);
    });

    if (reasonInput.value.trim()) {
        refreshSuggestions();
    }
})();

(() => {
    const form = document.querySelector("form.booking-wizard");
    if (!(form instanceof HTMLFormElement)) {
        return;
    }

    const hasValue = name => {
        const field = form.elements.namedItem(name);
        if (field instanceof HTMLInputElement || field instanceof HTMLTextAreaElement || field instanceof HTMLSelectElement) {
            return field.value.trim().length > 0;
        }

        return false;
    };

    const hasChecked = name => Boolean(form.querySelector(`input[name='${name}']:checked`));

    const isStepReady = step => {
        if (step === "1") {
            return hasValue("Reason") && hasChecked("DepartmentId");
        }

        if (step === "2") {
            return hasChecked("PatientId");
        }

        if (step === "3") {
            return hasChecked("SelectedSuggestionKey");
        }

        return true;
    };

    const updateNextButtons = () => {
        form.querySelectorAll("[data-step-next]").forEach(button => {
            if (!(button instanceof HTMLButtonElement)) {
                return;
            }

            const isReady = isStepReady(button.dataset.stepNext || "");
            button.disabled = !isReady;
            button.classList.toggle("is-disabled", !isReady);
            button.setAttribute("aria-disabled", String(!isReady));
        });
    };

    form.addEventListener("input", updateNextButtons);
    form.addEventListener("change", updateNextButtons);
    updateNextButtons();
})();

(() => {
    document.querySelectorAll("[data-auto-submit-command]").forEach(element => {
        if (!(element instanceof HTMLInputElement)) {
            return;
        }

        element.addEventListener("change", () => {
            if (!element.checked || !element.form) {
                return;
            }

            const command = element.dataset.autoSubmitCommand;
            if (!command) {
                return;
            }

            const targetFieldName = element.dataset.autoSetField;
            const targetFieldValue = element.dataset.autoSetValue;
            if (targetFieldName && targetFieldValue !== undefined) {
                const targetInput = element.form.elements.namedItem(targetFieldName);
                if (targetInput instanceof HTMLInputElement) {
                    targetInput.value = targetFieldValue;
                }
            }

            let commandInput = element.form.querySelector("[data-auto-command-input='true']");
            if (!(commandInput instanceof HTMLInputElement)) {
                commandInput = document.createElement("input");
                commandInput.type = "hidden";
                commandInput.name = "command";
                commandInput.dataset.autoCommandInput = "true";
                element.form.appendChild(commandInput);
            }

            commandInput.value = command;

            if (typeof element.form.requestSubmit === "function") {
                element.form.requestSubmit();
            } else {
                element.form.submit();
            }
        });
    });
})();

(() => {
    if (typeof bootstrap === "undefined") {
        return;
    }

    const modalElement = document.getElementById("userManualModal");
    if (!(modalElement instanceof HTMLElement)) {
        return;
    }

    if (modalElement.parentElement !== document.body) {
        document.body.appendChild(modalElement);
    }

    const modal = bootstrap.Modal.getOrCreateInstance(modalElement);
    const showManualModal = (attempt = 0) => {
        modal.show();

        window.setTimeout(() => {
            const dialog = modalElement.querySelector(".modal-dialog");
            const rect = dialog?.getBoundingClientRect();
            const isVisible = modalElement.classList.contains("show") &&
                rect &&
                rect.width > 0 &&
                rect.height > 0;

            if (!isVisible && attempt < 2) {
                modal.hide();
                window.setTimeout(() => showManualModal(attempt + 1), 180);
            }
        }, 300);
    };

    document.querySelectorAll("[data-user-manual-open='true']").forEach(button => {
        button.addEventListener("click", () => showManualModal());
    });

    if (modalElement.dataset.showOnLoad === "true") {
        window.setTimeout(() => showManualModal(), 160);
    }

    document.querySelectorAll("[data-user-manual-seen-form='true']").forEach(form => {
        if (!(form instanceof HTMLFormElement)) {
            return;
        }

        form.addEventListener("submit", async event => {
            event.preventDefault();

            try {
                const response = await fetch(form.action, {
                    method: form.method || "POST",
                    body: new FormData(form),
                    headers: { "Accept": "application/json" }
                });

                if (response.ok) {
                    modalElement.dataset.showOnLoad = "false";
                    modal.hide();
                } else {
                    form.submit();
                }
            } catch {
                form.submit();
            }
        });
    });
})();
