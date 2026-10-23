import { afterEach, describe, expect, it, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { defineComponent } from 'vue'
import { useRoute } from 'vue-router'
import AvailabilityView from '../views/AvailabilityView.vue'

vi.mock('vue-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('vue-router')>()

  return {
    ...actual,
    useRoute: vi.fn(),
  }
})

const mockResources = [
  {
    id: 1,
    name: 'Meeting Room A',
    type: 'MeetingRoom',
    officeId: 1,
    office: 'Madrid Office',
    city: 'Madrid',
  },
]

const mockAvailability = {
  timeZoneId: 'Europe/Madrid',
  bookings: [],
}

const RouterLinkStub = defineComponent({
  props: ['to'],
  template: '<a><slot /></a>',
})

function mockFetch() {
  vi.stubGlobal(
    'fetch',
    vi.fn(async (url: string | URL | Request) => {
      const urlString = String(url)

      if (urlString.endsWith('/api/resources')) {
        return {
          ok: true,
          json: async () => mockResources,
        }
      }

      if (urlString.includes('/api/resources/1/bookings')) {
        return {
          ok: true,
          json: async () => mockAvailability,
        }
      }

      throw new Error(`Unexpected URL: ${urlString}`)
    }),
  )
}

function mountView() {
  vi.mocked(useRoute).mockReturnValue({
    query: { resourceId: '1' },
  } as ReturnType<typeof useRoute>)

  return mount(AvailabilityView, {
    global: {
      stubs: {
        RouterLink: RouterLinkStub,
      },
    },
  })
}

afterEach(() => {
  vi.unstubAllGlobals()
  vi.clearAllMocks()
})

describe('AvailabilityView', () => {
  it('loads the resource and displays available time slots', async () => {
    mockFetch()

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('Meeting Room A')
    expect(wrapper.text()).toContain('Madrid Office')
    expect(wrapper.text()).toContain('09:00')
    expect(wrapper.text()).toContain('Available')

    wrapper.unmount()
  })

  it('opens the booking modal when clicking an available slot', async () => {
    mockFetch()

    const wrapper = mountView()
    await flushPromises()

    const slot = wrapper
      .findAll('button.slot')
      .find((button) => button.text().includes('09:00'))

    expect(slot).toBeDefined()

    await slot!.trigger('click')

    expect(document.body.textContent).toContain('Create a booking')
    expect(document.body.textContent).toContain('Confirm booking')
    expect(document.body.textContent).toContain('09:00')

    wrapper.unmount()
  })

  it('shows a success popup after a booking is created', async () => {
    const fetchMock = vi.fn(async (url: string | URL | Request, options?: RequestInit) => {
        const urlString = String(url)

        if (urlString.endsWith('/api/resources')) {
        return {
            ok: true,
            json: async () => mockResources,
        }
        }

        if (urlString.includes('/api/resources/1/bookings')) {
        return {
            ok: true,
            json: async () => mockAvailability,
        }
        }

        if (urlString.endsWith('/api/bookings') && options?.method === 'POST') {
        return {
            ok: true,
            json: async () => ({ id: 10 }),
        }
        }

        throw new Error(`Unexpected URL: ${urlString}`)
    })

    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mountView()
    await flushPromises()

    const slot = wrapper
        .findAll('button.slot')
        .find((button) => button.text().includes('09:00'))

    expect(slot).toBeDefined()
    await slot!.trigger('click')

    const confirmButton = Array.from(
        document.body.querySelectorAll('button'),
    ).find((button) => button.textContent?.includes('Confirm booking'))

    expect(confirmButton).toBeDefined()
    confirmButton?.click()

    await flushPromises()

    expect(document.body.textContent).toContain('Booking confirmed')
    expect(document.body.textContent).toContain(
        'Booking created successfully.',
    )

    expect(
        fetchMock.mock.calls.some(
        ([url, options]) =>
            String(url).endsWith('/api/bookings') &&
            options?.method === 'POST',
        ),
    ).toBe(true)

    wrapper.unmount()
    })
    it('shows an error popup when the API rejects a booking', async () => {
        const fetchMock = vi.fn(async (url: string | URL | Request, options?: RequestInit) => {
            const urlString = String(url)

            if (urlString.endsWith('/api/resources')) {
            return {
                ok: true,
                json: async () => mockResources,
            }
            }

            if (urlString.includes('/api/resources/1/bookings')) {
            return {
                ok: true,
                json: async () => mockAvailability,
            }
            }

            if (urlString.endsWith('/api/bookings') && options?.method === 'POST') {
            return {
                ok: false,
                json: async () => ({
                error: 'The resource is already booked for this time slot.',
                }),
            }
            }

            throw new Error(`Unexpected URL: ${urlString}`)
        })

        vi.stubGlobal('fetch', fetchMock)

        const wrapper = mountView()
        await flushPromises()

        const slot = wrapper
            .findAll('button.slot')
            .find((button) => button.text().includes('09:00'))

        expect(slot).toBeDefined()
        await slot!.trigger('click')

        const confirmButton = Array.from(
            document.body.querySelectorAll('button'),
        ).find((button) => button.textContent?.includes('Confirm booking'))

        expect(confirmButton).toBeDefined()
        confirmButton?.click()

        await flushPromises()

        expect(document.body.textContent).toContain('Booking failed')
        expect(document.body.textContent).toContain(
            'The resource is already booked for this time slot.',
        )

        wrapper.unmount()
    })

    it('closes the booking modal without creating a booking', async () => {
        const fetchMock = vi.fn(async (url: string | URL | Request) => {
            const urlString = String(url)

            if (urlString.endsWith('/api/resources')) {
            return {
                ok: true,
                json: async () => mockResources,
            }
            }

            if (urlString.includes('/api/resources/1/bookings')) {
            return {
                ok: true,
                json: async () => mockAvailability,
            }
            }

            throw new Error(`Unexpected URL: ${urlString}`)
        })

        vi.stubGlobal('fetch', fetchMock)

        const wrapper = mountView()
        await flushPromises()

        const slot = wrapper
            .findAll('button.slot')
            .find((button) => button.text().includes('09:00'))

        expect(slot).toBeDefined()
        await slot!.trigger('click')

        expect(document.body.textContent).toContain('Create a booking')

        const cancelButton = Array.from(
            document.body.querySelectorAll('button'),
        ).find((button) => button.textContent?.trim() === 'Cancel')

        expect(cancelButton).toBeDefined()
        cancelButton?.click()

        await flushPromises()

        expect(document.body.textContent).not.toContain('Create a booking')

        expect(
            fetchMock.mock.calls.some(
            ([url, options]) =>
                String(url).endsWith('/api/bookings') &&
                options?.method === 'POST',
            ),
        ).toBe(false)

        wrapper.unmount()
    })
})