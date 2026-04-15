// Modal show/hide functions for status feedback
// jQuery is loaded via CDN in Site.Master

function showModal(message) {
    document.getElementById("statusText").innerText = message;
    document.getElementById("statusModal").style.display = "flex";
}

function hideModal() {
    document.getElementById("statusModal").style.display = "none";
}

// Auto-hide the modal after a full postback completes
// This handles the case where the server-side event finishes and the page reloads
$(document).ready(function () {
    // If the page loaded normally (not via AJAX), hide any visible modal
    hideModal();

    // For ASP.NET AJAX partial postbacks, hide the modal when the async request ends
    if (typeof Sys !== "undefined" && Sys.WebForms && Sys.WebForms.PageRequestManager) {
        var prm = Sys.WebForms.PageRequestManager.getInstance();
        prm.add_endRequest(function () {
            hideModal();
        });
    }
});