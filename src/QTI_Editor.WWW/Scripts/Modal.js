function showModal(message) {
    document.getElementById("statusText").innerText = message;
    document.getElementById("statusModal").style.display = "flex";
}

function hideModal() {
    document.getElementById("statusModal").style.display = "none";
}