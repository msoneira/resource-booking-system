<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { DateTime } from 'luxon'

interface Booking {
  id: number
  resourceId: number
  resourceName: string
  officeName: string
  startUtc: string
  endUtc: string
  status: string
}

const apiUrl = 'http://localhost:5168'
const userId = 1

const bookings = ref<Booking[]>([])
const loading = ref(false)
const error = ref('')
const success = ref('')

async function loadBookings() {
  loading.value = true
  error.value = ''

  try {
    const response = await fetch(
      `${apiUrl}/api/bookings?userId=${userId}`,
    )

    if (!response.ok) {
      throw new Error('Unable to load your bookings.')
    }

    bookings.value = await response.json()
  } catch (e) {
    error.value = e instanceof Error
      ? e.message
      : 'Unexpected error.'
  } finally {
    loading.value = false
  }
}

async function cancelBooking(id: number) {
  error.value = ''
  success.value = ''

  try {
    const response = await fetch(`${apiUrl}/api/bookings/${id}?userId=1`, {
        method: 'DELETE',
    })

    if (!response.ok) {
      const body = await response.json().catch(() => ({}))
      throw new Error(body.error ?? 'Unable to cancel this booking.')
    }

    success.value = 'Booking cancelled successfully.'
    await loadBookings()
  } catch (e) {
    error.value = e instanceof Error
      ? e.message
      : 'Unexpected error.'
  }
}

function formatDate(value: string, officeName: string) {
  const timeZone = officeName.toLowerCase().includes('london')
    ? 'Europe/London'
    : 'Europe/Madrid'

  return DateTime.fromISO(value, { setZone: true })
    .setZone(timeZone)
    .toLocaleString({
      day: '2-digit',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      hourCycle: 'h23',
    })
}

onMounted(loadBookings)
</script>

<template>
  <main class="page">
    <RouterLink to="/" class="back-link">
      ← Back to resources
    </RouterLink>

    <p class="eyebrow">RESOURCE MANAGEMENT</p>
    <h1>My bookings</h1>
    <p class="subtitle">
      View and manage your reservations.
    </p>

    <p v-if="loading">Loading bookings...</p>
    <p v-if="error" class="error-message">{{ error }}</p>
    <p v-if="success" class="success-message">{{ success }}</p>

    <section v-if="!loading && bookings.length" class="booking-list">
      <article
        v-for="booking in bookings"
        :key="booking.id"
        class="booking-card"
      >
        <div>
          <span class="status-badge" :class="booking.status.toLowerCase()">
            {{ booking.status }}
          </span>

          <h2>{{ booking.resourceName }}</h2>
          <p class="muted">{{ booking.officeName }}</p>

          <p class="booking-date">
            {{ formatDate(booking.startUtc, booking.officeName) }} –
            {{ formatDate(booking.endUtc, booking.officeName) }}
          </p>
        </div>

        <button
          v-if="booking.status === 'Confirmed'"
          class="cancel-button"
          @click="cancelBooking(booking.id)"
        >
          Cancel booking
        </button>
      </article>
    </section>

    <section
      v-else-if="!loading && !error"
      class="empty-state"
    >
      <h2>No bookings yet</h2>
      <p>Explore the available resources to make your first reservation.</p>
      <RouterLink to="/" class="primary-button">
        Browse resources
      </RouterLink>
    </section>
  </main>
</template>

<style scoped>
.page {
  max-width: 1100px;
  margin: 0 auto;
  padding: 40px;
  color: #202b3c;
}

.back-link {
  display: inline-block;
  margin-bottom: 30px;
  color: #3758d7;
  text-decoration: none;
  font-size: 13px;
}

.eyebrow {
  color: #75839a;
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 1.2px;
}

h1 {
  font-size: 32px;
  margin: 8px 0;
}

.subtitle,
.muted {
  color: #8290a4;
  font-size: 13px;
}

.booking-list {
  display: grid;
  gap: 16px;
  margin-top: 28px;
}

.booking-card,
.empty-state {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 20px;
  padding: 24px;
  background: white;
  border: 1px solid #e5eaf2;
  border-radius: 12px;
}

h2 {
  font-size: 18px;
  margin: 14px 0 8px;
}

.booking-date {
  font-size: 13px;
}

.status-badge {
  display: inline-block;
  padding: 5px 9px;
  border-radius: 6px;
  background: #e6f7ef;
  color: #16825d;
  font-size: 11px;
}

.status-badge.cancelled {
  background: #f0f2f6;
  color: #68758a;
}

.cancel-button {
  border: 1px solid #efc8c5;
  border-radius: 8px;
  background: white;
  color: #b83f38;
  padding: 10px 13px;
  font-size: 12px;
}

.primary-button {
  display: inline-block;
  background: #3758d7;
  color: white;
  text-decoration: none;
  border-radius: 8px;
  padding: 12px 16px;
  font-size: 12px;
}

.empty-state {
  display: block;
  margin-top: 28px;
}

.error-message {
  color: #c03939;
}

.success-message {
  color: #16825d;
}

@media (max-width: 650px) {
  .page {
    padding: 20px;
  }

  .booking-card {
    align-items: flex-start;
    flex-direction: column;
  }
}
</style>