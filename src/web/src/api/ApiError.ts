/** An RFC 7807 problem returned by the Api, with per-field messages and stable codes for validation failures. */
export class ApiError extends Error {
  readonly status: number
  readonly code: string | undefined
  readonly fieldCodes: Record<string, string[]>

  constructor(status: number, title: string, code?: string, fieldCodes: Record<string, string[]> = {}) {
    super(title)
    this.status = status
    this.code = code
    this.fieldCodes = fieldCodes
  }

  /** The first error code for a form field, matching the Api's property name case-insensitively. */
  fieldCode(field: string): string | undefined {
    const key = Object.keys(this.fieldCodes).find((k) => k.toLowerCase() === field.toLowerCase())
    return key ? this.fieldCodes[key][0] : undefined
  }

  static async fromResponse(response: Response): Promise<ApiError> {
    let problem: { title?: string; code?: string; errorCodes?: Record<string, string[]> } = {}
    try {
      problem = await response.json()
    } catch {
      // Not a problem document; fall back to the status line.
    }
    return new ApiError(response.status, problem.title ?? response.statusText, problem.code, problem.errorCodes ?? {})
  }
}
