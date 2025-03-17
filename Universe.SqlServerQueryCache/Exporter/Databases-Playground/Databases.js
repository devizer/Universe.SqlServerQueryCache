// 1. On change show proper rows

prevCheckedDbId = 0;
window.addEventListener('load',
    function() {

        // var dbInputs = document.querySelectorAll('.InputChooseDb');
        var dbInputs = document.querySelectorAll('.InputChooseDb[data-for-db-id]');

        if (dbInputs) {
            dbInputs.forEach((input) => {
                input.addEventListener('click',
                    () => {
                        var dbId = input.getAttribute('data-for-db-id');
                        console.log("DB Radio clicked: " +
                            dbId +
                            " value=[" +
                            input.value +
                            "] checked=[" +
                            input.checked +
                            "]",
                            input);

                        dbInputs.forEach((otherInput) => {
                            if (otherInput !== input) {
                                otherInput.checked = false;
                            }
                        });

                        var getSelectorForDbId = id => {
                            return id == "0"
                                ? "tr[data-db-id]"
                                : (".DB-Id-" + id);
                        };

                        if (dbId !== prevCheckedDbId) {
                            var prevId = prevCheckedDbId;
                            prevCheckedDbId = dbId;

                            console.log("Filtering queries by DB ID=[" + dbId + "]");
                            setTimeout(() => {

                                    // GET queriesCount
                                    var queriesCount = undefined;
                                    var dbIdNumeric = Math.floor(dbId);
                                    var dbInfo = dbList.find(x => x.DatabaseId === dbIdNumeric);
                                    queriesCount = dbInfo ? dbInfo.QueriesCount : undefined;
                                    console.log("Queries Count = " + queriesCount);
                                    // var nodeSummaryCells = document.querySelectorAll("tr.Metrics > tr:first-child > th:first-child");
                                    var nodeSummaryCells = document.querySelectorAll(".MetricsSummaryHeaderCell");
                                    console.log("Summary Cells Count = " + nodeSummaryCells.length);
                                    if (queriesCount >= 0) {
                                        var summaryText = queriesCount === 0
                                            ? "No Data"
                                            : queriesCount === 1
                                            ? "Summary on 1 query"
                                            : "Summary on " + queriesCount + " queries";

                                        nodeSummaryCells.forEach(x => x.innerHTML = summaryText);
                                    }

                                    var selectorToHide = getSelectorForDbId(prevId);
                                    var toHide = document.querySelectorAll(selectorToHide);
                                    console.log("Hiding rows «" + selectorToHide + "»: " + toHide.length);

                                    var selectorToShow = getSelectorForDbId(dbId);
                                    var toShow = document.querySelectorAll(selectorToShow);
                                    console.log("Showing rows «" + selectorToShow + "»: " + toShow.length);

                                    toHide.forEach(el => el.classList.add("Hidden"));
                                    toShow.forEach(el => el.classList.remove("Hidden"));
                                },
                                1);

                        }
                    });
            });

        }
    });

// 2. Set same height for Databases (#DbListContainer) and Summary (#SummaryModalContent) Tabs
// https://stackoverflow.com/questions/15615552/get-div-height-with-plain-javascript


// On load height is not defined because display: none

// Height for the Tabs
globalHeightSummary = 0;
globalHeightDatabases = 0;
globalHeightColumns = 0;

// 1st Invocation: by handleFloatInfoClick()
// 2nd Invocation: Tab button click (twice)
function adjustModalTabHeight() {
    // return; // because visibility hidden, not display non
    var tabDatabases = document.querySelector('#DbListContainer');
    var tabSummary = document.querySelector('#SummaryModalContent');
    var tabColumns = document.querySelector('#ColumnsChooserModalContent');

    console.log("Type of tabDatabases is [" + (typeof tabDatabases) + "], Type of tabSummary is [" + (typeof tabSummary) + "]");

    if (tabDatabases && tabSummary && tabColumns) {
        // console.log("tabSummary", tabSummary);
        // console.log("tabDatabases", tabDatabases);
        var heightSummary = Math.floor(tabSummary.getBoundingClientRect().height);
        var heightDatabases = Math.floor(tabDatabases.getBoundingClientRect().height);
        var heightColumnsChooser = Math.floor(tabColumns.getBoundingClientRect().height);
        var visibleHeightThreshold = 10;
        if (heightSummary >= visibleHeightThreshold) globalHeightSummary = heightSummary;
        if (heightDatabases >= visibleHeightThreshold) globalHeightDatabases = heightDatabases;
        if (heightColumnsChooser >= visibleHeightThreshold) globalHeightColumns = heightColumnsChooser;


        console.log("Height Summary=[" + globalHeightSummary + "], Height Databases=[" + globalHeightDatabases + "]");

        if (globalHeightSummary) {
            tabDatabases.style.minHeight = globalHeightSummary + "px";
            tabColumns.style.minHeight = globalHeightSummary + "px";
        }

        // TODO: Align Top First
        if (globalHeightDatabases) tabSummary.style.minHeight = globalHeightDatabases + "px";



    }
}
