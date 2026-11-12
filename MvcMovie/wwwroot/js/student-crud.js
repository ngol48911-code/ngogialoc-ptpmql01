(() => {
    "use strict";

    const form = document.getElementById("studentForm");
    const tableBody = document.getElementById("studentTableBody");
    const modal = new bootstrap.Modal(document.getElementById("studentModal"));
    const alertElement = document.getElementById("studentAlert");
    const formError = document.getElementById("formError");
    const saveButton = document.getElementById("saveStudentButton");
    const pageInfo = document.getElementById("studentPageInfo");
    const pagination = document.getElementById("studentPagination");
    const state = { pageIndex: 1, pageSize: 10, searchString: "", facultyId: "" };
    let editingStudentId = null;

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
        if (state.facultyId) params.set("facultyId", state.facultyId);
        return params;
    };
    const trainingTypeName = (value) => ({
        "21": "Đại học chính quy", "22": "Cao đẳng",
        "23": "Liên thông", "24": "Vừa làm vừa học"
    }[value] || value || "");

    const renderStudents = (students) => {
        tableBody.replaceChildren();
        if (students.length === 0) {
            tableBody.innerHTML = '<tr><td colspan="8" class="text-center">Chưa có sinh viên.</td></tr>';
            return;
        }
        students.forEach((student) => {
            const row = document.createElement("tr");
            row.innerHTML = `
                <td><strong>${escapeHtml(student.studentId)}</strong></td>
                <td>${escapeHtml(student.fullName)}</td>
                <td>${escapeHtml(student.address)}</td>
                <td>${escapeHtml(student.university)}</td>
                <td>${escapeHtml(student.namNhapHoc)}</td>
                <td>${escapeHtml(trainingTypeName(student.heDaoTao))}</td>
                <td>${escapeHtml(student.facultyName)}</td>
                <td>
                    <button type="button" class="btn btn-sm btn-warning edit-student" data-id="${escapeHtml(student.studentId)}">Sửa</button>
                    <button type="button" class="btn btn-sm btn-danger delete-student" data-id="${escapeHtml(student.studentId)}">Xóa</button>
                </td>`;
            tableBody.appendChild(row);
        });
    };
    const loadStudents = async () => {
        try {
            const result = await requestJson(`/Student/GetStudents?${queryString()}`);
            state.pageIndex = result.pageIndex;
            renderStudents(result.items);
            renderPageInfo(pageInfo, result);
            renderPagination(pagination, result, (page) => {
                state.pageIndex = page;
                loadStudents();
            });
        } catch (error) {
            tableBody.innerHTML = `<tr><td colspan="8" class="text-danger text-center">${escapeHtml(error.message)}</td></tr>`;
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

    document.getElementById("studentSearch").addEventListener("input", debounce((event) => {
        state.searchString = event.target.value.trim();
        state.pageIndex = 1;
        loadStudents();
    }));
    document.getElementById("studentFacultyFilter").addEventListener("change", (event) => {
        state.facultyId = event.target.value;
        state.pageIndex = 1;
        loadStudents();
    });
    document.getElementById("studentPageSize").addEventListener("change", (event) => {
        state.pageSize = Number(event.target.value);
        state.pageIndex = 1;
        loadStudents();
    });
    document.getElementById("createStudentButton").addEventListener("click", () => {
        editingStudentId = null;
        form.reset();
        document.getElementById("university").value = "Trường Đại học Mỏ - Địa chất";
        document.getElementById("studentModalLabel").textContent = "Thêm sinh viên";
        clearErrors();
        modal.show();
    });
    tableBody.addEventListener("click", async (event) => {
        const button = event.target.closest("button");
        if (!button) return;
        const id = button.dataset.id;
        if (button.classList.contains("delete-student")) {
            if (!window.confirm(`Bạn có chắc muốn xóa sinh viên ${id}?`)) return;
            try {
                await requestJson(`/Student/DeleteAjax/${encodeURIComponent(id)}`, { method: "DELETE" });
                showAlert("Đã xóa sinh viên.");
                await loadStudents();
            } catch (error) {
                showAlert(error.message, "danger");
            }
            return;
        }
        editingStudentId = id;
        clearErrors();
        try {
            const student = await requestJson(`/Student/GetStudentById/${encodeURIComponent(id)}`);
            document.getElementById("fullName").value = student.fullName ?? "";
            document.getElementById("address").value = student.address ?? "";
            document.getElementById("university").value = student.university ?? "";
            document.getElementById("namNhapHoc").value = student.namNhapHoc ?? "";
            document.getElementById("heDaoTao").value = student.heDaoTao ?? "";
            document.getElementById("facultyId").value = student.facultyId ?? "";
            document.getElementById("studentModalLabel").textContent = "Sửa sinh viên";
            modal.show();
        } catch (error) {
            showAlert(error.message, "danger");
        }
    });
    form.addEventListener("submit", async (event) => {
        event.preventDefault();
        clearErrors();
        saveButton.disabled = true;
        const isEditing = editingStudentId !== null;
        const student = {
            FullName: document.getElementById("fullName").value.trim(),
            Address: document.getElementById("address").value.trim(),
            University: document.getElementById("university").value.trim(),
            NamNhapHoc: document.getElementById("namNhapHoc").value,
            HeDaoTao: document.getElementById("heDaoTao").value,
            FacultyId: document.getElementById("facultyId").value
        };
        try {
            await requestJson(isEditing
                ? `/Student/EditAjax/${encodeURIComponent(editingStudentId)}`
                : "/Student/CreateAjax", {
                    method: isEditing ? "PUT" : "POST",
                    body: JSON.stringify(student)
                });
            modal.hide();
            showAlert(isEditing ? "Đã cập nhật sinh viên." : "Đã thêm sinh viên.");
            await loadStudents();
        } catch (error) {
            showErrors(error.payload?.errors, error.message);
        } finally {
            saveButton.disabled = false;
        }
    });
    loadStudents();
})();
