(() => {
    "use strict";

    const form = document.getElementById("personForm");
    const tableBody = document.getElementById("personTableBody");
    const modal = new bootstrap.Modal(document.getElementById("personModal"));
    const alertElement = document.getElementById("personAlert");
    const formError = document.getElementById("personFormError");
    const saveButton = document.getElementById("savePersonButton");
    const pageInfo = document.getElementById("personPageInfo");
    const pagination = document.getElementById("personPagination");
    const state = { pageIndex: 1, pageSize: 10, searchString: "" };
    let editingId = null;

    const token = () => form.querySelector('input[name="__RequestVerificationToken"]').value;
    const escapeHtml = (value) => String(value ?? "")
        .replaceAll("&", "&amp;").replaceAll("<", "&lt;").replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;").replaceAll("'", "&#039;");
    const showAlert = (message, type = "success") => {
        alertElement.textContent = message;
        alertElement.className = `alert alert-${type}`;
    };
    const request = async (url, options = {}) => {
        const response = await fetch(url, {
            ...options,
            headers: {
                Accept: "application/json",
                ...(options.body ? { "Content-Type": "application/json" } : {}),
                ...(options.method && options.method !== "GET"
                    ? { RequestVerificationToken: token() } : {}),
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
            throw new Error(payload.message || "Yêu cầu không thành công.");
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
    const debounce = (callback, delay = 350) => {
        let timer;
        return (...args) => {
            clearTimeout(timer);
            timer = setTimeout(() => callback(...args), delay);
        };
    };
    const loadPersons = async () => {
        try {
            const result = await request(`/Person/GetPersons?${queryString()}`);
            state.pageIndex = result.pageIndex;
            tableBody.replaceChildren();
            if (result.items.length === 0) {
                tableBody.innerHTML = '<tr><td colspan="4" class="text-center">Chưa có Person.</td></tr>';
            } else {
                result.items.forEach((person) => {
                    const row = document.createElement("tr");
                    row.innerHTML = `
                        <td>${escapeHtml(person.personId)}</td>
                        <td>${escapeHtml(person.fullName)}</td>
                        <td>${escapeHtml(person.address)}</td>
                        <td>
                            <button class="btn btn-sm btn-warning edit-person" data-id="${escapeHtml(person.personId)}">Sửa</button>
                            <button class="btn btn-sm btn-danger delete-person" data-id="${escapeHtml(person.personId)}">Xóa</button>
                        </td>`;
                    tableBody.appendChild(row);
                });
            }
            renderPageInfo(pageInfo, result);
            renderPagination(pagination, result, (page) => {
                state.pageIndex = page;
                loadPersons();
            });
        } catch (error) {
            showAlert(error.message, "danger");
        }
    };

    document.getElementById("personSearch").addEventListener("input", debounce((event) => {
        state.searchString = event.target.value.trim();
        state.pageIndex = 1;
        loadPersons();
    }));
    document.getElementById("personPageSize").addEventListener("change", (event) => {
        state.pageSize = Number(event.target.value);
        state.pageIndex = 1;
        loadPersons();
    });
    document.getElementById("createPersonButton").addEventListener("click", () => {
        editingId = null;
        form.reset();
        document.getElementById("personId").readOnly = false;
        document.getElementById("personModalLabel").textContent = "Thêm Person";
        formError.classList.add("d-none");
        modal.show();
    });
    tableBody.addEventListener("click", async (event) => {
        const button = event.target.closest("button");
        if (!button) return;
        const id = button.dataset.id;
        if (button.classList.contains("delete-person")) {
            if (!window.confirm(`Bạn có chắc muốn xóa Person ${id}?`)) return;
            try {
                await request(`/Person/DeleteAjax/${encodeURIComponent(id)}`, { method: "DELETE" });
                showAlert("Đã xóa Person.");
                await loadPersons();
            } catch (error) {
                showAlert(error.message, "danger");
            }
            return;
        }
        editingId = id;
        try {
            const person = await request(`/Person/GetPersonById/${encodeURIComponent(id)}`);
            document.getElementById("personId").value = person.personId ?? "";
            document.getElementById("personId").readOnly = true;
            document.getElementById("personFullName").value = person.fullName ?? "";
            document.getElementById("personAddress").value = person.address ?? "";
            document.getElementById("personModalLabel").textContent = "Sửa Person";
            modal.show();
        } catch (error) {
            showAlert(error.message, "danger");
        }
    });
    form.addEventListener("submit", async (event) => {
        event.preventDefault();
        formError.classList.add("d-none");
        saveButton.disabled = true;
        const person = {
            PersonId: document.getElementById("personId").value.trim(),
            FullName: document.getElementById("personFullName").value.trim(),
            Address: document.getElementById("personAddress").value.trim()
        };
        try {
            await request(editingId
                ? `/Person/EditAjax/${encodeURIComponent(editingId)}`
                : "/Person/CreateAjax", {
                    method: editingId ? "PUT" : "POST",
                    body: JSON.stringify(person)
                });
            modal.hide();
            showAlert(editingId ? "Đã cập nhật Person." : "Đã thêm Person.");
            await loadPersons();
        } catch (error) {
            formError.textContent = error.message;
            formError.classList.remove("d-none");
        } finally {
            saveButton.disabled = false;
        }
    });
    loadPersons();
})();
