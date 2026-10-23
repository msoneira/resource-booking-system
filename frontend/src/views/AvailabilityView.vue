<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
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

type PopupType = 'success' | 'error'

const route = useRoute()
const apiUrl = 'http://localhost:5168'

const resourceId = computed(() => Number(route.query.resourceId ?? 1))
const date = ref(new Date().toLocaleDateString('en-CA'))

const resource = ref<Resource | null>(null)
const bookings = ref<Booking[]>([])
const loading = ref(false)
const submitting = ref(false)
const error = ref('')
const timeZoneId = ref('Europe/Madrid')

const startTime = ref('09:00')
const endTime = ref('10:00')

const isBookingModalOpen = ref(false)
const isResultModalOpen = ref(false)
const popupType = ref<PopupType>('success')
const popupMessage = ref('')
const selectedTime = ref('')

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

const availableStartTimes = computed(() =>
  slots.value.filter((slot) => !slot.booked),
)

const availableEndTimes = computed(() => {
  const start = DateTime.fromISO(`${date.value}T${startTime.value}`, {
    zone: timeZoneId.value,
  })

  return slots.value
    .filter((slot) => !slot.booked)
    .map((slot) => slot.time)
    .filter((time) => {
      const end = DateTime.fromISO(`${date.value}T${time}`, {
        zone: timeZoneId.value,
      })

      return end.isValid && start.isValid && end.toMillis() > start.toMillis()
    })
    .concat(
      // 18:00 is a valid end time, even though it is not a start slot.
      (() => {
        const end = DateTime.fromISO(`${date.value}T18:00`, {
          zone: timeZoneId.value,
        })

        const overlapsBooking = bookings.value.some((booking) => {
          const bookingStart = DateTime.fromISO(booking.start)
          const bookingEnd = DateTime.fromISO(booking.end)

          return (
            start.toMillis() < bookingEnd.toMillis() &&
            end.toMillis() > bookingStart.toMillis()
          )
        })

        return start.isValid && end.isValid && end.toMillis() > start.toMillis() &&
          !overlapsBooking
          ? ['18:00']
          : []
      })(),
    )
})

async function loadAvailability() {
  loading.value = true
  error.value = ''

  try {
    const resourceResponse = await fetch(`${apiUrl}/api/resources`)

    if (!resourceResponse.ok) {
      throw new Error('Could not load resources.')
    }

    const resources: Resource[] = await resourceResponse.json()
    resource.value =
      resources.find((item) => item.id === resourceId.value) ?? null

    if (!resource.value) {
      throw new Error('Resource not found.')
    }

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

function openBookingModal(time: string) {
  selectedTime.value = time
  startTime.value = time

  const nextHour = Number(time.slice(0, 2)) + 1
  endTime.value = `${String(nextHour).padStart(2, '0')}:00`

  isBookingModalOpen.value = true
}

function closeBookingModal() {
  if (submitting.value) return
  isBookingModalOpen.value = false
}

function showResult(type: PopupType, message: string) {
  popupType.value = type
  popupMessage.value = message
  isResultModalOpen.value = true
}

function closeResultModal() {
  isResultModalOpen.value = false

  if (popupType.value === 'success') {
    isBookingModalOpen.value = false
  }
}

async function createBooking() {
  error.value = ''

  const start = DateTime.fromISO(`${date.value}T${startTime.value}`, {
    zone: timeZoneId.value,
  })

  const end = DateTime.fromISO(`${date.value}T${endTime.value}`, {
    zone: timeZoneId.value,
  })

  if (
    !start.isValid ||
    !end.isValid ||
    end.toMillis() <= start.toMillis()
  ) {
    showResult('error', 'Please select a valid time range.')
    return
  }

  submitting.value = true

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
      let message = 'Booking failed.'

      try {
        const body = await response.json()
        const validationErrors = Object.values(body.errors ?? {})
          .flat()
          .join(' ')

        message =
          body.error ||
          validationErrors ||
          body.title ||
          message
      } catch {
        // Keep the fallback message if the response is not JSON.
      }

      throw new Error(message)
    }

    isBookingModalOpen.value = false

    await loadAvailability()

    showResult('success', 'Booking created successfully.')
  } catch (e) {
    showResult(
      'error',
      e instanceof Error ? e.message : 'Unexpected error.',
    )
  } finally {
    submitting.value = false
  }
}

function handleKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape') {
    if (isResultModalOpen.value) {
      closeResultModal()
    } else if (isBookingModalOpen.value) {
      closeBookingModal()
    }
  }
}

watch([date, resourceId], async () => {
  isBookingModalOpen.value = false
  await loadAvailability()
})

onMounted(() => {
  loadAvailability()
  window.addEventListener('keydown', handleKeydown)
})

onUnmounted(() => {
  window.removeEventListener('keydown', handleKeydown)
})
</script>

<template>
  <main class="availability-page">
    <header class="page-header">
      <div>
        <p class="eyebrow">RESOURCE MANAGEMENT</p>
        <h1>Resource availability</h1>
        <p class="subtitle">Choose a day and book a free time slot.</p>
      </div>

      <RouterLink to="/" class="back-link">
        ← Back to resources
      </RouterLink>
    </header>

    <section v-if="resource" class="panel resource-panel">
      <p class="eyebrow">{{ resource.office }}</p>
      <h2>{{ resource.name }}</h2>

      <label class="date-field">
        Booking date
        <input v-model="date" type="date" />
      </label>
    </section>

    <p v-if="loading" class="info-message">Loading availability...</p>
    <p v-if="error" class="error-message">{{ error }}</p>

    <section v-if="!loading && resource" class="panel">
      <h2>Working hours</h2>
      <p class="subtitle">
        09:00–18:00, shown in the office's local time.
      </p>

      <div class="slot-grid">
        <button
          v-for="slot in slots"
          :key="slot.time"
          type="button"
          class="slot"
          :class="{
            booked: slot.booked,
            selected: selectedTime === slot.time && isBookingModalOpen,
          }"
          :disabled="slot.booked"
          @click="openBookingModal(slot.time)"
        >
          <strong>{{ slot.time }}</strong>
          <span>{{ slot.booked ? 'Booked' : 'Available' }}</span>
        </button>
      </div>
    </section>

    <!-- Booking modal -->
    <Teleport to="body">
      <div
        v-if="isBookingModalOpen"
        class="modal-backdrop"
        @click.self="closeBookingModal"
      >
        <section
          class="modal-card"
          role="dialog"
          aria-modal="true"
          aria-labelledby="booking-modal-title"
        >
          <button
            type="button"
            class="close-button"
            aria-label="Close booking window"
            :disabled="submitting"
            @click="closeBookingModal"
          >
            ×
          </button>

          <p class="eyebrow">{{ resource?.office }}</p>
          <h2 id="booking-modal-title">Create a booking</h2>
          <p class="modal-description">
            {{ resource?.name }} · {{ date }}
          </p>

          <form class="booking-form" @submit.prevent="createBooking">
            <label>
              Start time
              <select v-model="startTime">
                <option
                  v-for="slot in availableStartTimes"
                  :key="slot.time"
                  :value="slot.time"
                >
                  {{ slot.time }}
                </option>
              </select>
            </label>

            <label>
              End time
              <select v-model="endTime">
                <option
                  v-for="time in availableEndTimes"
                  :key="time"
                  :value="time"
                >
                  {{ time }}
                </option>
              </select>
            </label>

            <p class="timezone-note">
              Times are shown in {{ timeZoneId }}.
            </p>

            <div class="modal-actions">
              <button
                type="button"
                class="secondary-button"
                :disabled="submitting"
                @click="closeBookingModal"
              >
                Cancel
              </button>

              <button
                type="submit"
                class="primary-button"
                :disabled="submitting"
              >
                {{ submitting ? 'Booking...' : 'Confirm booking' }}
              </button>
            </div>
          </form>
        </section>
      </div>
    </Teleport>

    <!-- Success / error popup -->
    <Teleport to="body">
      <div
        v-if="isResultModalOpen"
        class="modal-backdrop result-backdrop"
        @click.self="closeResultModal"
      >
        <section
          class="modal-card result-card"
          role="alertdialog"
          aria-modal="true"
          aria-labelledby="result-modal-title"
        >
          <div
            class="result-icon"
            :class="popupType"
          >
            <span v-if="popupType === 'success'">✓</span>
            <span v-else>!</span>
          </div>

          <h2 id="result-modal-title">
            {{ popupType === 'success' ? 'Booking confirmed' : 'Booking failed' }}
          </h2>

          <p class="result-message">{{ popupMessage }}</p>

          <div class="modal-actions result-actions">
            <button
              v-if="popupType === 'error'"
              type="button"
              class="secondary-button"
              @click="closeResultModal"
            >
              Close
            </button>

            <button
              v-if="popupType === 'error'"
              type="button"
              class="primary-button"
              @click="isResultModalOpen = false"
            >
              Try again
            </button>

            <button
              v-else
              type="button"
              class="primary-button"
              @click="closeResultModal"
            >
              OK
            </button>
          </div>
        </section>
      </div>
    </Teleport>
  </main>
</template>

<style scoped>
.availability-page {
  max-width: 1100px;
  margin: 0 auto;
  padding: 40px;
  color: #202b3c;
}

.page-header {
  display: flex;
  justify-content: space-between;
  gap: 20px;
  align-items: center;
  margin-bottom: 28px;
}

h1 {
  font-size: 32px;
  margin: 8px 0;
}

h2 {
  font-size: 20px;
  margin: 0 0 12px;
}

.eyebrow {
  font-size: 11px;
  letter-spacing: 1.2px;
  color: #75839a;
  font-weight: 700;
}

.subtitle {
  color: #8290a4;
  font-size: 14px;
  line-height: 1.5;
}

.panel {
  background: white;
  border: 1px solid #e5eaf2;
  border-radius: 12px;
  padding: 24px;
  margin-bottom: 20px;
}

.date-field,
.booking-form label {
  display: grid;
  gap: 8px;
  font-size: 13px;
  color: #64728a;
}

.date-field {
  max-width: 280px;
  margin-top: 20px;
}

input,
select {
  width: 100%;
  box-sizing: border-box;
  border: 1px solid #dce3ed;
  border-radius: 8px;
  padding: 12px;
  background: white;
  color: #202b3c;
  font: inherit;
}

.slot-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 12px;
  margin-top: 20px;
}

.slot {
  display: grid;
  gap: 8px;
  text-align: left;
  border: 1px solid #dce5f0;
  background: #f5fbf8;
  color: #247a56;
  border-radius: 8px;
  padding: 18px;
  cursor: pointer;
  transition:
    border-color 0.15s ease,
    background 0.15s ease,
    transform 0.15s ease;
}

.slot:hover:not(:disabled) {
  border-color: #247a56;
  transform: translateY(-1px);
}

.slot strong {
  font-size: 20px;
}

.slot span {
  font-size: 13px;
}

.slot.selected {
  border-color: #3758d7;
  box-shadow: 0 0 0 2px rgb(55 88 215 / 12%);
}

.slot.booked {
  background: #fff1f0;
  border-color: #f4ceca;
  color: #bd4941;
  cursor: not-allowed;
}

.back-link {
  color: #3758d7;
  text-decoration: none;
  font-size: 13px;
  white-space: nowrap;
}

.back-link:hover {
  text-decoration: underline;
}

.error-message {
  color: #c03939;
}

.info-message {
  color: #64728a;
}

/* Modal windows */

.modal-backdrop {
  position: fixed;
  inset: 0;
  z-index: 1000;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 20px;
  background: rgb(20 30 48 / 48%);
  backdrop-filter: blur(3px);
}

.modal-card {
  position: relative;
  width: min(100%, 480px);
  max-height: calc(100vh - 40px);
  overflow-y: auto;
  box-sizing: border-box;
  background: white;
  border: 1px solid #e5eaf2;
  border-radius: 16px;
  padding: 32px;
  box-shadow: 0 24px 80px rgb(20 30 48 / 20%);
  animation: modal-appear 0.18s ease-out;
}

.close-button {
  position: absolute;
  top: 16px;
  right: 18px;
  width: 36px;
  height: 36px;
  display: grid;
  place-items: center;
  border: 0;
  border-radius: 8px;
  background: transparent;
  color: #75839a;
  font-size: 28px;
  cursor: pointer;
}

.close-button:hover:not(:disabled) {
  background: #f1f4f9;
  color: #202b3c;
}

.close-button:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.modal-description {
  color: #75839a;
  font-size: 14px;
  margin: 0 0 24px;
}

.booking-form {
  display: grid;
  gap: 18px;
}

.timezone-note {
  margin: 0;
  color: #8290a4;
  font-size: 12px;
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  flex-wrap: wrap;
  gap: 10px;
  margin-top: 8px;
}

.primary-button,
.secondary-button {
  border: 0;
  border-radius: 8px;
  padding: 12px 18px;
  font: inherit;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.15s ease;
}

.primary-button {
  background: #3758d7;
  color: white;
}

.primary-button:hover:not(:disabled) {
  background: #2948bf;
}

.secondary-button {
  background: #f1f4f9;
  color: #46546a;
}

.secondary-button:hover:not(:disabled) {
  background: #e5eaf2;
}

.primary-button:disabled,
.secondary-button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

/* Result popup */

.result-backdrop {
  z-index: 1100;
}

.result-card {
  max-width: 420px;
  text-align: center;
  padding: 36px 32px 32px;
}

.result-icon {
  width: 58px;
  height: 58px;
  display: grid;
  place-items: center;
  margin: 0 auto 20px;
  border-radius: 50%;
  font-size: 30px;
  font-weight: 700;
}

.result-icon.success {
  background: #e7f7ef;
  color: #16825d;
}

.result-icon.error {
  background: #fff0ef;
  color: #c03939;
}

.result-card h2 {
  margin-bottom: 10px;
}

.result-message {
  color: #64728a;
  font-size: 14px;
  line-height: 1.6;
  overflow-wrap: anywhere;
}

.result-actions {
  justify-content: center;
  margin-top: 24px;
}

@keyframes modal-appear {
  from {
    opacity: 0;
    transform: translateY(8px) scale(0.98);
  }

  to {
    opacity: 1;
    transform: translateY(0) scale(1);
  }
}

@media (max-width: 650px) {
  .availability-page {
    padding: 20px;
  }

  .page-header {
    align-items: flex-start;
    flex-direction: column;
  }

  .slot-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .modal-card {
    padding: 28px 22px 22px;
  }

  .modal-actions {
    flex-direction: column-reverse;
  }

  .modal-actions button {
    width: 100%;
  }
}
</style>