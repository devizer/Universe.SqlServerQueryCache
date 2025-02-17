document.addEventListener('DOMContentLoaded', function (e) {
    console.log("Document is Ready");
    var sortButtons = document.querySelectorAll("button.SortButton");
    sortButtons.forEach(function (b) {
        b.addEventListener("click", function(sortButtonClickEvent) {
            console.log("Sort Clicked", sortButtonClickEvent);
        });
        console.log("[Sort Button] Click Listener added for: " + b.innerText, b);
    });
    var tables = document.querySelectorAll("table.Metrics");
    console.log("Tables", tables);
    tables.forEach(function (t) {
        t.addEventListener("click", function (tableClickEvent) {
            console.log("[CLICK] TABLE CLICKED '" + tableClickEvent.target.tagName + "'", tableClickEvent);
            var sortParameter = tableClickEvent.target.dataset.SortParameter;
            console.log("[CLICK] TARGET DATASET", tableClickEvent.target.dataset);
            if (sortParameter) {
                SelectContent(sortParameter);
            }
        });
        console.log("[TABLE] Click Listener added for a table");
    });

});

selectedSortProperty = "undefined";
function SelectContent(newSelectedSortProperty) {
    var prev = document.querySelector("#" + selectedSortProperty);
    if (prev) { prev.classList.add('Hidden'); }
    var next = document.querySelector("#" + newSelectedSortProperty);
    next.classList.remove('Hidden');
    selectedSortProperty = newSelectedSortProperty;
}