document.addEventListener('DOMContentLoaded', () => {
    const body = document.body;
    document.querySelectorAll('[data-sidebar-toggle]').forEach(button => button.addEventListener('click', () => body.classList.toggle('sidebar-open')));
    document.querySelectorAll('[data-sidebar-close]').forEach(button => button.addEventListener('click', () => body.classList.remove('sidebar-open')));
    document.querySelectorAll('[data-page-size]').forEach(select => select.addEventListener('change', () => {
        const url = new URL(window.location.href);
        url.searchParams.set('pageSize', select.value);
        url.searchParams.set('pageNumber', '1');
        window.location.assign(url.toString());
    }));
});
