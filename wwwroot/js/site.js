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

    document.querySelectorAll('[data-transfer-view]').forEach(container => {
        const mobileQuery = window.matchMedia('(max-width: 767px)');
        const validViews = new Set(['table', 'cards']);

        const storageKey = () => mobileQuery.matches
            ? 'pallets.transfers.view.mobile'
            : 'pallets.transfers.view.desktop';

        const defaultView = () => mobileQuery.matches ? 'cards' : 'table';

        const applyView = view => {
            const selectedView = validViews.has(view) ? view : defaultView();
            container.dataset.viewMode = selectedView;

            container.querySelectorAll('[data-view-option]').forEach(button => {
                const isActive = button.dataset.viewOption === selectedView;
                button.classList.toggle('active', isActive);
                button.setAttribute('aria-pressed', isActive.toString());
            });
        };

        const loadView = () => {
            applyView(localStorage.getItem(storageKey()) || defaultView());
        };

        container.querySelectorAll('[data-view-option]').forEach(button => {
            button.addEventListener('click', () => {
                const view = button.dataset.viewOption;
                localStorage.setItem(storageKey(), view);
                applyView(view);
            });
        });

        mobileQuery.addEventListener?.('change', loadView);
        loadView();
    });
});
