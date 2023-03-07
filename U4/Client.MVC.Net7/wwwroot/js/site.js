// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

function RequestUsingAuth(host, token) {
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
            response.json().then(text => {
                remoteResponse.innerText = JSON.stringify(text, null, 2);;
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
    const remoteResponse = document.getElementById('simpleRequestFetchResponse');
    console.info("Starting SimpleRequestUsingFetch...")
    console.info("Host: ", host)
    fetch(host + '/open',
    {
        headers: {
            //'origin': host, // looks like the browser adds this automatically (all except Firefox)
            'x-arve': 'just testing'
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

function SimpleRequestUsingXhr(host) {
    const remoteResponse = document.getElementById('simpleRequestXhrResponse');
    console.info("Starting SimpleRequestUsingXhr...")
    console.info("Host: ", host)
    var xhr = new XMLHttpRequest();
    xhr.open('GET', host + '/open');
    xhr.setRequestHeader('x-arve', 'just testing');
    xhr.onreadystatechange = () => {
        if (xhr.readyState === XMLHttpRequest.HEADERS_RECEIVED) {
            const headers = xhr.getAllResponseHeaders().trim().split(/[\r\n]+/);
            console.info('Number of headers: ' + headers.length);
            headers.forEach((line) => {
                const parts = line.split(': ');
                const header = parts.shift();
                const value = parts.join(': ');
                console.debug(`${header}: ${value}`);
            });
        }
    }
    xhr.onload = function (e) {
        if (xhr.readyState === XMLHttpRequest.DONE) {
            if (xhr.status === 200) {
                console.info(`redystate is ${xhr.readystate}`)
                console.info("Response OK");
                remoteResponse.innerText = JSON.parse(xhr.responseText);
            } else {
                console.error(xhr.statusText);
            }
        }
        else {
            console.info('some shit happened')
            console.info('redystate is ' + xhr.readystate)
        }
    };
    xhr.onerror = function (e) {
        remoteResponse.innerText = 'An error occurred: ' + xhr.statusText;
        console.error("Caught exception: ", xhr.statusText);
    };
    xhr.send()
    console.info("SimpleRequestUsingXhr finished")
}
