import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'
import AvailabilityView from '../views/AvailabilityView.vue'
import MyBookingsView from '../views/MyBookingsView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'resources',
      component: HomeView,
    },
    {
      path: '/availability',
      name: 'availability',
      component: AvailabilityView,
    },
    {
      path: '/my-bookings',
      name: 'my-bookings',
      component: MyBookingsView,
    },
  ],
})

export default router