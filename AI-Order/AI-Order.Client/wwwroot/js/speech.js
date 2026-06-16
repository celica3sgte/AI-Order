// AI-Order Speech Interop
// Handles Web Speech API for voice input and SpeechSynthesis for voice output

window.SpeechInterop = {

    recognition: null,
    dotnetRef: null,

    isSupported: function () {
        return 'SpeechRecognition' in window || 'webkitSpeechRecognition' in window;
    },

    startListening: function (dotnetRef, lang) {
        if (!window.SpeechInterop.isSupported()) {
            dotnetRef.invokeMethodAsync('OnSpeechError', 'Speech recognition is not supported in this browser. Please use Chrome or Edge.');
            return;
        }

        const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
        const recognition = new SpeechRecognition();

        recognition.lang = lang || 'en-US';
        recognition.interimResults = true;
        recognition.maxAlternatives = 1;
        recognition.continuous = false;

        recognition.onstart = () => {
            dotnetRef.invokeMethodAsync('OnListeningStarted');
        };

        recognition.onresult = (event) => {
            const transcript = Array.from(event.results)
                .map(r => r[0].transcript)
                .join('');
            const isFinal = event.results[event.results.length - 1].isFinal;
            dotnetRef.invokeMethodAsync('OnSpeechResult', transcript, isFinal);
        };

        recognition.onerror = (event) => {
            dotnetRef.invokeMethodAsync('OnSpeechError', event.error);
        };

        recognition.onend = () => {
            dotnetRef.invokeMethodAsync('OnListeningEnded');
        };

        window.SpeechInterop.recognition = recognition;
        window.SpeechInterop.dotnetRef = dotnetRef;
        recognition.start();
    },

    stopListening: function () {
        if (window.SpeechInterop.recognition) {
            window.SpeechInterop.recognition.stop();
            window.SpeechInterop.recognition = null;
        }
    },

    speak: function (text, lang, rate = 1.0, pitch = 1.0) {
        if (!('speechSynthesis' in window)) return;
        window.speechSynthesis.cancel();
        const utterance = new SpeechSynthesisUtterance(text);
        utterance.rate = rate;
        utterance.pitch = pitch;
        utterance.lang = lang || 'en-US';
        window.speechSynthesis.speak(utterance);
    },

    stopSpeaking: function () {
        if ('speechSynthesis' in window) {
            window.speechSynthesis.cancel();
        }
    }
};
