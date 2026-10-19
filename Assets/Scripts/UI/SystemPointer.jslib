// The browser side of SystemPointer.cs (9b): hides the system pointer with a transparent 1x1 image, not cursor: none,
// which Chrome on some Mac displays shows again on every mouse move. Unity's player writes "default" to the canvas
// while it starts up, so a hidden pointer is hidden again if anything else rewrites the canvas cursor.
mergeInto(LibraryManager.library, {
  SystemPointer_SetShown: function (shown) {
    var canvas = Module.canvas;
    var state = Module.systemPointer;
    if (!state) {
      state = Module.systemPointer = {
        hidden: false,
        image: 'url(data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAAC0lEQVR4nGNgAAIAAAUAAXpeqz8AAAAASUVORK5CYII=) 0 0, auto'
      };
      new MutationObserver(function () {
        if (state.hidden && canvas.style.cursor.indexOf('data:image/png') < 0) canvas.style.cursor = state.image;
      }).observe(canvas, { attributes: true, attributeFilter: ['style'] });
    }
    state.hidden = !shown;
    canvas.style.cursor = shown ? 'default' : state.image;
  }
});
