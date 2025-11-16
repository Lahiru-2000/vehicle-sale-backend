# Quick Setup Guide - .NET Core Backend

## Step-by-Step Setup

### 1. Prerequisites Check

```bash
# Check .NET SDK version (should be 8.0 or higher)
dotnet --version

# Check SQL Server is running
# Open SQL Server Management Studio or check services
```

### 2. Database Setup

```sql
-- Connect to SQL Server and run:
CREATE DATABASE vehicle_hub;
GO
```

### 3. Configure Connection String

Edit `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER\\SQLEXPRESS;Database=vehicle_hub;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;Encrypt=False;"
  }
}
```

**Common Connection String Examples:**

- **SQL Server Express (Windows Auth):**
  ```
  Server=localhost\\SQLEXPRESS;Database=vehicle_hub;Integrated Security=True;TrustServerCertificate=True;
  ```

- **SQL Server (SQL Auth):**
  ```
  Server=localhost\\SQLEXPRESS;Database=vehicle_hub;User Id=sa;Password=YourPassword;TrustServerCertificate=True;Encrypt=False;
  ```

### 4. Install Dependencies

```bash
cd VehiclePricePrediction.API
dotnet restore
```

### 5. Run the Application

```bash
dotnet run
```

The API will start on:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger: `https://localhost:5001/swagger`

### 6. Test the API

#### Option 1: Using Swagger UI
1. Navigate to `https://localhost:5001/swagger`
2. Try the `/api/auth/register` endpoint
3. Then try `/api/auth/login`

#### Option 2: Using curl/Postman

```bash
# Register a user
curl -X POST https://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test User",
    "email": "test@example.com",
    "password": "password123"
  }'

# Login
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "password123"
  }'
```

## Common Issues

### Issue: "Cannot connect to database"

**Solution:**
1. Verify SQL Server is running
2. Check connection string format
3. Ensure database exists
4. Verify SQL Server authentication mode

### Issue: "JWT Secret not configured"

**Solution:**
1. Open `appsettings.json`
2. Set a strong JWT secret (minimum 32 characters)
3. Example: `"Secret": "my-super-secret-key-minimum-32-characters-long"`

### Issue: "Port already in use"

**Solution:**
1. Change port in `Properties/launchSettings.json`
2. Or stop the application using the port

### Issue: "CORS errors from frontend"

**Solution:**
1. Update CORS origins in `Program.cs`
2. Add your frontend URL to allowed origins

## Next Steps

1. **Update Frontend**: Point your Next.js frontend to this API
2. **Configure Environment**: Set up production settings
3. **Deploy**: Deploy to your hosting platform

## Integration with Frontend

Update your Next.js frontend `.env.local`:

```env
NEXT_PUBLIC_API_URL=http://localhost:5000
# or
NEXT_PUBLIC_API_URL=https://localhost:5001
```

Then update your API calls to use this base URL.

## Production Checklist

- [ ] Change JWT secret to a strong random value
- [ ] Update connection string for production database
- [ ] Configure CORS for production domain
- [ ] Set up HTTPS certificates
- [ ] Configure logging
- [ ] Set up database backups
- [ ] Review security settings

