import { ref, reactive, computed, nextTick, onMounted, onBeforeUnmount, watch } from 'vue'
import { FilesetResolver, FaceDetector } from '@mediapipe/tasks-vision'
import { useCamera } from './useCamera'
import { useAudio } from './useAudio'
import { useToast } from './useToast'
import { useRecords } from './useRecords'
import { useAuthState } from '../stores/auth.store'

export function useCheckInFlow(options = {}) {
  const { onSuccess, notify } = options
  const authState = useAuthState()

  const { playBeep } = useAudio()
  const { showToast } = useToast()
  const {
    lookupCard,
    submitCheckIn,
    submitCheckOut,
    contactDeptItems,
    purposeItems,
    userTypeItems,
    loadContactDeptsCrud,
    loadPurposesCrud,
    loadUserTypesCrud
  } = useRecords()

  const {
    stream,
    ready: camReady,
    cameraError,
    facing,
    switching,
    hasMultipleCams,
    showCam,
    openCamera,
    closeCamera,
    flipCamera,
    capturePhoto
  } = useCamera()

  const notifyUser = (message, type = 'info') => {
    if (typeof notify === 'function') {
      notify(message, type)
      return
    }
    showToast(message, type)
  }

  const step = ref(1)
  const checkInCardNumber = ref('')
  const cardInputRef = ref(null)
  const lookupLoading = ref(false)
  const lookupMessage = ref('')
  const hasCardLookupResult = ref(false)
  const submitLoading = ref(false)
  const fieldsLockedByGuestData = ref(false)
  const lookupNote = ref('')

  const formState = reactive({
    cardNumber: '',
    userCode: '',
    userTypeId: '',
    userTypeName: '',
    fullName: '',
    contactPerson: '',
    company: '',
    deptCode: '',
    deptName: '',
    contactDept: '',
    purpose: '',
    photo: ''
  })

  const errors = reactive({
    userCode: '',
    fullName: '',
    userTypeId: ''
  })

  const videoRef = ref(null)
  const canvasRef = ref(null)
  const autoCaptureSupported = ref(true)
  const autoCaptureActive = ref(false)
  const autoCaptureStatus = ref('idle')

  let mediaPipeFaceDetector = null
  let mediaPipeInitializing = false
  let autoCaptureTimer = null
  let stableFrameCount = 0

  onMounted(() => {
    // Ensure dropdown master data is available in both dashboard modal and mobile route.
    loadContactDeptsCrud()
    loadPurposesCrud()
    loadUserTypesCrud()

    nextTick(() => {
      cardInputRef.value?.focus()
    })

    formState.contactPerson = authState.currentUser?.fullName || ''
  })

  onBeforeUnmount(() => {
    stopAutoCaptureLoop()
    if (mediaPipeFaceDetector?.close) {
      mediaPipeFaceDetector.close()
    }
    mediaPipeFaceDetector = null
  })

  watch(step, (newStep) => {
    if (newStep === 2 && !formState.photo && !showCam.value) {
      triggerCamera()
    }
    if (newStep !== 2) {
      stopAutoCaptureLoop()
    }
  })

  watch(showCam, (isVisible) => {
    if (isVisible && step.value === 2 && !formState.photo) {
      startAutoCaptureLoop()
      return
    }
    stopAutoCaptureLoop()
  })

  watch(() => formState.photo, (photoValue) => {
    if (photoValue) {
      stopAutoCaptureLoop()
    }
  })

  const scannerState = computed(() => {
    if (lookupLoading.value) return 'detecting'
    if (hasCardLookupResult.value) return 'found'
    return 'waiting'
  })

  async function handleCardLookup() {
    const cardNum = checkInCardNumber.value.trim()
    if (!cardNum) {
      notifyUser('Please enter a card number', 'warning')
      return
    }

    lookupLoading.value = true
    lookupMessage.value = ''
    hasCardLookupResult.value = false
    fieldsLockedByGuestData.value = false
    lookupNote.value = ''

    try {
      const res = await lookupCard(cardNum)
      if (res?.success && res.data) {
        const isInside = res.data.isInside ?? res.data.IsInside ?? false
        if (isInside) {
          const openLogId = res.data.openLogId || res.data.OpenLogId
          if (!openLogId) {
            lookupMessage.value = 'Open check-in record not found for checkout.'
            hasCardLookupResult.value = false
            notifyUser(lookupMessage.value, 'error')
            checkInCardNumber.value = ''
            nextTick(() => {
              cardInputRef.value?.focus()
            })
            return
          }

          const checkoutResponse = await submitCheckOut(openLogId)
          if (checkoutResponse?.success) {
            playBeep(true)
            notifyUser('Check-out successful.', 'success')
          } else {
            playBeep(false)
            notifyUser(checkoutResponse?.message || 'Check-out failed.', 'error')
          }

          hasCardLookupResult.value = false
          checkInCardNumber.value = ''
          nextTick(() => {
            cardInputRef.value?.focus()
          })
          return
        }

        playBeep(true)
        hasCardLookupResult.value = true
        formState.cardNumber = res.data.cardNumber || cardNum
        formState.userCode = res.data.userCode || cardNum
        formState.userTypeId = res.data.userTypeId || ''
        formState.userTypeName = res.data.userTypeName || ''
        formState.fullName = res.data.fullName || ''
        formState.contactPerson = authState.currentUser?.fullName || ''
        formState.company = res.data.company || ''
        formState.deptCode = res.data.deptCode || ''
        formState.deptName = res.data.deptName || ''
        formState.contactDept = res.data.contactDept || ''
        formState.purpose = res.data.purpose || ''
        lookupNote.value = ''
        fieldsLockedByGuestData.value = !!res.data.isExternalGuestDataApplied
        lookupMessage.value = ''

        if (!fieldsLockedByGuestData.value && contactDeptItems.value.length > 0 && !formState.contactDept) {
          formState.contactDept = contactDeptItems.value[0].contactDeptName
        }
        if (!fieldsLockedByGuestData.value && purposeItems.value.length > 0 && !formState.purpose) {
          formState.purpose = purposeItems.value[0].purposeName
        }
      } else {
        playBeep(false)
        lookupMessage.value = res?.message || 'Card not found'
        notifyUser(lookupMessage.value, 'error')
        checkInCardNumber.value = ''
        nextTick(() => {
          cardInputRef.value?.focus()
        })
      }
    } catch {
      playBeep(false)
      lookupMessage.value = 'System connection error.'
      notifyUser(lookupMessage.value, 'error')
      checkInCardNumber.value = ''
      nextTick(() => {
        cardInputRef.value?.focus()
      })
    } finally {
      lookupLoading.value = false
    }
  }

  function validateStepOne() {
    if (!hasCardLookupResult.value) {
      lookupMessage.value = 'Please scan/check card first.'
      return false
    }
    errors.userCode = formState.userCode.trim() ? '' : 'User Code is required'
    errors.fullName = formState.fullName.trim() ? '' : 'Full Name is required'
    // External users must explicitly select user type.
    errors.userTypeId = fieldsLockedByGuestData.value && !formState.userTypeId ? 'User Type is required' : ''
    return !errors.userCode && !errors.fullName && !errors.userTypeId
  }

  function nextStep() {
    if (validateStepOne()) {
      step.value = 2
    }
  }

  function prevStep() {
    if (showCam.value) {
      closeCamera()
    }
    step.value = 1
  }

  function resetCheckInFlow() {
    stopAutoCaptureLoop()
    if (showCam.value) {
      closeCamera()
    }

    step.value = 1
    checkInCardNumber.value = ''
    lookupLoading.value = false
    lookupMessage.value = ''
    hasCardLookupResult.value = false
    submitLoading.value = false
    fieldsLockedByGuestData.value = false
    lookupNote.value = ''

    formState.cardNumber = ''
    formState.userCode = ''
    formState.userTypeId = ''
    formState.userTypeName = ''
    formState.fullName = ''
    formState.contactPerson = authState.currentUser?.fullName || ''
    formState.company = ''
    formState.deptCode = ''
    formState.deptName = ''
    formState.contactDept = ''
    formState.purpose = ''
    formState.photo = ''

    errors.userCode = ''
    errors.fullName = ''
    errors.userTypeId = ''

    nextTick(() => {
      cardInputRef.value?.focus()
    })
  }

  function triggerCamera() {
    openCamera(null)
    nextTick(() => {
      openCamera(videoRef.value)
    })
  }

  async function triggerCapture() {
    const photoBase64Url = capturePhoto(videoRef.value, canvasRef.value)
    if (photoBase64Url) {
      playBeep(true)
      const marker = 'base64,'
      const index = photoBase64Url.indexOf(marker)
      formState.photo = index >= 0 ? photoBase64Url.substring(index + marker.length) : photoBase64Url
      // Auto-submit after successful face capture so operator does not need to tap submit again.
      await handleCheckInSubmit()
    } else {
      notifyUser('Capture failed. Please try again.', 'error')
    }
  }

  async function ensureFaceDetector() {
    if (!autoCaptureSupported.value || mediaPipeFaceDetector || mediaPipeInitializing) {
      return
    }

    mediaPipeInitializing = true
    try {
      const vision = await FilesetResolver.forVisionTasks(
        'https://cdn.jsdelivr.net/npm/@mediapipe/tasks-vision@latest/wasm'
      )

      mediaPipeFaceDetector = await FaceDetector.createFromOptions(vision, {
        baseOptions: {
          modelAssetPath:
            'https://storage.googleapis.com/mediapipe-models/face_detector/blaze_face_short_range/float16/latest/blaze_face_short_range.tflite'
        },
        runningMode: 'VIDEO',
        minDetectionConfidence: 0.6
      })
    } catch {
      autoCaptureSupported.value = false
      autoCaptureStatus.value = 'unsupported'
    } finally {
      mediaPipeInitializing = false
    }
  }

  function resolveFaceBox(face) {
    const box = face?.boundingBox || face
    if (!box) return null

    // MediaPipe detection box usually uses originX/originY/width/height.
    const x = Number.isFinite(box.x) ? box.x : box.originX
    const y = Number.isFinite(box.y) ? box.y : box.originY
    const width = box.width
    const height = box.height

    if (![x, y, width, height].every(Number.isFinite)) {
      return null
    }

    return { x, y, width, height }
  }

  function isFaceStable(face, videoElement) {
    const box = resolveFaceBox(face)
    if (!box || !videoElement?.videoWidth || !videoElement?.videoHeight) {
      return false
    }

    const frameWidth = videoElement.videoWidth
    const frameHeight = videoElement.videoHeight
    const faceAreaRatio = (box.width * box.height) / (frameWidth * frameHeight)
    if (faceAreaRatio < 0.04 || faceAreaRatio > 0.72) {
      return false
    }

    const faceCenterX = box.x + box.width / 2
    const faceCenterY = box.y + box.height / 2
    const offsetX = Math.abs(faceCenterX - frameWidth / 2) / frameWidth
    const offsetY = Math.abs(faceCenterY - frameHeight / 2) / frameHeight

    return offsetX < 0.28 && offsetY < 0.28
  }

  async function detectAndMaybeCapture() {
    if (!autoCaptureSupported.value || !showCam.value || formState.photo || step.value !== 2) {
      return
    }

    const videoElement = videoRef.value
    if (!videoElement || videoElement.readyState < 2) {
      autoCaptureStatus.value = 'warming'
      stableFrameCount = 0
      return
    }

    try {
      await ensureFaceDetector()
      if (!mediaPipeFaceDetector) {
        autoCaptureStatus.value = 'unsupported'
        return
      }

      const detectionResult = mediaPipeFaceDetector.detectForVideo(videoElement, performance.now())
      const faces = detectionResult?.detections || []
      if (!faces.length) {
        stableFrameCount = 0
        autoCaptureStatus.value = 'no-face'
        return
      }

      if (faces.length > 1) {
        stableFrameCount = 0
        autoCaptureStatus.value = 'multi-face'
        return
      }

      if (!isFaceStable(faces[0], videoElement)) {
        stableFrameCount = 0
        autoCaptureStatus.value = 'face-adjust'
        return
      }

      stableFrameCount += 1
      autoCaptureStatus.value = 'stabilizing'

      if (stableFrameCount >= 4) {
        autoCaptureStatus.value = 'capturing'
        triggerCapture()
      }
    } catch {
      stableFrameCount = 0
      autoCaptureStatus.value = 'unsupported'
      stopAutoCaptureLoop()
    }
  }

  function startAutoCaptureLoop() {
    if (!autoCaptureSupported.value || autoCaptureTimer) {
      return
    }
    stableFrameCount = 0
    autoCaptureActive.value = true
    autoCaptureStatus.value = 'warming'
    autoCaptureTimer = window.setInterval(() => {
      detectAndMaybeCapture()
    }, 300)
  }

  function stopAutoCaptureLoop() {
    autoCaptureActive.value = false
    stableFrameCount = 0
    if (autoCaptureTimer) {
      clearInterval(autoCaptureTimer)
      autoCaptureTimer = null
    }
    if (autoCaptureStatus.value !== 'unsupported') {
      autoCaptureStatus.value = 'idle'
    }
  }

  async function handleCheckInSubmit() {
    if (!validateStepOne()) {
      step.value = 1
      return false
    }

    submitLoading.value = true
    try {
      const payload = {
        cardNumber: formState.cardNumber ? formState.cardNumber.trim() : null,
        userCode: formState.userCode.trim(),
        userTypeId: formState.userTypeId ? String(formState.userTypeId).trim() : null,
        fullName: formState.fullName.trim(),
        deptCode: formState.deptCode ? formState.deptCode.trim() : null,
        contactPerson: formState.contactPerson ? formState.contactPerson.trim() : null,
        contactDept: formState.contactDept ? formState.contactDept.trim() : null,
        purpose: formState.purpose ? formState.purpose.trim() : null,
        photo: formState.photo || null
      }

      const res = await submitCheckIn(payload)
      if (res?.success) {
        playBeep(true)
        await onSuccess?.(res)
        return true
      }

      playBeep(false)
      notifyUser(res?.message || 'CheckIn submission failed.', 'error')
      return false
    } catch {
      playBeep(false)
      notifyUser('System error submitting check-in.', 'error')
      return false
    } finally {
      submitLoading.value = false
    }
  }

  return {
    stream,
    camReady,
    cameraError,
    facing,
    switching,
    hasMultipleCams,
    showCam,
    closeCamera,
    flipCamera,
    step,
    checkInCardNumber,
    cardInputRef,
    lookupLoading,
    lookupMessage,
    hasCardLookupResult,
    submitLoading,
    fieldsLockedByGuestData,
    lookupNote,
    formState,
    errors,
    videoRef,
    canvasRef,
    scannerState,
    autoCaptureSupported,
    autoCaptureActive,
    autoCaptureStatus,
    contactDeptItems,
    purposeItems,
    userTypeItems,
    handleCardLookup,
    nextStep,
    prevStep,
    resetCheckInFlow,
    triggerCamera,
    triggerCapture,
    handleCheckInSubmit
  }
}
