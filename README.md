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
│   └── ResourceBooking.slnx
├── frontend/
│   ├── src/
│   │   ├── assets/
│   │   ├── router/
│   │   └── views/
│   └── package.json
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

Configure the connection string in:

`backend/ResourceBooking.Api/appsettings.json`

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

The API is configured to run locally at:

`http://localhost:5168`

On startup, the application applies pending Entity Framework Core migrations and seeds sample data when no offices exist.

Keep the backend running while using the frontend.

### 4. Run the frontend

Open a second terminal at the repository root and run:

```bash
cd frontend
npm install
npm run dev
```

Open the local URL printed by Vite, typically:

`http://localhost:5173`

The frontend is configured to communicate with the local backend.

### 5. Build the frontend

To run the TypeScript checks and create a production build:

```bash
cd frontend
npm run build
```

## Database Migrations and Sample Data

Database schema changes are managed through Entity Framework Core migrations.

The application calls `DbInitializer.SeedAsync` during startup. This applies pending migrations and creates sample data if no offices exist.

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

## Manual Testing

The following scenarios can be tested through the UI and API:

- Browse resources and filter by type or office.
- Open a resource and select a booking date.
- Create a valid booking and verify that availability updates.
- Attempt to create an overlapping booking and verify that the API rejects it.
- Submit an invalid time range and verify that validation prevents the booking.
- Open **My Bookings** and check the displayed booking times.
- Cancel a booking and verify that it no longer blocks availability.
- Check availability for resources in both Madrid and London.
- Check dates around daylight-saving transitions.

Automated backend and frontend tests have not yet been added.

## Assumptions and Limitations

- The application uses seeded sample data and a fixed sample user for the challenge.
- User identity is represented by a `userId` parameter. This is a demonstration approach, not production authentication or authorization.
- Booking conflicts are checked by the application before a booking is saved. Concurrent booking requests would require additional database-level protection for production-grade consistency.
- The application is a technical challenge implementation and is not intended for production deployment without further security, operational, and concurrency safeguards.

## AI Usage

AI assistance was used during development to support implementation, debugging, code review, and documentation. The resulting code and technical decisions were reviewed and tested iteratively by the developer.

## License

This project was created for a technical assessment. No open-source license has been specified.
