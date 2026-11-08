import { Component, type ErrorInfo, type ReactNode } from 'react'
import { logger } from '@/shared/telemetry'

interface State {
  error: Error | null
}

/** Catches render errors, logs them (with the component stack) and shows a recovery screen. */
export class ErrorBoundary extends Component<{ children: ReactNode }, State> {
  state: State = { error: null }

  static getDerivedStateFromError(error: Error): State {
    return { error }
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    logger.error('React render error', error, { 'react.component_stack': info.componentStack ?? undefined })
  }

  render() {
    if (!this.state.error) return this.props.children

    return (
      <div className="crash">
        <div className="card crash__card">
          <h1 className="page-title">Something went wrong</h1>
          <p className="muted">The error has been logged. Reloading usually fixes it.</p>
          <pre className="crash__detail">{this.state.error.message}</pre>
          <button type="button" className="button button--primary" onClick={() => location.reload()}>
            Reload
          </button>
        </div>
      </div>
    )
  }
}
