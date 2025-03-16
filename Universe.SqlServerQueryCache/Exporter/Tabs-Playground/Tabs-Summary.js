window.addEventListener('load', function () {

    var tabButtons = document.querySelectorAll('.tabs__pills .TabLink');
    var tabContents = document.querySelectorAll('.tabs__panels > div');

    if (tabButtons && tabContents) {
        tabButtons.forEach((tabBtn) => {
            tabBtn.addEventListener('click', () => {
                var tabId = tabBtn.getAttribute('data-id');

                tabButtons.forEach((btn) => btn.classList.remove('active'));
                tabBtn.classList.add('active');

                tabContents.forEach((content) => {
                    if (content.id === tabId) {
                        content.classList.add('active');
                    } else {
                        content.classList.remove('active');
                    }
                });
                adjustModalTabHeight();
                setTimeout(() => adjustModalTabHeight());
            });
        });
    }
});