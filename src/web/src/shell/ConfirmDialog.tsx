import { forwardRef, useId } from 'react'
import { useTranslation } from 'react-i18next'

interface ConfirmDialogProps {
  title: string
  body: string
  confirmLabel: string
  onConfirm(): void
}

/** The markup of a Bootstrap confirmation modal; open and close it with `useBootstrapModal`. */
export const ConfirmDialog = forwardRef<HTMLDivElement, ConfirmDialogProps>(function ConfirmDialog(
  { title, body, confirmLabel, onConfirm },
  ref,
) {
  const { t } = useTranslation()
  const titleId = useId()
  return (
    <div className="modal fade" tabIndex={-1} ref={ref} aria-labelledby={titleId} aria-hidden="true">
      <div className="modal-dialog modal-dialog-centered">
        <div className="modal-content">
          <div className="modal-header">
            <h2 className="modal-title fs-5" id={titleId}>
              {title}
            </h2>
            <button type="button" className="btn-close" data-bs-dismiss="modal" aria-label={t('common.close')} />
          </div>
          <div className="modal-body">{body}</div>
          <div className="modal-footer">
            <button type="button" className="btn btn-secondary" data-bs-dismiss="modal">
              {t('common.cancel')}
            </button>
            <button type="button" className="btn btn-danger" onClick={onConfirm}>
              {confirmLabel}
            </button>
          </div>
        </div>
      </div>
    </div>
  )
})
