mergeInto(LibraryManager.library, {
  // Enregistre le système de fichiers du jeu dans le navigateur (IndexedDB). Sans cet appel, la sauvegarde
  // reste en mémoire et disparaît à la fermeture de la page.
  BasculeSyncFs: function () {
    if (typeof FS !== 'undefined' && FS.syncfs) {
      FS.syncfs(false, function (err) { if (err) console.warn('Bascule : sauvegarde non enregistrée', err); });
    }
  }
});
