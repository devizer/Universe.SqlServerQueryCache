document.addEventListener('DOMContentLoaded', function (e) {
    console.log("Document is Ready");
});

selectedSortProperty = ">undefined<";
function SelectContent(newSelectedSortProperty) {
    var prev = document.querySelector("#" + selectedSortProperty);
    if (prev) { prev.classList.add('Hidden'); }
    var next = document.querySelector("#" + newSelectedSortProperty);
    next.classList.remove('Hidden');
    selectedSortProperty = newSelectedSortProperty;
}