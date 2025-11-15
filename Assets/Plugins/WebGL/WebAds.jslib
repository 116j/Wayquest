mergeInto(LibraryManager.library, {
  ShowRewardedAdForRevive: function () {
    if (typeof ShowRewardedAdForRevive === "function") {
      ShowRewardedAdForRevive(); // глобальная функция из index.html
    } else {
      console.warn("JS-функция ShowRewardedAdForRevive не определена");
    }
  },

  ShowRewardedAdForBonus: function () {
    if (typeof ShowRewardedAdForBonus === "function") {
      ShowRewardedAdForBonus(); // глобальная функция из index.html
    } else {
      console.warn("JS-функция ShowRewardedAdForBonus не определена");
    }
  }
});