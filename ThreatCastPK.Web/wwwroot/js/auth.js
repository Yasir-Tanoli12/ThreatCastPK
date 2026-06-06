// ThreatCastPK.Web/wwwroot/js/auth.js
// Handles JWT storage in localStorage via JS Interop
// Called from AuthService.cs using IJSRuntime

window.tcpkAuth = {

    // Store the JWT token after successful login
    setToken: function (token) {
        localStorage.setItem('tcpk_token', token);
    },

    // Read the token — returns null if not present
    getToken: function () {
        return localStorage.getItem('tcpk_token');
    },

    // Remove token on logout
    removeToken: function () {
        localStorage.removeItem('tcpk_token');
    },

    // Store user info separately so we don't decode JWT every render
    setUserInfo: function (userId, username, role) {
        localStorage.setItem('tcpk_userId', userId);
        localStorage.setItem('tcpk_username', username);
        localStorage.setItem('tcpk_role', role);
    },

    getUserInfo: function () {
        return {
            userId: localStorage.getItem('tcpk_userId'),
            username: localStorage.getItem('tcpk_username'),
            role: localStorage.getItem('tcpk_role')
        };
    },

    removeUserInfo: function () {
        localStorage.removeItem('tcpk_userId');
        localStorage.removeItem('tcpk_username');
        localStorage.removeItem('tcpk_role');
    },

    // Called on logout — clears everything
    clearAll: function () {
        localStorage.removeItem('tcpk_token');
        localStorage.removeItem('tcpk_userId');
        localStorage.removeItem('tcpk_username');
        localStorage.removeItem('tcpk_role');
    },

    // Returns true if a token exists (does not validate expiry — server handles that)
    isLoggedIn: function () {
        return localStorage.getItem('tcpk_token') !== null;
    }
};