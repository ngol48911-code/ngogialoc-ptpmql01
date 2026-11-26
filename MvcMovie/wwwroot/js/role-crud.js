(() => {
    "use strict";

    const form = document.getElementById("roleForm");
    const permissionForm = document.getElementById("permissionForm");
    const tableBody = document.getElementById("roleTableBody");
    const modal = new bootstrap.Modal(document.getElementById("roleModal"));
    const permissionModal = new bootstrap.Modal(document.getElementById("permissionModal"));
    const alertElement = document.getElementById("roleAlert");
    const formError = document.getElementById("roleFormError");
    const permissionFormError = document.getElementById("permissionFormError");
    const saveButton = document.getElementById("saveRoleButton");
    const savePermissionsButton = document.getElementById("savePermissionsButton");
    const pageInfo = document.getElementById("rolePageInfo");
    const pagination = document.getElementById("rolePagination");
    const permissionGroups = document.getElementById("permissionGroups");
    const state = { pageIndex: 1, pageSize: 10, searchString: "" };
    let editingRoleId = null;
    let permissionRoleId = null;

    const getToken = (sourceForm = form) =>
        sourceForm.querySelector('input[name="__RequestVerificationToken"]').value;
    const escapeHtml = (value) => String(value ?? "")
        .replaceAll("&", "&amp;").replaceAll("<", "&lt;").replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;").replaceAll("'", "&#039;");
    const showAlert = (message, type = "success") => {
        alertElement.textContent = message;
        alertElement.className = `alert alert-${type}`;
    };
    const clearErrors = () => {
        formError.classList.add("d-none");
        formError.textContent = "";
        form.querySelectorAll(".is-invalid").forEach((element) => element.classList.remove("is-invalid"));
        form.querySelectorAll("[data-error-for]").forEach((element) => element.textContent = "");
    };
    const clearPermissionError = () => {
        permissionFormError.textContent = "";
        permissionFormError.classList.add("d-none");
    };
    const showErrors = (errors, message) => {
        formError.textContent = message || "Vui lòng kiểm tra lại dữ liệu.";
        formError.classList.remove("d-none");
        Object.entries(errors || {}).forEach(([field, messages]) => {
            form.querySelector(`[name="${field}"]`)?.classList.add("is-invalid");
            const errorElement = form.querySelector(`[data-error-for="${field}"]`);
            if (errorElement) errorElement.textContent = messages.join(" ");
        });
    };
    const requestJson = async (url, options = {}, tokenForm = form) => {
        const response = await fetch(url, {
            ...options,
            headers: {
                Accept: "application/json",
                ...(options.body ? { "Content-Type": "application/json" } : {}),
                ...(options.method && options.method !== "GET"
                    ? { RequestVerificationToken: getToken(tokenForm) } : {}),
                ...(options.headers || {})
            }
        });
        const payload = await response.json().catch(() => ({}));
        if (!response.ok) {
            if (response.status === 401) {
                window.location.assign(`/Identity/Account/Login?ReturnUrl=${encodeURIComponent(location.pathname + location.search)}`);
                throw new Error("Phiên đăng nhập đã hết hạn.");
            }
            if (response.status === 403) {
                throw new Error("Bạn không có quyền thực hiện thao tác này.");
            }
            const error = new Error(payload.message || "Yêu cầu không thành công.");
            error.payload = payload;
            throw error;
        }
        return payload;
    };
    const queryString = () => {
        const params = new URLSearchParams({
            pageIndex: state.pageIndex,
            pageSize: state.pageSize
        });
        if (state.searchString) params.set("searchString", state.searchString);
        return params;
    };
    const renderRoles = (roles) => {
        tableBody.replaceChildren();
        if (roles.length === 0) {
            tableBody.innerHTML = '<tr><td colspan="6" class="text-center">Chưa có nhóm quyền.</td></tr>';
            return;
        }

        roles.forEach((role) => {
            const row = document.createElement("tr");
            const systemBadge = role.isSystemRole
                ? '<span class="badge text-bg-secondary">Hệ thống</span>'
                : '<span class="badge text-bg-primary">Tùy chỉnh</span>';
            const disabled = role.isSystemRole ? "disabled" : "";
            const permissionAction = role.name?.toLowerCase() === "admin"
                ? '<span class="badge text-bg-success">Toàn quyền mặc định</span>'
                : `<button type="button" class="btn btn-sm btn-outline-primary permission-role" data-id="${escapeHtml(role.id)}">Phân quyền Action</button>`;
            row.innerHTML = `
                <td>${escapeHtml(role.name)}</td>
                <td>${escapeHtml(role.normalizedName)}</td>
                <td><span class="badge text-bg-info">${escapeHtml(role.userCount)}</span></td>
                <td>${systemBadge}</td>
                <td>${permissionAction}</td>
                <td>
                    <button type="button" class="btn btn-sm btn-warning edit-role" data-id="${escapeHtml(role.id)}" ${disabled}>Sửa</button>
                    <button type="button" class="btn btn-sm btn-danger delete-role" data-id="${escapeHtml(role.id)}" ${disabled}>Xóa</button>
                </td>`;
            tableBody.appendChild(row);
        });
    };
    const loadRoles = async () => {
        try {
            const result = await requestJson(`/Role/GetRoles?${queryString()}`);
            state.pageIndex = result.pageIndex;
            renderRoles(result.items);
            renderPageInfo(pageInfo, result);
            renderPagination(pagination, result, (page) => {
                state.pageIndex = page;
                loadRoles();
            });
        } catch (error) {
            tableBody.innerHTML = `<tr><td colspan="6" class="text-danger text-center">${escapeHtml(error.message)}</td></tr>`;
            showAlert(error.message, "danger");
        }
    };
    const syncPermissionToggleButtons = () => {
        const checkboxes = Array.from(permissionGroups.querySelectorAll("[data-permission-checkbox]"));
        const toggleAllButton = document.getElementById("toggleAllPermissions");
        toggleAllButton.textContent = checkboxes.length > 0 && checkboxes.every(item => item.checked)
            ? "Bỏ chọn tất cả"
            : "Chọn tất cả";

        permissionGroups.querySelectorAll("[data-toggle-controller]").forEach((button) => {
            const controllerName = button.dataset.toggleController;
            const controllerChecks = checkboxes.filter(
                checkbox => checkbox.dataset.controllerName === controllerName);
            button.textContent = controllerChecks.length > 0
                && controllerChecks.every(checkbox => checkbox.checked)
                ? "Bỏ chọn tất cả"
                : "Chọn tất cả";
        });
    };
    const renderPermissionGroups = (groups) => {
        permissionGroups.replaceChildren();
        groups.forEach((group, groupIndex) => {
            const column = document.createElement("div");
            column.className = "col-lg-6";
            const card = document.createElement("section");
            card.className = "card h-100";
            const header = document.createElement("div");
            header.className = "card-header d-flex justify-content-between align-items-center";
            const title = document.createElement("h6");
            title.className = "mb-0";
            title.textContent = group.controllerName;
            const toggleButton = document.createElement("button");
            toggleButton.type = "button";
            toggleButton.className = "btn btn-sm btn-outline-secondary";
            toggleButton.dataset.toggleController = group.controllerName;
            toggleButton.textContent = "Chọn tất cả";
            header.append(title, toggleButton);

            const body = document.createElement("div");
            body.className = "card-body";
            group.actions.forEach((action, actionIndex) => {
                const wrapper = document.createElement("div");
                wrapper.className = "form-check";
                const checkbox = document.createElement("input");
                checkbox.type = "checkbox";
                checkbox.className = "form-check-input";
                checkbox.id = `permission-${groupIndex}-${actionIndex}`;
                checkbox.value = action.claimValue;
                checkbox.checked = action.isSelected;
                checkbox.dataset.permissionCheckbox = "true";
                checkbox.dataset.controllerName = group.controllerName;
                const label = document.createElement("label");
                label.className = "form-check-label";
                label.htmlFor = checkbox.id;
                label.textContent = action.actionName;
                wrapper.append(checkbox, label);
                body.appendChild(wrapper);
            });
            card.append(header, body);
            column.appendChild(card);
            permissionGroups.appendChild(column);
        });
        syncPermissionToggleButtons();
    };
    const openPermissions = async (roleId) => {
        permissionRoleId = roleId;
        clearPermissionError();
        try {
            const result = await requestJson(
                `/Role/GetRolePermissions?roleId=${encodeURIComponent(roleId)}`);
            document.getElementById("permissionRoleName").textContent = result.roleName;
            renderPermissionGroups(result.controllers);
            permissionModal.show();
        } catch (error) {
            showAlert(error.message, "danger");
        }
    };
    const debounce = (callback, delay = 350) => {
        let timer;
        return (...args) => {
            clearTimeout(timer);
            timer = setTimeout(() => callback(...args), delay);
        };
    };

    document.getElementById("roleSearch").addEventListener("input", debounce((event) => {
        state.searchString = event.target.value.trim();
        state.pageIndex = 1;
        loadRoles();
    }));
    document.getElementById("rolePageSize").addEventListener("change", (event) => {
        state.pageSize = Number(event.target.value);
        state.pageIndex = 1;
        loadRoles();
    });
    document.getElementById("createRoleButton").addEventListener("click", () => {
        editingRoleId = null;
        form.reset();
        document.getElementById("roleModalLabel").textContent = "Thêm nhóm quyền";
        clearErrors();
        modal.show();
    });
    tableBody.addEventListener("click", async (event) => {
        const button = event.target.closest("button");
        if (!button || button.disabled) return;
        const id = button.dataset.id;
        if (button.classList.contains("permission-role")) {
            await openPermissions(id);
            return;
        }
        if (button.classList.contains("delete-role")) {
            if (!window.confirm("Bạn có chắc muốn xóa nhóm quyền này?")) return;
            try {
                const result = await requestJson(`/Role/DeleteAjax/${encodeURIComponent(id)}`, { method: "DELETE" });
                showAlert(result.message);
                await loadRoles();
            } catch (error) {
                showAlert(error.message, "danger");
            }
            return;
        }

        editingRoleId = id;
        clearErrors();
        try {
            const role = await requestJson(`/Role/GetRoleById/${encodeURIComponent(id)}`);
            document.getElementById("roleName").value = role.name ?? "";
            document.getElementById("roleModalLabel").textContent = "Sửa nhóm quyền";
            modal.show();
        } catch (error) {
            showAlert(error.message, "danger");
        }
    });
    form.addEventListener("submit", async (event) => {
        event.preventDefault();
        clearErrors();
        saveButton.disabled = true;
        const isEditing = editingRoleId !== null;
        const role = { Name: document.getElementById("roleName").value.trim() };
        try {
            await requestJson(isEditing
                ? `/Role/EditAjax/${encodeURIComponent(editingRoleId)}`
                : "/Role/CreateAjax", {
                    method: isEditing ? "PUT" : "POST",
                    body: JSON.stringify(role)
                });
            modal.hide();
            showAlert(isEditing ? "Đã cập nhật nhóm quyền." : "Đã thêm nhóm quyền.");
            await loadRoles();
        } catch (error) {
            showErrors(error.payload?.errors, error.message);
        } finally {
            saveButton.disabled = false;
        }
    });
    document.getElementById("toggleAllPermissions").addEventListener("click", () => {
        const checkboxes = Array.from(permissionGroups.querySelectorAll("[data-permission-checkbox]"));
        const shouldSelect = checkboxes.some(checkbox => !checkbox.checked);
        checkboxes.forEach((checkbox) => checkbox.checked = shouldSelect);
        syncPermissionToggleButtons();
    });
    permissionGroups.addEventListener("click", (event) => {
        const button = event.target.closest("[data-toggle-controller]");
        if (!button) return;
        const controllerName = button.dataset.toggleController;
        const checkboxes = Array.from(permissionGroups.querySelectorAll("[data-permission-checkbox]"))
            .filter(checkbox => checkbox.dataset.controllerName === controllerName);
        const shouldSelect = checkboxes.some(checkbox => !checkbox.checked);
        checkboxes.forEach((checkbox) => checkbox.checked = shouldSelect);
        syncPermissionToggleButtons();
    });
    permissionGroups.addEventListener("change", syncPermissionToggleButtons);
    permissionForm.addEventListener("submit", async (event) => {
        event.preventDefault();
        clearPermissionError();
        savePermissionsButton.disabled = true;
        const request = {
            RoleId: permissionRoleId,
            Permissions: Array.from(permissionGroups.querySelectorAll("[data-permission-checkbox]:checked"))
                .map(checkbox => checkbox.value)
        };
        try {
            const result = await requestJson("/Role/UpdateRolePermissions", {
                method: "POST",
                body: JSON.stringify(request)
            }, permissionForm);
            permissionModal.hide();
            showAlert(result.message);
        } catch (error) {
            permissionFormError.textContent = error.message;
            permissionFormError.classList.remove("d-none");
        } finally {
            savePermissionsButton.disabled = false;
        }
    });

    loadRoles();
})();
