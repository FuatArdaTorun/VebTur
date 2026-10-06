# VebTur

VebTur is a hotel discovery and reservation request web app. I built it during my summer internship as a learning project, to get hands-on with the stack used at the company: Angular and TypeScript on the frontend, ASP.NET Core (.NET) on the backend, and PostgreSQL for the database. My goal was to learn how a complete web application fits together, from the database up to the UI.

It is a demo project. It runs locally, it is not deployed anywhere, and it is not a real booking service.

## Features

- Browse hotels, filter them by destination, star rating, price and amenities, and sort the results
- Hotel pages with photos, room types, amenities, ratings and guest reviews
- Reservation requests: the guest sends a request and the hotel confirms or rejects it later, so nothing is booked instantly
- Customer accounts with a profile, favorites, a list of their own reservations, reviews after a confirmed stay and support messages
- Admin panel with a dashboard, hotels, amenities, reservation requests, reviews, support messages and a notification log

## About the data

- Everything is stored in a local PostgreSQL database.
- The hotels are real places. I entered their details by hand as seed data, using the information each hotel publishes on its official website. Photos are linked from the hotels' own websites. All names, descriptions and images belong to their owners and are only used here for learning, with no commercial purpose.
- Google ratings were also copied by hand from each hotel's public Google Maps page. The app doesn't pull data from any external API. There is a Google Places provider in the code, but it is intentionally not connected.
- Prices, room availability, the demo customers and their reviews are made up.
- No real emails are sent. Hotel notifications and password reset links are simulated inside the app.

## Tech stack

- Frontend: Angular, TypeScript, SCSS
- Backend: ASP.NET Core (.NET 10), Entity Framework Core, ASP.NET Core Identity with JWT, FluentValidation
- Database: PostgreSQL, running in Docker
- Tests: xUnit unit and integration tests (the integration tests run against a real PostgreSQL), Vitest for the Angular app

## Running it with Docker

All you need is Docker.

```bash
cp .env.example .env    # then set your own passwords and JWT key in .env
docker compose up -d --build
```

Then open http://localhost:8080. On the first start the API creates the database schema and loads the demo data. To open the admin panel, sign in with the `ADMIN_EMAIL` and `ADMIN_PASSWORD` from your `.env`.

`docker compose down` stops everything. The database is kept in a Docker volume, so your data is still there next time.

The stack has three containers: PostgreSQL, the API, and nginx, which serves the Angular build and forwards `/api` requests to the API.

## Local development

For development I run only the database in Docker, and the API and the frontend on my machine with hot reload. This needs the .NET 10 SDK and Node.js as well.

1. Create `.env` as above and start PostgreSQL:

   ```bash
   docker compose up -d postgres
   ```

2. Set the API's user secrets, apply the migrations and start the API:

   ```bash
   cd backend
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=vebtur;Username=vebtur_app;Password=<your .env password>" --project src/VebTur.Api
   dotnet user-secrets set "Jwt:SigningKey" "<any random string, at least 32 characters>" --project src/VebTur.Api
   dotnet user-secrets set "Jwt:Issuer" "VebTur.Api" --project src/VebTur.Api
   dotnet user-secrets set "Jwt:Audience" "VebTur.Frontend" --project src/VebTur.Api
   dotnet user-secrets set "Admin:Email" "admin@vebtur.local" --project src/VebTur.Api
   dotnet user-secrets set "Admin:Password" "<a password for the admin account>" --project src/VebTur.Api

   dotnet tool restore
   dotnet ef database update --project src/VebTur.Infrastructure --startup-project src/VebTur.Api
   dotnet run --project src/VebTur.Api --launch-profile https
   ```

   In development the API seeds the hotels, the demo customers and the admin account on startup.

3. Start the frontend and open http://localhost:4200:

   ```bash
   cd frontend
   npm install
   npm start
   ```

## Tests

```bash
cd backend && dotnet test
cd frontend && npm test
```

The backend integration tests need the PostgreSQL container to be running. They read the database settings from your `.env` (or from environment variables) and use a separate `vebtur_test` database, which they create and delete themselves.
