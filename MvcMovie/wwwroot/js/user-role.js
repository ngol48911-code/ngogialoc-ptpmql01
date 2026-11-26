(() => {
    "use strict";

    const form = document.getElementById("userRoleForm");
    const tableBody = document.getElementById("userTableBody");
    const modal = new bootstrap.Modal(document.getElementById("userRoleModal"));
    const alertElement = document.getElementById("userAlert");
    const formError = document.getElementById("userRoleError");
    const saveButton = document.getElementById("saveUserRolesButton");
    const pageInfo = document.getElementById("userPageInfo");
    const pagination = document.getElementById("userPagination");
    const checkboxContainer = document.getElementById("userRoleCheckboxes");
    const state = { pageIndex: 1, pageSize: 10, searchString: "", roleFilter: "" };
    let selectedUserId = null;

    const getToken = () => form.querySelector('input[name="__RequestVerificationToken"]').value;
    const escapeHtml = (value) => String(value ?? "")
        .replaceAll("&", "&amp;").replaceAll("<", "&lt;").replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;").replaceAll("'", "&#039;");
    const showAlert = (message, type = "success") => {
        alertElement.textContent = message;
        alertElement.className = `alert alert-${type}`;
    };
    const clearFormError = () => {
        formError.textContent = "";
        formError.classList.add("d-none");
    };
    const showFormError = (message) => {
        formError.textContent = message || "Vui lòng kiểm tra lại dữ liệu.";
        formError.classList.remove("d-none");
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
        if (state.roleFilter) params.set("roleFilter", state.roleFilter);
        return params;
    };
    const renderUsers = (users) => {
        tableBody.replaceChildren();
        if (users.length === 0) {
            tableBody.innerHTML = '<tr><td colspan="4" class="text-center">Không có người dùng.</td></tr>';
            return;
        }

        users.forEach((user) => {
            const row = document.createElement("tr");
            const roles = user.roles.length
                ? user.roles.map((role) =>
                    `<span class="badge text-bg-primary me-1">${escapeHtml(role)}</span>`).join("")
                : '<span class="text-muted">Chưa gán</span>';
            const emailStatus = user.emailConfirmed
                ? '<span class="badge text-bg-success">Đã xác thực</span>'
                : '<span class="badge text-bg-secondary">Chưa xác thực</span>';
            row.innerHTML = `
                <td>${escapeHtml(user.email || "—")}</td>
                <td>${emailStatus}</td>
                <td>${roles}</td>
                <td>
                    <button type="button" class="btn btn-sm btn-primary manage-user-roles"
                            data-id="${escapeHtml(user.id)}">Phân quyền</button>
                </td>`;
            tableBody.appendChild(row);
        });
    };
    const loadUsers = async () => {
        try {
            const result = await requestJson(`/User/GetUsers?${queryString()}`);
            state.pageIndex = result.pageIndex;
            renderUsers(result.items);
            renderPageInfo(pageInfo, result);
            renderPagination(pagination, result, (page) => {
                state.pageIndex = page;
                loadUsers();
            });
        } catch (error) {
            tableBody.innerHTML = `<tr><td colspan="4" class="text-danger text-center">${escapeHtml(error.message)}</td></tr>`;
            showAlert(error.message, "danger");
        }
    };
    const renderRoleSelections = (roles) => {
        checkboxContainer.replaceChildren();
        roles.forEach((role, index) => {
            const wrapper = document.createElement("div");
            wrapper.className = "form-check";
            const checkbox = document.createElement("input");
            checkbox.type = "checkbox";
            checkbox.className = "form-check-input";
            checkbox.id = `userRole-${index}`;
            checkbox.value = role.name;
            checkbox.checked = role.isSelected;
            const label = document.createElement("label");
            label.className = "form-check-label";
            label.htmlFor = checkbox.id;
            label.textContent = role.name;
            wrapper.append(checkbox, label);
            checkboxContainer.appendChild(wrapper);
        });
        if (roles.length === 0) {
            const message = document.createElement("p");
            message.className = "text-muted mb-0";
            message.textContent = "Hệ thống chưa có nhóm quyền.";
            checkboxContainer.appendChild(message);
        }
    };
    const debounce = (callback, delay = 350) => {
        let timer;
        return (...args) => {
            clearTimeout(timer);
            timer = setTimeout(() => callback(...args), delay);
        };
    };

    document.getElementById("userSearch").addEventListener("input", debounce((event) => {
        state.searchString = event.target.value.trim();
        state.pageIndex = 1;
        loadUsers();
    }));
    document.getElementById("userRoleFilter").addEventListener("change", (event) => {
        state.roleFilter = event.target.value;
        state.pageIndex = 1;
        loadUsers();
    });
    document.getElementById("userPageSize").addEventListener("change", (event) => {
        state.pageSize = Number(event.target.value);
        state.pageIndex = 1;
        loadUsers();
    });
    tableBody.addEventListener("click", async (event) => {
        const button = event.target.closest(".manage-user-roles");
        if (!button) return;
        selectedUserId = button.dataset.id;
        clearFormError();
        try {
            const result = await requestJson(`/User/GetUserRoles?userId=${encodeURIComponent(selectedUserId)}`);
            document.getElementById("managedUserEmail").textContent = result.email || "Người dùng không có email";
            renderRoleSelections(result.roles);
            modal.show();
        } catch (error) {
            showAlert(error.message, "danger");
        }
    });
    form.addEventListener("submit", async (event) => {
        event.preventDefault();
        clearFormError();
        saveButton.disabled = true;
        const request = {
            UserId: selectedUserId,
            Roles: Array.from(checkboxContainer.querySelectorAll('input[type="checkbox"]:checked'))
                .map(checkbox => checkbox.value)
        };
        try {
            await requestJson("/User/UpdateUserRoles", {
                method: "PUT",
                body: JSON.stringify(request)
            });
            modal.hide();
            showAlert("Đã cập nhật nhóm quyền cho người dùng.");
            await loadUsers();
        } catch (error) {
            showFormError(error.message);
        } finally {
            saveButton.disabled = false;
        }
    });

    loadUsers();
})();
