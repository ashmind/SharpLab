import $ from 'jquery';

export default function sendCodeAsync(code, options, branchUrl) {
    let url = 'api/compilation';
    if (branchUrl)
        url = branchUrl.replace(/\/?$/, '/') + url;

    const data = Object.assign({ code: code }, options);
    const xhr = $.ajax({
        type: 'POST',
        url:  url,
        datatype: 'application/json',
        contentType: 'application/json',
        data: JSON.stringify(data)
    });
    const promise = xhr.then(
        data => data,
        response => {
            const error = new Error('Request failed.');
            error.response = {
                data: JSON.parse(response.responseText)
            };
            return error;
        }
    );
    promise.abort = () => xhr.abort();
    return promise;
}