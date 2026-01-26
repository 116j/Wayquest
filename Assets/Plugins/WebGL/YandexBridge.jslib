mergeInto(LibraryManager.library, {
  GetYandexLanguage: function () {
    if (typeof GetYandexLanguage === "function") {
      // Вызываем глобальную функцию из index.html
      var result = GetYandexLanguage(); // Она должна вернуть строку, например "ru"
      // Код для возврата строки в C# (скопируйте из WebAds.jslib, если там есть пример для строк)
      var bufferSize = lengthBytesUTF8(result) + 1;
      var buffer = _malloc(bufferSize);
      stringToUTF8(result, buffer, bufferSize);
      return buffer;
    } else {
      console.warn("JS-функция GetYandexLanguage не определена");
      // Возвращаем строку по умолчанию (например, "en") на случай ошибки
      var defaultLang = "";
      var bufferSize = lengthBytesUTF8(defaultLang) + 1;
      var buffer = _malloc(bufferSize);
      stringToUTF8(defaultLang, buffer, bufferSize);
      return buffer;
    }
  }
});