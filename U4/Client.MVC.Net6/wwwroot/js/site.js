// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

function CallApiJS(token) {
    const remoteResponse = document.getElementById('apiClaims');

    fetch('https://localhost:6001/identity',
    {
        method: 'GET',
        headers: {
            Authorization: `Bearer ${token}`
        }
    }).then(response => {
        if (response.ok) {
            response.text().then(text => {
                remoteResponse.innerText = text;
            });
        }
        else {
            remoteResponse.innerText = 'Not OK status: ' + response.status;
        }
    })
    .catch(() => remoteResponse.innerText = 'An error occurred');
}