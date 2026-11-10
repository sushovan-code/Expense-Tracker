import { createHttpClient } from '@/shared/api/httpClient'
import { FAMILY_HEADER, getActiveFamilyId, setActiveFamilyId } from './activeFamily'

describe('active family', () => {
  beforeEach(() => window.localStorage.clear())

  it('remembers the chosen family in this browser', () => {
    expect(getActiveFamilyId()).toBeNull()

    setActiveFamilyId('fam-7')

    expect(getActiveFamilyId()).toBe('fam-7')
  })

  it('is sent with every API call so the server knows which family the request is about', async () => {
    setActiveFamilyId('fam-7')
    const fetch = vi.fn().mockResolvedValue(new Response('[]', { status: 200 }))
    const http = createHttpClient({ baseUrl: '/api/v1', clientId: 'tab-1', fetch })

    await http.get('/expenses')

    const headers = fetch.mock.calls[0][1].headers as Record<string, string>
    expect(headers[FAMILY_HEADER]).toBe('fam-7')
  })

  it('sends no family header until one is chosen, so the server uses the default family', async () => {
    const fetch = vi.fn().mockResolvedValue(new Response('[]', { status: 200 }))
    const http = createHttpClient({ baseUrl: '/api/v1', clientId: 'tab-1', fetch })

    await http.get('/expenses')

    const headers = fetch.mock.calls[0][1].headers as Record<string, string>
    expect(headers[FAMILY_HEADER]).toBeUndefined()
  })
})
