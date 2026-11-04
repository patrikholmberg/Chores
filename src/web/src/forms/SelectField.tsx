import { useId, type ReactNode, type SelectHTMLAttributes } from 'react'

interface SelectFieldProps extends Omit<SelectHTMLAttributes<HTMLSelectElement>, 'id'> {
  label: string
  error?: string
  children: ReactNode
}

export function SelectField({ label, error, children, ...select }: SelectFieldProps) {
  const id = useId()
  return (
    <div className="mb-3">
      <label htmlFor={id} className="form-label">
        {label}
      </label>
      <select id={id} className={`form-select${error ? ' is-invalid' : ''}`} aria-invalid={error ? true : undefined} {...select}>
        {children}
      </select>
      {error && <div className="invalid-feedback">{error}</div>}
    </div>
  )
}
