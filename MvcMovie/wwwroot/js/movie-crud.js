(() => {
    "use strict";

    const form = document.getElementById("movieForm");
    const tableBody = document.getElementById("movieTableBody");
    const modal = new bootstrap.Modal(document.getElementById("movieModal"));
    const alertElement = document.getElementById("movieAlert");
    const formError = document.getElementById("movieFormError");
    const saveButton = document.getElementById("saveMovieButton");
    const pageInfo = document.getElementById("moviePageInfo");
    const pagination = document.getElementById("moviePagination");
    const state = { pageIndex: 1, pageSize: 10, searchString: "", genre: "" };
    let editingId = null;

    const token = () => form.querySelector('input[name="__RequestVerificationToken"]').value;
    const escapeHtml = (value) => String(value ?? "")
        .replaceAll("&", "&amp;").replaceAll("<", "&lt;").replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;").replaceAll("'", "&#039;");
    const showAlert = (message, type = "success") => {
        alertElement.textContent = message;
        alertElement.className = `alert alert-${type}`;
    };
    const dateValue = (value) => value ? value.substring(0, 10) : "";
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
        if (state.genre) params.set("genre", state.genre);
        return params;
    };
    const debounce = (callback, delay = 350) => {
        let timer;
        return (...args) => {
            clearTimeout(timer);
            timer = setTimeout(() => callback(...args), delay);
        };
    };
    const loadMovies = async () => {
        try {
            const result = await request(`/Movie/GetMovies?${queryString()}`);
            state.pageIndex = result.pageIndex;
            tableBody.replaceChildren();
            if (result.items.length === 0) {
                tableBody.innerHTML = '<tr><td colspan="6" class="text-center">Chưa có phim.</td></tr>';
            } else {
                result.items.forEach((movie) => {
                    const row = document.createElement("tr");
                    row.innerHTML = `
                        <td>${movie.id}</td>
                        <td>${escapeHtml(movie.title)}</td>
                        <td>${escapeHtml(dateValue(movie.releaseDate))}</td>
                        <td>${escapeHtml(movie.genre)}</td>
                        <td>${escapeHtml(movie.price)}</td>
                        <td>
                            <button class="btn btn-sm btn-warning edit-movie" data-id="${movie.id}">Sửa</button>
                            <button class="btn btn-sm btn-danger delete-movie" data-id="${movie.id}">Xóa</button>
                        </td>`;
                    tableBody.appendChild(row);
                });
            }
            renderPageInfo(pageInfo, result);
            renderPagination(pagination, result, (page) => {
                state.pageIndex = page;
                loadMovies();
            });
        } catch (error) {
            showAlert(error.message, "danger");
        }
    };

    const reloadFromSearch = debounce(() => {
        state.pageIndex = 1;
        loadMovies();
    });
    document.getElementById("movieSearch").addEventListener("input", (event) => {
        state.searchString = event.target.value.trim();
        reloadFromSearch();
    });
    document.getElementById("movieGenreFilter").addEventListener("input", (event) => {
        state.genre = event.target.value.trim();
        reloadFromSearch();
    });
    document.getElementById("moviePageSize").addEventListener("change", (event) => {
        state.pageSize = Number(event.target.value);
        state.pageIndex = 1;
        loadMovies();
    });
    document.getElementById("createMovieButton").addEventListener("click", () => {
        editingId = null;
        form.reset();
        document.getElementById("movieModalLabel").textContent = "Thêm phim";
        formError.classList.add("d-none");
        modal.show();
    });
    tableBody.addEventListener("click", async (event) => {
        const button = event.target.closest("button");
        if (!button) return;
        const id = button.dataset.id;
        if (button.classList.contains("delete-movie")) {
            if (!window.confirm(`Bạn có chắc muốn xóa phim ${id}?`)) return;
            try {
                await request(`/Movie/DeleteAjax/${id}`, { method: "DELETE" });
                showAlert("Đã xóa phim.");
                await loadMovies();
            } catch (error) {
                showAlert(error.message, "danger");
            }
            return;
        }
        editingId = id;
        try {
            const movie = await request(`/Movie/GetMovieById/${id}`);
            document.getElementById("movieTitle").value = movie.title ?? "";
            document.getElementById("movieReleaseDate").value = dateValue(movie.releaseDate);
            document.getElementById("movieGenre").value = movie.genre ?? "";
            document.getElementById("moviePrice").value = movie.price ?? "";
            document.getElementById("movieModalLabel").textContent = "Sửa phim";
            modal.show();
        } catch (error) {
            showAlert(error.message, "danger");
        }
    });
    form.addEventListener("submit", async (event) => {
        event.preventDefault();
        formError.classList.add("d-none");
        saveButton.disabled = true;
        const movie = {
            Title: document.getElementById("movieTitle").value.trim(),
            ReleaseDate: document.getElementById("movieReleaseDate").value,
            Genre: document.getElementById("movieGenre").value.trim(),
            Price: Number(document.getElementById("moviePrice").value || 0)
        };
        try {
            await request(editingId ? `/Movie/EditAjax/${editingId}` : "/Movie/CreateAjax", {
                method: editingId ? "PUT" : "POST",
                body: JSON.stringify(movie)
            });
            modal.hide();
            showAlert(editingId ? "Đã cập nhật phim." : "Đã thêm phim.");
            await loadMovies();
        } catch (error) {
            formError.textContent = error.message;
            formError.classList.remove("d-none");
        } finally {
            saveButton.disabled = false;
        }
    });
    loadMovies();
})();
