# Resource Booking System

A full-stack resource booking application developed as part of a Full Stack Tech Challenge. The application allows users to browse workplace resources, check availability across different offices, create reservations, and manage their own bookings.

## Features

- **Resource browsing:** List and filter resources by type and office.
- **Availability management:** Check resource availability for a selected date.
- **Booking creation:** Create reservations with validation of time ranges and prevention of overlapping confirmed bookings.
- **Booking management:** View and cancel bookings associated with a user.
- **Time zone awareness:** Display booking times in the local time zone of each office.
- **Persistent storage:** Store application data in PostgreSQL using Entity Framework Core migrations.
- **Sample data:** Seed offices, resources, and a sample user during application startup.
- **Booking feedback:** Show a booking modal and success/error feedback when attempting to reserve an available time slot.

## Technology Stack

### Backend

- **Language:** C#
- **Framework:** ASP.NET Core on .NET 10
- **API:** Minimal APIs
- **ORM:** Entity Framework Core 10
- **Database:** PostgreSQL
- **Database provider:** Npgsql Entity Framework Core provider 10.0.3
- **Migration tooling:** Microsoft.EntityFrameworkCore.Design 10.0.13

### Frontend

- **Framework:** Vue 3
- **Language:** TypeScript
- **Routing:** Vue Router
- **Date and time handling:** Luxon
- **Build tool and development server:** Vite

### Automated Testing

- **Backend test framework:** xUnit
- **Backend API integration testing:** Microsoft.AspNetCore.Mvc.Testing
- **Backend test database:** SQLite in-memory
- **Backend coverage tooling:** Coverlet
- **Frontend test framework:** Vitest
- **Vue component testing:** Vue Test Utils
- **Frontend DOM environment:** jsdom

## Project Structure

```text
resource-booking-system/
├── backend/
│   ├── ResourceBooking.Api/
│   │   ├── Data/
│   │   ├── Dtos/
│   │   ├── Migrations/
│   │   ├── Models/
│   │   └── Program.cs
│   ├── ResourceBooking.Api.Tests/
│   │   ├── BookingApiFactory.cs
│   │   └── BookingApiTests.cs
│   └── ResourceBooking.slnx
├── frontend/
│   ├── src/
│   │   ├── __tests__/
│   │   │   └── AvailabilityView.spec.ts
│   │   ├── assets/
│   │   ├── router/
│   │   └── views/
│   ├── package.json
│   └── vite.config.ts
├── .gitignore
└── README.md
```

## Prerequisites

Install the following tools before running the application:

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js](https://nodejs.org/) compatible with the version specified in `frontend/package.json`
- npm
- [PostgreSQL](https://www.postgresql.org/)
- [Git](https://git-scm.com/)

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/msoneira/resource-booking-system.git
cd resource-booking-system
```

If you do not have access to the private repository, request access from its owner.

### 2. Configure the database

Create a PostgreSQL database named `resource_booking`.

Configure the connection string in `backend/ResourceBooking.Api/appsettings.json`.

Example:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=resource_booking;Username=YOUR_USERNAME"
  }
}
```

Replace `YOUR_USERNAME` with a PostgreSQL user that has permission to create and modify tables in the database.

Configure credentials using an appropriate local configuration or secret-management mechanism when required. Do not commit real passwords or other secrets.

### 3. Run the backend

Open a terminal at the repository root and run:

```bash
cd backend/ResourceBooking.Api
dotnet restore
dotnet build
dotnet run
```

The API is configured to run locally at `http://localhost:5168`.

On startup, the application applies pending Entity Framework Core migrations and seeds sample data when no offices exist. Keep the backend running while using the frontend.

### 4. Run the frontend

Open a second terminal at the repository root and run:

```bash
cd frontend
npm install
npm run dev
```

Open the local URL printed by Vite, typically `http://localhost:5173`. The frontend is configured to communicate with the local backend.

### 5. Build the frontend

To run the TypeScript checks and create a production build:

```bash
cd frontend
npm run build
```

## Database Migrations and Sample Data

Database schema changes are managed through Entity Framework Core migrations.

The application calls `DbInitializer.SeedAsync` during startup. This applies pending migrations and creates sample data if no offices exist. Sample data initialization is skipped in the `Testing` environment.

The sample data includes:

| Entity | Sample data |
|---|---|
| Offices | Madrid Office and London Office |
| Time zones | `Europe/Madrid` and `Europe/London` |
| User | Maria (`maria@example.com`) |
| Resources | Meeting Room A, Hot Desk 01, Projector 01 |

The sample resources are associated with their respective offices. The initializer skips seeding when offices already exist.

## API Endpoints

| Method | Endpoint | Purpose |
|---|---|---|
| `GET` | `/api/resources` | List resources, with optional filters |
| `POST` | `/api/bookings` | Create a booking |
| `GET` | `/api/bookings?userId=1` | List bookings for a user |
| `DELETE` | `/api/bookings/{id}?userId=1` | Cancel a booking |
| `GET` | `/api/resources/{resourceId}/bookings?date=YYYY-MM-DD` | Retrieve bookings for a resource on a selected local date |

The API uses HTTP status codes and error responses to report invalid requests, missing resources or users, and booking conflicts.

## Time Zone Handling

Each office has an IANA time zone identifier:

- Madrid: `Europe/Madrid`
- London: `Europe/London`

Availability is calculated for the selected calendar date in the resource's office time zone.

When a booking is created, the frontend interprets the selected date and time in the relevant office time zone and converts the resulting timestamps to UTC before sending them to the API. Booking times are stored in UTC and returned for display in the office's local time zone.

Luxon is used to handle date and time conversions. Using named time zones allows the application to account for daylight-saving changes in Madrid and London rather than relying on the computer's local time zone or a fixed UTC offset.

## Automated Testing

### Backend tests

Backend integration tests are implemented using xUnit, `WebApplicationFactory`, and an in-memory SQLite database. These tests exercise the API through HTTP requests without requiring a running PostgreSQL instance.

Run the backend tests from the repository root:

```bash
dotnet test backend/ResourceBooking.Api.Tests/ResourceBooking.Api.Tests.csproj
```

The backend test suite covers:

- Database provider configuration in the testing environment.
- Successful booking creation and persistence.
- Rejection of overlapping confirmed bookings (`409 Conflict`).
- Rejection of invalid booking time ranges (`400 Bad Request`).
- Successful booking cancellation (`204 No Content`) and persistence of the cancelled status.
- Rejection of cancellation attempts using a different `userId` (`403 Forbidden`).

The tests use separate `BookingApiFactory` instances with in-memory SQLite databases to avoid sharing booking data between test cases.

### Frontend tests

Frontend component tests use Vitest, Vue Test Utils, and jsdom. The tests for `AvailabilityView` cover:

- Loading the resource and displaying available time slots.
- Opening the booking modal when an available time slot is selected.
- Showing a success popup after a booking is created.
- Showing an error popup when the API rejects a booking.
- Closing the booking modal without creating a booking.

Run the frontend tests from the repository root:

```bash
cd frontend
npm run test -- --run
```

The current frontend test suite contains five tests. To run Vitest in watch mode during development, use `npm run test`.

## Manual Testing

The following scenarios can be tested through the UI and API:

- Browse resources and filter by type or office.
- Open a resource and select a booking date.
- Select an available time slot and verify that the booking modal opens.
- Create a valid booking and verify that success feedback appears and availability updates.
- Attempt to create an overlapping booking and verify that the API rejects it and the UI displays an error.
- Close the booking modal without confirming and verify that no booking is created.
- Open **My Bookings** and check the displayed booking times.
- Cancel a booking and verify that it no longer blocks availability.
- Check availability for resources in both Madrid and London.
- Check dates around daylight-saving transitions.

## Assumptions and Limitations

- The application uses seeded sample data and a fixed sample user for the challenge.
- User identity is represented by a `userId` parameter. This is a demonstration approach, not production authentication or authorization.
- The cancellation endpoint checks that the supplied `userId` matches the booking owner, but this check does not replace authentication. The current implementation does not establish a verified user identity.
- Booking conflicts are checked by the application before a booking is saved. Concurrent booking requests would require additional database-level protection for production-grade consistency.
- The application is a technical challenge implementation and is not intended for production deployment without further security, operational, and concurrency safeguards.

## AI Usage

AI assistance was used during development to support implementation, debugging, code review, and documentation. The resulting code and technical decisions were reviewed and tested iteratively by the developer.

## License

This project was created for a technical assessment. No open-source license has been specified.
