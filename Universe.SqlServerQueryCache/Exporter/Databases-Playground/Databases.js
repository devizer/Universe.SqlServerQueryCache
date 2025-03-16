// 1. On change show proper rows

prevCheckedDbId = 0;
window.addEventListener('load',
    function() {

        var dbInputs = document.querySelectorAll('.InputChooseDb');
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
function adjustModalTabHeight() {
    var tabDatabases = document.querySelector('#DbListContainer');
    var tabSummary = document.querySelector('#SummaryModalContent');

    console.log("Type of tabDatabases is [" + (typeof tabDatabases) + "]");
    console.log("Type of tabSummary is [" + (typeof tabSummary) + "]");

    if (tabDatabases && tabSummary) {
        // console.log("tabSummary", tabSummary);
        // console.log("tabDatabases", tabDatabases);
        var heightSummary = tabSummary.getBoundingClientRect().height;
        var heightDatabases = tabDatabases.getBoundingClientRect().height;

        console.log("Height Databases=[" + heightDatabases + "], Height Summary=[" + heightSummary + "]");
        var hs = Math.floor(heightSummary);
        if (hs > 100) tabDatabases.style.minHeight = hs + "px";
    }
}
