// 1. On change show proper rows

// 2. Set same height for Databases (.DbListContainer) and Summary (#SummaryModalContent) Tabs
// https://stackoverflow.com/questions/15615552/get-div-height-with-plain-javascript


function adjustModalTabHeight() {
    var tabDatabases = document.querySelector('#DbListContainer');
    var tabSummary = document.querySelector('#SummaryModalContent');

    console.log("Type of tabDatabases is [" + (typeof tabDatabases) + "]");
    console.log("Type of tabSummary is [" + (typeof tabSummary) + "]");

    if (tabDatabases && tabSummary) {
        console.log("tabSummary", tabSummary);
        console.log("tabDatabases", tabDatabases);
        var heightSummary = tabSummary.getBoundingClientRect().height;
        var heightDatabases = tabDatabases.getBoundingClientRect().height;


        tabDatabases.style.minHeight = Math.floor(heightSummary) + "px";
        console.log("Height Databases=[" + heightDatabases + "], Height Summary=[" + heightSummary + "]");

        
    }

}
window.addEventListener('load', function () {

});