import { ref, nextTick } from 'vue';

export function useCamera() {
  const stream = ref(null);
  const ready = ref(false);
  const cameraError = ref("");
  const facing = ref("user"); // "user" | "environment"
  const switching = ref(false);
  const hasMultipleCams = ref(false);
  const showCam = ref(false);

  async function startCamera(videoRef, mode) {
    if (stream.value) {
      stream.value.getTracks().forEach(tr => tr.stop());
    }
    ready.value = false;
    switching.value = true;
    cameraError.value = "";

    try {
      const s = await navigator.mediaDevices.getUserMedia({
        video: { facingMode: mode }
      });
      stream.value = s;
      await nextTick();
      if (videoRef) {
        videoRef.srcObject = s;
        videoRef.onloadedmetadata = () => {
          ready.value = true;
          switching.value = false;
        };
      } else {
        switching.value = false;
      }
    } catch (e) {
      console.error("Camera access failed", e);
      cameraError.value = "camera_error";
      switching.value = false;
    }
  }

  function openCamera(videoRef) {
    showCam.value = true;
    facing.value = "user";
    navigator.mediaDevices?.enumerateDevices().then(devices => {
      hasMultipleCams.value = devices.filter(d => d.kind === "videoinput").length > 1;
    });
    startCamera(videoRef, "user");
  }

  function closeCamera() {
    if (stream.value) {
      stream.value.getTracks().forEach(tr => tr.stop());
      stream.value = null;
    }
    showCam.value = false;
    ready.value = false;
  }

  function flipCamera(videoRef) {
    const next = facing.value === "user" ? "environment" : "user";
    facing.value = next;
    startCamera(videoRef, next);
  }

  function capturePhoto(videoEl, canvasEl) {
    if (!videoEl || !canvasEl) return null;
    canvasEl.width = videoEl.videoWidth;
    canvasEl.height = videoEl.videoHeight;
    const ctx = canvasEl.getContext("2d");

    if (facing.value === "user") {
      ctx.translate(canvasEl.width, 0);
      ctx.scale(-1, 1);
    }
    ctx.drawImage(videoEl, 0, 0);
    const photoData = canvasEl.toDataURL("image/jpeg", 0.82);
    closeCamera();
    return photoData;
  }

  return {
    stream,
    ready,
    cameraError,
    facing,
    switching,
    hasMultipleCams,
    showCam,
    startCamera,
    openCamera,
    closeCamera,
    flipCamera,
    capturePhoto
  };
}
