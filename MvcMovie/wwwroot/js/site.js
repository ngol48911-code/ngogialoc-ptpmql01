// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

window.renderPagination = (container, pageInfo, onPageChange) => {
    container.replaceChildren();

    const createItem = (label, page, disabled = false, active = false) => {
        const item = document.createElement("li");
        item.className = `page-item${disabled ? " disabled" : ""}${active ? " active" : ""}`;
        const button = document.createElement("button");
        button.type = "button";
        button.className = "page-link";
        button.textContent = label;
        button.disabled = disabled;
        if (!disabled && !active) {
            button.addEventListener("click", () => onPageChange(page));
        }
        item.appendChild(button);
        return item;
    };

    if (pageInfo.totalItems === 0) {
        return;
    }

    container.appendChild(createItem("Trước", pageInfo.pageIndex - 1, pageInfo.pageIndex <= 1));

    const firstPage = Math.max(1, Math.min(
        pageInfo.pageIndex - 3,
        pageInfo.totalPages - 6));
    const lastPage = Math.min(pageInfo.totalPages, firstPage + 6);

    for (let page = firstPage; page <= lastPage; page += 1) {
        container.appendChild(createItem(
            String(page),
            page,
            false,
            page === pageInfo.pageIndex));
    }

    container.appendChild(createItem(
        "Sau",
        pageInfo.pageIndex + 1,
        pageInfo.pageIndex >= pageInfo.totalPages));
};

window.renderPageInfo = (element, pageInfo) => {
    if (pageInfo.totalItems === 0) {
        element.textContent = "Không có kết quả";
        return;
    }

    const first = (pageInfo.pageIndex - 1) * pageInfo.pageSize + 1;
    const last = Math.min(pageInfo.pageIndex * pageInfo.pageSize, pageInfo.totalItems);
    element.textContent = `Hiển thị ${first}–${last} trên tổng số ${pageInfo.totalItems} kết quả`;
};
