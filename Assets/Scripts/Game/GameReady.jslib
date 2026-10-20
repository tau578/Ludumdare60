// The browser side of PageReady.cs: tells our page template (Assets/WebGLTemplates/Operator) that the main menu's first
// frame is drawn, so it can lift the loading cover. A page without the hook (another template) is left alone.
mergeInto(LibraryManager.library, {
  OperatorPage_GameReady: function () {
    if (typeof window.operatorGameReady === 'function') window.operatorGameReady();
  }
});
