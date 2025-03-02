function openModal(id) {
    document.getElementById(id).classList.add('open');
    document.body.classList.add('Modal-Summary-open');
}

// close currently open modal
function closeModal() {
    document.querySelector('.Modal-Summary.open').classList.remove('open');
    document.body.classList.remove('Modal-Summary-open');
}

window.addEventListener('load', function () {
    // close modals on background click
    document.addEventListener('click', event => {
        if (event.target.classList.contains('Modal-Summary')) {
            closeModal();
        }
    });
});