document.addEventListener('DOMContentLoaded', function (e) {
    console.log("Document is Ready");
    var sortButtons = document.querySelectorAll("button.SortButton");
    sortButtons.forEach(function (b) {
        b.addEventListener("click", function(sortButtonClickEvent) {
            // console.log("Sort Clicked", sortButtonClickEvent);
        });
        // console.log("[Sort Button] Click Listener added for: " + b.innerText, b);
    });
    var tables = document.querySelectorAll("table.Metrics");
    // console.log("Tables", tables);
    tables.forEach(function (t) {
        t.addEventListener("click", function (tableClickEvent) {
            console.log("[CLICK] TABLE CLICKED '" + tableClickEvent.target.tagName + "'", tableClickEvent);
            var sortParameter = tableClickEvent.target.dataset.sorting;
            console.log("[CLICK] TARGET DATASET", tableClickEvent.target.dataset);
            var parent = tableClickEvent.target.parentElement;
            if (parent) {
                console.log("[CLICK] TARGET's PARENT", parent);
                console.log("[CLICK] TARGET's PARENT DATASET", parent.dataset);
                sortParameter = parent.dataset.sorting;
/*
            var sortParameterElement = tableClickEvent.target.querySelector("#SortingParameter");
            if (sortParameterElement) { sortParameter = sortParameterElement.innerText; }
            console.log("[CLICK] INNER SORTING PARAMETER is " + sortParameter);
*/

                if (sortParameter) {
                    SelectContent(sortParameter);
                }
            }
        });
    });
    console.log("[TABLE] Click Listener added for a " + tables.length + " tables");

});

selectedSortProperty = "undefined";
function SelectContent(newSelectedSortProperty) {
    var prev = document.querySelector("#" + selectedSortProperty);
    var next = document.querySelector("#" + newSelectedSortProperty);
    if (prev !== null && next !== null) {
         prev.classList.add('Hidden');
        next.classList.remove('Hidden');
        selectedSortProperty = newSelectedSortProperty;
    }
}