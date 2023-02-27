// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

function CallApiJS(host, token) {
    const remoteResponse = document.getElementById('apiResult');
    console.info("Starting CallApiJS...")
    console.info("Host: ", host)
    console.info("Token: ", token)
    fetch(host + '/identity',
    {
        method: 'GET',
        //mode: 'cors',
        //credentials: 'include',
        //referrerPolicy: 'origin',
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

function SimpleRequestUsingFetch(host) {
    const remoteResponse = document.getElementById('apiResult');
    console.info("Starting SimpleRequestUsingFetch...")
    console.info("Host: ", host)
    fetch(host + '/open',
    {
        headers: {
            'origin': host
        },
        method: 'GET',
    }).then(response => {
        if (response.ok) {
            console.info("Response OK");
            response.text().then(text => {
                remoteResponse.innerText = text;
            });
            console.info("HEADERS");
            console.info("Access-Control-Allow-Origin: ", response.headers.get('Access-Control-Allow-Origin'));
            response.headers.forEach(function(val, key) { console.log(key + ' -> ' + val); });
        }
        else {
            console.error("Response Not OK: ", response.status);
            remoteResponse.innerText = 'Not OK status: ' + response.status;
        }
    }).catch((error) => {
        remoteResponse.innerText = 'An error occurred: ' + error;
        console.error("Caught exception: ", error);
    });
    console.info("SimpleRequestUsingFetch finished")
}