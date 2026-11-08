import type { Context } from '@opentelemetry/api'
import { logs, SeverityNumber, type LogAttributes } from '@opentelemetry/api-logs'

/**
 * Structured application logger.
 *
 * Each call emits an OpenTelemetry log record (correlated with the active trace, if any) that is
 * exported to the configured OTLP endpoint, e.g. the Aspire dashboard's Structured logs page.
 * It is also mirrored to the browser console: everything in development, warnings and errors otherwise.
 * Until telemetry starts (or when it's disabled) the OTel logger is a no-op.
 */
type Attributes = Record<string, string | number | boolean | null | undefined>

const LOGGER_NAME = 'expense-tracker-client'
const CONSOLE_MIN = import.meta.env.DEV ? SeverityNumber.DEBUG : SeverityNumber.WARN

const LEVELS = {
  debug: { severity: SeverityNumber.DEBUG, text: 'DEBUG', console: console.debug },
  info: { severity: SeverityNumber.INFO, text: 'INFO', console: console.info },
  warn: { severity: SeverityNumber.WARN, text: 'WARN', console: console.warn },
  error: { severity: SeverityNumber.ERROR, text: 'ERROR', console: console.error },
} as const

type Level = keyof typeof LEVELS

/** OpenTelemetry semantic-convention attributes for an exception. */
function exceptionAttributes(error: unknown): LogAttributes {
  if (error instanceof Error) {
    return {
      'exception.type': error.name,
      'exception.message': error.message,
      ...(error.stack ? { 'exception.stacktrace': error.stack } : {}),
    }
  }
  return error === undefined ? {} : { 'exception.message': String(error) }
}

function clean(attributes: Attributes | undefined): LogAttributes {
  const result: LogAttributes = {}
  for (const [key, value] of Object.entries(attributes ?? {})) {
    if (value !== undefined && value !== null) result[key] = value
  }
  return result
}

interface EmitOptions {
  error?: unknown
  context?: Context
  /** Stable event name, e.g. `expense.created` (OpenTelemetry event semantics). */
  eventName?: string
}

function emit(level: Level, message: string, attributes: Attributes | undefined, options: EmitOptions = {}) {
  const { error, context, eventName } = options
  const { severity, text, console: write } = LEVELS[level]
  const allAttributes: LogAttributes = {
    ...clean(attributes),
    ...exceptionAttributes(error),
    ...(eventName ? { 'event.name': eventName } : {}),
    'app.route': location.pathname,
  }

  logs.getLogger(LOGGER_NAME).emit({
    severityNumber: severity,
    severityText: text,
    body: message,
    attributes: allAttributes,
    ...(eventName ? { eventName } : {}),
    // Without an explicit context the SDK uses the active one (which native async/await can lose).
    ...(context ? { context } : {}),
  })

  if (severity >= CONSOLE_MIN) {
    if (error !== undefined) write(`[${text}] ${message}`, clean(attributes), error)
    else write(`[${text}] ${message}`, clean(attributes))
  }
}

function createLogger(context?: Context) {
  return {
    debug: (message: string, attributes?: Attributes) => emit('debug', message, attributes, { context }),
    info: (message: string, attributes?: Attributes) => emit('info', message, attributes, { context }),
    warn: (message: string, attributes?: Attributes, error?: unknown) =>
      emit('warn', message, attributes, { error, context }),
    error: (message: string, error?: unknown, attributes?: Attributes) =>
      emit('error', message, attributes, { error, context }),
    /**
     * A named application event (user action or domain change), e.g.
     * `logger.event('expense.created', 'Expense created', { 'expense.id': 42 })`.
     * Names are stable and filterable; messages stay human-readable.
     */
    event: (eventName: string, message: string, attributes?: Attributes, level: Level = 'info') =>
      emit(level, message, attributes, { context, eventName }),
  }
}

export const logger = {
  ...createLogger(),
  /** A logger whose records belong to the given trace context (e.g. a span started before an `await`). */
  withContext: (context: Context) => createLogger(context),
}
