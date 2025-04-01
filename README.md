# Room Booking System

A comprehensive ASP.NET Core MVC application for managing room bookings and student visits.

## Features

- **Real-time Booking System**: Monitor room availability in real-time
- **Student Check-in/Check-out**: Track when students enter and leave designated rooms
- **Period-based Scheduling**: Book rooms for specific school periods (1st through 9th)
- **Booking Management**: Create, view, cancel, and manage bookings
- **Timeline Views**: See today's and tomorrow's bookings at a glance
- **Responsive Design**: Works on tablets and other touchscreen devices

## Technologies Used

- ASP.NET Core MVC
- Entity Framework Core
- SignalR for real-time updates
- Bootstrap 5 for responsive UI
- SQLite database (can be configured for other providers)

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022, Visual Studio Code, or any preferred IDE

### Installation

1. Clone the repository
```
git clone https://github.com/TimotejZavski/room-booking-system.git
```

2. Navigate to the project directory
```
cd room-booking-system
```

3. Restore dependencies
```
dotnet restore
```

4. Apply database migrations
```
dotnet ef database update
```

5. Run the application
```
dotnet run
```

The application will be available at `https://localhost:5001` or `http://localhost:5000`.

## Configuration

Database and other application settings can be configured in `appsettings.json`.

## Usage

The main interface provides:
- Current room status (available, reserved, or occupied)
- Booking form for making new reservations
- Timeline view showing today's and tomorrow's bookings
- Management options for existing bookings

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgements

- Bootstrap for the responsive UI components
- SignalR for enabling real-time functionality
