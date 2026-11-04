import Modal from 'bootstrap/js/dist/modal'
import { useCallback, useEffect, useRef } from 'react'

/**
 * Drives a Bootstrap modal whose markup React renders: attach `ref` to the `.modal` element, then call `show`/`hide`.
 * Only the modal plugin is loaded, not the full Bootstrap bundle.
 */
export function useBootstrapModal<T extends HTMLElement = HTMLDivElement>() {
  const ref = useRef<T>(null)

  const show = useCallback(() => {
    if (ref.current) {
      Modal.getOrCreateInstance(ref.current).show()
    }
  }, [])

  const hide = useCallback(() => {
    if (ref.current) {
      Modal.getInstance(ref.current)?.hide()
    }
  }, [])

  useEffect(() => {
    const element = ref.current
    return () => {
      if (element) {
        Modal.getInstance(element)?.dispose()
      }
    }
  }, [])

  return { ref, show, hide }
}
