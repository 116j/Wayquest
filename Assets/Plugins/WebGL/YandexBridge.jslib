mergeInto(LibraryManager.library, {
  GetYandexLanguage: function () {
    if (typeof GetYandexLanguage === "function") {
      var result = GetYandexLanguage();
      var bufferSize = lengthBytesUTF8(result) + 1;
      var buffer = _malloc(bufferSize);
      stringToUTF8(result, buffer, bufferSize);
      return buffer;
    } else {
      console.warn("JS-С„СѓРЅРєС†РёСЏ GetYandexLanguage РЅРµ РѕРїСЂРµРґРµР»РµРЅР°");
      var defaultLang = "";
      var bufferSize = lengthBytesUTF8(defaultLang) + 1;
      var buffer = _malloc(bufferSize);
      stringToUTF8(defaultLang, buffer, bufferSize);
      return buffer;
    }
  },
  SetGameReady: function () {
    if (typeof SetGameReady === "function") {
 	 SetGameReady();
    } else {
      console.warn("JS-функция SetGameReady не определена");
    }
  },
  SetGameStart: function () {
    if (typeof SetGameStart === "function") {
 	 SetGameReady();
    } else {
      console.warn("JS-функция SetGameStartне определена");
    }
  },
  SetGameStop: function () {
    if (typeof SetGameStop === "function") {
 	 SetGameReady();
    } else {
      console.warn("JS-функция SetGameStop не определена");
    }
  }
});