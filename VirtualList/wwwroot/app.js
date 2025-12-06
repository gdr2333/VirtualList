window.WriteCookie = {
    WriteCookie: function (name, value, expires) {
        var expireDate = new Date(expires);
        document.cookie = name + "=" + value + "; expires = " + expireDate.toUTCString() + "; path=/";
    }
}