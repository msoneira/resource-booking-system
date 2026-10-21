<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { DateTime } from 'luxon'

interface Booking {
  id: number
  start: string
  end: string
  status: string
}

interface AvailabilityResponse {
  timeZoneId: string
  bookings: Booking[]
}

interface Resource {
  id: number
  name: string
  type: string
  officeId: number
  office: string
  city: string
}

const route = useRoute()
const apiUrl = 'http://localhost:5168'
const resourceId = computed(() => Number(route.query.resourceId ?? 1))
const date = ref(new Date().toLocaleDateString('en-CA'))
const resource = ref<Resource | null>(null)
const bookings = ref<Booking[]>([])
const loading = ref(false)
const error = ref('')
const success = ref('')
const startTime = ref('09:00')
const endTime = ref('10:00')
const timeZoneId = ref('Europe/Madrid')

const slots = computed(() => {
  const result: { time: string; booked: boolean }[] = []

  for (let hour = 9; hour < 18; hour++) {
    const time = `${String(hour).padStart(2, '0')}:00`
    const slotStart = DateTime.fromISO(`${date.value}T${time}`, {
        zone: timeZoneId.value,
    })
    const slotEnd = slotStart.plus({ hours: 1 })

    const booked = bookings.value.some((booking) => {
        const bookingStart = DateTime.fromISO(booking.start)
        const bookingEnd = DateTime.fromISO(booking.end)

        return (
            slotStart.toMillis() < bookingEnd.toMillis() &&
            slotEnd.toMillis() > bookingStart.toMillis()
        )
    })

    result.push({ time, booked })
  }

  return result
})

async function loadAvailability() {
  loading.value = true
  error.value = ''

  try {
    const resourceResponse = await fetch(`${apiUrl}/api/resources`)
    if (!resourceResponse.ok) throw new Error('Could not load resources.')

    const resources: Resource[] = await resourceResponse.json()
    resource.value = resources.find((item) => item.id === resourceId.value) ?? null

    if (!resource.value) throw new Error('Resource not found.')

    const response = await fetch(
    `${apiUrl}/api/resources/${resourceId.value}/bookings?date=${date.value}`,
    )

    if (!response.ok) {
    throw new Error('Could not load availability.')
    }

    const data: AvailabilityResponse = await response.json()

    timeZoneId.value = data.timeZoneId
    bookings.value = data.bookings
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Unexpected error.'
  } finally {
    loading.value = false
  }
}

async function createBooking() {
  error.value = ''
  success.value = ''

  const start = DateTime.fromISO(
    `${date.value}T${startTime.value}`,
    { zone: timeZoneId.value },
  )

  const end = DateTime.fromISO(
    `${date.value}T${endTime.value}`,
    { zone: timeZoneId.value },
  )

  if (!start.isValid || !end.isValid || end.toMillis() <= start.toMillis()) {
    error.value = 'Please select a valid time range.'
    return
  }

  try {
    const response = await fetch(`${apiUrl}/api/bookings`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        resourceId: resourceId.value,
        userId: 1,
        start: start.toUTC().toISO(),
        end: end.toUTC().toISO(),
      }),
    })

    if (!response.ok) {
      const body = await response.json()
      const validationErrors = Object.values(body.errors ?? {})
        .flat()
        .join(' ')

      throw new Error(
        body.error || validationErrors || body.title || 'Booking failed.',
      )
    }

   success.value = 'Booking created successfully.'
    await loadAvailability()

    // Select the next consecutive available time range
    const availableSlots = slots.value.filter((slot) => !slot.booked)

    let nextStart: string | undefined
    let nextEnd: string | undefined

    for (const slot of availableSlots) {
        const hour = Number(slot.time.slice(0, 2))
        const followingTime = `${String(hour + 1).padStart(2, '0')}:00`

        if (availableSlots.some((candidate) => candidate.time === followingTime)) {
            nextStart = slot.time
            nextEnd = followingTime
            break
        }
    }

    startTime.value = nextStart ?? ''
    endTime.value = nextEnd ?? ''
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Unexpected error.'
  }
}

watch([date, resourceId], loadAvailability)
onMounted(loadAvailability)
</script>

<template>
  <main class="availability-page">
    <header class="page-header">
      <div>
        <p class="eyebrow">RESOURCE MANAGEMENT</p>
        <h1>Resource availability</h1>
        <p class="subtitle">Choose a day and book a free time slot.</p>
      </div>
      <RouterLink to="/" class="back-link">← Back to resources</RouterLink>
    </header>

    <section class="panel" v-if="resource">
      <p class="eyebrow">{{ resource.office }}</p>
      <h2>{{ resource.name }}</h2>
      <label class="date-field">
        Booking date
        <input v-model="date" type="date" />
      </label>
    </section>

    <p v-if="loading">Loading availability...</p>
    <p v-if="error" class="error-message">{{ error }}</p>
    <p v-if="success" class="success-message">{{ success }}</p>

    <section class="panel" v-if="!loading && resource">
      <h2>Working hours</h2>
      <p class="subtitle">09:00–18:00, shown in the office's local time.</p>

      <div class="slot-grid">
        <button
          v-for="slot in slots"
          :key="slot.time"
          class="slot"
          :class="{ booked: slot.booked }"
          :disabled="slot.booked"
          @click="startTime = slot.time; endTime = `${String(Number(slot.time.slice(0, 2)) + 1).padStart(2, '0')}:00`"
        >
          <strong>{{ slot.time }}</strong>
          <span>{{ slot.booked ? 'Booked' : 'Available' }}</span>
        </button>
      </div>
    </section>

    <section class="panel" v-if="!loading && resource">
      <h2>Create a booking</h2>
      <form class="booking-form" @submit.prevent="createBooking">
        <label>
          Start time
          <select v-model="startTime">
            <option v-for="slot in slots.filter((item) => !item.booked)" :key="slot.time" :value="slot.time">
              {{ slot.time }}
            </option>
          </select>
        </label>
        <label>
          End time
          <select v-model="endTime">
            <option v-for="hour in [10,11,12,13,14,15,16,17,18]" :key="hour" :value="`${String(hour).padStart(2, '0')}:00`">
              {{ String(hour).padStart(2, '0') }}:00
            </option>
          </select>
        </label>
        <button type="submit" class="primary-button">Confirm booking</button>
      </form>
    </section>
  </main>
</template>

<style scoped>
.availability-page { max-width: 1100px; margin: 0 auto; padding: 40px; color: #202b3c; }
.page-header { display: flex; justify-content: space-between; gap: 20px; align-items: center; margin-bottom: 28px; }
h1 { font-size: 32px; margin: 8px 0; }
h2 { font-size: 18px; margin: 0 0 12px; }
.eyebrow { font-size: 11px; letter-spacing: 1.2px; color: #75839a; font-weight: 700; }
.subtitle { color: #8290a4; font-size: 13px; }
.panel { background: white; border: 1px solid #e5eaf2; border-radius: 12px; padding: 24px; margin-bottom: 20px; }
.date-field, .booking-form label { display: grid; gap: 8px; font-size: 12px; color: #64728a; }
.date-field { max-width: 240px; margin-top: 20px; }
input, select { border: 1px solid #dce3ed; border-radius: 8px; padding: 10px; background: white; }
.slot-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 12px; margin-top: 20px; }
.slot { display: grid; gap: 8px; text-align: left; border: 1px solid #dce5f0; background: #f5fbf8; color: #247a56; border-radius: 8px; padding: 14px; }
.slot span { font-size: 12px; }
.slot.booked { background: #fff1f0; border-color: #f4ceca; color: #bd4941; cursor: not-allowed; }
.booking-form { display: flex; flex-wrap: wrap; align-items: end; gap: 16px; }
.booking-form label { min-width: 150px; }
.primary-button { border: 0; background: #3758d7; color: white; border-radius: 8px; padding: 12px 18px; }
.back-link { color: #3758d7; text-decoration: none; font-size: 13px; }
.error-message { color: #c03939; }
.success-message { color: #16825d; }
@media (max-width: 650px) { .availability-page { padding: 20px; } .page-header { align-items: start; flex-direction: column; } .slot-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); } }
</style>