import type Editor from '@toast-ui/editor'
import { attachmentsApi } from '@/api/attachments'

/**
 * ToastUI's addImageBlobHook only fires for images (paste, drag-drop, the
 * built-in toolbar image button) — there's no equivalent for arbitrary
 * files, so "attach any file" needs a fully custom toolbar button: open a
 * native file picker, upload it, insert a markdown link at the cursor.
 *
 * Returns a cleanup function — call it from the same onBeforeUnmount that
 * destroys the editor. The toolbar button element itself is owned by
 * ToastUI and torn down with editor.destroy(); this only needs to clean
 * up the hidden file input this function creates alongside it.
 */
export function useToastUiFileAttachments(
  editor: Editor,
  getProjectId: () => string,
  onError: (message: string) => void,
): () => void {
  const fileInput = document.createElement('input')
  fileInput.type = 'file'
  fileInput.style.display = 'none'
  document.body.appendChild(fileInput)

  const button = document.createElement('button')
  button.type = 'button'
  button.className = 'toastui-editor-toolbar-icons'
  button.style.backgroundImage = 'none'
  button.innerHTML = '<span class="mdi mdi-paperclip" style="font-size: 18px; line-height: 1;"></span>'

  button.addEventListener('click', () => fileInput.click())

  fileInput.addEventListener('change', () => {
    const file = fileInput.files?.[0]
    // Cleared unconditionally, success or not, so selecting the exact
    // same file twice in a row still fires a second 'change' event —
    // browsers don't fire 'change' for a no-op re-selection otherwise.
    fileInput.value = ''
    if (!file) {
      return
    }

    void uploadAndInsert(file)
  })

  async function uploadAndInsert(file: File) {
    button.disabled = true
    try {
      const attachment = await attachmentsApi.upload(getProjectId(), file)
      const url = attachmentsApi.downloadUrl(attachment.projectId, attachment.id)
      // Image vs. link syntax purely by content type — both render fine
      // read-only in ToastUiViewer.vue (see useAttachmentRendering.ts),
      // but only the image form actually displays inline while still
      // composing, in the editor's own live preview pane.
      const markdown = attachment.contentType.startsWith('image/')
        ? `![${attachment.fileName}](${url})`
        : `[${attachment.fileName}](${url})`
      editor.insertText(markdown)
    } catch {
      onError(`Could not upload '${file.name}'.`)
    } finally {
      button.disabled = false
    }
  }

  editor.insertToolbarItem(
    { groupIndex: -1, itemIndex: -1 },
    { name: 'attachFile', tooltip: 'Attach file', el: button },
  )

  return () => {
    fileInput.remove()
  }
}
