mergeInto(LibraryManager.library, {
    GetUserAgent: function() {
        var userAgent = navigator.userAgent;
        var bufferSize = lengthBytesUTF8(userAgent) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(userAgent, buffer, bufferSize);
        console.log(`userAgent: ${userAgent}`);
        return buffer;
    },

    IsMobileOS: function() {
        // Check for mobile operating systems using multiple detection methods
        var userAgent = navigator.userAgent.toLowerCase();
        var platform = navigator.platform.toLowerCase();
        
        // Check user agent for mobile indicators
        var isMobileUA = /android|webos|iphone|ipad|ipod|blackberry|iemobile|opera mini|mobile/i.test(userAgent);
        
        // Check platform for mobile indicators
        var isMobilePlatform = /iphone|ipad|ipod|android|blackberry|mini|windows\sce|palm/i.test(platform);
        
        // Check for touch support (additional indicator)
        var hasTouch = 'ontouchstart' in window || navigator.maxTouchPoints > 0;
        
        // Check for specific mobile browsers
        var isMobileBrowser = /mobile|tablet|android|ios/i.test(userAgent);
        
        // Additional checks for specific mobile OS
        var isIOS = /ipad|iphone|ipod/.test(userAgent);
        var isAndroid = /android/.test(userAgent);
        var isWindowsMobile = /windows phone|iemobile|wpdesktop/.test(userAgent);
        var isBlackberry = /blackberry|rim tablet os/.test(userAgent);

        // log all info in one log message
        console.log(`userAgent: ${userAgent}, platform: ${platform}, isMobileUA: ${isMobileUA}, isMobilePlatform: ${isMobilePlatform}, hasTouch: ${hasTouch}, isMobileBrowser: ${isMobileBrowser}, isIOS: ${isIOS}, isAndroid: ${isAndroid}, isWindowsMobile: ${isWindowsMobile}, isBlackberry: ${isBlackberry}`);
        
        // Return true if any mobile indicator is found
        return isMobileUA || isMobilePlatform || isIOS || isAndroid || isWindowsMobile || isBlackberry;
    }
}); 