﻿// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

function CallApiJS(path, token) {
    const remoteResponse = document.getElementById('apiClaims');
    console.info("Starting CallApiJS...")
    console.info("Path: ", path)
    console.info("Token: ", token)
    fetch(path,
    {
        method: 'GET',
        headers: {
            Authorization: 'Bearer ' + token
        }
    }).then(response => {
        if (response.ok) {
            console.info("Response OK");
            response.text().then(text => {
                remoteResponse.innerText = text;
            });
        }
        else {
            console.error("Response Not OK: ", response.status);
            remoteResponse.innerText = 'Not OK status: ' + response.status;
        }
    }).catch((error) => {
        remoteResponse.innerText = 'An error occurred: ' + error;
        console.error("Caught exception: ", error);
    });
    console.info("CallApiJS finished")
}