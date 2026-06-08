function openFullscreen() {
    var elem = document.getElementById("main-body");
    if (!elem) return;
    if (elem.requestFullscreen) {
        elem.requestFullscreen();
    } else if (elem.webkitRequestFullscreen) {
        elem.webkitRequestFullscreen();
    } else if (elem.msRequestFullscreen) {
        elem.msRequestFullscreen();
    }
}

function exitFullscreen() {
    if (document.exitFullscreen) {
        document.exitFullscreen();
    } else if (document.webkitExitFullscreen) {
        document.webkitExitFullscreen();
    } else if (document.msExitFullscreen) {
        document.msExitFullscreen();
    }
}

function isFullscreen() {
    return !!(document.fullscreenElement || document.webkitFullscreenElement || document.msFullscreenElement);
}

function registerFullscreenListener(dotnetHelper) {
    var handler = function () {
        var isFs = !!(document.fullscreenElement || document.webkitFullscreenElement || document.msFullscreenElement);
        var mainBody = document.getElementById('main-body');
        if (mainBody) {
            mainBody.classList.toggle('is-fullscreen', isFs);
        }
        dotnetHelper.invokeMethodAsync('OnFullscreenChanged', isFs);
    };
    document.addEventListener('fullscreenchange', handler);
    document.addEventListener('webkitfullscreenchange', handler);
    document.addEventListener('MSFullscreenChange', handler);
}
