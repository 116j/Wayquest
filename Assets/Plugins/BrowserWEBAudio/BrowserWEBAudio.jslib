var BrowserDebugLib = {
  InitBrowserWEBAudio: function () {

    function recreateAudioContext() {
      try {
        if (WEBAudio.audioContext) {
          WEBAudio.audioContext.close().catch(function (err) {
            console.warn("Error closing AudioContext: " + err.message);
          });
        }

        // Create new AudioContext
        WEBAudio.audioContext = new AudioContext();

        // Verify the state of the new AudioContext
        if (WEBAudio.audioContext.state === "running") {
          console.warn("AudioContext recreated successfully");
          return true;
        } else {
          console.warn("AudioContext created but not running. State: " + WEBAudio.audioContext.state);
          // Attempt to resume immediately if possible
          WEBAudio.audioContext.resume().then(function () {
            console.warn("AudioContext resumed after creation");
          }).catch(function (err) {
            console.warn("Failed to resume new AudioContext: " + err.message);
          });
          return true;
        }
      } catch (error) {
        console.warn("Failed to recreate AudioContext: " + error.message);
        return false;
      }
    }

    // Handle visibility change
    document.addEventListener("visibilitychange", function () {
      if (document.visibilityState === "hidden") {
        // Suspend
        if (typeof WEBAudio !== "undefined" && WEBAudio.audioContext) {
          if (WEBAudio.audioContext.state === "running") {
            WEBAudio.audioContext
              .suspend()
              .then(function () {
                console.warn("Audio suspended on hidden");
              })
              .catch(function (err) {
                console.warn("Error suspending AudioContext: " + err.message);
              });
          } else {
            console.warn("AudioContext not running, skip suspend. State: " + WEBAudio.audioContext.state);
          }
        }
      } else {
        // Resume
        if (typeof WEBAudio !== "undefined" && WEBAudio.audioContext) {
          if (WEBAudio.audioContext.state === "closed") {
            console.warn("AudioContext is closed, recreating");
            recreateAudioContext();
          } else {
            // Delay resume to ensure audio device is ready
            setTimeout(function () {
              WEBAudio.audioContext
                .resume()
                .then(function () {
                  console.warn("Audio resumed on visible");
                })
                .catch(function (err) {
                  console.warn("Resume failed: " + err.message + " - Attempting to recreate AudioContext");
                  if (recreateAudioContext()) {
                    console.warn("AudioContext recreated successfully");
                  } else {
                    console.warn("AudioContext recreation failed");
                  }
                });
            }, 100); // Small delay to allow audio device to stabilize
          }
        }
      }
    });

    // Touch event
    document.addEventListener("touchstart", function (event) {
      if (typeof WEBAudio === "undefined") {
        console.warn("WEBAudio undefined on touchstart");
        return;
      }

      if (!WEBAudio.audioContext || WEBAudio.audioContext.state === "closed") {
        console.warn("AudioContext missing or closed, recreating on touchstart");
        recreateAudioContext();
      } else if (WEBAudio.audioContext.state === "suspended") {
        WEBAudio.audioContext
          .resume()
          .then(function () {
            console.warn("Audio resumed on touchstart");
          })
          .catch(function (err) {
            console.warn("Touch resume failed: " + err.message + " - Recreating AudioContext");
            recreateAudioContext();
          });
      }
    }, { once: false });
  },
};

mergeInto(LibraryManager.library, BrowserDebugLib);