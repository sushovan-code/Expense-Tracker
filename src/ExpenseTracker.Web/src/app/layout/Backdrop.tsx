/** Animated gradient-mesh background: drifting blurred color blobs under a fine grain. */
export function Backdrop() {
  return (
    <div className="backdrop" aria-hidden="true">
      <span className="backdrop__blob backdrop__blob--1" />
      <span className="backdrop__blob backdrop__blob--2" />
      <span className="backdrop__blob backdrop__blob--3" />
      <span className="backdrop__blob backdrop__blob--4" />
      <span className="backdrop__grid" />
      <span className="backdrop__grain" />
    </div>
  )
}
