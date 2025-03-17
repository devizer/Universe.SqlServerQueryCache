// data-columns-header-id - cells
// data-for-columns-header-id - checkboxes
window.addEventListener('load',
    function () {

        // var dbInputs = document.querySelectorAll('.InputChooseDb');
        var dbInputs = document.querySelectorAll('.InputChooseDb[data-for-columns-header-id]');

        if (dbInputs) {
            dbInputs.forEach((input) => {
                input.addEventListener('click',
                    () => {
                        var columnsHeaderId = input.getAttribute('data-for-columns-header-id');
                        console.log(
                            "Columns Header clicked: " + columnsHeaderId + " value=[" + input.value + "] "
                            + " checked=[" + input.checked + "]",
                            input);

                        var isChecked = input.checked;

                        var totalCheckedCount = 0;
                        dbInputs.forEach(x => totalCheckedCount += x.checked ? 1 : 0);
                        if (totalCheckedCount > 0 || isChecked) {
                            // at least one column header is checked
                            var cells = document.querySelectorAll('*[data-columns-header-id="' + columnsHeaderId + '"]');
                            console.log("Cells count is " + cells.length + " for Columns Header [" + columnsHeaderId + "] visible=" + isChecked);
                            var act = isChecked
                                ? cell => cell.classList.remove("Hidden")
                                : cell => cell.classList.add("Hidden");

                            cells.forEach(cell => act(cell));
                        }
                        else {
                            // cancel un-check
                            input.checked = true;
                        }




                    });
            });

        }
    });
