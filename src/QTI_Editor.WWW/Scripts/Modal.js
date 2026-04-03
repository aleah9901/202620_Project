$(function () {

    $("#saveBtn").on("click", function () {

        showModal("Saving...");

        $.ajax({
            url: "/api/save",
            type: "POST",
            data: {
                value: $("#inputField").val()
            },
            success: function (response) {
                showModal("Saved successfully!");
            },
            error: function () {
                showModal("Error saving data");
            }
        });

    });

});
