import { onCLS, onFCP, onINP, onLCP, onTTFB, type Metric } from 'web-vitals'
import { logger } from '@/shared/telemetry/logger'

const UNITS: Record<Metric['name'], string> = {
  CLS: '',
  FCP: 'ms',
  INP: 'ms',
  LCP: 'ms',
  TTFB: 'ms',
}

/**
 * Logs Core Web Vitals (LCP, INP, CLS) and supporting metrics (FCP, TTFB) as structured events.
 * "poor" ratings are warnings, so slow pages stand out in the log stream.
 */
export function reportWebVitals() {
  const report = (metric: Metric) => {
    const value = metric.name === 'CLS' ? Number(metric.value.toFixed(3)) : Math.round(metric.value)
    logger.event(
      'browser.web_vital',
      `Web vital ${metric.name}: ${value}${UNITS[metric.name]} (${metric.rating})`,
      {
        'web_vital.name': metric.name,
        'web_vital.value': value,
        'web_vital.rating': metric.rating,
        'web_vital.navigation_type': metric.navigationType,
      },
      metric.rating === 'poor' ? 'warn' : 'info',
    )
  }

  onLCP(report)
  onINP(report)
  onCLS(report)
  onFCP(report)
  onTTFB(report)
}
