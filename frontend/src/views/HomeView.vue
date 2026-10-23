<script setup lang="ts">
import { ref, onMounted } from 'vue'

interface Resource {
  id: number
  name: string
  type: string
  office: string
}

const resources = ref<Resource[]>([])
const selectedType = ref('')
const selectedOffice = ref('')
const loading = ref(false)
const error = ref('')

const apiUrl = 'http://localhost:5168'

async function loadResources() {
  loading.value = true
  error.value = ''

  try {
    const params = new URLSearchParams()

    if (selectedType.value) params.set('type', selectedType.value)

    if (selectedOffice.value === 'Madrid') params.set('officeId', '1')
    if (selectedOffice.value === 'London') params.set('officeId', '2')

    const query = params.toString()
    const response = await fetch(
      `${apiUrl}/api/resources${query ? `?${query}` : ''}`,
    )

    if (!response.ok) throw new Error('Unable to load resources.')

    resources.value = await response.json()
  } catch {
    error.value = 'Could not connect to the API. Make sure the backend is running.'
  } finally {
    loading.value = false
  }
}

onMounted(loadResources)
</script>

<template>
  <div class="app-shell">
    <aside class="sidebar">
      <div class="brand">
        <div class="brand-icon">R</div>
        <div>
          <strong>ResourceBook</strong>
          <small>WORKPLACE MANAGEMENT</small>
        </div>
      </div>

      <p class="nav-label">WORKSPACE</p>
      <nav>
        <RouterLink class="nav-item active" to="/">Resources</RouterLink>
        <RouterLink class="nav-item"  to="/my-bookings">My bookings</RouterLink>
      </nav>

      <div class="sidebar-footer">
        <div class="avatar">M</div>
        <div>
          <strong>Maria</strong>
          <small>Employee</small>
        </div>
      </div>
    </aside>

    <main class="main-content">
      <header class="topbar">
        <span>Workspace / Resources</span>
        <span class="status"><i></i> Internal workspace</span>
      </header>

      <section id="resources" class="page-heading">
        <div>
          <p class="eyebrow">RESOURCE MANAGEMENT</p>
          <h1>Find your workspace</h1>
          <p class="subtitle">
            Discover and manage shared resources across our offices.
          </p>
        </div>
        <button class="primary-button" @click="loadResources">
          ↻ Refresh resources
        </button>
      </section>

      <section class="stats">
        <article class="stat-card">
          <span>Total resources</span>
          <strong>{{ resources.length }}</strong>
          <small>Matching your filters</small>
        </article>
        <article class="stat-card">
          <span>Office locations</span>
          <strong>02</strong>
          <small>Madrid and London</small>
        </article>
        <article class="stat-card">
          <span>Resource categories</span>
          <strong>03</strong>
          <small>Rooms, desks and equipment</small>
        </article>
      </section>

      <section class="resource-section">
        <div class="section-heading">
          <div>
            <h2>Available resources</h2>
            <p>Browse the resources registered in the system.</p>
          </div>
        </div>

        <div class="filters">
          <label>
            Resource type
            <select v-model="selectedType" @change="loadResources">
              <option value="">All types</option>
              <option value="MeetingRoom">Meeting rooms</option>
              <option value="HotDesk">Hot desks</option>
              <option value="Equipment">Equipment</option>
            </select>
          </label>

          <label>
            Office
            <select v-model="selectedOffice" @change="loadResources">
              <option value="">All offices</option>
              <option value="Madrid">Madrid</option>
              <option value="London">London</option>
            </select>
          </label>
        </div>

        <p v-if="loading" class="message">Loading resources...</p>
        <p v-else-if="error" class="message error">{{ error }}</p>

        <div v-else-if="resources.length" class="resource-grid">
          <article
            v-for="resource in resources"
            :key="resource.id"
            class="resource-card"
          >
            <div class="resource-card-top">
              <span class="resource-icon">
                {{ resource.type === 'MeetingRoom' ? '▤' : resource.type === 'HotDesk' ? '▦' : '▣' }}
              </span>
              <span class="resource-badge">{{ resource.type }}</span>
            </div>

            <h3>{{ resource.name }}</h3>
            <p class="location">⌖ {{ resource.office }}</p>

            <div class="card-footer">
              <span class="active-dot"></span>
              <span>Registered resource</span>
              <button
                class="text-button"
                @click="$router.push({ name: 'availability', query: { resourceId: resource.id } })"
              >
                View availability →
              </button>
            </div>
          </article>
        </div>

        <p v-else class="message">No resources match these filters.</p>
      </section>

      <section id="availability" class="coming-soon">
        <div>
          <p class="eyebrow">NEXT STEP</p>
          <h2>Availability and booking</h2>
          <p>Next we'll connect the booking flow to the API.</p>
        </div>
        <span class="coming-soon-badge">In progress</span>
      </section>
    </main>
  </div>
</template>