(() => {
    "use strict";

    const form = document.getElementById("roleForm");
    const tableBody = document.getElementById("roleTableBody");
    const modal = new bootstrap.Modal(document.getElementById("roleModal"));
    const alertElement = document.getElementById("roleAlert");
    const formError = document.getElementById("roleFormError");
    const saveButton = document.getElementById("saveRoleButton");
    const pageInfo = document.getElementById("rolePageInfo");
    const pagination = document.getElementById("rolePagination");
    const state = { pageIndex: 1, pageSize: 10, searchString: "" };
    let editingRoleId = null;

    const getToken = () => form.querySelector('input[name="__RequestVerificationToken"]').value;
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
    const showErrors = (errors, message) => {
        formError.textContent = message || "Vui lòng kiểm tra lại dữ liệu.";
        formError.classList.remove("d-none");
        Object.entries(errors || {}).forEach(([field, messages]) => {
            form.querySelector(`[name="${field}"]`)?.classList.add("is-invalid");
            const errorElement = form.querySelector(`[data-error-for="${field}"]`);
            if (errorElement) errorElement.textContent = messages.join(" ");
        });
    };
    const requestJson = async (url, options = {}) => {
        const response = await fetch(url, {
            ...options,
            headers: {
                Accept: "application/json",
                ...(options.body ? { "Content-Type": "application/json" } : {}),
                ...(options.method && options.method !== "GET"
                    ? { RequestVerificationToken: getToken() } : {}),
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
            tableBody.innerHTML = '<tr><td colspan="5" class="text-center">Chưa có nhóm quyền.</td></tr>';
            return;
        }

        roles.forEach((role) => {
            const row = document.createElement("tr");
            const systemBadge = role.isSystemRole
                ? '<span class="badge text-bg-secondary">Hệ thống</span>'
                : '<span class="badge text-bg-primary">Tùy chỉnh</span>';
            const disabled = role.isSystemRole ? "disabled" : "";
            row.innerHTML = `
                <td>${escapeHtml(role.name)}</td>
                <td>${escapeHtml(role.normalizedName)}</td>
                <td><span class="badge text-bg-info">${escapeHtml(role.userCount)}</span></td>
                <td>${systemBadge}</td>
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
            tableBody.innerHTML = `<tr><td colspan="5" class="text-danger text-center">${escapeHtml(error.message)}</td></tr>`;
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

    loadRoles();
})();
