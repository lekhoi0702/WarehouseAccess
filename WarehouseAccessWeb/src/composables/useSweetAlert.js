import Swal from 'sweetalert2'

const getGlobalText = (key, fallback) => {
  const value = globalThis?.[key]
  return typeof value === 'string' && value.trim() ? value.trim() : fallback
}

export function useSweetAlert() {
  async function showError(message) {
    await Swal.fire({
      icon: 'error',
      title: getGlobalText('hfSwalErrorTitle', 'Error'),
      text: message || '',
      timer: 1400,
      showConfirmButton: false,
      buttonsStyling: false,
      customClass: {
        container: 'swal2-container-over-modal',
        popup: 'of-swal-popup',
        title: 'of-swal-title',
        htmlContainer: 'of-swal-html',
        actions: 'of-swal-actions',
        confirmButton: 'of-swal-btn of-swal-btn-confirm'
      }
    })
  }

  async function showSuccess(message) {
    await Swal.fire({
      icon: 'success',
      text: message || getGlobalText('hfSwalSuccessText', 'Success'),
      timer: 1200,
      showConfirmButton: false,
      customClass: {
        container: 'swal2-container-over-modal'
      }
    })
  }

  async function showConfirm() {
    const result = await Swal.fire({
      icon: 'question',
      title: 'Confirm?',
      showCancelButton: true,
      confirmButtonText: getGlobalText('hfSwalOkBtn', 'OK'),
      cancelButtonText: 'Cancel',
      buttonsStyling: false,
      customClass: {
        container: 'swal2-container-over-modal',
        popup: 'of-swal-popup',
        title: 'of-swal-title',
        htmlContainer: 'of-swal-html',
        actions: 'of-swal-actions',
        confirmButton: 'of-swal-btn of-swal-btn-confirm',
        cancelButton: 'of-swal-btn'
      }
    })
    return !!result.isConfirmed
  }

  return {
    showError,
    showSuccess,
    showConfirm
  }
}

