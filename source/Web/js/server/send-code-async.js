import $ from 'jquery';

export default function sendCodeAsync(code, options, branchUrl) {
    let url = 'api/compilation';
    if (branchUrl)
        url = branchUrl.replace(/\/?$/, '/') + url;

    const data = Object.assign({ code: code }, options);
    return $.post(url, data);
}