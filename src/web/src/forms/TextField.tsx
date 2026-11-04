import { useId, type InputHTMLAttributes, type ReactNode } from 'react'

interface TextFieldProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'id'> {
  label: string
  error?: string
  hint?: ReactNode
}

/** A Bootstrap form control with its label, an optional hint and a server error shown as `invalid-feedback`. */
export function TextField({ label, error, hint, className, ...input }: TextFieldProps) {
  const id = useId()
  return (
    <div className="mb-3">
      <label htmlFor={id} className="form-label">
        {label}
      </label>
      <input
        id={id}
        className={`form-control${error ? ' is-invalid' : ''}${className ? ` ${className}` : ''}`}
        aria-describedby={hint ? `${id}-hint` : undefined}
        aria-invalid={error ? true : undefined}
        {...input}
      />
      {error && <div className="invalid-feedback">{error}</div>}
      {hint && !error && (
        <div id={`${id}-hint`} className="form-text">
          {hint}
        </div>
      )}
    </div>
  )
}
