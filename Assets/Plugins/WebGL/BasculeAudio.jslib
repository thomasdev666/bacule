mergeInto(LibraryManager.library, {
  // Safari (iPhone) suspend le contexte audio du navigateur quand il sature, après un appel, un verrouillage, ou
  // quand la page passe en arrière-plan ; Unity ne le relance pas toujours. On le relance à chaque toucher et
  // au retour sur la page. Sans contexte audio actif, relancer la musique ne fait que des « tic ».
  BasculeAudioInit: function () {
    function resume() {
      try {
        if (typeof WEBAudio !== "undefined" && WEBAudio.audioContext && WEBAudio.audioContext.state !== "running") WEBAudio.audioContext.resume();
      } catch (e) {}
    }
    ["touchend", "mousedown", "keydown"].forEach(function (ev) { document.addEventListener(ev, resume, true); });
    document.addEventListener("visibilitychange", function () { if (!document.hidden) resume(); });
    window.addEventListener("pageshow", resume);
  },

  // 1 si le son du navigateur tourne, 0 s'il est suspendu (on attend alors avant de relancer la musique).
  BasculeAudioRunning: function () {
    try {
      if (typeof WEBAudio !== "undefined" && WEBAudio.audioContext) return WEBAudio.audioContext.state === "running" ? 1 : 0;
    } catch (e) {}
    return 1;
  }
});
