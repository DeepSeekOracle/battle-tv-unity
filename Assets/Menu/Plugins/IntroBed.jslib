mergeInto(LibraryManager.library, {
  IntroBedPlay: function () {
    if (!window.battleTvStartIntro) return;
    window.battleTvStartIntro();
  },
  IntroBedSetMuted: function (muted) {
    var bed = document.getElementById("intro-bed");
    if (!bed) return;
    bed.muted = muted !== 0;
    if (muted === 0 && window.battleTvStartIntro) window.battleTvStartIntro();
  }
});
