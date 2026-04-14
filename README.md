# Meteo - Weather Application

A C# WinForms weather application that provides real-time weather forecasts and location search.

## Setup

### API Key Configuration

This application requires an OpenWeatherMap API key to fetch weather data.

#### Step 1: Get an API Key

1. Go to [OpenWeatherMap](https://openweathermap.org/api)
2. Sign up for a free account
3. Copy your API key from your account dashboard

#### Step 2: Create the `.env` File

Create a `.env` file in the **same directory as the executable** (where `Meteo.exe` runs) with the following content:

```
API_KEY=your_api_key_here
```

Replace `your_api_key_here` with your actual OpenWeatherMap API key.

#### Step 3: Run the Application

The application will automatically load the API key from the `.env` file on startup.

## Features

- Search for cities and get weather forecasts
- Save favorite cities to a local SQLite database
- View 5-day weather predictions with hourly details
- Display sunrise/sunset times
- Cloud coverage, humidity, wind speed, and visibility information

## Database

Favorites are stored locally in a SQLite database (`favoris.db`).

## Troubleshooting

**"Clé API manquante" (Missing API Key) error:**
- Ensure the `.env` file exists next to the executable
- Verify the `API_KEY=` line is correctly formatted
- Restart the application after creating/modifying the `.env` file
